using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace GGemCo2DCore
{
    /// <summary>
    /// Unity 프로젝트 전반에서 사용하는 공통 로깅 유틸리티 클래스입니다.
    /// Unity Debug 로그를 래핑하고, 호출자 정보와 Unity 오브젝트 컨텍스트를 함께 기록할 수 있습니다.
    /// </summary>
    public static class GcLogger
    {
        private const int DefaultSkipFrames = 2;

        /// <summary>
        /// Unity 로그 타입별 스택 트레이스 출력 정책을 설정합니다.
        /// </summary>
        /// <param name="log">일반 로그의 스택 트레이스 출력 방식입니다.</param>
        /// <param name="warning">경고 로그의 스택 트레이스 출력 방식입니다.</param>
        /// <param name="error">에러 로그의 스택 트레이스 출력 방식입니다.</param>
        /// <param name="exception">예외 로그의 스택 트레이스 출력 방식입니다.</param>
        /// <param name="assert">Assert 로그의 스택 트레이스 출력 방식입니다.</param>
        public static void ConfigureStackTraceLogging(
            StackTraceLogType log = StackTraceLogType.None,
            StackTraceLogType warning = StackTraceLogType.ScriptOnly,
            StackTraceLogType error = StackTraceLogType.ScriptOnly,
            StackTraceLogType exception = StackTraceLogType.Full,
            StackTraceLogType assert = StackTraceLogType.ScriptOnly)
        {
            Application.SetStackTraceLogType(LogType.Log, log);
            Application.SetStackTraceLogType(LogType.Warning, warning);
            Application.SetStackTraceLogType(LogType.Error, error);
            Application.SetStackTraceLogType(LogType.Exception, exception);
            Application.SetStackTraceLogType(LogType.Assert, assert);
        }

        /// <summary>
        /// 일반 로그 메시지를 출력합니다.
        /// </summary>
        /// <param name="message">출력할 메시지입니다.</param>
        public static void Log(string message) => Debug.Log(message);

        /// <summary>
        /// Unity 오브젝트 컨텍스트를 포함하여 일반 로그 메시지를 출력합니다.
        /// </summary>
        /// <param name="context">로그와 연결할 Unity 오브젝트입니다.</param>
        /// <param name="message">출력할 메시지입니다.</param>
        public static void Log(UnityEngine.Object context, string message) => Debug.Log(message, context);

        /// <summary>
        /// 포맷 문자열을 적용한 일반 로그 메시지를 출력합니다.
        /// </summary>
        /// <param name="format">출력할 메시지 포맷 문자열입니다.</param>
        /// <param name="args">포맷 문자열에 삽입할 인자 목록입니다.</param>
        public static void LogFormat(string format, params object[] args)
        {
            if (string.IsNullOrEmpty(format)) return;
            Debug.Log(string.Format(format, args));
        }

        /// <summary>
        /// Unity 오브젝트 컨텍스트를 포함하여 포맷 문자열을 적용한 일반 로그 메시지를 출력합니다.
        /// </summary>
        /// <param name="context">로그와 연결할 Unity 오브젝트입니다.</param>
        /// <param name="format">출력할 메시지 포맷 문자열입니다.</param>
        /// <param name="args">포맷 문자열에 삽입할 인자 목록입니다.</param>
        public static void LogFormat(UnityEngine.Object context, string format, params object[] args)
        {
            if (string.IsNullOrEmpty(format)) return;
            Debug.Log(string.Format(format, args), context);
        }

        /// <summary>
        /// 경고 로그 메시지를 출력합니다.
        /// </summary>
        /// <param name="message">출력할 경고 메시지입니다.</param>
        public static void LogWarning(string message) => Debug.LogWarning(message);

        /// <summary>
        /// Unity 오브젝트 컨텍스트를 포함하여 경고 로그 메시지를 출력합니다.
        /// </summary>
        /// <param name="context">로그와 연결할 Unity 오브젝트입니다.</param>
        /// <param name="message">출력할 경고 메시지입니다.</param>
        public static void LogWarning(UnityEngine.Object context, string message) => Debug.LogWarning(message, context);

        /// <summary>
        /// 에러 로그 메시지를 출력합니다.
        /// </summary>
        /// <param name="message">출력할 에러 메시지입니다.</param>
        public static void LogError(string message) => Debug.LogError(message);

        /// <summary>
        /// Unity 오브젝트 컨텍스트를 포함하여 에러 로그 메시지를 출력합니다.
        /// </summary>
        /// <param name="context">로그와 연결할 Unity 오브젝트입니다.</param>
        /// <param name="message">출력할 에러 메시지입니다.</param>
        public static void LogError(UnityEngine.Object context, string message) => Debug.LogError(message, context);

        /// <summary>
        /// 예외 정보를 Unity 콘솔에 출력합니다.
        /// </summary>
        /// <param name="ex">출력할 예외 객체입니다.</param>
        /// <param name="context">예외 로그와 연결할 Unity 오브젝트입니다.</param>
        public static void LogException(Exception ex, UnityEngine.Object context = null)
        {
            if (context != null) Debug.LogException(ex, context);
            else Debug.LogException(ex);
        }

        /// <summary>
        /// 호출자 정보와 스택 트레이스를 포함한 로그 메시지를 출력합니다.
        /// </summary>
        /// <param name="message">출력할 기본 메시지입니다.</param>
        /// <param name="context">로그와 연결할 Unity 오브젝트입니다.</param>
        /// <param name="logType">출력할 Unity 로그 타입입니다.</param>
        /// <param name="skipFrames">스택 트레이스에서 제외할 프레임 수입니다.</param>
        /// <param name="includeFileInfo">파일명과 라인 번호를 로그에 포함할지 여부입니다.</param>
        /// <param name="memberName">컴파일러가 전달하는 호출자 멤버 이름입니다.</param>
        /// <param name="filePath">컴파일러가 전달하는 호출자 파일 경로입니다.</param>
        /// <param name="lineNumber">컴파일러가 전달하는 호출자 라인 번호입니다.</param>
        /// <example>
        /// <code>
        /// GcLogger.LogWithStackTrace("CharacterStop", context: this, logType: LogType.Error, includeFileInfo: true);
        /// </code>
        /// </example>
        public static void LogWithStackTrace(
            string message,
            UnityEngine.Object context = null,
            LogType logType = LogType.Log,
            int skipFrames = DefaultSkipFrames,
            bool includeFileInfo = false,
            [CallerMemberName] string memberName = "",
            [CallerFilePath] string filePath = "",
            [CallerLineNumber] int lineNumber = 0)
        {
            string fullMessage = BuildMessageWithStackTrace(
                message,
                skipFrames,
                includeFileInfo,
                memberName,
                filePath,
                lineNumber);

            LogByType(logType, fullMessage, context);
        }

        /// <summary>
        /// 로그 타입에 따라 적절한 Unity Debug API를 호출합니다.
        /// </summary>
        /// <param name="logType">출력할 Unity 로그 타입입니다.</param>
        /// <param name="message">출력할 로그 메시지입니다.</param>
        /// <param name="context">로그와 연결할 Unity 오브젝트입니다.</param>
        private static void LogByType(LogType logType, string message, UnityEngine.Object context)
        {
            switch (logType)
            {
                case LogType.Warning:
                    if (context != null) Debug.LogWarning(message, context);
                    else Debug.LogWarning(message);
                    break;

                case LogType.Error:
                case LogType.Assert:
                case LogType.Exception:
                    if (context != null) Debug.LogError(message, context);
                    else Debug.LogError(message);
                    break;

                default:
                    if (context != null) Debug.Log(message, context);
                    else Debug.Log(message);
                    break;
            }
        }

        /// <summary>
        /// 기본 메시지, 호출자 정보, 스택 트레이스를 결합한 로그 문자열을 생성합니다.
        /// </summary>
        /// <param name="message">출력할 기본 메시지입니다.</param>
        /// <param name="skipFrames">스택 트레이스에서 제외할 프레임 수입니다.</param>
        /// <param name="includeFileInfo">파일명과 라인 번호를 포함할지 여부입니다.</param>
        /// <param name="memberName">호출자 멤버 이름입니다.</param>
        /// <param name="filePath">호출자 파일 경로입니다.</param>
        /// <param name="lineNumber">호출자 라인 번호입니다.</param>
        /// <returns>호출자 정보와 스택 트레이스가 포함된 전체 로그 문자열입니다.</returns>
        private static string BuildMessageWithStackTrace(
            string message,
            int skipFrames,
            bool includeFileInfo,
            string memberName,
            string filePath,
            int lineNumber)
        {
            var sb = new StringBuilder(512);

            sb.Append(message);
            sb.AppendLine();
            sb.Append("[Caller] ").Append(memberName);

            if (includeFileInfo)
            {
                sb.Append(" (")
                    .Append(System.IO.Path.GetFileName(filePath))
                    .Append(':')
                    .Append(lineNumber)
                    .Append(')');
            }

            sb.AppendLine();
            sb.AppendLine("[StackTrace]");

            var stackTrace = new StackTrace(skipFrames, includeFileInfo);
            sb.Append(stackTrace);

            return sb.ToString();
        }

        /// <summary>
        /// Unity 오브젝트가 null이거나 Destroyed 상태인지 검사하고, 문제가 있으면 에러 로그를 출력합니다.
        /// </summary>
        /// <typeparam name="T">검사할 Unity 오브젝트 타입입니다.</typeparam>
        /// <param name="obj">검사할 Unity 오브젝트입니다.</param>
        /// <param name="paramName">로그에 표시할 파라미터 또는 필드 이름입니다.</param>
        /// <returns>null 또는 Destroyed 상태이면 <c>true</c>, 유효하면 <c>false</c>입니다.</returns>
        public static bool IsNullUnity<T>(T obj, string paramName) where T : UnityEngine.Object
        {
            if (obj) return false;
            LogError($"{paramName} is null or destroyed.");
            return true;
        }

        /// <summary>
        /// GameObject가 null인지 검사하고, null이면 에러 로그를 출력합니다.
        /// </summary>
        /// <param name="value">검사할 GameObject입니다.</param>
        /// <param name="errorLogMessage">null일 때 출력할 에러 메시지입니다.</param>
        /// <returns>null이면 <c>true</c>, 유효하면 <c>false</c>입니다.</returns>
        public static bool IsNullGameObject(GameObject value, string errorLogMessage)
        {
            if (value != null) return false;
            LogError(errorLogMessage);
            return true;
        }

        /// <summary>
        /// 참조 타입 값이 null인지 검사하고, null이면 에러 로그를 출력합니다.
        /// </summary>
        /// <typeparam name="T">검사할 참조 타입입니다.</typeparam>
        /// <param name="value">검사할 참조 타입 값입니다.</param>
        /// <param name="errorLogMessage">에러 로그에 사용할 메시지 접두어입니다.</param>
        /// <returns>null이면 <c>true</c>, 유효하면 <c>false</c>입니다.</returns>
        public static bool IsNull<T>(T value, string errorLogMessage) where T : class
        {
            if (value != null) return false;
            LogError($"{errorLogMessage} is null.");
            return true;
        }

        /// <summary>
        /// 컴포넌트가 소유한 Inspector 필드가 미할당 상태인지 검사하고, 미할당이면 에러 로그를 출력합니다.
        /// </summary>
        /// <typeparam name="T">검사할 Unity 오브젝트 타입입니다.</typeparam>
        /// <param name="owner">필드를 소유한 컴포넌트입니다.</param>
        /// <param name="field">검사할 Inspector 필드 값입니다.</param>
        /// <param name="fieldName">로그에 표시할 필드 이름입니다.</param>
        /// <returns>owner가 없거나 필드가 미할당이면 <c>true</c>, 유효하면 <c>false</c>입니다.</returns>
        public static bool IsUnassigned<T>(Component owner, T field, string fieldName)
            where T : UnityEngine.Object
        {
            if (owner == null)
            {
                LogError($"{nameof(IsUnassigned)}: owner is null.");
                return true;
            }

            if (field != null) return false;

            Debug.LogError(
                $"{owner.GetType().Name}: Inspector reference not assigned. Field='{fieldName}'",
                owner);

            return true;
        }

        /// <summary>
        /// 정수 값이 0인지 검사하고, 0이면 에러 로그를 출력합니다.
        /// </summary>
        /// <param name="value">검사할 정수 값입니다.</param>
        /// <param name="errorLogMessage">0일 때 출력할 에러 메시지입니다.</param>
        /// <returns>값이 0이면 <c>true</c>, 0이 아니면 <c>false</c>입니다.</returns>
        public static bool IsZero(int value, string errorLogMessage)
        {
            if (value != 0) return false;
            LogError(errorLogMessage);
            return true;
        }

        /// <summary>
        /// GameObject가 소유한 Inspector 필드가 미할당 상태인지 검사하고, 미할당이면 에러 로그를 출력합니다.
        /// </summary>
        /// <typeparam name="T">검사할 Unity 오브젝트 타입입니다.</typeparam>
        /// <param name="owner">필드를 소유한 GameObject입니다.</param>
        /// <param name="field">검사할 Inspector 필드 값입니다.</param>
        /// <param name="fieldName">로그에 표시할 필드 이름입니다.</param>
        /// <returns>owner가 없거나 필드가 미할당이면 <c>true</c>, 유효하면 <c>false</c>입니다.</returns>
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
        /// 여러 Inspector 필드 중 하나라도 미할당 상태인지 검사하고, 미할당 필드마다 에러 로그를 출력합니다.
        /// </summary>
        /// <param name="owner">필드들을 소유한 컴포넌트입니다.</param>
        /// <param name="fields">검사할 필드와 필드 이름의 목록입니다.</param>
        /// <returns>하나 이상의 필드가 미할당이면 <c>true</c>, 모두 유효하면 <c>false</c>입니다.</returns>
        public static bool HasAnyUnassigned(Component owner,
            params (UnityEngine.Object field, string fieldName)[] fields)
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

        /// <summary>
        /// 문자열이 null이거나 빈 문자열인지 검사하고, 문제가 있으면 에러 로그를 출력합니다.
        /// </summary>
        /// <param name="value">검사할 문자열 값입니다.</param>
        /// <param name="errorLogMessage">null 또는 빈 문자열일 때 출력할 에러 메시지입니다.</param>
        /// <returns>문자열이 null이거나 비어 있으면 <c>true</c>, 값이 있으면 <c>false</c>입니다.</returns>
        public static bool IsNullString(string value, string errorLogMessage)
        {
            if (!string.IsNullOrEmpty(value)) return false;
            LogError(errorLogMessage);
            return true;
        }
    }
}
