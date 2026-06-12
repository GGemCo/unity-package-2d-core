namespace GGemCo2DCore
{
    /// <summary>
    /// 사운드 재생을 어떤 기준으로 정지할지 결정하는 정책입니다.
    /// </summary>
    public enum SoundPlaybackStopPolicy
    {
        /// <summary>
        /// 기존 동작입니다. 루프 사운드는 지정 지속 시간이 있으면 그 시간 뒤, 없으면 클립 길이 뒤에 정리합니다.
        /// </summary>
        Auto = 0,

        /// <summary>
        /// 요청에 지정된 지속 시간이 끝나면 정리합니다.
        /// </summary>
        ByDuration = 1,

        /// <summary>
        /// 외부에서 받은 <see cref="SoundPlaybackHandle"/>이 정지될 때까지 재생을 유지합니다.
        /// </summary>
        ByHandle = 2,
    }
}
