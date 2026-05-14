namespace GGemCo2DCore
{
    /// <summary>
    /// 스킬 애니메이션 이름 규칙.
    /// Skill 패키지는 구현(Spine/Sprite)을 알지 못하므로, 이름 생성 규칙은 Core에서 관리한다.
    /// </summary>
    public static class SkillAnimationNaming
    {
        /// <summary>
        /// 기본 네이밍 규칙으로 스킬 애니메이션 이름을 만든다.
        /// 예) skill_10001_cast_start
        /// </summary>
        public static string GetName(int skillUid, SkillAnimationPhase phase)
        {
            return phase switch
            {
                SkillAnimationPhase.CastingStart => $"skill_{skillUid}_cast_start",
                SkillAnimationPhase.CastingLoop  => $"skill_{skillUid}_cast_loop",
                SkillAnimationPhase.CastingEnd    => $"skill_{skillUid}_cast_end",
                SkillAnimationPhase.Action        => $"skill_{skillUid}_action",
                SkillAnimationPhase.ChargeStart   => $"skill_{skillUid}_charge_start",
                SkillAnimationPhase.ChargeLoop    => $"skill_{skillUid}_charge_loop",
                SkillAnimationPhase.ChargeComplete=> $"skill_{skillUid}_charge_complete",
                SkillAnimationPhase.ChargeFail    => $"skill_{skillUid}_charge_fail",
                _ => string.Empty
            };
        }
    }
}
