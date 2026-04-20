using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UnityEngine.Localization.Tables;
using UnityEngine.ResourceManagement.AsyncOperations;
using Object = UnityEngine.Object;

namespace GGemCo2DCore
{
    /// <summary>
    /// <para>Localization 매니저</para>
    /// </summary>
    public class LocalizationManagerBase : MonoBehaviour
    {
        // 언어 변경시 발생하는 이벤트
        public event Action<string, int> OnChangeLocale;
        // 언어 변경 중인지
        private bool _isChanging;
        // 현재 언어 코드
        private static string CurrentLanguageCode { get; set; }
        // string table
        protected LocalizedStringDatabase StringDatabase;
        // asset table
        private LocalizedAssetDatabase _assetDatabase;
        // 사용자 언어 테이블 존재 여부
        // - 파생 클래스(LocalizationManager)가 체크 결과를 채운다.
        protected readonly Dictionary<string, bool> UserTableExistsMap = new();
        // 현재 사용하는 언어 Locale
        private static readonly Dictionary<string, Locale> Locales = new Dictionary<string, Locale>();
        // 로드 진행율
        private float _loadProgress;

        protected virtual void Awake()
        {
            _loadProgress = 0f;
            StringDatabase = LocalizationSettings.StringDatabase;
            _assetDatabase = LocalizationSettings.AssetDatabase;

            InitializeAvailableLocale();
        }
        /// <summary>
        /// 현재 사용하고 있는 Locale 설정
        /// </summary>
        private void InitializeAvailableLocale()
        {
            var locales = LocalizationSettings.AvailableLocales.Locales;
            if (locales.Count == 0)
            {
                Debug.LogWarning("Localization Settings에 등록된 Locale이 없습니다.");
            }
            foreach (var locale in locales)
            {
                Locales.TryAdd(locale.Identifier.Code, locale);
            }
        }

        public Dictionary<string, Locale> GetAvailableLocales()
        {
            return Locales;
        }
        /// <summary>
        /// Locale을 받아 언어를 변경합니다.
        /// </summary>
        public void StartChangeLocale(Locale locale, bool isSave = true)
        {
            if (_isChanging) return;
            StartCoroutine(ChangeLocaleRoutine(locale, isSave));
        }
        /// <summary>
        /// Index를 받아 언어를 변경합니다.
        /// </summary>
        public void StartChangeLocale(int index, bool isSave = true)
        {
            Locale locale = GetLocaleByIndex(index);
            if (_isChanging || locale == null) return;
            StartCoroutine(ChangeLocaleRoutine(locale, isSave));
        }
        /// <summary>
        /// Code를 받아 언어를 변경합니다.
        /// </summary>
        public IEnumerator ChangeLocaleRoutine(string code, bool isSave = true)
        {
            Locale locale = GetLocaleByCode(code);
            yield return StartCoroutine(ChangeLocaleRoutine(locale, isSave));
        }
        /// <summary>
        /// 언어 바꾸기
        /// </summary>
        /// <param name="locale"></param>
        /// <param name="isSave"></param>
        /// <returns></returns>
        private IEnumerator ChangeLocaleRoutine(Locale locale, bool isSave = true)
        {
            _isChanging = true;

            yield return LocalizationSettings.InitializationOperation;

            LocalizationSettings.SelectedLocale = locale;
            CurrentLanguageCode = locale.Identifier.Code;
            _isChanging = false;
            // GcLogger.Log($"[LocalizationManager] change success. locale index: {index}");
            if (isSave)
            {
                PlayerPrefsManager.SaveLocalizationLocaleCode(locale.Identifier.Code);
            }

            OnChangeLocale?.Invoke(CurrentLanguageCode, GetLocaleIndexByCode(CurrentLanguageCode));
            
            yield return StartCoroutine(CheckUserTablesExist());
        }

        protected virtual IEnumerator CheckUserTablesExist()
        {
            yield return null;
        }

