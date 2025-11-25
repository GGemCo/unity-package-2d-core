using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
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
        protected LocalizedStringDatabase stringDatabase;
        // asset table
        private LocalizedAssetDatabase _assetDatabase;
        // 사용자 언어 테이블 존재 여부
        private readonly Dictionary<string, bool> _userTableExistsMap = new();
        // 현재 사용하는 언어 Locale
        private static readonly Dictionary<string, Locale> Locales = new Dictionary<string, Locale>();
        // 로드 진행율
        private float _loadProgress;

        protected virtual void Awake()
        {
            _loadProgress = 0f;
            stringDatabase = LocalizationSettings.StringDatabase;
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
            
            StartCoroutine(CheckUserTablesExist());
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
            if (_userTableExistsMap.TryGetValue(table, out bool hasUserTable) && hasUserTable)
            {
                string userTable = $"{table}_User";
                var userLocalized = stringDatabase.GetTableEntry(userTable, key, LocalizationSettings.SelectedLocale);
                if (userLocalized.Entry != null)
                {
                    return userLocalized.Entry.Value;
                }
            }

            // 유저 테이블에 없으면 기존 테이블 조회
            var tableEntryResult = stringDatabase.GetTableEntry(table, key, LocalizationSettings.SelectedLocale);
            if (tableEntryResult.Entry != null)
                return tableEntryResult.Entry.Value;

            GcLogger.LogWarning($"MISSING:{key} / table:{table}");
            return "";
        }

        /// <summary>
        /// 에셋 테이블에서 로컬라이즈된 에셋을 반환합니다.
        /// </summary>
        public T GetLocalizedAsset<T>(string table, string key) where T : Object
        {
            AsyncOperationHandle<T> handle =
                _assetDatabase.GetLocalizedAssetAsync<T>(table, key, LocalizationSettings.SelectedLocale);
            return handle.WaitForCompletion();
        }
        public float GetLoadProgress() => _loadProgress;

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
    }
}
