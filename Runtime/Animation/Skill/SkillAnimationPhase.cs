namespace GGemCo2DCore
{
    /// <summary>
    /// 스킬 애니메이션 단계.
    /// - CastingStart: 캐스팅 시작(1회)
    /// - CastingLoop : 캐스팅 루프(캐스팅 시간 동안)
    /// - CastingEnd  : 캐스팅 종료(1회)
    /// - Action      : 실제 스킬 사용(1회)
    /// </summary>
    public enum SkillAnimationPhase
    {
        CastingStart,
        CastingLoop,
        CastingEnd,
        Action
    }
}
