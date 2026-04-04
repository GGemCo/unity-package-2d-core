namespace GGemCo2DCore
{
    /// <summary>
    /// UI Shake 효과의 수평 시작 방향 정책입니다.
    /// </summary>
    public enum UIEffectShakeDirectionMode
    {
        /// <summary>
        /// 재생 시작 시 좌/우 방향을 무작위로 결정합니다.
        /// </summary>
        RandomHorizontal = 0,

        /// <summary>
        /// 첫 흔들림이 좌측 방향으로 시작합니다.
        /// </summary>
        Left = 1,

        /// <summary>
        /// 첫 흔들림이 우측 방향으로 시작합니다.
        /// </summary>
        Right = 2,
    }
}