        /// <summary>
        /// 지정한 테이블과 키로 로컬라이즈된 문자열을 가져옵니다.
        /// </summary>
        protected string GetString(string table, string key)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                GcLogger.LogWarning($"key is null or whitespace: {key}");
                return "";
            }

            // 유저 테이블 존재 시 우선 조회
            if (UserTableExistsMap.TryGetValue(table, out bool hasUserTable) && hasUserTable)
            {
                string userTable = $"{table}_User";
                var userLocalized = StringDatabase.GetTableEntry(userTable, key, LocalizationSettings.SelectedLocale);
                if (userLocalized.Entry != null)
                {
                    return userLocalized.Entry.Value;
                }
            }

            // 유저 테이블에 없으면 기존 테이블 조회
            var tableEntryResult = StringDatabase.GetTableEntry(table, key, LocalizationSettings.SelectedLocale);
            if (tableEntryResult.Entry != null)
                return tableEntryResult.Entry.Value;

            GcLogger.LogWarning($"MISSING:{key} / table:{table}");
            return "";
        }

        /// <summary>
        /// Smart String 또는 일반 String을 평가하여 반환합니다.
        /// - UserTable(사용자 커스텀 테이블)이 있으면 우선 적용합니다.
        /// - 없으면 기본 테이블에서 평가합니다.
        /// </summary>
        public string GetSmartString(string tableName, string key, params object[] arguments)
        {
            return GetSmartString(tableName, key, LocalizationSettings.SelectedLocale, arguments);
        }
        /// <summary>
        /// 지정 Locale로 Smart String 또는 일반 String을 평가하여 반환합니다.
        /// </summary>
        public string GetSmartString(string tableName, string key, Locale locale, params object[] arguments)
        {
            if (string.IsNullOrEmpty(tableName) || string.IsNullOrEmpty(key))
                return string.Empty;

            // Localization 초기화가 끝나기 전에 호출될 수 있다면 방어
            if (LocalizationSettings.InitializationOperation.IsValid() &&
                !LocalizationSettings.InitializationOperation.IsDone)
            {
                // 필요 시 WaitForCompletion 가능(단, WebGL은 제한) :contentReference[oaicite:2]{index=2}
                LocalizationSettings.InitializationOperation.WaitForCompletion();
            }

            // 1) UserTable 우선
            if (TryGetUserTableExists(tableName))
            {
                var userTableName = GetUserTableName(tableName);
                var userValue = GetLocalizedStringByTableAndKey(userTableName, key, locale, arguments);
                if (!string.IsNullOrEmpty(userValue))
                    return userValue;
            }

            // 2) 기본 테이블
            var baseValue = GetLocalizedStringByTableAndKey(tableName, key, locale, arguments);
            return baseValue ?? string.Empty;
        }
        /// <summary>
        /// UserTable 이름 규칙(프로젝트 규칙에 맞게 유지)
        /// </summary>
        protected virtual string GetUserTableName(string baseTableName)
        {
            // 예: "GGemCo_UIWindowSkillInfo" -> "GGemCo_UIWindowSkillInfo_User"
            // 프로젝트에서 쓰는 규칙이 있다면 그대로 사용하세요.
            return $"{baseTableName}_User";
        }

        /// <summary>
        /// UserTable 존재여부를 캐시에서 확인합니다. (캐시 미존재 시 false 반환)
        /// - 프로젝트에 이미 "테이블 파일 존재 여부"를 갱신하는 단계가 있다면
        ///   그 단계에서 _userTableExistsMap을 채우는 방식을 유지하는 것이 가장 안전합니다.
        /// </summary>
        protected bool TryGetUserTableExists(string userTableName)
        {
            if (string.IsNullOrEmpty(userTableName))
                return false;

            return UserTableExistsMap.TryGetValue(userTableName, out var exists) && exists;
        }
        /// <summary>
        /// Unity Localization 정식 오버로드로 문자열을 가져오고(Smart 포함) 즉시 반환합니다.
        /// </summary>
        private static string GetLocalizedStringByTableAndKey(
            string tableName,
            string key,
            Locale locale,
            params object[] arguments)
        {
            try
            {
                // Ability 템플릿(키: uid)을 Smart String으로 평가하고, 인자(Trigger/Target/Value...)를 주입합니다.
                var smartString = new LocalizedString(tableName, key)
                {
                    Arguments = arguments,
                    LocaleOverride = locale
                };
                // 정식 시그니처:
                // GetLocalizedStringAsync(TableReference, TableEntryReference, Locale, FallbackBehavior, params object[]) :contentReference[oaicite:3]{index=3}
                AsyncOperationHandle<string> handle = smartString.GetLocalizedStringAsync();

                if (!handle.IsDone)
                    handle.WaitForCompletion(); // WebGL에서는 제한 가능 :contentReference[oaicite:4]{index=4}

                return handle.Result;
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[Localization] GetLocalizedString failed. table={tableName}, key={key}\n{e}");
                return string.Empty;
            }
        }
        private static string GetLocalizedStringByTableAndKeySync(
            string tableName,
            string key,
            Locale locale,
            params object[] arguments)
        {
            var smartString = new LocalizedString(tableName, key)
            {
                Arguments = arguments,
                LocaleOverride = locale
            };
            // GetLocalizedString(...)은 내부적으로 WaitForCompletion을 사용합니다. :contentReference[oaicite:6]{index=6}
            return smartString.GetLocalizedString();
        }

        /// <summary>
        /// 현재 언어 코드 (예: "En", "Ko") 반환
        /// </summary>
        public string GetCurrentLanguageCode() => CurrentLanguageCode;
        /// <summary>
        /// Code로 Locale 찾기
        /// </summary>
        /// <param name="code"></param>
        /// <returns></returns>
        public int GetLocaleIndexByCode(string code)
        {
            Locale locale = GetLocaleByCode(code);
            if (locale == null) return -1;
            return GetIndexOfLocale(locale);
        }
        /// <summary>
        /// Locale로 Index 찾기
        /// </summary>
        /// <param name="locale"></param>
        /// <returns></returns>
        private int GetIndexOfLocale(Locale locale)
        {
            if (Locales == null || locale == null) return -1;
            // 코드 기준 매칭(ko, ko-KR 등)
            var code = locale.Identifier.Code;
            int index = 0;
            foreach (var data in Locales)
            {
                if (data.Key == code) return index;
                index++;
            }
            return -1;
        }
        /// <summary>
        /// Code로 Locale 찾기
        /// </summary>
        /// <param name="code"></param>
        /// <returns></returns>
        public Locale GetLocaleByCode(string code)
        {
            if (string.IsNullOrEmpty(code) || Locales == null) return null;
            // 완전일치 우선, 없으면 접두 일치(예: "ko"로 저장되어 있고 프로젝트에는 "ko-KR"만 있는 경우)
            var exact = Locales.FirstOrDefault(l => l.Key == code);
            if (exact.Value != null) return exact.Value;

            return Locales.FirstOrDefault().Value;
        }
        /// <summary>
        /// Index로 Locale 찾기
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        private Locale GetLocaleByIndex(int index)
        {
            int i = 0;
            foreach (var data in Locales)
            {
                if (i == index) return data.Value;
                i++;
            }

            return null;
        }

        protected bool HasLocalizationKey(string tableName, string key)
        {
            if (string.IsNullOrEmpty(tableName) ||
                string.IsNullOrEmpty(key))
                return false;

            // 이미 로드된 테이블만 사용 (동기)
            StringTable table =
                LocalizationSettings.StringDatabase.GetTable(tableName);

            if (table == null)
                return false;

            return table.GetEntry(key) != null;
        }
    }
}
