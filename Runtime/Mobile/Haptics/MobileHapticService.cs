using UnityEngine;

namespace GGemCo2DCore
{
    /// <summary>
    /// 전투에서 발생할 수 있는 모바일 햅틱 이벤트를 정의합니다.
    /// </summary>
    public enum CombatHapticEventType
    {
        /// <summary>
        /// 일반 가드 성공입니다.
        /// </summary>
        GuardSuccess = 0,

        /// <summary>
        /// 저스트 가드 성공입니다.
        /// </summary>
        JustGuardSuccess = 1,

        /// <summary>
        /// 플레이어가 몬스터에게 확정 피해를 적용한 결과입니다.
        /// </summary>
        MonsterHit = 2,
    }

    /// <summary>
    /// 사용자 옵션과 이벤트별 재생 간격을 반영해 플랫폼 햅틱 드라이버를 호출합니다.
    /// </summary>
    public static class MobileHapticService
    {
        private const int EventTypeCount = 3;
        private const int MaxDurationMilliseconds = 2000;

        private static readonly float[] LastPlayedTimes =
        {
            float.NegativeInfinity,
            float.NegativeInfinity,
            float.NegativeInfinity,
        };

        private static IMobileHapticDriver _driver;
        private static bool _hasCachedEnabled;
        private static bool _cachedEnabled;

        /// <summary>
        /// 현재 사용자가 모바일 햅틱을 활성화했는지 확인합니다.
        /// </summary>
        /// <returns>사용자 옵션에서 햅틱이 활성화되어 있으면 <c>true</c>입니다.</returns>
        public static bool IsUserEnabled()
        {
            GGemCoOptionSettings settings = GetOptionSettings();
            if (settings == null)
            {
                return false;
            }

            EnsureEnabledCache(settings);
            return _cachedEnabled;
        }

        /// <summary>
        /// 현재 사용자 설정과 기기 지원 상태를 기준으로 햅틱을 사용할 수 있는지 확인합니다.
        /// </summary>
        /// <returns>햅틱 요청을 처리할 수 있으면 <c>true</c>입니다.</returns>
        public static bool IsEnabled()
        {
            return IsUserEnabled() && GetDriver().IsSupported;
        }

        /// <summary>
        /// 사용자의 모바일 햅틱 활성화 옵션을 저장하고 런타임 캐시에 반영합니다.
        /// </summary>
        /// <param name="enabled">햅틱을 활성화하려면 <c>true</c>입니다.</param>
        public static void SetEnabled(bool enabled)
        {
            _cachedEnabled = enabled;
            _hasCachedEnabled = true;
            PlayerPrefsManager.SaveHapticEnabled(enabled);

            if (!enabled)
            {
                GetDriver().Cancel();
            }
        }

        /// <summary>
        /// 지정한 전투 햅틱 이벤트를 설정된 프로필로 재생합니다.
        /// </summary>
        /// <param name="eventType">재생할 전투 햅틱 이벤트입니다.</param>
        /// <returns>플랫폼 드라이버에 재생 요청을 전달했으면 <c>true</c>입니다.</returns>
        public static bool TryPlay(CombatHapticEventType eventType)
        {
            GGemCoOptionSettings settings = GetOptionSettings();
            if (settings == null)
            {
                return false;
            }

            EnsureEnabledCache(settings);
            if (!_cachedEnabled || !TryGetEventIndex(eventType, out int eventIndex))
            {
                return false;
            }

            IMobileHapticDriver driver = GetDriver();
            if (!driver.IsSupported)
            {
                return false;
            }

            MobileHapticProfile profile = settings.GetHapticProfile(eventType);
            if (!profile.IsPlayable)
            {
                return false;
            }

            float now = Time.unscaledTime;
            float minInterval = Mathf.Max(0f, profile.minIntervalSeconds);
            if (now - LastPlayedTimes[eventIndex] < minInterval)
            {
                return false;
            }

            int durationMilliseconds =
                Mathf.Clamp(profile.durationMilliseconds, 1, MaxDurationMilliseconds);
            float intensity = Mathf.Clamp01(profile.intensity);
            if (!driver.TryPlay(durationMilliseconds, intensity))
            {
                return false;
            }

            LastPlayedTimes[eventIndex] = now;
            return true;
        }

        /// <summary>
        /// 저장 데이터 전체 초기화 또는 Subsystem 재시작 후 런타임 햅틱 상태를 초기화합니다.
        /// </summary>
        public static void ResetRuntimeState()
        {
            if (_driver != null)
            {
                _driver.Cancel();
            }

            ResetCachedState();
        }

        /// <summary>
        /// 저장값 캐시와 이벤트별 재생 제한 시각을 초기 상태로 되돌립니다.
        /// </summary>
        private static void ResetCachedState()
        {
            _hasCachedEnabled = false;
            _cachedEnabled = false;

            for (int i = 0; i < EventTypeCount; i++)
            {
                LastPlayedTimes[i] = float.NegativeInfinity;
            }
        }

