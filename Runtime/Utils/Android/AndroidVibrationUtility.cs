using UnityEngine;

namespace GGemCo2DCore
{
    public static class AndroidVibrationUtility
    {
        /// <summary>
        /// 안드로이드 진동을 실행합니다.
        /// </summary>
        /// <param name="milliseconds">진동 시간(ms)</param>
        /// <param name="amplitude">진동 세기(1~255)</param>
        public static void Vibrate(long milliseconds, int amplitude)
        {
#if UNITY_ANDROID && !UNITY_EDITOR
        amplitude = Mathf.Clamp(amplitude, 1, 255);

        using AndroidJavaClass unityPlayer =
            new AndroidJavaClass("com.unity3d.player.UnityPlayer");

        using AndroidJavaObject activity =
            unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");

        using AndroidJavaObject context =
            activity.Call<AndroidJavaObject>("getApplicationContext");

        AndroidJavaObject vibrator;

        if (GetSdkInt() >= 31)
        {
            using AndroidJavaObject vibratorManager =
                context.Call<AndroidJavaObject>("getSystemService", "vibrator_manager");

            vibrator = vibratorManager.Call<AndroidJavaObject>("getDefaultVibrator");
        }
        else
        {
            vibrator = context.Call<AndroidJavaObject>("getSystemService", "vibrator");
        }

        if (vibrator == null)
            return;

        bool hasVibrator = vibrator.Call<bool>("hasVibrator");

        if (!hasVibrator)
            return;

        if (GetSdkInt() >= 26)
        {
            using AndroidJavaClass vibrationEffect =
                new AndroidJavaClass("android.os.VibrationEffect");

            using AndroidJavaObject effect =
                vibrationEffect.CallStatic<AndroidJavaObject>(
                    "createOneShot",
                    milliseconds,
                    amplitude);

            vibrator.Call("vibrate", effect);
        }
        else
        {
            vibrator.Call("vibrate", milliseconds);
        }

        vibrator.Dispose();
#endif
        }

        /// <summary>
        /// 현재 SDK 버전을 반환합니다.
        /// </summary>
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
    }
}