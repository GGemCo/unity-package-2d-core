using System;
using System.IO;
using GGemCo2DCore;
using UnityEditor;

namespace GGemCo2DCoreEditor
{
    /// <summary>
    /// 작업자별 Settings 프로파일 선택값을 EditorPrefs에 저장하고 로컬 에셋 경로를 계산합니다.
    /// </summary>
    public static class SettingsProfileEditorPrefs
    {
        private const string ProfileKindKey = ConfigDefine.NameSDK + ".SettingsProfile.Kind";
        private const string WorkerNameKey = ConfigDefine.NameSDK + ".SettingsProfile.WorkerName";
        private const string LocalSettingsRoot = "Assets/" + ConfigDefine.NameSDK + "Local/Settings";

        /// <summary>
        /// 현재 에디터에서 선택된 Settings 프로파일을 가져오거나 저장합니다.
        /// </summary>
        public static SettingsProfileKind CurrentProfile
        {
            get => (SettingsProfileKind)EditorPrefs.GetInt(ProfileKindKey, (int)SettingsProfileKind.Service);
            set => EditorPrefs.SetInt(ProfileKindKey, (int)value);
        }

        /// <summary>
        /// 개발용 Settings를 저장할 작업자 이름을 가져오거나 저장합니다.
        /// </summary>
        public static string WorkerName
        {
            get
            {
                string value = EditorPrefs.GetString(WorkerNameKey, string.Empty);
                if (!string.IsNullOrWhiteSpace(value))
                    return SanitizePathSegment(value);

                string fallback = Environment.UserName;
                if (string.IsNullOrWhiteSpace(fallback))
                    fallback = "Local";

                fallback = SanitizePathSegment(fallback);
                EditorPrefs.SetString(WorkerNameKey, fallback);
                return fallback;
            }
            set => EditorPrefs.SetString(WorkerNameKey, SanitizePathSegment(value));
        }

        /// <summary>
        /// 현재 작업자용 개발 Settings 루트 경로를 반환합니다.
        /// </summary>
        /// <returns>Assets로 시작하는 Unity 프로젝트 상대 경로입니다.</returns>
        public static string GetCurrentWorkerRoot()
        {
            return $"{LocalSettingsRoot}/{WorkerName}";
        }

        /// <summary>
        /// Addressables Key에 대응하는 개발용 Settings 에셋 경로를 반환합니다.
        /// </summary>
        /// <param name="addressableKey">서비스용 Settings Addressables Key입니다.</param>
        /// <returns>개발용 Settings 에셋 경로입니다.</returns>
        public static string GetDevelopmentAssetPath(string addressableKey)
        {
            return $"{GetCurrentWorkerRoot()}/{SanitizeFileName(addressableKey)}.Development.asset";
        }

        /// <summary>
        /// 현재 작업자용 개발 Settings 폴더를 생성합니다.
        /// </summary>
        public static void EnsureCurrentWorkerDirectory()
        {
            string root = GetCurrentWorkerRoot();
            if (AssetDatabase.IsValidFolder(root))
                return;

            Directory.CreateDirectory(root);
            AssetDatabase.Refresh();
        }

        /// <summary>
        /// Unity 에셋 경로에 사용할 수 있도록 폴더명 문자열을 정리합니다.
        /// </summary>
        /// <param name="value">원본 문자열입니다.</param>
        /// <returns>경로 구분자와 공백이 제거된 문자열입니다.</returns>
        private static string SanitizePathSegment(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return "Local";

            string sanitized = value.Trim();
            foreach (char c in Path.GetInvalidFileNameChars())
            {
                sanitized = sanitized.Replace(c.ToString(), "_");
            }

            sanitized = sanitized.Replace('/', '_').Replace('\\', '_');
            return string.IsNullOrWhiteSpace(sanitized) ? "Local" : sanitized;
        }

        /// <summary>
        /// Addressables Key를 개발용 Settings 파일명으로 사용할 수 있도록 정리합니다.
        /// </summary>
        /// <param name="value">Addressables Key입니다.</param>
        /// <returns>파일명으로 안전한 문자열입니다.</returns>
        private static string SanitizeFileName(string value)
        {
            return SanitizePathSegment(value);
        }
    }
}
