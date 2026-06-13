using UnityEngine;

namespace GGemCo2DCore
{
    /// <summary>
    /// 몬스터의 홈 이탈 판정과 귀환 동작을 나타내는 상태입니다.
    /// </summary>
    public enum MonsterLeashState
    {
        /// <summary>Leash 범위가 설정되지 않아 시스템이 비활성화된 상태입니다.</summary>
        Disabled = 0,

        /// <summary>전투 중 홈과의 거리를 감시하는 정상 상태입니다.</summary>
        Monitoring = 1,

        /// <summary>소프트 Leash 범위를 벗어나 유예 시간을 확인하는 상태입니다.</summary>
        SoftLimitPending = 2,

        /// <summary>전투를 중단하고 홈 위치로 이동하는 상태입니다.</summary>
        ReturningHome = 3,

        /// <summary>홈 도착 후 감지와 AI 재활성화를 잠시 지연하는 상태입니다.</summary>
        ReturnDelay = 4,
    }

    /// <summary>
    /// 몬스터가 Evade를 시작한 원인입니다.
    /// </summary>
    public enum MonsterLeashTrigger
    {
        /// <summary>명시적인 원인이 지정되지 않았습니다.</summary>
        None = 0,

        /// <summary>소프트 Leash 범위를 유예 시간 이상 벗어났습니다.</summary>
        SoftLimit = 1,

        /// <summary>하드 Leash 범위를 벗어나 즉시 귀환해야 합니다.</summary>
        HardLimit = 2,

        /// <summary>외부 시스템이 명시적으로 Evade를 요청했습니다.</summary>
        Manual = 3,
    }

    /// <summary>
    /// Leash Evade 중 몬스터 자원을 회복할 시점을 정의합니다.
    /// </summary>
    public enum MonsterLeashRecoveryPolicy
    {
        /// <summary>Leash 시스템에서 자원을 회복하지 않습니다.</summary>
        None = 0,

        /// <summary>Evade를 시작하면서 즉시 모든 자원을 회복합니다.</summary>
        OnEvadeStart = 1,

        /// <summary>홈 위치에 도착했을 때 모든 자원을 회복합니다.</summary>
        OnHomeReached = 2,
    }

