using System.Collections.Generic;
using UnityEngine;

namespace GGemCo2DCore
{
    /// <summary>
    /// 하나의 전투 대상에 예약된 근접·원거리 공격 슬롯을 관리합니다.
    /// </summary>
    /// <remarks>
    /// 예약은 몬스터별로 하나만 유지하며, 갱신되지 않은 예약은 만료 시간 이후 자동으로 반환됩니다.
    /// 공격 슬롯은 실제 타격 판정과 무관하고 동시에 공격 행동을 시작할 수 있는 몬스터 수만 제한합니다.
    /// </remarks>
    [DisallowMultipleComponent]
    public sealed class CombatAttackSlotCoordinator : MonoBehaviour
    {
        private const float MaintenanceIntervalSeconds = 0.2f;

        private sealed class Reservation
        {
            public int OwnerInstanceId;
            public Monster Owner;
            public MonsterAttackSlotType SlotType;
            public int SlotIndex;
            public int RequestedCapacity;
            public float ExpireTime;
        }

        private readonly Dictionary<int, Reservation> _reservations = new();
        private readonly List<int> _expiredOwnerIds = new();
        private float _nextMaintenanceTime;

        /// <summary>
        /// 지정한 전투 대상에 슬롯 조정자를 반환하고, 없으면 동적으로 추가합니다.
        /// </summary>
        /// <param name="target">공격 슬롯을 소유할 전투 대상입니다.</param>
        /// <returns>대상의 공격 슬롯 조정자입니다.</returns>
        public static CombatAttackSlotCoordinator GetOrCreate(CharacterBase target)
        {
            if (target == null)
            {
                return null;
            }

            CombatAttackSlotCoordinator coordinator = target.GetComponent<CombatAttackSlotCoordinator>();
            return coordinator != null
                ? coordinator
                : target.gameObject.AddComponent<CombatAttackSlotCoordinator>();
        }

        /// <summary>
        /// 지정한 몬스터가 현재 슬롯을 예약할 수 있는지 확인합니다.
        /// </summary>
        /// <param name="owner">예약 가능 여부를 확인할 몬스터입니다.</param>
        /// <param name="slotType">확인할 슬롯 종류입니다.</param>
        /// <param name="capacity">동일 종류의 최대 동시 예약 수입니다.</param>
        /// <returns>기존 예약을 유지하거나 새 슬롯을 예약할 수 있으면 <see langword="true"/>입니다.</returns>
        public bool CanReserve(Monster owner, MonsterAttackSlotType slotType, int capacity)
        {
            if (!IsValidRequest(owner, slotType, capacity))
            {
                return false;
            }

            PruneExpiredReservations();
            if (_reservations.TryGetValue(owner.GetInstanceID(), out Reservation existing) &&
                existing.Owner == owner &&
                existing.SlotType == slotType)
            {
                return true;
            }

            int effectiveCapacity = ResolveEffectiveCapacity(slotType, capacity);
            return CountReservations(slotType) < effectiveCapacity;
        }

        /// <summary>
        /// 지정한 몬스터에게 비어 있는 공격 슬롯을 예약합니다.
        /// </summary>
        /// <param name="owner">슬롯을 예약할 몬스터입니다.</param>
        /// <param name="slotType">예약할 슬롯 종류입니다.</param>
        /// <param name="capacity">동일 종류의 최대 슬롯 수입니다.</param>
        /// <param name="leaseSeconds">예약을 유지할 시간입니다.</param>
        /// <param name="slotIndex">예약된 0 기반 슬롯 인덱스입니다.</param>
        /// <returns>기존 예약을 갱신하거나 새 슬롯을 예약했으면 <see langword="true"/>입니다.</returns>
        public bool TryReserve(
            Monster owner,
            MonsterAttackSlotType slotType,
            int capacity,
            float leaseSeconds,
            out int slotIndex)
        {
            slotIndex = -1;
            if (!IsValidRequest(owner, slotType, capacity))
            {
                return false;
            }

            PruneExpiredReservations();
            int ownerInstanceId = owner.GetInstanceID();
            if (_reservations.TryGetValue(ownerInstanceId, out Reservation existing) && existing.Owner == owner)
            {
                if (existing.SlotType != slotType)
                {
                    _reservations.Remove(ownerInstanceId);
                }
                else
                {
                    existing.RequestedCapacity = capacity;
                    existing.ExpireTime = Time.time + Mathf.Max(0.2f, leaseSeconds);
                    slotIndex = existing.SlotIndex;
                    return true;
                }
            }

            int effectiveCapacity = ResolveEffectiveCapacity(slotType, capacity);
            if (CountReservations(slotType) >= effectiveCapacity)
            {
                return false;
            }

            int freeIndex = FindFreeSlotIndex(slotType, effectiveCapacity);
            if (freeIndex < 0)
            {
                return false;
            }

            _reservations[ownerInstanceId] = new Reservation
            {
                OwnerInstanceId = ownerInstanceId,
                Owner = owner,
                SlotType = slotType,
                SlotIndex = freeIndex,
                RequestedCapacity = capacity,
                ExpireTime = Time.time + Mathf.Max(0.2f, leaseSeconds),
            };
            slotIndex = freeIndex;
            return true;
        }

