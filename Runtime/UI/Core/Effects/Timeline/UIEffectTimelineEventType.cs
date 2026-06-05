namespace GGemCo2DCore
{
    /// <summary>
    /// UI 효과 타임라인에서 실행할 이벤트 종류입니다.
    /// </summary>
    public enum UIEffectTimelineEventType
    {
        /// <summary>
        /// 알파 값을 보간하는 Fade 이벤트입니다.
        /// </summary>
        Fade = 0,

        /// <summary>
        /// RectTransform 위치를 보간하는 Move 이벤트입니다.
        /// </summary>
        Move = 1,

        /// <summary>
        /// RectTransform 스케일을 보간하는 Scale 이벤트입니다.
        /// </summary>
        Scale = 2,

        /// <summary>
        /// RectTransform 위치를 기준으로 흔들림을 적용하는 Shake 이벤트입니다.
        /// </summary>
        Shake = 3,

        /// <summary>
        /// Graphic 색상을 짧게 강조하는 Flash 이벤트입니다.
        /// </summary>
        Flash = 4,
    }
}
