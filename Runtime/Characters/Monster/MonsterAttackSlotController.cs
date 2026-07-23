using UnityEngine;

namespace GGemCo2DCore
{
    /// <summary>
    /// 몬스터가 현재 전투 대상의 공격 슬롯을 예약하고 행동 종료 시 반환하도록 관리합니다.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class MonsterAttackSlotController : MonoBehaviour, IMonsterPoolLifecycle
    {
        private const float RenewalIntervalSeconds = 0.5f;
        private const float ActionStartGraceSeconds = 0.35f;

        private Monster _owner;
        private MonsterAttackSlotProfile _profile;
        private CombatAttackSlotCoordinator _coordinator;
        private CharacterBase _reservedTarget;
        private int _reservedSlotIndex = -1;
        private bool _actionLeaseActive;
        private bool _requiresExplicitCompletion;
        private bool _attackStateObserved;
        private float _actionStartedTime;
        private float _releaseAt;
        private float _nextRenewTime;

        /// <summary>현재 적용된 공격 슬롯 프로필입니다.</summary>
        public MonsterAttackSlotProfile Profile => _profile;

        /// <summary>현재 유효한 슬롯 예약을 보유하는지 여부입니다.</summary>
        public bool HasReservation =>
            _owner != null &&
            _coordinator != null &&
            _reservedTarget != null &&
            _coordinator.HasReservation(_owner, out _, out _);

        /// <summary>현재 예약된 슬롯 인덱스입니다. 예약이 없으면 -1입니다.</summary>
        public int ReservedSlotIndex => HasReservation ? _reservedSlotIndex : -1;

        /// <summary>
        /// 슬롯을 사용할 몬스터를 연결합니다.
        /// </summary>
        /// <param name="owner">공격 슬롯을 예약할 몬스터입니다.</param>
        public void Initialize(Monster owner)
        {
            if (_owner != null && _owner != owner)
            {
                ReleaseReservation();
            }

            _owner = owner;
        }

        /// <summary>
        /// 테이블에서 정규화한 공격 슬롯 정책을 적용합니다.
        /// </summary>
        /// <param name="profile">적용할 공격 슬롯 종류와 수용량 정책입니다.</param>
        public void Configure(MonsterAttackSlotProfile profile)
        {
            bool policyChanged = _profile.SlotType != profile.SlotType ||
                                 _profile.MaxConcurrentAttackers != profile.MaxConcurrentAttackers;
            _profile = profile;
            if (!_profile.IsEnabled || policyChanged)
            {
                ReleaseReservation();
            }
        }

        /// <summary>
        /// 현재 전투 대상의 공격 슬롯을 예약할 수 있는지 확인합니다.
        /// </summary>
        public bool CanReserveCurrentTarget()
        {
            if (!_profile.IsEnabled)
            {
                return true;
            }

            if (!TryResolveCurrentTarget(out CharacterBase target))
            {
                return false;
            }

            if (HasReservation && _reservedTarget == target)
            {
                return true;
            }

            CombatAttackSlotCoordinator coordinator = target.GetComponent<CombatAttackSlotCoordinator>();
            return coordinator == null || coordinator.CanReserve(
                _owner,
                _profile.SlotType,
                _profile.MaxConcurrentAttackers);
        }

        /// <summary>
        /// 현재 전투 대상의 공격 슬롯을 예약합니다.
        /// </summary>
        /// <returns>정책이 비활성이거나 슬롯 예약에 성공하면 <see langword="true"/>입니다.</returns>
        public bool TryReserveCurrentTarget()
        {
            if (!_profile.IsEnabled)
            {
                return true;
            }

            if (!TryResolveCurrentTarget(out CharacterBase target))
            {
                return false;
            }

            if (_reservedTarget != null && _reservedTarget != target)
            {
                ReleaseReservation();
            }

            CombatAttackSlotCoordinator coordinator = CombatAttackSlotCoordinator.GetOrCreate(target);
            if (coordinator == null ||
                !coordinator.TryReserve(
                    _owner,
                    _profile.SlotType,
                    _profile.MaxConcurrentAttackers,
                    _profile.ReservationSeconds,
                    out int slotIndex))
            {
                return false;
            }

            _coordinator = coordinator;
            _reservedTarget = target;
            _reservedSlotIndex = slotIndex;
            _nextRenewTime = Time.time + RenewalIntervalSeconds;
            _releaseAt = 0f;
            return true;
        }

        /// <summary>
        /// 기본 공격 또는 스킬 행동이 시작되었음을 알리고 행동 중 예약 갱신을 시작합니다.
        /// </summary>
        /// <param name="waitForExplicitCompletion">스킬 완료 이벤트처럼 명시적 완료 통지까지 예약을 유지할지 여부입니다.</param>
        public void NotifyCombatActionStarted(bool waitForExplicitCompletion = false)
        {
            // 공격 슬롯 사용 여부와 무관하게 실제 공격 행동을 시작한 순간부터 플레이어 교전으로 처리합니다.
            if (TryResolveCurrentTarget(out CharacterBase combatTarget))
            {
                _owner.TryBeginPlayerCombatEngagement(combatTarget);
            }

            if (!_profile.IsEnabled)
            {
                return;
            }

            if (!TryReserveCurrentTarget())
            {
                return;
            }

            _actionLeaseActive = true;
            _requiresExplicitCompletion = waitForExplicitCompletion;
            _attackStateObserved = false;
            _actionStartedTime = Time.time;
            _releaseAt = 0f;
            RenewReservation();
        }

        /// <summary>
        /// 기본 공격 또는 스킬 행동이 완료되었음을 알리고 후속 유지 시간 뒤 슬롯을 반환합니다.
        /// </summary>
        public void NotifyCombatActionCompleted()
        {
            if (!HasReservation)
            {
                ResetActionState();
                return;
            }

            _actionLeaseActive = false;
            _releaseAt = Time.time + _profile.PostActionHoldSeconds;
        }

        /// <summary>
        /// 현재 보유한 공격 슬롯을 즉시 반환합니다.
        /// </summary>
        public void ReleaseReservation()
        {
            if (_coordinator != null && _owner != null)
            {
                _coordinator.Release(_owner);
            }

            _coordinator = null;
            _reservedTarget = null;
            _reservedSlotIndex = -1;
            ResetActionState();
        }

        /// <summary>
        /// Threat 선택 결과가 변경되면 이전 대상에 예약한 슬롯을 반환합니다.
        /// </summary>
        /// <param name="previousTarget">변경 전 전투 대상입니다.</param>
        /// <param name="currentTarget">새로 선택된 전투 대상입니다.</param>
        public void OnCombatTargetChanged(CharacterBase previousTarget, CharacterBase currentTarget)
        {
            if (_reservedTarget != null && _reservedTarget != currentTarget)
            {
                ReleaseReservation();
            }
        }

        private void Update()
        {
            if (!HasReservation)
            {
                if (_reservedTarget != null)
                {
                    ReleaseReservation();
                }
                return;
            }

            if (_owner == null ||
                _owner.IsStatusDead() ||
                _owner.IsLeashReturnLocked ||
                !TryResolveCurrentTarget(out CharacterBase currentTarget) ||
                currentTarget != _reservedTarget)
            {
                ReleaseReservation();
                return;
            }

            if (_actionLeaseActive)
            {
                if (_requiresExplicitCompletion)
                {
                    RenewReservation();
                    return;
                }

                if (_owner.IsStatusAttack())
                {
                    _attackStateObserved = true;
                    RenewReservation();
                    return;
                }

                if (_attackStateObserved || Time.time - _actionStartedTime >= ActionStartGraceSeconds)
                {
                    NotifyCombatActionCompleted();
                }
                else
                {
                    RenewReservation();
                }
                return;
            }

            if (_releaseAt <= 0f)
            {
                // 예약만 선점하고 실제 행동을 시작하지 않은 경우에는 임대를 갱신하지 않습니다.
                // 이후 노드 실패나 인터럽트가 발생해도 ReservationSeconds가 지나면 자동 반환됩니다.
                return;
            }

            if (Time.time >= _releaseAt)
            {
                ReleaseReservation();
                return;
            }

            RenewReservation();
        }

        private bool TryResolveCurrentTarget(out CharacterBase target)
        {
            target = null;
            return _owner != null &&
                   !_owner.IsStatusDead() &&
                   !_owner.IsLeashReturnLocked &&
                   _owner.TryGetCurrentCombatTarget(out target) &&
                   target != null &&
                   !target.IsStatusDead();
        }

        private void RenewReservation()
        {
            if (_coordinator == null || _owner == null || Time.time < _nextRenewTime)
            {
                return;
            }

            if (!_coordinator.Renew(_owner, _profile.ReservationSeconds))
            {
                ReleaseReservation();
                return;
            }

            _nextRenewTime = Time.time + RenewalIntervalSeconds;
        }

        private void ResetActionState()
        {
            _actionLeaseActive = false;
            _requiresExplicitCompletion = false;
            _attackStateObserved = false;
            _actionStartedTime = 0f;
            _releaseAt = 0f;
            _nextRenewTime = 0f;
        }

        /// <inheritdoc />
        public void OnPoolRent(Monster owner)
        {
            ReleaseReservation();
            _owner = owner;
        }

        /// <inheritdoc />
        public void OnPoolReturn(Monster owner)
        {
            ReleaseReservation();
        }
    }
}
