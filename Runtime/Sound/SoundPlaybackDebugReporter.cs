using UnityEngine;

namespace GGemCo2DCore
{
    /// <summary>
    /// 실제 AudioSource 재생 시작 시점의 사운드 식별 정보를 개발용 로그로 출력합니다.
    /// 설정이 비활성화된 경우 로그 문자열을 생성하지 않습니다.
    /// </summary>
    internal sealed class SoundPlaybackDebugReporter
    {
        private readonly GGemCoSoundSettings _settings;

        /// <summary>
        /// 런타임에 변경 가능한 사운드 디버그 설정을 참조하도록 보고기를 생성합니다.
        /// </summary>
        /// <param name="settings">사운드 디버그 활성화 여부를 제공하는 설정입니다.</param>
        internal SoundPlaybackDebugReporter(GGemCoSoundSettings settings)
        {
            _settings = settings;
        }

        /// <summary>
        /// 대표 sound UID에서 해석된 사운드가 실제로 재생되기 시작했음을 출력합니다.
        /// </summary>
        /// <param name="resolved">대표 UID와 실제 리소스 UID를 포함한 해석 결과입니다.</param>
        /// <param name="clip">AudioSource에서 재생을 시작한 클립입니다.</param>
        internal void ReportStarted(ResolvedSound resolved, AudioClip clip)
        {
            if (_settings == null || !_settings.EnablePlayingSoundUid)
                return;

            string clipName = clip != null ? clip.name : resolved.FileName;
            GcLogger.Log(
                $"[Sound][Started] soundUid={resolved.RequestedSoundUid}, " +
                $"resourceUid={resolved.ResourceUid}, type={resolved.Type}, " +
                $"clip={clipName}, loop={resolved.Loop}");
        }

        /// <summary>
        /// 대표 sound UID를 거치지 않고 직접 전달된 클립이나 리소스가 재생되기 시작했음을 출력합니다.
        /// </summary>
        /// <param name="resourceUid">직접 재생한 리소스 UID입니다. 알 수 없으면 0입니다.</param>
        /// <param name="type">재생된 사운드 종류입니다.</param>
        /// <param name="clip">AudioSource에서 재생을 시작한 클립입니다.</param>
        /// <param name="loop">루프 재생 여부입니다.</param>
        internal void ReportDirectStarted(
            int resourceUid,
            SoundConstants.Type type,
            AudioClip clip,
            bool loop)
        {
            if (_settings == null || !_settings.EnablePlayingSoundUid)
                return;

            string resourceUidText = resourceUid > 0 ? resourceUid.ToString() : "N/A";
            string clipName = clip != null ? clip.name : "N/A";
            GcLogger.Log(
                $"[Sound][Started] soundUid=N/A, resourceUid={resourceUidText}, " +
                $"type={type}, clip={clipName}, loop={loop}");
        }
    }
}