    /// <summary>
    /// 몬스터 홈 및 Leash 동작에 필요한 테이블 값을 런타임용으로 정규화한 불변 프로필입니다.
    /// </summary>
    public readonly struct MonsterLeashProfile
    {
        private const float DefaultSoftGraceSeconds = 1.5f;
        private const float DefaultReturnStopDistance = 0.1f;
        private const float DefaultReturnMoveSpeedMultiplier = 1f;
        private const float DefaultReturnTimeoutSeconds = 8f;

        /// <summary>홈 기준 소프트 Leash 거리입니다. 0 이하면 비활성입니다.</summary>
        public float SoftLeashRange { get; }

        /// <summary>홈 기준 하드 Leash 거리입니다. 0 이하면 비활성입니다.</summary>
        public float HardLeashRange { get; }

        /// <summary>소프트 범위를 벗어난 뒤 Evade를 시작하기 전 유예 시간입니다.</summary>
        public float SoftLeashGraceSeconds { get; }

        /// <summary>홈 도착으로 판정할 거리입니다.</summary>
        public float ReturnStopDistance { get; }

        /// <summary>홈 도착 후 감지와 AI를 다시 활성화하기 전 대기 시간입니다.</summary>
        public float ReturnDelaySeconds { get; }

        /// <summary>홈 복귀 이동에 적용할 이동 속도 배율입니다.</summary>
        public float ReturnMoveSpeedMultiplier { get; }

        /// <summary>귀환 이동이 이 시간을 초과하면 홈 위치로 보정할 제한 시간입니다.</summary>
        public float ReturnTimeoutSeconds { get; }

        /// <summary>Evade 중 자원을 회복할 시점입니다.</summary>
        public MonsterLeashRecoveryPolicy RecoveryPolicy { get; }

        /// <summary>귀환 및 재활성 대기 중 피해를 무시할지 여부입니다.</summary>
        public bool InvulnerableDuringReturn { get; }

        /// <summary>Evade 시작 시 현재 적용 중인 Affect를 모두 제거할지 여부입니다.</summary>
        public bool ClearAffectsOnEvade { get; }

        /// <summary>소프트 또는 하드 Leash 거리 중 하나 이상이 활성화되었는지 여부입니다.</summary>
        public bool IsEnabled => SoftLeashRange > 0f || HardLeashRange > 0f;

        /// <summary>소프트 Leash 판정을 사용하는지 여부입니다.</summary>
        public bool HasSoftLimit => SoftLeashRange > 0f;

        /// <summary>하드 Leash 판정을 사용하는지 여부입니다.</summary>
        public bool HasHardLimit => HardLeashRange > 0f;

        /// <summary>
        /// 정규화된 Leash 프로필을 생성합니다.
        /// </summary>
        private MonsterLeashProfile(
            float softLeashRange,
            float hardLeashRange,
            float softLeashGraceSeconds,
            float returnStopDistance,
            float returnDelaySeconds,
            float returnMoveSpeedMultiplier,
            float returnTimeoutSeconds,
            MonsterLeashRecoveryPolicy recoveryPolicy,
            bool invulnerableDuringReturn,
            bool clearAffectsOnEvade)
        {
            SoftLeashRange = softLeashRange;
            HardLeashRange = hardLeashRange;
            SoftLeashGraceSeconds = softLeashGraceSeconds;
            ReturnStopDistance = returnStopDistance;
            ReturnDelaySeconds = returnDelaySeconds;
            ReturnMoveSpeedMultiplier = returnMoveSpeedMultiplier;
            ReturnTimeoutSeconds = returnTimeoutSeconds;
            RecoveryPolicy = recoveryPolicy;
            InvulnerableDuringReturn = invulnerableDuringReturn;
            ClearAffectsOnEvade = clearAffectsOnEvade;
        }

        /// <summary>
        /// monster_combat_profile 테이블 행을 런타임 Leash 프로필로 변환합니다.
        /// </summary>
        /// <param name="tableData">선택한 몬스터 전투 프로필 행입니다.</param>
        /// <returns>Leash 범위와 복귀 정책이 정규화된 프로필입니다.</returns>
        public static MonsterLeashProfile Create(StruckTableMonsterCombatProfile tableData)
        {
            float softRange = Mathf.Max(0f, tableData?.SoftLeashRange ?? 0f);
            float hardRange = Mathf.Max(0f, tableData?.HardLeashRange ?? 0f);
            if (softRange > 0f && hardRange > 0f)
            {
                hardRange = Mathf.Max(softRange, hardRange);
            }

            float softGrace = tableData != null && tableData.SoftLeashGraceSeconds > 0f
                ? tableData.SoftLeashGraceSeconds
                : DefaultSoftGraceSeconds;
            float stopDistance = tableData != null && tableData.ReturnStopDistance > 0f
                ? tableData.ReturnStopDistance
                : DefaultReturnStopDistance;
            float speedMultiplier = tableData != null && tableData.ReturnMoveSpeedMultiplier > 0f
                ? tableData.ReturnMoveSpeedMultiplier
                : DefaultReturnMoveSpeedMultiplier;
            float timeout = tableData != null && tableData.ReturnTimeoutSeconds > 0f
                ? tableData.ReturnTimeoutSeconds
                : DefaultReturnTimeoutSeconds;

            return new MonsterLeashProfile(
                softRange,
                hardRange,
                Mathf.Max(0f, softGrace),
                Mathf.Max(0.01f, stopDistance),
                Mathf.Max(0f, tableData?.ReturnDelaySeconds ?? 0f),
                Mathf.Max(0.01f, speedMultiplier),
                Mathf.Max(0.1f, timeout),
                tableData?.LeashRecoveryPolicy ?? MonsterLeashRecoveryPolicy.OnHomeReached,
                tableData?.InvulnerableDuringReturn ?? true,
                tableData?.ClearAffectsOnEvade ?? true);
        }
    }

    /// <summary>
    /// 몬스터가 복귀할 홈 위치와 초기 방향을 보관하는 불변 컨텍스트입니다.
    /// </summary>
    public readonly struct MonsterHomeContext
    {
        /// <summary>홈 월드 좌표입니다.</summary>
        public Vector3 Position { get; }

        /// <summary>홈에서 적용할 초기 좌우 반전 값입니다.</summary>
        public bool IsFlip { get; }

        /// <summary>홈이 속한 맵 UID입니다.</summary>
        public int MapUid { get; }

        /// <summary>유효한 홈 좌표를 보유하는지 여부입니다.</summary>
        public bool IsValid { get; }

        /// <summary>
        /// 홈 컨텍스트를 생성합니다.
        /// </summary>
        public MonsterHomeContext(Vector3 position, bool isFlip, int mapUid, bool isValid)
        {
            Position = position;
            IsFlip = isFlip;
            MapUid = mapUid;
            IsValid = isValid;
        }
    }
}
