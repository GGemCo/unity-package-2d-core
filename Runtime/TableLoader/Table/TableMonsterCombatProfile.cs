using System.Collections.Generic;

namespace GGemCo2DCore
{
    /// <summary>
    /// 몬스터 전투 범위와 Threat 정책 프로필 테이블의 1행 데이터입니다.
    /// </summary>
    public sealed class StruckTableMonsterCombatProfile : IUidName
    {
        /// <inheritdoc />
        public int Uid { get; set; }

        /// <inheritdoc />
        public string Name { get; set; }

        /// <summary>디자이너 메모입니다.</summary>
        public string Memo;

        /// <summary>몬스터 중심 기준 선공 감지 X축 반경입니다.</summary>
        public float DetectionRangeX;

        /// <summary>몬스터 중심 기준 선공 감지 Y축 반경입니다.</summary>
        public float DetectionRangeY;

        /// <summary>감지된 플레이어를 놓친 것으로 판정할 X축 이탈 반경입니다.</summary>
        public float DetectionExitRangeX;

        /// <summary>감지된 플레이어를 놓친 것으로 판정할 Y축 이탈 반경입니다.</summary>
        public float DetectionExitRangeY;

        /// <summary>기본 공격을 시작할 수 있는 X축 거리입니다.</summary>
        public float BasicAttackRangeX;

        /// <summary>기본 공격을 시작할 수 있는 Y축 거리입니다.</summary>
        public float BasicAttackRangeY;

        /// <summary>몬스터가 유지하려는 최소 전투 거리입니다.</summary>
        public float PreferredRangeMin;

        /// <summary>몬스터가 유지하려는 최대 전투 거리입니다.</summary>
        public float PreferredRangeMax;

        /// <summary>타겟 추적을 포기할 2D 거리입니다. 0 이하면 별도 추적 확장 범위를 사용하지 않습니다.</summary>
        public float ChaseRange;

        /// <summary>홈 위치 기준 소프트 리시 거리입니다. 0 이하면 아직 사용하지 않습니다.</summary>
        public float SoftLeashRange;

        /// <summary>홈 위치 기준 하드 리시 거리입니다. 0 이하면 비활성입니다.</summary>
        public float HardLeashRange;

        /// <summary>소프트 Leash 범위를 벗어난 뒤 Evade를 시작하기 전 유예 시간입니다.</summary>
        public float SoftLeashGraceSeconds;

        /// <summary>홈 도착으로 판정할 거리입니다.</summary>
        public float ReturnStopDistance;

        /// <summary>홈 도착 후 감지와 AI를 다시 활성화하기 전 대기 시간입니다.</summary>
        public float ReturnDelaySeconds;

        /// <summary>홈 복귀 이동에 적용할 이동 속도 배율입니다.</summary>
        public float ReturnMoveSpeedMultiplier;

        /// <summary>귀환 이동 제한 시간입니다. 초과하면 홈 위치로 안전하게 보정합니다.</summary>
        public float ReturnTimeoutSeconds;

        /// <summary>Evade 중 자원을 회복할 시점입니다.</summary>
        public MonsterLeashRecoveryPolicy LeashRecoveryPolicy;

        /// <summary>귀환 및 재활성 대기 중 피해를 무시할지 여부입니다.</summary>
        public bool InvulnerableDuringReturn = true;

        /// <summary>Evade 시작 시 현재 적용 중인 Affect를 모두 제거할지 여부입니다.</summary>
        public bool ClearAffectsOnEvade = true;

        /// <summary>감지 범위 진입 시 유지할 기본 Threat입니다. 0 이하면 기본값 1을 사용합니다.</summary>
        public float DetectionThreat;

        /// <summary>확정 피해량을 Threat로 변환할 때 적용하는 배율입니다. 0 이하면 기본값 1을 사용합니다.</summary>
        public float DamageThreatMultiplier;

        /// <summary>피해량이 작더라도 보장할 최소 피해 Threat입니다. 0 이하면 기본값 1을 사용합니다.</summary>
        public float MinimumDamageThreat;

        /// <summary>현재 타겟을 전환하기 위해 새 후보가 넘어야 하는 Threat 비율입니다. 1 이하면 즉시 전환합니다.</summary>
        public float TargetSwitchThreatRatio;

        /// <summary>동시에 기억할 최대 Threat 대상 수입니다. 0 이하면 기본값 16을 사용합니다.</summary>
        public int MaxThreatTargets;

