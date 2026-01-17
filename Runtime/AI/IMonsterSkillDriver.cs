using UnityEngine;

namespace GGemCo2DCore
{
    /// <summary>
    /// 몬스터 AI(예: BT)가 스킬 시스템을 간접 호출할 수 있도록 제공하는 최소 실행 인터페이스.
    /// </summary>
    /// <remarks>
    /// Core는 Skill 패키지 타입에 의존하지 않기 위해, 스킬 식별자는 문자열(skillId)로만 전달한다.
    /// </remarks>
    public interface IMonsterSkillDriver
    {
        /// <summary>
        /// 현재 스킬(캐스팅/사용 애니메이션 포함)이 진행 중인지 여부.
        /// </summary>
        bool IsSkillBusy { get; }

        /// <summary>
        /// 스킬 사용을 시도한다.
        /// </summary>
        /// <param name="skillId">사용할 스킬 식별자(예: SK_0001).</param>
        /// <param name="target">스킬 타겟/방향 정보.</param>
        /// <returns>시도 결과.</returns>
        SkillUseResult TryUseSkill(string skillId, in MonsterSkillTarget target);
    }

    public enum SkillUseResult
    {
        Rejected = 0,
        Started = 1,
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