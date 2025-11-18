using System;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.Requests;
using UnityEngine;

namespace GGemCo2DCoreEditor
{
    public class SettingLocalization
    {
        private const string Title = "Loclization 설정 파일 추가하기";
        private const string PkgName = "com.unity.localization";
        private const string PendingKey = "GGemCo:LocalizationSetupPending";
        private const string AssetRoot = "Assets/Localization";
        private static AddRequest _addRequest;

        public void OnGUI()
        {
            HelperEditorUI.OnGUITitle(Title);

            if (GUILayout.Button(Title))
            {
                CreateLocalizationSetting();
            }
        }

        public void CreateLocalizationSetting(EditorSetupContext ctx = null)
        {
            // 패키지 클래스(예: UnityEngine.Localization.Locale)가 로드되어 있는지 Reflection으로 확인
            if (!HasLocalizationRuntime())
            {
                if (EditorPrefs.GetBool(PendingKey, false))
                {
                    Debug.Log("[GGemCo][Localization] 이미 설치 대기 중입니다. 잠시 후 Settings/Locale이 자동 생성됩니다.");
                    return;
                }

                Debug.Log("[GGemCo][Localization] 패키지가 없어 설치를 시작합니다: " + PkgName);
                EditorPrefs.SetBool(PendingKey, true);

                // 패키지 설치 (비동기)
                _addRequest = Client.Add(PkgName); // 최신 버전 설치
                EditorApplication.update += PollAddRequest;
                return;
            }

            // 패키지가 이미 있는 경우 즉시 생성 진행
            CreateSettingsAndLocalesIfNeeded();
        }
        private static void PollAddRequest()
        {
            if (_addRequest == null) return;
            if (!_addRequest.IsCompleted) return;

            EditorApplication.update -= PollAddRequest;

            if (_addRequest.Status == StatusCode.Success)
            {
                Debug.Log("[GGemCo][Localization] 패키지 설치 완료. 도메인 리로드 후 자동 생성이 진행됩니다.");
                // 설치 직후 도메인 리로드가 발생하며, InitializeOnLoadMethod 훅에서 생성 루틴이 실행됩니다.
            }
            else
            {
                EditorPrefs.DeleteKey(PendingKey);
                Debug.LogError($"[GGemCo][Localization] 패키지 설치 실패: {_addRequest.Error?.message}");
            }
        }

        // 에디터가 로드될 때(도메인 리로드 포함) 자동 후속 처리
        [InitializeOnLoadMethod]
        private static void OnDomainReload()
        {
            if (!EditorPrefs.GetBool(PendingKey, false)) return;
            if (!HasLocalizationRuntime()) return; // 아직도 타입이 없으면 다음 리로드까지 대기

            try
            {
                CreateSettingsAndLocalesIfNeeded();
                Debug.Log("[GGemCo][Localization] Localization Settings 및 en/ko Locale 자동 생성 완료.");
            }
            finally
            {
                EditorPrefs.DeleteKey(PendingKey);
            }
        }
        // Reflection: UnityEngine.Localization 및 UnityEditor.Localization 타입 존재 여부
        private static bool HasLocalizationRuntime()
        {
            return Type.GetType("UnityEngine.Localization.Locale, Unity.Localization") != null
                   && Type.GetType("UnityEngine.Localization.Settings.LocalizationSettings, Unity.Localization") != null;
        }

