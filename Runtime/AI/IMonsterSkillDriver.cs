using UnityEngine;

namespace GGemCo2DCore
{
    /// <summary>
    /// CharacterBase가 스킬 시스템을 간접 호출할 수 있도록 제공하는 공용 실행 인터페이스입니다.
    /// </summary>
    public interface ICharacterSkillDriver
    {
        /// <summary>
        /// 현재 스킬(캐스팅/사용 애니메이션 포함)이 진행 중인지 여부입니다.
        /// </summary>
        bool IsSkillBusy { get; }

        /// <summary>
        /// 스킬 사용을 시도합니다.
        /// </summary>
        /// <param name="skillUid">사용할 스킬 UID입니다.</param>
        /// <param name="request">스킬 타겟, 방향, 테이블 소스 정보입니다.</param>
        /// <returns>시도 결과입니다.</returns>
        SkillUseResult TryUseSkill(int skillUid, in SkillDriverRequest request);
    }

    /// <summary>
    /// 몬스터 AI(예: BT)가 스킬 시스템을 간접 호출할 수 있도록 제공하는 몬스터 전용 실행 인터페이스입니다.
    /// </summary>
    /// <remarks>
    /// 기존 호출부와의 호환을 위해 유지됩니다.
    /// </remarks>
    public interface IMonsterSkillDriver : ICharacterSkillDriver
    {
        /// <summary>
        /// 몬스터 타겟 컨텍스트로 스킬 사용을 시도합니다.
        /// </summary>
        /// <param name="skillUid">사용할 스킬 UID(테이블 Uid).</param>
        /// <param name="target">몬스터 스킬 타겟/방향 정보.</param>
        /// <returns>시도 결과.</returns>
        SkillUseResult TryUseSkill(int skillUid, in MonsterSkillTarget target);
    }

    public enum MonsterSkillExecutionState
    {
        None = 0,
        Started = 1,
        Succeeded = 2,
        Canceled = 3,
        Failed = 4,
    }

    /// <summary>
    /// 몬스터 스킬 실행 종료 결과를 BT 등 외부 의사결정 시스템이 읽을 수 있도록 제공하는 결과 모델입니다.
    /// </summary>
    public readonly struct MonsterSkillExecutionResult
    {
        public readonly int SkillUid;
        public readonly MonsterSkillExecutionState State;
        public readonly int Sequence;
        public readonly float EndTime;

        public MonsterSkillExecutionResult(int skillUid, MonsterSkillExecutionState state, int sequence, float endTime)
        {
            SkillUid = skillUid;
            State = state;
            Sequence = sequence;
            EndTime = endTime;
        }
    }

    public enum MonsterSkillCombatOutcome
    {
        Hit = 0,
        Guarded = 1,
        JustGuarded = 2,
        Missed = 3,
        Immune = 4,
        Evaded = 5,
        GuardBroken = 6,
    }

    /// <summary>
    /// 몬스터 스킬의 전투 결과(명중/가드/빗나감 등)를 BT가 확인할 수 있도록 제공하는 리포트입니다.
    /// </summary>
    public readonly struct MonsterSkillCombatReport
    {
        public readonly int SkillUid;
        public readonly MonsterSkillCombatOutcome Outcome;
        public readonly int AttackId;
        public readonly int Sequence;
        public readonly float Time;

        public MonsterSkillCombatReport(int skillUid, MonsterSkillCombatOutcome outcome, int attackId, int sequence, float time)
        {
            SkillUid = skillUid;
            Outcome = outcome;
            AttackId = attackId;
            Sequence = sequence;
            Time = time;
        }
    }

    /// <summary>
    /// BT가 몬스터 스킬의 진행/종료 결과를 추적할 수 있도록 확장한 스킬 드라이버 인터페이스입니다.
    /// </summary>
    public interface IMonsterSkillDriverFeedback : IMonsterSkillDriver
    {
        /// <summary>
        /// 지정한 스킬 UID가 현재 실행 중인지 여부입니다.
        /// </summary>
        bool IsRunningSkill(int skillUid);

        /// <summary>
        /// 지정한 스킬 UID의 마지막 실행 결과를 조회합니다.
        /// </summary>
        bool TryGetLastSkillResult(int skillUid, out MonsterSkillExecutionResult result);

        /// <summary>
        /// 지정한 스킬 UID의 마지막 실행 결과를 소비합니다.
        /// 같은 결과는 한 번만 읽을 수 있습니다.
        /// </summary>
        bool ConsumeLastSkillResult(int skillUid, out MonsterSkillExecutionResult result);

        /// <summary>
        /// 지정한 스킬 UID의 마지막 전투 결과를 조회합니다.
        /// </summary>
        bool TryGetLastSkillCombatReport(int skillUid, out MonsterSkillCombatReport report);

