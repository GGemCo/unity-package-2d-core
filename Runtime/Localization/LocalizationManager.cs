using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Events;
using UnityEngine.Localization.Settings;
using UnityEngine.ResourceManagement.AsyncOperations;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif

namespace GGemCo2DCore
{
    /// <summary>
    /// <para>Localization 매니저</para>
    /// </summary>
    public class LocalizationManager : MonoBehaviour
    {
        public static LocalizationManager Instance;
        // 언어 변경시 발생하는 이벤트
        public UnityEvent onChangeLocale;
        // 사용중인 언어 코드 리스트
        private List<string> _languageCodes;
        // 언어 변경 중인지
        private bool _isChanging;
        // 현재 언어 코드
        private static string CurrentLanguageCode { get; set; }
        // string table
        private LocalizedStringDatabase _stringDatabase;
        // asset table
        private LocalizedAssetDatabase _assetDatabase;
        // 사용자 언어 테이블 존재 여부
        private readonly Dictionary<string, bool> _userTableExistsMap = new();
        private float _loadProgress;

        private void Awake()
        {
            _loadProgress = 0f;
            if (!Instance)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }

            _stringDatabase = LocalizationSettings.StringDatabase;
            _assetDatabase = LocalizationSettings.AssetDatabase;

            InitializeLanguageCodes();
        }
        /// <summary>
        /// LanguageIndex enum을 기반으로 코드 리스트를 초기화합니다.
        /// </summary>
        private void InitializeLanguageCodes()
        {
            _languageCodes = new List<string>();
            foreach (LocalizationConstants.LanguageIndex lang in Enum.GetValues(
                         typeof(LocalizationConstants.LanguageIndex)))
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
        /// <summary>
        /// 언어 바꾸기
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public IEnumerator ChangeLocaleRoutine(int index)
        {
            _isChanging = true;

            yield return LocalizationSettings.InitializationOperation;

            var locales = LocalizationSettings.AvailableLocales.Locales;
            if (index < 0 || index >= locales.Count)
            {
                GcLogger.LogWarning($"[LocalizationManager] Invalid locale index: {index}");
                _isChanging = false;
                yield break;
            }

            LocalizationSettings.SelectedLocale = locales[index];
            PlayerPrefsManager.SaveIndexLocalizationLocale(index);
            CurrentLanguageCode = _languageCodes[index];

            _isChanging = false;
            // GcLogger.Log($"[LocalizationManager] change success. locale index: {index}");
            PlayerPrefsManager.SaveIndexLocalizationLocale(index);
            onChangeLocale?.Invoke();
            
            StartCoroutine(CheckUserTablesExist());
        }
        /// <summary>
        /// 사용자 언어 테이블 존재 체크
        /// </summary>
        /// <returns></returns>
        private IEnumerator CheckUserTablesExist()
        {
            foreach (string baseTable in LocalizationConstants.Tables.All)
            {
                string userTableName = $"{baseTable}_User";
                var handle = _stringDatabase.GetTableAsync(userTableName, LocalizationSettings.SelectedLocale);
                yield return handle;

                bool exists = false;

                if (handle.IsValid())
                {
                    if (handle.Status == AsyncOperationStatus.Succeeded && handle.Result != null)
                    {
                        exists = true;
                        // GcLogger.Log($"table: {userTableName} / exist: true");
                    }
                    else
                    {
                        // GcLogger.Log($"table: {userTableName} / exist: false");
                    }
                }
                else
                {
                    GcLogger.LogWarning($"Invalid handle for table: {userTableName}");
                }

                _userTableExistsMap[baseTable] = exists;

                // handle이 Release 가능한 경우라면 아래 코드도 추가
                if (handle.IsValid())
                    Addressables.Release(handle);
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
        /// 지정한 테이블과 키로 로컬라이즈된 문자열을 가져옵니다.
        /// </summary>
        private string GetString(string table, string key)
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
                var userLocalized = _stringDatabase.GetTableEntry(userTable, key, LocalizationSettings.SelectedLocale);
                if (userLocalized.Entry != null)
                {
                    return userLocalized.Entry.Value;
                }
            }

            // 유저 테이블에 없으면 기존 테이블 조회
            var tableEntryResult = _stringDatabase.GetTableEntry(table, key, LocalizationSettings.SelectedLocale);
            if (tableEntryResult.Entry != null)
                return tableEntryResult.Entry.Value;

            GcLogger.LogWarning($"MISSING:{key} / table:{table}");
            return "";
        }

        /// <summary>
        /// 에셋 테이블에서 로컬라이즈된 에셋을 반환합니다.
        /// </summary>
        public T GetLocalizedAsset<T>(string table, string key) where T : UnityEngine.Object
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
        /// UI 에서 사용하는 공용 단어
        /// </summary>
        public string GetCommonUIByKey(string key) => GetString(LocalizationConstants.Tables.CommonUI, key);

        /// <summary>
        /// 시스템 메시지
        /// </summary>
        public string GetSystemByKey(string key) => GetString(LocalizationConstants.Tables.System, key);

        /// <summary>
        /// Scene (인트로, 로딩, 게임)
        /// </summary>
        public string GetSceneByKey(string key) => GetString(LocalizationConstants.Tables.Scene, key);
        /// <summary>
        /// 아이템 정보 윈도우
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public string GetUIWindowItemInfoByKey(string key) => GetString(LocalizationConstants.Tables.UIWindowItemInfo, key);
        /// <summary>
        /// 인게임에서 사용하는 공용 단어
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public string GetCommonGameByKey(string key) => GetString(LocalizationConstants.Tables.CommonGame, key);
        
        /// <summary>
        /// 데이터 테이블별 단어 가져오기
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public string GetStatusNameByKey(string key) => GetString(LocalizationConstants.Tables.StatusName, key);
        public string GetUIWindowSkillInfoByKey(string key) => GetString(LocalizationConstants.Tables.UIWindowSkillInfo, key);
        public string GetUIWindowSkillByKey(string key) => GetString(LocalizationConstants.Tables.UIWindowSkill, key);
        public string GetUIWindowItemUpgradeByKey(string key) => GetString(LocalizationConstants.Tables.UIWindowItemUpgrade, key);
        public string GetUIWindowItemCraftByKey(string key) => GetString(LocalizationConstants.Tables.UIWindowItemCraft, key);
        public string GetUIWindowQuestRewardByKey(string key) => GetString(LocalizationConstants.Tables.UIWindowQuestReward, key);
        public string GetItemNameByKey(string key) => GetString(LocalizationConstants.Tables.ItemName, key);
        public string GetItemDescriptionByKey(string key) => GetString(LocalizationConstants.Tables.ItemDescription, key);
        public string GetMapNameByKey(string key) => GetString(LocalizationConstants.Tables.MapName, key);
        public string GetSkillNameByKey(string key) => GetString(LocalizationConstants.Tables.SkillName, key);
        public string GetNpcNameByKey(string key) => GetString(LocalizationConstants.Tables.NpcName, key);
        public string GetMonsterNameByKey(string key) => GetString(LocalizationConstants.Tables.MonsterName, key);
        public string GetUIWindowTitleByKey(string key) => GetString(LocalizationConstants.Tables.UIWindowTitle, key);
        public string GetAffectNameByKey(string key) => GetString(LocalizationConstants.Tables.AffectName, key);
    }
}
