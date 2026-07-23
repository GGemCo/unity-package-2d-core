using System;
using UnityEngine;

namespace GGemCo2DCore
{
    /// <summary>
    /// Android Vibrator API를 래핑하여 시간과 세기를 지정할 수 있는 진동 기능을 제공합니다.
    /// </summary>
    public static class AndroidVibrationUtility
    {
        private const int VibrationEffectApiLevel = 26;
        private const int VibratorManagerApiLevel = 31;
        private const int DefaultAmplitude = -1;

        private static AndroidJavaObject _vibrator;
        private static bool _isInitialized;
        private static bool _isSupported;
        private static bool _hasAmplitudeControl;
        private static bool _hasLoggedFailure;
        private static int _sdkInt;

        /// <summary>
        /// Android 진동을 실행합니다.
        /// </summary>
        /// <param name="milliseconds">진동 시간(ms)입니다.</param>
        /// <param name="amplitude">진동 세기(1~255)입니다.</param>
        public static void Vibrate(long milliseconds, int amplitude)
        {
            TryVibrate(milliseconds, amplitude);
        }

        /// <summary>
        /// 현재 Android 기기에서 진동 기능을 사용할 수 있는지 확인합니다.
        /// </summary>
        /// <returns>진동 하드웨어를 사용할 수 있으면 <c>true</c>입니다.</returns>
        public static bool IsSupported()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            EnsureInitialized();
            return _isSupported;
#else
            return false;
#endif
        }

        /// <summary>
        /// 지정한 시간과 세기로 Android 진동을 실행합니다.
        /// </summary>
        /// <param name="milliseconds">진동 시간(ms)입니다.</param>
        /// <param name="amplitude">진동 세기(1~255)입니다.</param>
        /// <returns>Android Vibrator에 재생 요청을 전달했으면 <c>true</c>입니다.</returns>
        public static bool TryVibrate(long milliseconds, int amplitude)
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            if (milliseconds <= 0L || amplitude <= 0)
            {
                return false;
            }

            EnsureInitialized();
            if (!_isSupported || _vibrator == null)
            {
                return false;
            }

            try
            {
                int safeAmplitude = Mathf.Clamp(amplitude, 1, 255);
                if (_sdkInt >= VibrationEffectApiLevel)
                {
                    using AndroidJavaClass vibrationEffect =
                        new AndroidJavaClass("android.os.VibrationEffect");

                    int resolvedAmplitude =
                        _hasAmplitudeControl ? safeAmplitude : DefaultAmplitude;
                    using AndroidJavaObject effect =
                        vibrationEffect.CallStatic<AndroidJavaObject>(
                            "createOneShot",
                            milliseconds,
                            resolvedAmplitude);

                    _vibrator.Call("vibrate", effect);
                }
                else
                {
                    // API 26 미만에서는 세기 조절을 지원하지 않으므로 시간만 전달합니다.
                    _vibrator.Call("vibrate", milliseconds);
                }

                return true;
            }
            catch (Exception exception)
            {
                LogFailureOnce(exception);
                return false;
            }
#else
            return false;
#endif
        }

        /// <summary>
        /// 현재 재생 중인 Android 진동을 중단합니다.
        /// </summary>
        public static void Cancel()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            EnsureInitialized();
            if (!_isSupported || _vibrator == null)
            {
                return;
            }

            try
            {
                _vibrator.Call("cancel");
            }
            catch (Exception exception)
            {
                LogFailureOnce(exception);
            }
#endif
        }

        /// <summary>
        /// Android Java 객체와 기기 기능 정보를 최초 요청 시 한 번만 조회합니다.
        /// </summary>
        private static void EnsureInitialized()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            if (_isInitialized)
            {
                return;
            }

            _isInitialized = true;

            try
            {
                _sdkInt = GetSdkInt();

                using AndroidJavaClass unityPlayer =
                    new AndroidJavaClass("com.unity3d.player.UnityPlayer");
                using AndroidJavaObject activity =
                    unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
                using AndroidJavaObject context =
                    activity?.Call<AndroidJavaObject>("getApplicationContext");

                if (context == null)
                {
                    return;
                }

                if (_sdkInt >= VibratorManagerApiLevel)
                {
                    using AndroidJavaObject vibratorManager =
                        context.Call<AndroidJavaObject>(
                            "getSystemService",
                            "vibrator_manager");
                    _vibrator =
                        vibratorManager?.Call<AndroidJavaObject>("getDefaultVibrator");
                }
                else
                {
                    _vibrator =
                        context.Call<AndroidJavaObject>("getSystemService", "vibrator");
                }

                _isSupported =
                    _vibrator != null &&
                    _vibrator.Call<bool>("hasVibrator");
                _hasAmplitudeControl =
                    _isSupported &&
                    _sdkInt >= VibrationEffectApiLevel &&
                    _vibrator.Call<bool>("hasAmplitudeControl");
            }
            catch (Exception exception)
            {
                ReleaseCachedVibrator();
                LogFailureOnce(exception);
            }
#endif
        }

        /// <summary>
        /// 현재 Android SDK 버전을 반환합니다.
        /// </summary>
        /// <returns>Android SDK 버전이며, Android 런타임이 아니면 0입니다.</returns>
        private static int GetSdkInt()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            using AndroidJavaClass version =
                new AndroidJavaClass("android.os.Build$VERSION");

            return version.GetStatic<int>("SDK_INT");
#else
            return 0;
#endif
        }

        /// <summary>
        /// Unity Subsystem 재시작 시 캐시한 Android Java 객체를 해제합니다.
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetOnSubsystemRegistration()
        {
            ReleaseCachedVibrator();
            _isInitialized = false;
            _isSupported = false;
            _hasAmplitudeControl = false;
            _hasLoggedFailure = false;
            _sdkInt = 0;
        }

        /// <summary>
        /// 캐시한 Android Vibrator Java 객체를 안전하게 해제합니다.
        /// </summary>
        private static void ReleaseCachedVibrator()
        {
            _vibrator?.Dispose();
            _vibrator = null;
            _isSupported = false;
            _hasAmplitudeControl = false;
        }

        /// <summary>
        /// Android 진동 API 실패 로그를 세션 중 한 번만 출력합니다.
        /// </summary>
        /// <param name="exception">Android Java 호출 중 발생한 예외입니다.</param>
        private static void LogFailureOnce(Exception exception)
        {
            if (_hasLoggedFailure)
            {
                return;
            }

            _hasLoggedFailure = true;
            GcLogger.LogWarning(
                $"Android 진동 API 호출에 실패했습니다. message={exception.Message}");
        }
    }
}