        private static bool HasLocalizationEditor()
        {
            return Type.GetType("UnityEditor.Localization.LocalizationEditorSettings, Unity.Localization.Editor") != null;
        }
        // Settings/Locales 생성 본체 (패키지 유무·리로드 타이밍과 무관하게 안전 호출)
        private static void CreateSettingsAndLocalesIfNeeded()
        {
            if (!HasLocalizationRuntime() || !HasLocalizationEditor())
                throw new InvalidOperationException("Localization 에디터/런타임 어셈블리가 아직 로드되지 않았습니다.");

            Directory.CreateDirectory(AssetRoot);

            // --- 타입 캐시
            var tLocale = Type.GetType("UnityEngine.Localization.Locale, Unity.Localization");
            var tLocaleId = Type.GetType("UnityEngine.Localization.LocaleIdentifier, Unity.Localization");
            var tLocSettings = Type.GetType("UnityEngine.Localization.Settings.LocalizationSettings, Unity.Localization");
            var tLocEditorSettings = Type.GetType("UnityEditor.Localization.LocalizationEditorSettings, Unity.Localization.Editor");

            // LocalizationSettings 자산이 존재하는지 확인
            ScriptableObject settings = GetActiveLocalizationSettings(tLocEditorSettings);
            if (settings == null)
            {
                settings = ScriptableObject.CreateInstance(tLocSettings);
                var settingsPath = $"{AssetRoot}/LocalizationSettings.asset";
                AssetDatabase.CreateAsset(settings, settingsPath);
                AssetDatabase.SaveAssets();

                // ActiveLocalizationSettings = settings;
                var propActive = tLocEditorSettings.GetProperty("ActiveLocalizationSettings",
                    BindingFlags.Public | BindingFlags.Static);
                propActive.SetValue(null, settings, null);

                Debug.Log($"[GGemCo][Localization] Settings 생성: {settings.name}");
            }

            // en & ko Locale 생성/등록
            CreateAndAddLocaleIfMissing(tLocale, tLocaleId, tLocEditorSettings, "en");
            CreateAndAddLocaleIfMissing(tLocale, tLocaleId, tLocEditorSettings, "ko");

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }
        private static ScriptableObject GetActiveLocalizationSettings(Type tLocEditorSettings)
        {
            var propActive = tLocEditorSettings.GetProperty("ActiveLocalizationSettings",
                BindingFlags.Public | BindingFlags.Static);
            return propActive?.GetValue(null, null) as ScriptableObject;
        }

        private static void CreateAndAddLocaleIfMissing(Type tLocale, Type tLocaleId, Type tLocEditorSettings, string code)
        {
            // 이미 등록되어 있는지 검사: GetLocales().Any(l.Identifier.Code == code)
            var miGetLocales = tLocEditorSettings.GetMethod("GetLocales",
                BindingFlags.Public | BindingFlags.Static);
            var locales = miGetLocales.Invoke(null, null) as System.Collections.IEnumerable;
            foreach (var l in locales)
            {
                var propIdentifier = tLocale.GetProperty("Identifier");
                var identifier = propIdentifier.GetValue(l, null);
                var propCode = tLocaleId.GetProperty("Code");
                var valCode = (string)propCode.GetValue(identifier, null);
                if (string.Equals(valCode, code, StringComparison.OrdinalIgnoreCase))
                {
                    // 이미 있음
                    return;
                }
            }

            // Locale.CreateLocale("en") 또는 SystemLanguage 기반 생성
            // 여기서는 코드("en","ko")로 생성
            var miCreateLocale = tLocale.GetMethod("CreateLocale",
                BindingFlags.Public | BindingFlags.Static, null, new[] { tLocaleId }, null);

            var ctorId = tLocaleId.GetConstructor(new[] { typeof(string) });
            var id = ctorId.Invoke(new object[] { code });
            var locale = miCreateLocale.Invoke(null, new[] { id }) as ScriptableObject;

            var path = $"{AssetRoot}/Locale_{code.ToUpper()}.asset";
            AssetDatabase.CreateAsset(locale, path);

            // LocalizationEditorSettings.AddLocale(locale, true);
            var miAddLocale = tLocEditorSettings.GetMethod("AddLocale",
                BindingFlags.Public | BindingFlags.Static, null, new[] { tLocale, typeof(bool) }, null);
            miAddLocale.Invoke(null, new object[] { locale, true });

            Debug.Log($"[GGemCo][Localization] Locale 생성/등록: {code} ({path})");
        }
    }
}