using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UnityEngine.Localization.Tables;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace GGemCo2DCore
{
    /// <summary>
    /// Unity Localization 시스템을 래핑하여 Locale 변경, 문자열 조회, Smart String 평가를 제공하는 기본 매니저입니다.
    /// 사용자 정의 테이블이 존재하는 경우 기본 테이블보다 우선 조회합니다.
    /// </summary>
    public class LocalizationManagerBase : MonoBehaviour
    {
        /// <summary>
        /// 현재 언어가 변경된 뒤 언어 코드와 Locale 인덱스를 전달하는 이벤트입니다.
        /// </summary>
        public event Action<string, int> OnChangeLocale;

        private bool _isChanging;

        private static string CurrentLanguageCode { get; set; }

        protected LocalizedStringDatabase StringDatabase;

        /// <summary>
        /// 기본 테이블명별 사용자 정의 테이블 존재 여부를 저장하는 캐시입니다.
        /// </summary>
        protected readonly Dictionary<string, bool> UserTableExistsMap = new();

        private static readonly Dictionary<string, Locale> Locales = new Dictionary<string, Locale>();

        /// <summary>
        /// Unity Localization 데이터베이스를 캐시하고 사용 가능한 Locale 목록을 초기화합니다.
        /// </summary>
        protected virtual void Awake()
        {
            StringDatabase = LocalizationSettings.StringDatabase;

            InitializeAvailableLocale();
        }

        /// <summary>
        /// Localization Settings에 등록된 Locale 목록을 내부 캐시에 등록합니다.
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

        /// <summary>
        /// 현재 사용할 수 있는 Locale 목록을 언어 코드 기준으로 반환합니다.
        /// </summary>
        /// <returns>언어 코드를 키로 사용하는 Locale 사전입니다.</returns>
        public Dictionary<string, Locale> GetAvailableLocales()
        {
            return Locales;
        }

        /// <summary>
        /// 지정한 Locale로 언어 변경 코루틴을 시작합니다.
        /// </summary>
        /// <param name="locale">변경할 대상 Locale입니다.</param>
        /// <param name="isSave">변경한 언어 코드를 저장하려면 <c>true</c>입니다.</param>
        public void StartChangeLocale(Locale locale, bool isSave = true)
        {
            if (_isChanging) return;
            StartCoroutine(ChangeLocaleRoutine(locale, isSave));
        }

        /// <summary>
        /// Locale 인덱스를 기준으로 언어 변경 코루틴을 시작합니다.
        /// </summary>
        /// <param name="index">사용 가능한 Locale 목록에서 변경할 Locale의 인덱스입니다.</param>
        /// <param name="isSave">변경한 언어 코드를 저장하려면 <c>true</c>입니다.</param>
        public void StartChangeLocale(int index, bool isSave = true)
        {
            Locale locale = GetLocaleByIndex(index);
            if (_isChanging || locale == null) return;
            StartCoroutine(ChangeLocaleRoutine(locale, isSave));
        }

        /// <summary>
        /// 언어 코드를 기준으로 Locale을 찾아 언어 변경 코루틴을 실행합니다.
        /// </summary>
        /// <param name="code">변경할 Locale의 언어 코드입니다.</param>
        /// <param name="isSave">변경한 언어 코드를 저장하려면 <c>true</c>입니다.</param>
        /// <returns>Locale 변경 처리를 수행하는 코루틴입니다.</returns>
        public IEnumerator ChangeLocaleRoutine(string code, bool isSave = true)
        {
            Locale locale = GetLocaleByCode(code);
            yield return StartCoroutine(ChangeLocaleRoutine(locale, isSave));
        }

        /// <summary>
        /// Localization 초기화 완료 후 선택 Locale을 변경하고, 저장 및 변경 이벤트 처리를 수행합니다.
        /// </summary>
        /// <param name="locale">변경할 대상 Locale입니다.</param>
        /// <param name="isSave">변경한 언어 코드를 저장하려면 <c>true</c>입니다.</param>
        /// <returns>Locale 변경과 사용자 테이블 확인을 순차 실행하는 코루틴입니다.</returns>
        private IEnumerator ChangeLocaleRoutine(Locale locale, bool isSave = true)
        {
            _isChanging = true;

            yield return LocalizationSettings.InitializationOperation;

            LocalizationSettings.SelectedLocale = locale;
            CurrentLanguageCode = locale.Identifier.Code;
            _isChanging = false;

            if (isSave)
            {
                PlayerPrefsManager.SaveLocalizationLocaleCode(locale.Identifier.Code);
            }

            OnChangeLocale?.Invoke(CurrentLanguageCode, GetLocaleIndexByCode(CurrentLanguageCode));

            yield return StartCoroutine(CheckUserTablesExist());
        }

        /// <summary>
        /// 사용자 정의 로컬라이즈 테이블 존재 여부를 확인합니다.
        /// 파생 클래스에서 프로젝트별 테이블 확인 로직을 구현합니다.
        /// </summary>
        /// <returns>사용자 테이블 확인 처리를 수행하는 코루틴입니다.</returns>
        protected virtual IEnumerator CheckUserTablesExist()
        {
            yield return null;
        }

        /// <summary>
        /// 지정한 테이블과 키로 현재 Locale에 맞는 로컬라이즈 문자열을 가져옵니다.
        /// 사용자 정의 테이블이 존재하면 해당 테이블을 먼저 조회합니다.
        /// </summary>
        /// <param name="table">조회할 기본 문자열 테이블 이름입니다.</param>
        /// <param name="key">조회할 문자열 엔트리 키입니다.</param>
        /// <returns>조회된 로컬라이즈 문자열이며, 키가 없으면 빈 문자열입니다.</returns>
        protected string GetString(string table, string key)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                GcLogger.LogWarning($"key is null or whitespace: {key}");
                return "";
            }

            if (UserTableExistsMap.TryGetValue(table, out bool hasUserTable) && hasUserTable)
            {
                string userTable = $"{table}_User";
                var userLocalized = StringDatabase.GetTableEntry(userTable, key, LocalizationSettings.SelectedLocale);
                if (userLocalized.Entry != null)
                {
                    return userLocalized.Entry.Value;
                }
            }

            var tableEntryResult = StringDatabase.GetTableEntry(table, key, LocalizationSettings.SelectedLocale);
            if (tableEntryResult.Entry != null)
                return tableEntryResult.Entry.Value;

            GcLogger.LogWarning($"MISSING:{key} / table:{table}");
            return "";
        }

        /// <summary>
        /// 현재 선택된 Locale을 기준으로 Smart String 또는 일반 문자열을 평가하여 반환합니다.
        /// 사용자 정의 테이블이 존재하면 해당 테이블을 먼저 조회합니다.
        /// </summary>
        /// <param name="tableName">조회할 기본 문자열 테이블 이름입니다.</param>
        /// <param name="key">조회할 문자열 엔트리 키입니다.</param>
        /// <param name="arguments">Smart String 평가에 사용할 인자 목록입니다.</param>
        /// <returns>평가된 로컬라이즈 문자열이며, 조회에 실패하면 빈 문자열입니다.</returns>
        public string GetSmartString(string tableName, string key, params object[] arguments)
        {
            return GetSmartString(tableName, key, LocalizationSettings.SelectedLocale, arguments);
        }

        /// <summary>
        /// 지정한 Locale을 기준으로 Smart String 또는 일반 문자열을 평가하여 반환합니다.
        /// 사용자 정의 테이블이 존재하면 해당 테이블을 먼저 조회합니다.
        /// </summary>
        /// <param name="tableName">조회할 기본 문자열 테이블 이름입니다.</param>
        /// <param name="key">조회할 문자열 엔트리 키입니다.</param>
        /// <param name="locale">문자열 평가에 사용할 Locale입니다.</param>
        /// <param name="arguments">Smart String 평가에 사용할 인자 목록입니다.</param>
        /// <returns>평가된 로컬라이즈 문자열이며, 조회에 실패하면 빈 문자열입니다.</returns>
        public string GetSmartString(string tableName, string key, Locale locale, params object[] arguments)
        {
            if (string.IsNullOrEmpty(tableName) || string.IsNullOrEmpty(key))
                return string.Empty;

            if (LocalizationSettings.InitializationOperation.IsValid() &&
                !LocalizationSettings.InitializationOperation.IsDone)
            {
                LocalizationSettings.InitializationOperation.WaitForCompletion();
            }

            if (TryGetUserTableExists(tableName))
            {
                var userTableName = GetUserTableName(tableName);
                var userValue = GetLocalizedStringByTableAndKey(userTableName, key, locale, arguments);
                if (!string.IsNullOrEmpty(userValue))
                    return userValue;
            }

            var baseValue = GetLocalizedStringByTableAndKey(tableName, key, locale, arguments);
            return baseValue ?? string.Empty;
        }

        /// <summary>
        /// 기본 테이블 이름에서 사용자 정의 테이블 이름을 생성합니다.
        /// </summary>
        /// <param name="baseTableName">사용자 정의 테이블 이름을 만들 기본 테이블 이름입니다.</param>
        /// <returns>프로젝트 규칙에 따른 사용자 정의 테이블 이름입니다.</returns>
        protected virtual string GetUserTableName(string baseTableName)
        {
            return $"{baseTableName}_User";
        }

        /// <summary>
        /// 사용자 정의 테이블 존재 여부를 내부 캐시에서 확인합니다.
        /// </summary>
        /// <param name="userTableName">존재 여부를 확인할 테이블 기준 이름입니다.</param>
        /// <returns>캐시에 존재하고 값이 <c>true</c>이면 <c>true</c>, 그렇지 않으면 <c>false</c>입니다.</returns>
        protected bool TryGetUserTableExists(string userTableName)
        {
            if (string.IsNullOrEmpty(userTableName))
                return false;

            return UserTableExistsMap.TryGetValue(userTableName, out var exists) && exists;
        }

        /// <summary>
        /// Unity Localization API를 사용하여 지정한 테이블과 키의 문자열을 동기적으로 가져옵니다.
        /// Smart String 인자가 있으면 함께 적용합니다.
        /// </summary>
        /// <param name="tableName">조회할 문자열 테이블 이름입니다.</param>
        /// <param name="key">조회할 문자열 엔트리 키입니다.</param>
        /// <param name="locale">문자열 조회에 사용할 Locale입니다.</param>
        /// <param name="arguments">Smart String 평가에 사용할 인자 목록입니다.</param>
        /// <returns>조회된 로컬라이즈 문자열이며, 실패하면 빈 문자열입니다.</returns>
        private static string GetLocalizedStringByTableAndKey(
            string tableName,
            string key,
            Locale locale,
            params object[] arguments)
        {
            try
            {
                var smartString = new LocalizedString(tableName, key)
                {
                    Arguments = arguments,
                    LocaleOverride = locale
                };

                AsyncOperationHandle<string> handle = smartString.GetLocalizedStringAsync();

                if (!handle.IsDone)
                    handle.WaitForCompletion();

                return handle.Result;
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[Localization] GetLocalizedString failed. table={tableName}, key={key}\n{e}");
                return string.Empty;
            }
        }

        /// <summary>
        /// LocalizedString의 동기 API를 사용하여 지정한 테이블과 키의 문자열을 가져옵니다.
        /// </summary>
        /// <param name="tableName">조회할 문자열 테이블 이름입니다.</param>
        /// <param name="key">조회할 문자열 엔트리 키입니다.</param>
        /// <param name="locale">문자열 조회에 사용할 Locale입니다.</param>
        /// <param name="arguments">Smart String 평가에 사용할 인자 목록입니다.</param>
        /// <returns>조회된 로컬라이즈 문자열입니다.</returns>
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

            return smartString.GetLocalizedString();
        }

        /// <summary>
        /// 현재 선택된 언어 코드를 반환합니다.
        /// </summary>
        /// <returns>현재 언어 코드입니다.</returns>
        public string GetCurrentLanguageCode() => CurrentLanguageCode;

        /// <summary>
        /// 언어 코드에 해당하는 Locale의 인덱스를 반환합니다.
        /// </summary>
        /// <param name="code">찾을 Locale의 언어 코드입니다.</param>
        /// <returns>일치하는 Locale 인덱스이며, 찾지 못하면 -1입니다.</returns>
        public int GetLocaleIndexByCode(string code)
        {
            Locale locale = GetLocaleByCode(code);
            if (locale == null) return -1;
            return GetIndexOfLocale(locale);
        }

        /// <summary>
        /// 지정한 Locale이 내부 Locale 목록에서 몇 번째인지 반환합니다.
        /// </summary>
        /// <param name="locale">인덱스를 찾을 Locale입니다.</param>
        /// <returns>일치하는 Locale 인덱스이며, 찾지 못하면 -1입니다.</returns>
        private int GetIndexOfLocale(Locale locale)
        {
            if (Locales == null || locale == null) return -1;

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
        /// 언어 코드와 일치하는 Locale을 반환합니다.
        /// 일치하는 코드가 없으면 등록된 첫 번째 Locale을 반환합니다.
        /// </summary>
        /// <param name="code">찾을 Locale의 언어 코드입니다.</param>
        /// <returns>일치하는 Locale 또는 기본으로 사용할 첫 번째 Locale입니다.</returns>
        public Locale GetLocaleByCode(string code)
        {
            if (string.IsNullOrEmpty(code) || Locales == null) return null;

            var exact = Locales.FirstOrDefault(l => l.Key == code);
            if (exact.Value != null) return exact.Value;

            return Locales.FirstOrDefault().Value;
        }

        /// <summary>
        /// 내부 Locale 목록에서 지정한 인덱스의 Locale을 반환합니다.
        /// </summary>
        /// <param name="index">찾을 Locale의 인덱스입니다.</param>
        /// <returns>인덱스에 해당하는 Locale이며, 범위를 벗어나면 <c>null</c>입니다.</returns>
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

        /// <summary>
        /// 지정한 문자열 테이블에 특정 키가 존재하는지 확인합니다.
        /// </summary>
        /// <param name="tableName">조회할 문자열 테이블 이름입니다.</param>
        /// <param name="key">존재 여부를 확인할 문자열 엔트리 키입니다.</param>
        /// <returns>테이블과 키가 모두 존재하면 <c>true</c>, 그렇지 않으면 <c>false</c>입니다.</returns>
        protected bool HasLocalizationKey(string tableName, string key)
        {
            if (string.IsNullOrEmpty(tableName) ||
                string.IsNullOrEmpty(key))
                return false;

            StringTable table =
                LocalizationSettings.StringDatabase.GetTable(tableName);

            if (table == null)
                return false;

            return table.GetEntry(key) != null;
        }
    }
}
