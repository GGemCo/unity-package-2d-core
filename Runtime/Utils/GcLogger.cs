using UnityEngine;

namespace GGemCo.Scripts
{
    public static class GcLogger
    {
        public static void Log(string message)
        {
            Debug.Log(message);
        }
        public static void Log(string message, params object[] parameters)
        {
            message = string.Format(message, parameters);
            Debug.Log(message);
        }
        public static void Log(object message)
        {
            Debug.Log(message);
        }
        public static void LogWarning(string message)
        {
            Debug.LogWarning(message);
        }
        public static void LogWarning(string message, params object[] parameters)
        {
            message = string.Format(message, parameters);
            Debug.LogWarning(message);
        }
        public static void LogWarning(object message)
        {
            Debug.LogWarning(message);
        }
        public static void LogError(string message)
        {
            Debug.LogError(message);
        }
        public static void LogError(string message, params object[] parameters)
        {
            message = string.Format(message, parameters);
            Debug.LogError(message);
        }
        public static void LogError(object message)
        {
            Debug.LogError(message);
        }
        public static void LogFormat(string format, params object[] args)
        {
            Debug.LogFormat(format, args);
        }
        
        /// <summary>
        /// value 값이 0 인지 체크한 후 에러 메세지 보여주기 
        /// </summary>
        /// <param name="value"></param>
        /// <param name="errorLogMessage"></param>
        /// <returns></returns>
        public static bool IsNullInt(int value, string errorLogMessage)
        {
            if (value <= 0)
            {
                LogError(errorLogMessage);
                return true;
            }
            return false;
        }
        /// <summary>
        /// value 값이 null 인지 체크한 후 에러메시지 보여주기 
        /// </summary>
        /// <param name="value"></param>
        /// <param name="errorLogMessage"></param>
        /// <returns></returns>
        public static bool IsNullGameObject(GameObject value, string errorLogMessage)
        {
            if (value == null)
            {
                LogError(errorLogMessage);
                return true;
            }
            return false;
        }
    }
}