        /// <summary>Encounter 볼륨 또는 동료 지원으로 등록할 기본 Threat입니다.</summary>
        public float EncounterThreat;

        /// <summary>같은 Encounter 그룹에 지원 어그로를 전달할 최대 거리입니다. 0 이하면 거리 제한을 사용하지 않습니다.</summary>
        public float EncounterAssistRadius;

        /// <summary>한 번의 지원 요청으로 활성화할 최대 Encounter 동료 수입니다. 0 이하면 제한하지 않습니다.</summary>
        public int MaxEncounterAssistCount;

        /// <summary>다수 공격 제한에 사용할 슬롯 종류입니다.</summary>
        public MonsterAttackSlotType AttackSlotType;

        /// <summary>동일 대상에 동시에 공격 행동을 예약할 수 있는 최대 몬스터 수입니다.</summary>
        public int MaxConcurrentAttackers;

        /// <summary>갱신되지 않은 공격 슬롯 예약을 자동 반환할 시간입니다.</summary>
        public float AttackSlotReservationSeconds;

        /// <summary>공격 또는 스킬 종료 후 슬롯을 추가로 유지할 시간입니다.</summary>
        public float AttackSlotPostActionHoldSeconds;
    }

    /// <summary>
    /// 몬스터 전투 범위와 Threat 정책 프로필 테이블입니다.
    /// </summary>
    public sealed class TableMonsterCombatProfile : DefaultTable<StruckTableMonsterCombatProfile>
    {
        /// <inheritdoc />
        public override string Key => ConfigAddressableTable.MonsterCombatProfile;

        /// <summary>
        /// 테이블 원문 한 행을 몬스터 전투 범위와 Threat 정책 프로필 데이터로 변환합니다.
        /// </summary>
        /// <param name="data">컬럼명과 원문 값으로 구성된 행 데이터입니다.</param>
        /// <returns>파싱된 몬스터 전투 프로필입니다.</returns>
        protected override StruckTableMonsterCombatProfile BuildRow(Dictionary<string, string> data)
        {
            TableRowReader reader = ReadRow(data);
            int uid = reader.Int("Uid");
            string memo = reader.String("Memo");
            return new StruckTableMonsterCombatProfile
            {
                Uid = uid,
                Name = string.IsNullOrWhiteSpace(memo) ? $"Monster Combat Profile {uid}" : memo,
                Memo = memo,
                DetectionRangeX = ReadOptionalFloat(data, "DetectionRangeX"),
                DetectionRangeY = ReadOptionalFloat(data, "DetectionRangeY"),
                DetectionExitRangeX = ReadOptionalFloat(data, "DetectionExitRangeX"),
                DetectionExitRangeY = ReadOptionalFloat(data, "DetectionExitRangeY"),
                BasicAttackRangeX = ReadOptionalFloat(
                    data,
                    "BasicAttackRangeX",
                    ReadOptionalFloat(data, "BasicAttackRange")),
                BasicAttackRangeY = ReadOptionalFloat(data, "BasicAttackRangeY"),
                PreferredRangeMin = ReadOptionalFloat(data, "PreferredRangeMin"),
                PreferredRangeMax = ReadOptionalFloat(data, "PreferredRangeMax"),
                ChaseRange = ReadOptionalFloat(data, "ChaseRange"),
                SoftLeashRange = ReadOptionalFloat(data, "SoftLeashRange"),
                HardLeashRange = ReadOptionalFloat(data, "HardLeashRange"),
                SoftLeashGraceSeconds = ReadOptionalFloat(data, "SoftLeashGraceSeconds"),
                ReturnStopDistance = ReadOptionalFloat(data, "ReturnStopDistance"),
                ReturnDelaySeconds = ReadOptionalFloat(data, "ReturnDelaySeconds"),
                ReturnMoveSpeedMultiplier = ReadOptionalFloat(data, "ReturnMoveSpeedMultiplier"),
                ReturnTimeoutSeconds = ReadOptionalFloat(data, "ReturnTimeoutSeconds"),
                LeashRecoveryPolicy = ReadOptionalEnum(
                    data,
                    "LeashRecoveryPolicy",
                    MonsterLeashRecoveryPolicy.OnHomeReached),
                InvulnerableDuringReturn = ReadOptionalBool(data, "InvulnerableDuringReturn", true),
                ClearAffectsOnEvade = ReadOptionalBool(data, "ClearAffectsOnEvade", true),
                DetectionThreat = ReadOptionalFloat(data, "DetectionThreat"),
                DamageThreatMultiplier = ReadOptionalFloat(data, "DamageThreatMultiplier"),
                MinimumDamageThreat = ReadOptionalFloat(data, "MinimumDamageThreat"),
                TargetSwitchThreatRatio = ReadOptionalFloat(data, "TargetSwitchThreatRatio"),
                MaxThreatTargets = ReadOptionalInt(data, "MaxThreatTargets"),
                EncounterThreat = ReadOptionalFloat(data, "EncounterThreat"),
                EncounterAssistRadius = ReadOptionalFloat(data, "EncounterAssistRadius"),
                MaxEncounterAssistCount = ReadOptionalInt(data, "MaxEncounterAssistCount"),
                AttackSlotType = ReadOptionalEnum(
                    data,
                    "AttackSlotType",
                    MonsterAttackSlotType.None),
                MaxConcurrentAttackers = ReadOptionalInt(data, "MaxConcurrentAttackers"),
                AttackSlotReservationSeconds = ReadOptionalFloat(data, "AttackSlotReservationSeconds"),
                AttackSlotPostActionHoldSeconds = ReadOptionalFloat(data, "AttackSlotPostActionHoldSeconds", -1f),
            };
        }

