namespace GGemCo2DCore
{
    /// <summary>
    /// 사운드 UID와 선택적인 페이드 시간 Override를 함께 전달하는 요청 값입니다.
    /// </summary>
    public readonly struct SoundFadeRequest
    {
        /// <summary>
        /// 사운드 요청 값을 생성합니다.
        /// </summary>
        /// <param name="soundUid">재생할 대표 sound UID입니다.</param>
        /// <param name="useFadeDurationOverride">요청 단위 페이드 시간을 사용할지 여부입니다.</param>
        /// <param name="fadeDurationOverride">요청 단위 페이드 시간입니다.</param>
        public SoundFadeRequest(
            int soundUid,
            bool useFadeDurationOverride = false,
            float fadeDurationOverride = 0f)
        {
            SoundUid = soundUid;
            UseFadeDurationOverride = useFadeDurationOverride;
            FadeDurationOverride = fadeDurationOverride > 0f ? fadeDurationOverride : 0f;
        }

        /// <summary>재생할 대표 sound UID입니다.</summary>
        public int SoundUid { get; }

        /// <summary>요청 단위 페이드 시간을 사용할지 여부입니다.</summary>
        public bool UseFadeDurationOverride { get; }

        /// <summary>요청 단위 페이드 시간입니다.</summary>
        public float FadeDurationOverride { get; }
    }
}