        /// <summary>
        /// 지정한 스킬 UID의 마지막 전투 결과를 소비합니다.
        /// 같은 결과는 한 번만 읽을 수 있습니다.
        /// </summary>
        bool ConsumeLastSkillCombatReport(int skillUid, out MonsterSkillCombatReport report);
    }

    public enum SkillUseFailReason
    {
        None = 0,
        InvalidSource = 1,
        InvalidInput = 2,
        Busy = 3,
        Cooldown = 4,
        InvalidDefinition = 5,
        NoTarget = 6,
        OutOfRange = 7,
        ExecutionRejected = 8,
        ControlLocked = 9,
        InsufficientMp = 10,
    }

    public readonly struct SkillUseResult
    {
        public bool IsStarted { get; }
        public SkillUseFailReason FailReason { get; }

        public static SkillUseResult Started => new SkillUseResult(true, SkillUseFailReason.None);
        public static SkillUseResult Rejected => new SkillUseResult(false, SkillUseFailReason.None);

        public SkillUseResult(bool isStarted, SkillUseFailReason failReason)
        {
            IsStarted = isStarted;
            FailReason = isStarted ? SkillUseFailReason.None : failReason;
        }

        public static SkillUseResult Fail(SkillUseFailReason failReason)
            => new SkillUseResult(false, failReason);
    }

    /// <summary>
    /// 스킬 실행 시 1회성으로 적용할 옵션 스냅샷입니다.
    /// </summary>
    public readonly struct SkillExecutionOptions
    {
        /// <summary>옵션이 없는 기본 실행 옵션입니다.</summary>
        public static SkillExecutionOptions None => new SkillExecutionOptions(1f, 0f, 0L, 0, 0L, 1f, 0);

        /// <summary>스킬 데미지에 곱할 최종 배율입니다.</summary>
        public readonly float DamageMultiplier;

        /// <summary>스킬이 적용하는 상태 지속시간에 더할 초 단위 보너스입니다.</summary>
        public readonly float StatusDurationBonusSeconds;

        /// <summary>스킬 시작 시 캐스터에게 부여할 런타임 Temp HP 값입니다.</summary>
        public readonly long RuntimeTempHpOnStart;

        /// <summary>런타임 Temp HP source key를 직접 지정할 때 사용하는 값입니다. 0이면 스킬 UID를 사용합니다.</summary>
        public readonly int RuntimeTempHpSourceKeyOverride;

        /// <summary>힐 계열 Affect가 실제 회복을 실행할 때 최종 회복량에 더할 HP 값입니다.</summary>
        public readonly long HealHpBonus;

        /// <summary>힐 계열 Affect가 실제 회복을 실행할 때 최종 회복량에 곱할 배율입니다.</summary>
        public readonly float HealHpMultiplier;

        /// <summary>
        /// 스킬 타격 성공 시 이벤트 기본 MP 획득량에 더할 보너스 MP입니다.
        /// </summary>
        public readonly int SkillHitMpGainBonus;

        /// <summary>
        /// 스킬 실행 옵션 스냅샷을 생성합니다.
        /// </summary>
        /// <param name="damageMultiplier">스킬 데미지에 곱할 배율입니다. 0 이하이면 1로 보정합니다.</param>
        /// <param name="statusDurationBonusSeconds">상태 지속시간에 더할 초 단위 보너스입니다.</param>
        /// <param name="runtimeTempHpOnStart">스킬 시작 시 부여할 런타임 Temp HP 값입니다.</param>
        /// <param name="runtimeTempHpSourceKeyOverride">Temp HP source key 오버라이드입니다. 0이면 스킬 UID를 사용합니다.</param>
        /// <param name="healHpBonus">힐 계열 Affect가 실제 회복을 실행할 때 최종 회복량에 더할 HP 값입니다.</param>
        /// <param name="healHpMultiplier">힐 계열 Affect가 실제 회복을 실행할 때 최종 회복량에 곱할 배율입니다.</param>
        /// <param name="skillHitMpGainBonus">스킬 적중 시 추가할 MP 값 입니다.</param>
        public SkillExecutionOptions(
            float damageMultiplier,
            float statusDurationBonusSeconds,
            long runtimeTempHpOnStart,
            int runtimeTempHpSourceKeyOverride = 0,
            long healHpBonus = 0L,
            float healHpMultiplier = 1f,
            int skillHitMpGainBonus = 0)
        {
            DamageMultiplier = damageMultiplier > 0f ? damageMultiplier : 1f;
            StatusDurationBonusSeconds = statusDurationBonusSeconds > 0f ? statusDurationBonusSeconds : 0f;
            RuntimeTempHpOnStart = runtimeTempHpOnStart > 0L ? runtimeTempHpOnStart : 0L;
            RuntimeTempHpSourceKeyOverride = runtimeTempHpSourceKeyOverride;
            HealHpBonus = healHpBonus > 0L ? healHpBonus : 0L;
            HealHpMultiplier = healHpMultiplier > 0f ? healHpMultiplier : 1f;
            SkillHitMpGainBonus = skillHitMpGainBonus > 0 ? skillHitMpGainBonus : 0;
        }

        /// <summary>
        /// 현재 옵션에 다른 옵션을 누적해 새 스냅샷을 반환합니다.
        /// </summary>
        /// <param name="other">누적할 추가 옵션입니다.</param>
        /// <returns>두 옵션이 병합된 스냅샷입니다.</returns>
        public SkillExecutionOptions Combine(in SkillExecutionOptions other)
        {
            float baseDamageMultiplier = DamageMultiplier > 0f ? DamageMultiplier : 1f;
            float otherDamageMultiplier = other.DamageMultiplier > 0f ? other.DamageMultiplier : 1f;
            int sourceKey = other.RuntimeTempHpSourceKeyOverride != 0
                ? other.RuntimeTempHpSourceKeyOverride
                : RuntimeTempHpSourceKeyOverride;

            float baseHealHpMultiplier = HealHpMultiplier > 0f ? HealHpMultiplier : 1f;
            float otherHealHpMultiplier = other.HealHpMultiplier > 0f ? other.HealHpMultiplier : 1f;

            return new SkillExecutionOptions(
                baseDamageMultiplier * otherDamageMultiplier,
                StatusDurationBonusSeconds + other.StatusDurationBonusSeconds,
                RuntimeTempHpOnStart + other.RuntimeTempHpOnStart,
                sourceKey,
                HealHpBonus + other.HealHpBonus,
                baseHealHpMultiplier * otherHealHpMultiplier,
                SkillHitMpGainBonus + other.SkillHitMpGainBonus);
        }
    }