        /// <summary>
        /// 신규 컬럼이 없는 마이그레이션 데이터도 읽을 수 있도록 선택 실수 컬럼을 안전하게 파싱합니다.
        /// </summary>
        /// <param name="data">테이블 행 데이터입니다.</param>
        /// <param name="columnName">읽을 컬럼 이름입니다.</param>
        /// <param name="fallback">컬럼이 없거나 비어 있을 때 사용할 값입니다.</param>
        /// <returns>파싱된 실수 값입니다.</returns>
        private static float ReadOptionalFloat(
            IReadOnlyDictionary<string, string> data,
            string columnName,
            float fallback = 0f)
        {
            if (data == null || !data.TryGetValue(columnName, out string value) || string.IsNullOrWhiteSpace(value))
            {
                return fallback;
            }

            return MathHelper.ParseFloat(value, fallback);
        }

        /// <summary>
        /// 신규 컬럼이 없는 마이그레이션 데이터도 읽을 수 있도록 선택 열거형 컬럼을 안전하게 파싱합니다.
        /// </summary>
        private static TEnum ReadOptionalEnum<TEnum>(
            IReadOnlyDictionary<string, string> data,
            string columnName,
            TEnum fallback) where TEnum : struct, System.Enum
        {
            if (data == null || !data.TryGetValue(columnName, out string value) || string.IsNullOrWhiteSpace(value))
            {
                return fallback;
            }

            return EnumHelper.ConvertEnum<TEnum>(value);
        }

        /// <summary>
        /// 신규 컬럼이 없는 마이그레이션 데이터도 읽을 수 있도록 선택 불리언 컬럼을 안전하게 파싱합니다.
        /// </summary>
        private static bool ReadOptionalBool(
            IReadOnlyDictionary<string, string> data,
            string columnName,
            bool fallback)
        {
            if (data == null || !data.TryGetValue(columnName, out string value) || string.IsNullOrWhiteSpace(value))
            {
                return fallback;
            }

            string normalized = value.Trim();
            if (string.Equals(normalized, "Y", System.StringComparison.OrdinalIgnoreCase) ||
                string.Equals(normalized, "true", System.StringComparison.OrdinalIgnoreCase) ||
                normalized == "1")
            {
                return true;
            }

            if (string.Equals(normalized, "N", System.StringComparison.OrdinalIgnoreCase) ||
                string.Equals(normalized, "false", System.StringComparison.OrdinalIgnoreCase) ||
                normalized == "0")
            {
                return false;
            }

            return fallback;
        }

        /// <summary>
        /// 신규 컬럼이 없는 마이그레이션 데이터도 읽을 수 있도록 선택 정수 컬럼을 안전하게 파싱합니다.
        /// </summary>
        /// <param name="data">테이블 행 데이터입니다.</param>
        /// <param name="columnName">읽을 컬럼 이름입니다.</param>
        /// <param name="fallback">컬럼이 없거나 비어 있을 때 사용할 값입니다.</param>
        /// <returns>파싱된 정수 값입니다.</returns>
        private static int ReadOptionalInt(
            IReadOnlyDictionary<string, string> data,
            string columnName,
            int fallback = 0)
        {
            if (data == null || !data.TryGetValue(columnName, out string value) || string.IsNullOrWhiteSpace(value))
            {
                return fallback;
            }

            return MathHelper.ParseInt(value, fallback);
        }

    }
}
