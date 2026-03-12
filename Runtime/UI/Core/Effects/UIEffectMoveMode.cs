namespace GGemCo2DCore
{
    /// <summary>
    /// UI 이동 효과의 기준 위치와 오프셋 적용 방향을 정의합니다.
    /// </summary>
    public enum UIEffectMoveMode
    {
        /// <summary>
        /// 기준 위치 + 오프셋에서 시작하여 기준 위치로 이동합니다.
        /// 주로 윈도우 열기 연출에 사용합니다.
        /// </summary>
        FromOffsetToBase = 0,

        /// <summary>
        /// 기준 위치에서 시작하여 기준 위치 + 오프셋으로 이동합니다.
        /// 주로 윈도우 닫기 연출에 사용합니다.
        /// </summary>
        FromBaseToOffset = 1,
    }
}