        /// <summary>
        /// 기존 예약의 만료 시간을 연장합니다.
        /// </summary>
        /// <param name="owner">예약을 보유한 몬스터입니다.</param>
        /// <param name="leaseSeconds">현재 시점부터 연장할 임대 시간입니다.</param>
        /// <returns>유효한 예약을 갱신했으면 <see langword="true"/>입니다.</returns>
        public bool Renew(Monster owner, float leaseSeconds)
        {
            if (owner == null || !_reservations.TryGetValue(owner.GetInstanceID(), out Reservation reservation) || reservation.Owner != owner)
            {
                return false;
            }

            reservation.ExpireTime = Time.time + Mathf.Max(0.2f, leaseSeconds);
            return true;
        }

        /// <summary>
        /// 지정한 몬스터가 보유한 슬롯 예약을 반환합니다.
        /// </summary>
        /// <param name="owner">예약을 반환할 몬스터입니다.</param>
        public void Release(Monster owner)
        {
            if (owner == null)
            {
                return;
            }

            _reservations.Remove(owner.GetInstanceID());
        }

        /// <summary>
        /// 지정한 몬스터가 유효한 슬롯 예약을 보유하는지 확인합니다.
        /// </summary>
        /// <param name="owner">예약 상태를 확인할 몬스터입니다.</param>
        /// <param name="slotType">예약된 슬롯 종류입니다.</param>
        /// <param name="slotIndex">예약된 0 기반 슬롯 인덱스입니다.</param>
        /// <returns>만료되지 않은 예약이 있으면 <see langword="true"/>입니다.</returns>
        public bool HasReservation(Monster owner, out MonsterAttackSlotType slotType, out int slotIndex)
        {
            slotType = MonsterAttackSlotType.None;
            slotIndex = -1;
            if (owner == null)
            {
                return false;
            }

            PruneExpiredReservations();
            if (!_reservations.TryGetValue(owner.GetInstanceID(), out Reservation reservation) || reservation.Owner != owner)
            {
                return false;
            }

            slotType = reservation.SlotType;
            slotIndex = reservation.SlotIndex;
            return true;
        }

        private void Update()
        {
            if (Time.time < _nextMaintenanceTime)
            {
                return;
            }

            _nextMaintenanceTime = Time.time + MaintenanceIntervalSeconds;
            PruneExpiredReservations();
        }

        private static bool IsValidRequest(Monster owner, MonsterAttackSlotType slotType, int capacity)
        {
            return owner != null &&
                   owner.isActiveAndEnabled &&
                   !owner.IsStatusDead() &&
                   slotType != MonsterAttackSlotType.None &&
                   capacity > 0;
        }

        private int CountReservations(MonsterAttackSlotType slotType)
        {
            int count = 0;
            foreach (Reservation reservation in _reservations.Values)
            {
                if (reservation.SlotType == slotType)
                {
                    count++;
                }
            }

            return count;
        }

        /// <summary>
        /// 같은 슬롯 종류를 공유하는 공격자 중 가장 엄격한 수용량을 적용합니다.
        /// </summary>
        private int ResolveEffectiveCapacity(MonsterAttackSlotType slotType, int requestedCapacity)
        {
            int effectiveCapacity = Mathf.Max(1, requestedCapacity);
            foreach (Reservation reservation in _reservations.Values)
            {
                if (reservation.SlotType == slotType && reservation.RequestedCapacity > 0)
                {
                    effectiveCapacity = Mathf.Min(effectiveCapacity, reservation.RequestedCapacity);
                }
            }

            return effectiveCapacity;
        }

        private int FindFreeSlotIndex(MonsterAttackSlotType slotType, int capacity)
        {
            for (int slotIndex = 0; slotIndex < capacity; slotIndex++)
            {
                bool occupied = false;
                foreach (Reservation reservation in _reservations.Values)
                {
                    if (reservation.SlotType == slotType && reservation.SlotIndex == slotIndex)
                    {
                        occupied = true;
                        break;
                    }
                }

                if (!occupied)
                {
                    return slotIndex;
                }
            }

            return -1;
        }

        private void PruneExpiredReservations()
        {
            if (_reservations.Count == 0)
            {
                return;
            }

            _expiredOwnerIds.Clear();
            foreach (KeyValuePair<int, Reservation> pair in _reservations)
            {
                Reservation reservation = pair.Value;
                if (reservation.Owner == null ||
                    !reservation.Owner.isActiveAndEnabled ||
                    reservation.Owner.IsStatusDead() ||
                    Time.time >= reservation.ExpireTime)
                {
                    _expiredOwnerIds.Add(pair.Key);
                }
            }

            for (int i = 0; i < _expiredOwnerIds.Count; i++)
            {
                _reservations.Remove(_expiredOwnerIds[i]);
            }

            _expiredOwnerIds.Clear();
        }
    }
}
