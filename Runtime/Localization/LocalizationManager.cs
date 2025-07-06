using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Localization.Settings;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace GGemCo2DCore
{
    /// <summary>
    /// <para>Localization 매니저</para>
    /// </summary>
    public class LocalizationManager : MonoBehaviour
    {
        public static LocalizationManager Instance { get; private set; }

        public UnityEvent onChangeLocale;

        private List<string> _languageCodes;
        private bool _isChanging;

        private static string CurrentLanguageCode { get; set; }

        private LocalizedStringDatabase _stringDatabase;
        private LocalizedAssetDatabase _assetDatabase;
        
        private readonly Dictionary<string, bool> _userTableExistsMap = new();
        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
                return;
            }

            InitializeLanguageCodes();
            _stringDatabase = LocalizationSettings.StringDatabase;
            _assetDatabase = LocalizationSettings.AssetDatabase;
        }

        /// <summary>
        /// LanguageIndex enum을 기반으로 코드 리스트를 초기화합니다.
        /// </summary>
        private void InitializeLanguageCodes()
        {
            _languageCodes = new List<string>();
            foreach (LocalizationConstants.LanguageIndex lang in Enum.GetValues(typeof(LocalizationConstants.LanguageIndex)))
            {
                _languageCodes.Add(lang.ToString());
            }
        }

        /// <summary>
        /// 언어 인덱스를 받아 로케일을 변경합니다.
        /// </summary>
        public void StartChangeLocale(int index)
        {
            if (_isChanging) return;
            StartCoroutine(ChangeLocaleRoutine(index));
        }

        private IEnumerator ChangeLocaleRoutine(int index)
        {
            _isChanging = true;

            yield return LocalizationSettings.InitializationOperation;

            var locales = LocalizationSettings.AvailableLocales.Locales;
            if (index < 0 || index >= locales.Count)
            {
                Debug.LogWarning($"[LocalizationManager] Invalid locale index: {index}");
                _isChanging = false;
                yield break;
            }
            yield return CheckUserTablesExist();

            LocalizationSettings.SelectedLocale = locales[index];
            PlayerPrefsManager.SaveIndexLocalizationLocale(index);
            CurrentLanguageCode = _languageCodes[index];

            _isChanging = false;
            onChangeLocale?.Invoke();
        }
        private IEnumerator CheckUserTablesExist()
        {
            foreach (string baseTable in LocalizationConstants.Tables.All)
            {
                string userTableName = $"{baseTable}_User";
                var handle = _stringDatabase.GetTableAsync(userTableName, LocalizationSettings.SelectedLocale);
                yield return handle;

                bool exists = handle.Status == AsyncOperationStatus.Succeeded && handle.Result != null;
                _userTableExistsMap[baseTable] = exists;

                GcLogger.Log($"[LocalizationManager] table: {userTableName} / exist: {exists}");
            }
        }

        /// <summary>
        /// 현재 선택된 언어 인덱스를 반환합니다.
        /// </summary>
        public LocalizationConstants.LanguageIndex GetCurrentLanguageIndex()
        {
            int savedIndex = PlayerPrefsManager.LoadIndexLocalizationLocale();
            return (LocalizationConstants.LanguageIndex)Mathf.Clamp(savedIndex, 0, _languageCodes.Count - 1);
        }

        /// <summary>
        /// 공용 테이블에서 문자열을 가져옵니다.
        /// </summary>
        public string GetCommonByKey(string key) => GetString(LocalizationConstants.Tables.Common, key);

        /// <summary>
        /// 시스템 테이블에서 문자열을 가져옵니다.
        /// </summary>
        public string GetSystemByKey(string key) => GetString(LocalizationConstants.Tables.System, key);

        /// <summary>
        /// 시스템 테이블에서 int 키로 문자열을 가져옵니다.
        /// </summary>
        public string GetSystemByKey(int key) => key > 0 ? GetSystemByKey($"{key}") : string.Empty;

        /// <summary>
        /// 씬 테이블에서 문자열을 가져옵니다.
        /// </summary>
        public string GetSceneByKey(string key) => GetString(LocalizationConstants.Tables.Scene, key);

        /// <summary>
        /// 지정한 테이블과 키로 로컬라이즈된 문자열을 가져옵니다.
        /// </summary>
        public string GetString(string table, string key)
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
                var userLocalized = _stringDatabase.GetLocalizedString(userTable, key, LocalizationSettings.SelectedLocale);
                if (!string.IsNullOrWhiteSpace(userLocalized))
                {
                    return userLocalized;
                }
            }

            var localized = _stringDatabase.GetLocalizedString(table, key, LocalizationSettings.SelectedLocale);
            if (string.IsNullOrWhiteSpace(localized))
            {
                GcLogger.LogWarning($"[MISSING:{key}]");
                return "";
            }
            else
            {
                return localized;
            }
        }

        /// <summary>
        /// 에셋 테이블에서 로컬라이즈된 에셋을 반환합니다.
        /// </summary>
        public T GetLocalizedAsset<T>(string table, string key) where T : UnityEngine.Object
        {
            AsyncOperationHandle<T> handle = _assetDatabase.GetLocalizedAssetAsync<T>(table, key, LocalizationSettings.SelectedLocale);
            return handle.WaitForCompletion();
        }

        /// <summary>
        /// 현재 언어 코드 (예: "En", "Ko") 반환
        /// </summary>
        public string GetCurrentLanguageCode() => CurrentLanguageCode;
    }
}
