using System;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace GGemCo2DCore
{
    /// <summary>
    /// Unity 프로젝트에서 공통으로 사용하는 로깅 유틸리티입니다.
    /// 
    /// - 기본 로그/경고/에러/예외 로깅을 래핑하여 호출 지점을 통일합니다.
    /// - 필요 시에만 <see cref="string.Format(string,object[])"/>을 수행하여 불필요한 GC를 줄입니다.
    /// - <see cref="UnityEngine.Object"/> 컨텍스트 로그를 지원하여,
    ///   콘솔 클릭 시 해당 오브젝트를 선택/추적할 수 있습니다.
    /// </summary>
    public static class GcLogger
    {
        /// <summary>
        /// 일반 로그를 출력합니다.
        /// </summary>
        /// <param name="message">출력할 메시지입니다.</param>
        public static void Log(string message) => Debug.Log(message);

        /// <summary>
        /// 컨텍스트(Unity 오브젝트)를 포함하여 일반 로그를 출력합니다.
        /// </summary>
        /// <param name="context">로그 컨텍스트로 사용할 Unity 오브젝트입니다.</param>
        /// <param name="message">출력할 메시지입니다.</param>
        public static void Log(UnityEngine.Object context, string message) => Debug.Log(message, context);

        /// <summary>
        /// 포맷 문자열로 일반 로그를 출력합니다.
        /// </summary>
        /// <param name="format">포맷 문자열입니다.</param>
        /// <param name="args">포맷 인자입니다.</param>
        public static void LogFormat(string format, params object[] args)
        {
            if (string.IsNullOrEmpty(format)) return;
            Debug.Log(string.Format(format, args));
        }

        /// <summary>
        /// 컨텍스트(Unity 오브젝트)를 포함하여 포맷 문자열 로그를 출력합니다.
        /// </summary>
        /// <param name="context">로그 컨텍스트로 사용할 Unity 오브젝트입니다.</param>
        /// <param name="format">포맷 문자열입니다.</param>
        /// <param name="args">포맷 인자입니다.</param>
        public static void LogFormat(UnityEngine.Object context, string format, params object[] args)
        {
            if (string.IsNullOrEmpty(format)) return;
            Debug.Log(string.Format(format, args), context);
        }

        /// <summary>
        /// 경고 로그를 출력합니다.
        /// </summary>
        /// <param name="message">출력할 메시지입니다.</param>
        public static void LogWarning(string message) => Debug.LogWarning(message);

        /// <summary>
        /// 컨텍스트(Unity 오브젝트)를 포함하여 경고 로그를 출력합니다.
        /// </summary>
        /// <param name="context">로그 컨텍스트로 사용할 Unity 오브젝트입니다.</param>
        /// <param name="message">출력할 메시지입니다.</param>
        public static void LogWarning(UnityEngine.Object context, string message) => Debug.LogWarning(message, context);

        /// <summary>
        /// 에러 로그를 출력합니다.
        /// </summary>
        /// <param name="message">출력할 메시지입니다.</param>
        public static void LogError(string message) => Debug.LogError(message);

        /// <summary>
        /// 컨텍스트(Unity 오브젝트)를 포함하여 에러 로그를 출력합니다.
        /// </summary>
        /// <param name="context">로그 컨텍스트로 사용할 Unity 오브젝트입니다.</param>
        /// <param name="message">출력할 메시지입니다.</param>
        public static void LogError(UnityEngine.Object context, string message) => Debug.LogError(message, context);

        /// <summary>
        /// 예외 로그를 출력합니다.
        /// </summary>
        /// <param name="ex">기록할 예외입니다.</param>
        /// <param name="context">로그 컨텍스트로 사용할 Unity 오브젝트입니다. null 가능.</param>
        public static void LogException(Exception ex, UnityEngine.Object context = null)
        {
            if (context != null) Debug.LogException(ex, context);
            else Debug.LogException(ex);
        }

        /// <summary>
        /// Unity Object가 null 또는 Destroyed 상태인지 검사합니다.
        /// (UnityEngine.Object는 Destroyed 상태도 null처럼 평가됩니다.)
        /// </summary>
        /// <typeparam name="T">검사할 UnityEngine.Object 타입입니다.</typeparam>
        /// <param name="obj">검사할 Unity 오브젝트입니다.</param>
        /// <param name="paramName">로그에 포함할 파라미터/필드 이름입니다.</param>
        /// <returns>null 또는 Destroyed 이면 true, 아니면 false를 반환합니다.</returns>
        public static bool IsNullUnity<T>(T obj, string paramName) where T : UnityEngine.Object
        {
            if (obj) return false;
            LogError($"{paramName} is null or destroyed.");
            return true;
        }

        /// <summary>
        /// <see cref="GameObject"/>가 null인지 검사하고, null이면 지정한 메시지로 에러 로그를 남깁니다.
        /// </summary>
        /// <param name="value">검사할 GameObject입니다.</param>
        /// <param name="errorLogMessage">null일 때 출력할 에러 메시지입니다.</param>
        /// <returns>null이면 true, 아니면 false를 반환합니다.</returns>
        public static bool IsNullGameObject(GameObject value, string errorLogMessage)
        {
            if (value != null) return false;
            LogError(errorLogMessage);
            return true;
        }

        /// <summary>
        /// 참조 타입 값이 null인지 검사하고, null이면 지정한 메시지로 에러 로그를 남깁니다.
        /// </summary>
        /// <typeparam name="T">검사할 참조 타입입니다.</typeparam>
        /// <param name="value">검사할 값입니다.</param>
        /// <param name="errorLogMessage">null일 때 출력할 에러 메시지(접두어)입니다.</param>
        /// <returns>null이면 true, 아니면 false를 반환합니다.</returns>
        public static bool IsNull<T>(T value, string errorLogMessage) where T : class
        {
            if (value != null) return false;
            LogError($"{errorLogMessage} is null.");
            return true;
        }

        /// <summary>
        /// Inspector에 반드시 할당되어야 하는 Unity Object 필드가 비어있는지 검사합니다.
        /// 컨텍스트를 포함해 로그를 남기므로 콘솔 클릭 시 해당 컴포넌트가 선택됩니다.
        /// </summary>
        /// <typeparam name="T">검사할 UnityEngine.Object 타입입니다.</typeparam>
        /// <param name="owner">필드를 소유한 컴포넌트(로그 컨텍스트)입니다.</param>
        /// <param name="field">Inspector에 할당되어야 하는 필드입니다.</param>
        /// <param name="fieldName">필드 이름(권장: nameof(필드))입니다.</param>
        /// <returns>미할당(null/Destroyed)인 경우 true, 정상 할당이면 false를 반환합니다.</returns>
        public static bool IsUnassigned<T>(Component owner, T field, string fieldName)
            where T : UnityEngine.Object
        {
            if (owner == null)
            {
                LogError($"{nameof(IsUnassigned)}: owner is null.");
                return true;
            }

            // Unity Object는 Destroyed 상태도 (field == null)로 판정됩니다.
            if (field != null) return false;

            Debug.LogError(
                $"{owner.GetType().Name}: Inspector reference not assigned. Field='{fieldName}'",
                owner);

            return true;
        }

        /// <summary>
        /// <see cref="GameObject"/>를 로그 컨텍스트로 사용하는 Inspector 미할당 검사 버전입니다.
        /// (컴포넌트가 아닌 GameObject 기준으로 선택되게 하고 싶을 때 사용합니다.)
        /// </summary>
        /// <typeparam name="T">검사할 UnityEngine.Object 타입입니다.</typeparam>
        /// <param name="owner">필드를 소유한 GameObject(로그 컨텍스트)입니다.</param>
        /// <param name="field">Inspector에 할당되어야 하는 필드입니다.</param>
        /// <param name="fieldName">필드 이름(권장: nameof(필드))입니다.</param>
        /// <returns>미할당(null/Destroyed)인 경우 true, 정상 할당이면 false를 반환합니다.</returns>
        public static bool IsUnassigned<T>(GameObject owner, T field, string fieldName)
            where T : UnityEngine.Object
        {
            if (owner == null)
            {
                LogError($"{nameof(IsUnassigned)}: owner is null.");
                return true;
            }

            if (field != null) return false;

            Debug.LogError(
                $"Inspector reference not assigned. Owner='{owner.name}', Field='{fieldName}'",
                owner);

            return true;
        }

        /// <summary>
        /// 여러 Inspector 필드를 한 번에 검사하고, 하나라도 미할당이면 true를 반환합니다.
        /// </summary>
        /// <param name="owner">필드들을 소유한 컴포넌트(로그 컨텍스트)입니다.</param>
        /// <param name="fields">검사할 필드와 필드 이름 목록입니다.</param>
        /// <returns>하나라도 미할당이면 true, 모두 정상 할당이면 false를 반환합니다.</returns>
        public static bool HasAnyUnassigned(Component owner, params (UnityEngine.Object field, string fieldName)[] fields)
        {
            if (owner == null)
            {
                LogError($"{nameof(HasAnyUnassigned)}: owner is null.");
                return true;
            }

            bool hasAny = false;

            foreach (var (field, fieldName) in fields)
            {
                if (field != null) continue;

                Debug.LogError(
                    $"{owner.GetType().Name}: Inspector reference not assigned. Field='{fieldName}'",
                    owner);

                hasAny = true;
            }

            return hasAny;
        }
    }
}
