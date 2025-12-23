using System;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace GGemCo2DCore
{
    /// <summary>
    /// 공통 로깅 유틸리티.
    /// - 포맷 문자열은 필요한 경우에만 string.Format 수행(불필요한 GC 최소화)
    /// - UnityEngine.Object 컨텍스트 로그 지원
    /// </summary>
    public static class GcLogger
    {
        public static void Log(string message) => Debug.Log(message);

        public static void Log(UnityEngine.Object context, string message) => Debug.Log(message, context);

        public static void LogFormat(string format, params object[] args)
        {
            if (string.IsNullOrEmpty(format)) return;
            Debug.Log(string.Format(format, args));
        }

        public static void LogFormat(UnityEngine.Object context, string format, params object[] args)
        {
            if (string.IsNullOrEmpty(format)) return;
            Debug.Log(string.Format(format, args), context);
        }

        public static void LogWarning(string message) => Debug.LogWarning(message);

        public static void LogWarning(UnityEngine.Object context, string message) => Debug.LogWarning(message, context);

        public static void LogError(string message) => Debug.LogError(message);

        public static void LogError(UnityEngine.Object context, string message) => Debug.LogError(message, context);

        public static void LogException(Exception ex, UnityEngine.Object context = null)
        {
            if (context != null) Debug.LogException(ex, context);
            else Debug.LogException(ex);
        }

        /// <summary>
        /// Unity Object가 null(혹은 Destroyed) 인지 검사합니다.
        /// </summary>
        public static bool IsNullUnity<T>(T obj, string paramName) where T : UnityEngine.Object
        {
            if (obj) return false;
            LogError($"{paramName} is null or destroyed.");
            return true;
        }

        public static bool IsNullGameObject(GameObject value, string errorLogMessage)
        {
            if (value != null) return false;
            LogError(errorLogMessage);
            return true;
        }

        public static bool IsNull<T>(T value, string errorLogMessage) where T : class
        {
            if (value != null) return false;
            LogError(errorLogMessage);
            return true;
        }
    }
}
