using UnityEngine;

namespace GGemCo2DCore
{
    /// <summary>
    /// 몬스터가 전투 대상 주변의 동시 공격 슬롯을 사용하는 방식을 나타냅니다.
    /// </summary>
    public enum MonsterAttackSlotType
    {
        /// <summary>공격 슬롯을 사용하지 않습니다.</summary>
        None = 0,

        /// <summary>근접 공격자용 슬롯을 사용합니다.</summary>
        Melee = 1,

        /// <summary>원거리 공격자용 슬롯을 사용합니다.</summary>
        Ranged = 2,
    }

    /// <summary>
    /// 몬스터의 다수 공격 슬롯 예약 정책을 런타임용으로 정규화한 불변 프로필입니다.
    /// </summary>
    public readonly struct MonsterAttackSlotProfile
    {
        private const int DefaultMeleeCapacity = 2;
        private const int DefaultRangedCapacity = 3;
        private const int MaximumCapacity = 16;
        private const float DefaultReservationSeconds = 4f;
        private const float DefaultPostActionHoldSeconds = 0.2f;

        /// <summary>사용할 공격 슬롯 종류입니다.</summary>
        public MonsterAttackSlotType SlotType { get; }

        /// <summary>동일 대상에 동시에 예약할 수 있는 최대 공격자 수입니다.</summary>
        public int MaxConcurrentAttackers { get; }

        /// <summary>예약 갱신이 끊겼을 때 슬롯을 자동 반환할 시간입니다.</summary>
        public float ReservationSeconds { get; }

        /// <summary>공격 또는 스킬 종료 후 슬롯을 추가로 유지할 시간입니다.</summary>
        public float PostActionHoldSeconds { get; }

        /// <summary>공격 슬롯 정책이 활성화되어 있는지 여부입니다.</summary>
        public bool IsEnabled => SlotType != MonsterAttackSlotType.None && MaxConcurrentAttackers > 0;

        private MonsterAttackSlotProfile(
            MonsterAttackSlotType slotType,
            int maxConcurrentAttackers,
            float reservationSeconds,
            float postActionHoldSeconds)
        {
            SlotType = slotType;
            MaxConcurrentAttackers = maxConcurrentAttackers;
            ReservationSeconds = reservationSeconds;
            PostActionHoldSeconds = postActionHoldSeconds;
        }

        /// <summary>
        /// monster_combat_profile 테이블 데이터에서 공격 슬롯 프로필을 생성합니다.
        /// </summary>
        /// <param name="tableData">선택한 몬스터 전투 프로필 테이블 행입니다.</param>
        /// <returns>기존 데이터에서는 비활성 상태를 유지하는 정규화된 프로필입니다.</returns>
        public static MonsterAttackSlotProfile Create(StruckTableMonsterCombatProfile tableData)
        {
            MonsterAttackSlotType slotType = tableData?.AttackSlotType ?? MonsterAttackSlotType.None;
            int defaultCapacity = slotType switch
            {
                MonsterAttackSlotType.Melee => DefaultMeleeCapacity,
                MonsterAttackSlotType.Ranged => DefaultRangedCapacity,
                _ => 0,
            };

            int capacity = tableData != null && tableData.MaxConcurrentAttackers > 0
                ? Mathf.Clamp(tableData.MaxConcurrentAttackers, 1, MaximumCapacity)
                : defaultCapacity;

            float reservationSeconds = tableData != null && tableData.AttackSlotReservationSeconds > 0f
                ? tableData.AttackSlotReservationSeconds
                : DefaultReservationSeconds;
            float postActionHoldSeconds = tableData != null && tableData.AttackSlotPostActionHoldSeconds >= 0f
                ? tableData.AttackSlotPostActionHoldSeconds
                : DefaultPostActionHoldSeconds;

            return new MonsterAttackSlotProfile(
                slotType,
                capacity,
                Mathf.Max(0.2f, reservationSeconds),
                Mathf.Max(0f, postActionHoldSeconds));
        }
    }
}
