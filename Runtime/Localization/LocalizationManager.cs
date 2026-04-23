using System.Collections;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Localization.Settings;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace GGemCo2DCore
{
    /// <summary>
    /// <para>Localization 매니저</para>
    /// </summary>
    public class LocalizationManager : LocalizationManagerBase
    {
        public static LocalizationManager Instance;

        protected override void Awake()
        {
            base.Awake();
            if (!Instance)
            {
                Instance = this;
                if (Application.isPlaying)
                {
                    DontDestroyOnLoad(gameObject);
                }


                // Locale 변경 바인딩은 중앙 LocalizationManager에서만 관리한다.
                // - AffectDescriptionService는 이벤트를 구독하지 않고, 캐시만 Clear 한다.
                // - 중복 구독 방지를 위해 -= 후 += 패턴을 사용한다.
                OnChangeLocale -= HandleLocaleChanged;
                OnChangeLocale += HandleLocaleChanged;
            }
            else
            {
                Destroy(gameObject);
            }

        }

        private void HandleLocaleChanged(string _, int __)
        {
            // Affect 설명 캐시 무효화
            // AffectDescriptionService.ClearCache();

            // StatusName을 참조하는 테이블(Name 캐시) 갱신
            var tables = TableLoaderManager.Instance;
            if (tables != null)
            {
                tables.RefreshStatusNames();
            }
        }

        /// <summary>
        /// 사용자 언어 테이블 존재 체크
        /// </summary>
        /// <returns></returns>
        protected override IEnumerator CheckUserTablesExist()
        {
            foreach (string baseTable in LocalizationConstants.Tables.All)
            {
                string userTableName = $"{baseTable}_User";
                var handle = StringDatabase.GetTableAsync(userTableName, LocalizationSettings.SelectedLocale);
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

                // LocalizationManagerBase.GetString/GetSmartString에서 사용한다.
                UserTableExistsMap[baseTable] = exists;

                // handle이 Release 가능한 경우라면 아래 코드도 추가
                if (handle.IsValid())
                    Addressables.Release(handle);
            }
        }

        /// <summary>
        /// Smart String (동적 치환) 평가.
        /// </summary>
        public string GetSmartCommonUIByKey(string key, params object[] arguments) =>
            GetSmartString(LocalizationConstants.Tables.CommonUI, key, arguments);

        public string GetSmartSystemByKey(string key, params object[] arguments) =>
            GetSmartString(LocalizationConstants.Tables.System, key, arguments);

        public string GetSmartSceneByKey(string key, params object[] arguments) =>
            GetSmartString(LocalizationConstants.Tables.Scene, key, arguments);

        public string GetSmartUIWindowItemInfoByKey(string key, params object[] arguments) =>
            GetSmartString(LocalizationConstants.Tables.UIWindowItemInfo, key, arguments);


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
        public string GetUIWindowItemUpgradeByKey(string key) => GetString(LocalizationConstants.Tables.UIWindowItemUpgrade, key);
        public string GetUIWindowItemCraftByKey(string key) => GetString(LocalizationConstants.Tables.UIWindowItemCraft, key);
        public string GetUIWindowQuestRewardByKey(string key) => GetString(LocalizationConstants.Tables.UIWindowQuestReward, key);
        public string GetItemNameByKey(string key) => GetString(LocalizationConstants.Tables.ItemName, key);
        public string GetItemDescriptionByKey(string key) => GetString(LocalizationConstants.Tables.ItemDescription, key);
        /// <summary>
        /// ItemDescription 테이블의 Smart String(동적 치환) 평가.
        /// </summary>
        public string GetItemDescriptionSmartByKey(string key, params object[] arguments) =>
            GetSmartString(LocalizationConstants.Tables.ItemDescription, key, arguments);
        public string GetMapNameByKey(string key) => GetString(LocalizationConstants.Tables.MapName, key);
        public string GetNpcNameByKey(string key) => GetString(LocalizationConstants.Tables.NpcName, key);
        public string GetMonsterNameByKey(string key) => GetString(LocalizationConstants.Tables.MonsterName, key);
        public string GetUIWindowTitleByKey(string key) => GetString(LocalizationConstants.Tables.UIWindowTitle, key);
        public string GetUIWindowOptionByKey(string key) => GetString(LocalizationConstants.Tables.UIWindowOption, key);

        public string GetInteractionByKey(string key) => GetString(LocalizationConstants.Tables.UIWindowInteractionDialogue, key);

        public string GetUIWindowTcgBattleHudByKey(string key) => GetString(LocalizationConstants.Tables.UIWindowTcgBattleHud, key);

        public string GetUIWindowPlayerInfoByKey(string key) =>
            GetString(LocalizationConstants.Tables.UIWindowPlayerStatInfo, key);

        public string GetUIWindowShopByKey(string key) =>
            GetString(LocalizationConstants.Tables.UIWindowShop, key);
    }
}
