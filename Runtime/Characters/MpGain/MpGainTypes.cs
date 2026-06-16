namespace GGemCo2DCore
{
    /// <summary>
    /// MP 획득 판정을 시작한 피드백 종류입니다.
    /// </summary>
    public enum MpGainTrigger
    {
        /// <summary>
        /// 알 수 없는 피드백입니다.
        /// </summary>
        None = 0,

        /// <summary>
        /// 공격자가 타격 결과를 확정받은 피드백입니다.
        /// </summary>
        OutgoingAttackHit = 1,

        /// <summary>
        /// 플레이어가 가드 성공 결과를 확정받은 피드백입니다.
        /// </summary>
        PlayerGuardSuccess = 2,
    }

    /// <summary>
    /// MP 획득 보상 종류입니다.
    /// </summary>
    public enum MpGainRewardKind
    {
        /// <summary>
        /// 보상이 없습니다.
        /// </summary>
        None = 0,

        /// <summary>
        /// 게임별 Provider가 정의하는 일반 보상입니다.
        /// </summary>
        Custom = 1,

        /// <summary>
        /// 카운터 공격 성공 보상입니다.
        /// </summary>
        CounterAttackSuccess = 10,

        /// <summary>
        /// 일반 가드 성공 보상입니다.
        /// </summary>
        GuardSuccess = 20,

        /// <summary>
        /// 가드 브레이크 성공 보상입니다.
        /// </summary>
        GuardBreakSuccess = 21,

        /// <summary>
        /// 저스트 가드 성공 보상입니다.
        /// </summary>
        JustGuardSuccess = 22,

        /// <summary>
        /// 기본 콤보 마지막 타격 성공 보상입니다.
        /// </summary>
        BasicComboLastHitSuccess = 30,

        /// <summary>
        /// 일반 스킬 타격 성공 보상입니다.
        /// </summary>
        SkillHitSuccess = 40,
    }
}
