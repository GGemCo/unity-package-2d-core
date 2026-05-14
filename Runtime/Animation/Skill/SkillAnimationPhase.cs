namespace GGemCo2DCore
{
    /// <summary>
    /// 스킬 애니메이션 단계입니다.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Skill 패키지는 이 값과 선택적 애니메이션 이름 오버라이드를 함께 전달하여,
    /// Sprite/Spine 구현체가 동일한 요청 규격으로 스킬 애니메이션을 재생하게 합니다.
    /// </para>
    /// <para>
    /// 차징 단계는 기존 캐스팅 단계와 별도로 분리하여, 사용 전 차징 루프/완료/실패 연출을 안전하게 표현합니다.
    /// </para>
    /// </remarks>
    public enum SkillAnimationPhase
    {
        /// <summary>캐스팅 시작 시 1회 재생하는 애니메이션 단계입니다.</summary>
        CastingStart,

        /// <summary>캐스팅 유지 시간 동안 루프로 재생하는 애니메이션 단계입니다.</summary>
        CastingLoop,

        /// <summary>캐스팅 종료 시 1회 재생하는 애니메이션 단계입니다.</summary>
        CastingEnd,

        /// <summary>실제 스킬 사용 시 1회 재생하는 애니메이션 단계입니다.</summary>
        Action,

        /// <summary>차징 시작 시 1회 재생하는 애니메이션 단계입니다.</summary>
        ChargeStart,

        /// <summary>차징 단계 유지 중 루프로 재생하는 애니메이션 단계입니다.</summary>
        ChargeLoop,

        /// <summary>차징 완료 후 실제 사용 단계로 넘어가기 전에 재생하는 애니메이션 단계입니다.</summary>
        ChargeComplete,

        /// <summary>차징 게이지가 0이 되어 실패했을 때 재생하는 애니메이션 단계입니다.</summary>
        ChargeFail
    }
}