        /// <summary>
        /// Unity Subsystem이 초기화될 때 정적 캐시와 플랫폼 드라이버를 초기 상태로 되돌립니다.
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetOnSubsystemRegistration()
        {
            // Subsystem 초기화 순서는 보장되지 않으므로 플랫폼 API를 호출하지 않고 관리 상태만 비웁니다.
            _driver = null;
            ResetCachedState();
        }

        /// <summary>
        /// Addressables에서 로드된 옵션 설정을 반환합니다.
        /// </summary>
        /// <returns>현재 옵션 설정이며, 아직 로드되지 않았으면 <c>null</c>입니다.</returns>
        private static GGemCoOptionSettings GetOptionSettings()
        {
            AddressableLoaderSettings loader = AddressableLoaderSettings.Instance;
            return loader != null ? loader.optionSettings : null;
        }

        /// <summary>
        /// PlayerPrefs의 사용자 햅틱 옵션을 최초 한 번만 읽어 런타임 캐시에 보관합니다.
        /// </summary>
        /// <param name="settings">저장값이 없을 때 기본값을 제공할 옵션 설정입니다.</param>
        private static void EnsureEnabledCache(GGemCoOptionSettings settings)
        {
            if (_hasCachedEnabled)
            {
                return;
            }

            _cachedEnabled = PlayerPrefsManager.LoadHapticEnabled(
                settings.hapticEnabledByDefault);
            _hasCachedEnabled = true;
        }

        /// <summary>
        /// 현재 플랫폼에 맞는 햅틱 드라이버를 지연 생성합니다.
        /// </summary>
        /// <returns>현재 플랫폼용 햅틱 드라이버입니다.</returns>
        private static IMobileHapticDriver GetDriver()
        {
            if (_driver != null)
            {
                return _driver;
            }

#if UNITY_ANDROID && !UNITY_EDITOR
            _driver = new AndroidMobileHapticDriver();
#else
            _driver = new NullMobileHapticDriver();
#endif
            return _driver;
        }

        /// <summary>
        /// 이벤트 타입을 재생 시각 캐시 배열의 인덱스로 변환합니다.
        /// </summary>
        /// <param name="eventType">변환할 햅틱 이벤트입니다.</param>
        /// <param name="index">유효한 배열 인덱스입니다.</param>
        /// <returns>지원하는 이벤트 타입이면 <c>true</c>입니다.</returns>
        private static bool TryGetEventIndex(CombatHapticEventType eventType, out int index)
        {
            index = (int)eventType;
            return index >= 0 && index < EventTypeCount;
        }

        /// <summary>
        /// 플랫폼별 모바일 햅틱 구현이 따라야 하는 드라이버 계약입니다.
        /// </summary>
        private interface IMobileHapticDriver
        {
            /// <summary>
            /// 현재 기기에서 햅틱을 사용할 수 있는지 여부입니다.
            /// </summary>
            bool IsSupported { get; }

            /// <summary>
            /// 지정한 시간과 세기로 햅틱을 재생합니다.
            /// </summary>
            /// <param name="durationMilliseconds">햅틱 재생 시간(ms)입니다.</param>
            /// <param name="intensity">0~1 범위의 햅틱 세기입니다.</param>
            /// <returns>재생 요청에 성공했으면 <c>true</c>입니다.</returns>
            bool TryPlay(int durationMilliseconds, float intensity);

            /// <summary>
            /// 현재 재생 중인 햅틱을 중단합니다.
            /// </summary>
            void Cancel();
        }

        /// <summary>
        /// Android 진동 API를 사용하는 모바일 햅틱 드라이버입니다.
        /// </summary>
        private sealed class AndroidMobileHapticDriver : IMobileHapticDriver
        {
            /// <inheritdoc />
            public bool IsSupported => AndroidVibrationUtility.IsSupported();

            /// <inheritdoc />
            public bool TryPlay(int durationMilliseconds, float intensity)
            {
                int amplitude = Mathf.Clamp(
                    Mathf.RoundToInt(Mathf.Clamp01(intensity) * 255f),
                    1,
                    255);
                return AndroidVibrationUtility.TryVibrate(
                    durationMilliseconds,
                    amplitude);
            }

            /// <inheritdoc />
            public void Cancel()
            {
                AndroidVibrationUtility.Cancel();
            }
        }

        /// <summary>
        /// 햅틱을 지원하지 않는 플랫폼에서 사용하는 무동작 드라이버입니다.
        /// </summary>
        private sealed class NullMobileHapticDriver : IMobileHapticDriver
        {
            /// <inheritdoc />
            public bool IsSupported => false;

            /// <inheritdoc />
            public bool TryPlay(int durationMilliseconds, float intensity)
            {
                return false;
            }

            /// <inheritdoc />
            public void Cancel()
            {
            }
        }
    }
}