    /// <summary>
    /// 일반 스킬 사용 제한을 선택적으로 완화하는 발동 정책입니다.
    /// </summary>
    /// <remarks>
    /// 기본값은 모든 완화 옵션이 꺼진 상태입니다.
    /// 긴급 회복기처럼 명시적으로 허용된 요청만 이 값을 설정해야 합니다.
    /// </remarks>
    public readonly struct SkillActivationOptions
    {
        /// <summary>
        /// 일반 스킬과 동일한 제한을 사용하는 기본 발동 정책입니다.
        /// </summary>
        public static SkillActivationOptions None => default;

        /// <summary>
        /// 캐릭터가 조작 불가 상태여도 요청을 계속 검증할지 여부입니다.
        /// </summary>
        public readonly bool AllowWhileControlLocked;

        /// <summary>
        /// 실행 중인 스킬을 중단하고 새 스킬로 교체할지 여부입니다.
        /// </summary>
        public readonly bool InterruptRunningSkill;

        /// <summary>
        /// 새 스킬을 시작하기 직전에 현재 및 예약된 Crowd Control을 해제할지 여부입니다.
        /// </summary>
        public readonly bool StopCrowdControlOnStart;

        /// <summary>
        /// 새 스킬을 시작하기 직전에 기본 공격과 이동을 포함한 모든 플레이어 행동을 취소할지 여부입니다.
        /// </summary>
        public readonly bool CancelAllActionsOnStart;

        /// <summary>
        /// 스킬 발동 정책을 생성합니다.
        /// </summary>
        /// <param name="allowWhileControlLocked">조작 불가 상태에서의 발동 허용 여부입니다.</param>
        /// <param name="interruptRunningSkill">실행 중 스킬의 중단 허용 여부입니다.</param>
        /// <param name="stopCrowdControlOnStart">스킬 시작 직전 Crowd Control 해제 여부입니다.</param>
        /// <param name="cancelAllActionsOnStart">스킬 시작 직전 모든 플레이어 행동의 강제 취소 여부입니다.</param>
        public SkillActivationOptions(
            bool allowWhileControlLocked,
            bool interruptRunningSkill,
            bool stopCrowdControlOnStart,
            bool cancelAllActionsOnStart = false)
        {
            AllowWhileControlLocked = allowWhileControlLocked;
            InterruptRunningSkill = interruptRunningSkill;
            StopCrowdControlOnStart = stopCrowdControlOnStart;
            CancelAllActionsOnStart = cancelAllActionsOnStart;
        }
    }

