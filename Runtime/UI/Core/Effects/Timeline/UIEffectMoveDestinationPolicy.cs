namespace GGemCo2DCore
{
    /// <summary>
    /// UI Move 효과의 종료 위치 계산 기준을 정의합니다.
    /// </summary>
    public enum UIEffectMoveDestinationPolicy
    {
        /// <summary>
        /// 대상 RectTransform의 최초 기준 위치에 toOffset을 더한 위치로 이동합니다.
        /// </summary>
        InitialPositionOffset = 0,

        /// <summary>
        /// toOffset 값을 RectTransform.anchoredPosition 절대 좌표로 사용합니다.
        /// </summary>
        AbsoluteAnchoredPosition = 1,

        /// <summary>
        /// 효과가 발생한 시점의 현재 RectTransform.anchoredPosition에 toOffset을 더한 위치로 이동합니다.
        /// </summary>
        CurrentPositionOffset = 2,
    }
}
