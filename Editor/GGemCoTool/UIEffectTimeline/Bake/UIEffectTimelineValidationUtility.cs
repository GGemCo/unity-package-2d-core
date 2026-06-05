using System.Collections.Generic;
using UnityEngine.Timeline;

namespace GGemCo2DCoreEditor
{
    /// <summary>
    /// UI 효과 Timeline 베이크 전 정합성을 검사하는 유틸리티입니다.
    /// </summary>
    internal static class UIEffectTimelineValidationUtility
    {
        /// <summary>
        /// TimelineAsset 안에 포함된 UI 효과 Clip을 검사합니다.
        /// </summary>
        /// <param name="timelineAsset">검사할 TimelineAsset입니다.</param>
        /// <param name="messages">검사 결과 메시지 목록입니다.</param>
        /// <returns>오류가 없으면 true입니다.</returns>
        public static bool Validate(TimelineAsset timelineAsset, out List<string> messages)
        {
            messages = new List<string>();
            if (timelineAsset == null)
            {
                messages.Add("TimelineAsset이 선택되지 않았습니다.");
                return false;
            }

            foreach (var track in timelineAsset.GetOutputTracks())
            {
                if (track is not UIEffectTrack)
                {
                    continue;
                }

                foreach (TimelineClip timelineClip in track.GetClips())
                {
                    if (timelineClip.asset is not UIEffectClipBase clip)
                    {
                        continue;
                    }

                    ValidateClip(timelineClip, clip, messages);
                }
            }

            return messages.Count == 0;
        }

        /// <summary>
        /// 단일 UI 효과 Clip의 필수 값과 파라미터 범위를 검사합니다.
        /// </summary>
        private static void ValidateClip(TimelineClip timelineClip, UIEffectClipBase clip, List<string> messages)
        {
            string clipName = string.IsNullOrEmpty(timelineClip.displayName) ? clip.GetType().Name : timelineClip.displayName;
            if (string.IsNullOrWhiteSpace(clip.targetKey))
            {
                messages.Add($"{clipName}: targetKey가 비어 있습니다.");
            }

            if (timelineClip.duration <= 0d)
            {
                messages.Add($"{clipName}: Clip 길이는 0보다 커야 합니다.");
            }

            switch (clip)
            {
                case UIEffectShakeClip shakeClip when shakeClip.strength < 0f:
                    messages.Add($"{clipName}: Shake 강도는 0 이상이어야 합니다.");
                    break;
                case UIEffectShakeClip shakeClip when shakeClip.vibrato <= 0:
                    messages.Add($"{clipName}: Shake vibrato는 1 이상이어야 합니다.");
                    break;
                case UIEffectFlashClip flashClip when flashClip.repeatCount <= 0:
                    messages.Add($"{clipName}: Flash 반복 횟수는 1 이상이어야 합니다.");
                    break;
            }
        }
    }
}