    /// <summary>
    /// 공용 스킬 드라이버가 스킬 실행 계층으로 전달하는 최소 요청 컨텍스트입니다.
    /// </summary>
    public readonly struct SkillDriverRequest
    {
        public readonly Transform LockedTarget;
        public readonly Vector3 GroundPoint;
        public readonly Vector2 Forward;
        public readonly ConfigCommon.SkillTableSource Source;
        public readonly SkillExecutionOptions ExecutionOptions;
        public readonly SkillActivationOptions ActivationOptions;

        public SkillDriverRequest(
            Transform lockedTarget,
            Vector3 groundPoint,
            Vector2 forward,
            ConfigCommon.SkillTableSource source)
            : this(
                lockedTarget,
                groundPoint,
                forward,
                source,
                SkillExecutionOptions.None,
                SkillActivationOptions.None)
        {
        }

        /// <summary>
        /// 스킬 드라이버 요청 컨텍스트를 생성합니다.
        /// </summary>
        /// <param name="lockedTarget">락온 대상입니다.</param>
        /// <param name="groundPoint">지면 대상 좌표입니다.</param>
        /// <param name="forward">전방 방향입니다.</param>
        /// <param name="source">스킬 테이블 출처입니다.</param>
        /// <param name="executionOptions">이번 스킬 실행에만 적용할 옵션 스냅샷입니다.</param>
        public SkillDriverRequest(
            Transform lockedTarget,
            Vector3 groundPoint,
            Vector2 forward,
            ConfigCommon.SkillTableSource source,
            SkillExecutionOptions executionOptions)
            : this(
                lockedTarget,
                groundPoint,
                forward,
                source,
                executionOptions,
                SkillActivationOptions.None)
        {
        }

        /// <summary>
        /// 실행 옵션과 발동 정책을 포함한 스킬 요청 컨텍스트를 생성합니다.
        /// </summary>
        /// <param name="lockedTarget">락온 대상입니다.</param>
        /// <param name="groundPoint">지면 대상 좌표입니다.</param>
        /// <param name="forward">전방 방향입니다.</param>
        /// <param name="source">스킬 테이블 출처입니다.</param>
        /// <param name="executionOptions">이번 스킬 실행에만 적용할 옵션 스냅샷입니다.</param>
        /// <param name="activationOptions">이번 요청에만 적용할 발동 제한 완화 정책입니다.</param>
        public SkillDriverRequest(
            Transform lockedTarget,
            Vector3 groundPoint,
            Vector2 forward,
            ConfigCommon.SkillTableSource source,
            SkillExecutionOptions executionOptions,
            SkillActivationOptions activationOptions)
        {
            LockedTarget = lockedTarget;
            GroundPoint = groundPoint;
            Forward = forward.sqrMagnitude < 1e-6f ? Vector2.right : forward.normalized;
            Source = source;
            ExecutionOptions = executionOptions;
            ActivationOptions = activationOptions;
        }

        public SkillDriverRequest(in MonsterSkillTarget target, ConfigCommon.SkillTableSource source)
            : this(target.LockedTarget, target.GroundPoint, target.Forward, source)
        {
        }

        /// <summary>
        /// 기존 타겟팅 정보는 유지하고 실행 옵션만 교체한 요청을 반환합니다.
        /// </summary>
        /// <param name="executionOptions">새로 적용할 실행 옵션입니다.</param>
        /// <returns>실행 옵션이 교체된 요청입니다.</returns>
        public SkillDriverRequest WithExecutionOptions(in SkillExecutionOptions executionOptions)
        {
            return new SkillDriverRequest(
                LockedTarget,
                GroundPoint,
                Forward,
                Source,
                executionOptions,
                ActivationOptions);
        }

        /// <summary>
        /// 기존 타겟팅과 실행 옵션은 유지하고 발동 정책만 교체한 요청을 반환합니다.
        /// </summary>
        /// <param name="activationOptions">새로 적용할 발동 정책입니다.</param>
        /// <returns>발동 정책이 교체된 요청입니다.</returns>
        public SkillDriverRequest WithActivationOptions(in SkillActivationOptions activationOptions)
        {
            return new SkillDriverRequest(
                LockedTarget,
                GroundPoint,
                Forward,
                Source,
                ExecutionOptions,
                activationOptions);
        }
    }

    /// <summary>
    /// BT 등 외부 의사결정 시스템이 스킬 실행 계층으로 전달하는 최소 타겟 컨텍스트.
    /// </summary>
    public readonly struct MonsterSkillTarget
    {
        public readonly Transform LockedTarget;
        public readonly Vector3 GroundPoint;
        public readonly Vector2 Forward;

        public MonsterSkillTarget(Transform lockedTarget, Vector3 groundPoint, Vector2 forward)
        {
            LockedTarget = lockedTarget;
            GroundPoint = groundPoint;
            Forward = forward.sqrMagnitude < 1e-6f ? Vector2.right : forward.normalized;
        }
    }
}
