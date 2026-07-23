using System;
using UnityEngine;

namespace GGemCo2DCore
{
    /// <summary>
    /// 단일 모바일 햅틱 이벤트의 재생 강도, 시간, 반복 제한 정책을 정의합니다.
    /// </summary>
    [Serializable]
    public struct MobileHapticProfile
    {
        [Tooltip("이 햅틱 이벤트를 사용할지 여부입니다.")]
        public bool enabled;

        [Tooltip("햅틱 세기입니다. 플랫폼 드라이버가 지원하는 세기 범위로 변환합니다.")]
        [Range(0f, 1f)]
        public float intensity;

        [Tooltip("햅틱 재생 시간(ms)입니다.")]
        [Min(1)]
        public int durationMilliseconds;

        [Tooltip("같은 햅틱 이벤트를 다시 재생하기까지 필요한 최소 간격(초)입니다.")]
        [Min(0f)]
        public float minIntervalSeconds;

        /// <summary>
        /// 현재 프로필이 실제 햅틱 요청에 사용할 수 있는지 확인합니다.
        /// </summary>
        public bool IsPlayable =>
            enabled &&
            intensity > 0f &&
            durationMilliseconds > 0;

        /// <summary>
        /// 지정한 값으로 모바일 햅틱 프로필을 생성합니다.
        /// </summary>
        /// <param name="intensity">0~1 범위의 햅틱 세기입니다.</param>
        /// <param name="durationMilliseconds">햅틱 재생 시간(ms)입니다.</param>
        /// <param name="minIntervalSeconds">동일 이벤트의 최소 재생 간격(초)입니다.</param>
        /// <returns>활성화된 햅틱 프로필입니다.</returns>
        public static MobileHapticProfile Create(
            float intensity,
            int durationMilliseconds,
            float minIntervalSeconds)
        {
            return new MobileHapticProfile
            {
                enabled = true,
                intensity = Mathf.Clamp01(intensity),
                durationMilliseconds = Mathf.Max(1, durationMilliseconds),
                minIntervalSeconds = Mathf.Max(0f, minIntervalSeconds),
            };
        }
    }
}
