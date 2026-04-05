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
    /// 공용 스킬 드라이버가 스킬 실행 계층으로 전달하는 최소 요청 컨텍스트입니다.
    /// </summary>
    public readonly struct SkillDriverRequest
    {
        public readonly Transform LockedTarget;
        public readonly Vector3 GroundPoint;
        public readonly Vector2 Forward;
        public readonly ConfigCommon.SkillTableSource Source;

        public SkillDriverRequest(
            Transform lockedTarget,
            Vector3 groundPoint,
            Vector2 forward,
            ConfigCommon.SkillTableSource source)
        {
            LockedTarget = lockedTarget;
            GroundPoint = groundPoint;
            Forward = forward.sqrMagnitude < 1e-6f ? Vector2.right : forward.normalized;
            Source = source;
        }

        public SkillDriverRequest(in MonsterSkillTarget target, ConfigCommon.SkillTableSource source)
            : this(target.LockedTarget, target.GroundPoint, target.Forward, source)
        {
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