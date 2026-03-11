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

    public enum SkillUseResult
    {
        Rejected = 0,
        Started = 1,
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