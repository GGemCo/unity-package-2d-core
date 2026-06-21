using System;
using System.Collections;
using System.Collections.Generic;
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
                    DontDestroyOnLoad(gameObject);

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
            var tables = TableLoaderManager.Instance;
            if (tables != null)
                tables.RefreshStatusNames();
        }

        /// <summary>
        /// 사용자 언어 테이블 존재 체크
        /// </summary>
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
                        exists = true;
                }
                else
                {
                    GcLogger.LogWarning($"Invalid handle for table: {userTableName}");
                }

                UserTableExistsMap[baseTable] = exists;

                if (handle.IsValid())
                    Addressables.Release(handle);
            }
        }

        public string GetSmartCommonUIByKey(string key, params object[] arguments) =>
            GetSmartString(LocalizationConstants.Tables.CommonUI, key, arguments);

        public string GetSmartSystemByKey(string key, params object[] arguments) =>
            GetSmartString(LocalizationConstants.Tables.System, key, arguments);

        public string GetSmartSceneByKey(string key, params object[] arguments) =>
            GetSmartString(LocalizationConstants.Tables.Scene, key, arguments);

        public string GetSmartUIWindowItemInfoByKey(string key, params object[] arguments) =>
            GetSmartString(LocalizationConstants.Tables.UIWindowItemInfo, key, arguments);

        public string GetCommonUIByKey(string key) => GetString(LocalizationConstants.Tables.CommonUI, key);
        public string GetSystemByKey(string key) => GetString(LocalizationConstants.Tables.System, key);
        public string GetSceneByKey(string key) => GetString(LocalizationConstants.Tables.Scene, key);
        public string GetUIWindowItemInfoByKey(string key) => GetString(LocalizationConstants.Tables.UIWindowItemInfo, key);
        public string GetCommonGameByKey(string key) => GetString(LocalizationConstants.Tables.CommonGame, key);
        public string GetStatusNameByKey(string key) => GetString(LocalizationConstants.Tables.StatusName, key);
        public string GetUIWindowItemUpgradeByKey(string key) => GetString(LocalizationConstants.Tables.UIWindowItemUpgrade, key);
        public string GetUIWindowItemCraftByKey(string key) => GetString(LocalizationConstants.Tables.UIWindowItemCraft, key);
        /// <summary>
        /// 외부 패키지가 지정한 문자열 테이블과 키로 현지화 문자열을 조회합니다.
        /// </summary>
        /// <param name="tableName">조회할 문자열 테이블 이름입니다.</param>
        /// <param name="key">조회할 문자열 키입니다.</param>
        /// <returns>현지화된 문자열입니다.</returns>
        public string GetExternalString(string tableName, string key) => GetString(tableName, key);
        public string GetItemNameByKey(string key) => GetString(LocalizationConstants.Tables.ItemName, key);
        public string GetItemDescriptionByKey(string key) => GetString(LocalizationConstants.Tables.ItemDescription, key);
        public string GetItemTaxonomyByKey(string key) => GetString(LocalizationConstants.Tables.ItemTaxonomy, key);
        public string GetMapNameByKey(string key) => GetString(LocalizationConstants.Tables.MapName, key);
        public string GetNpcNameByKey(string key) => GetString(LocalizationConstants.Tables.NpcName, key);
        public string GetMonsterNameByKey(string key) => GetString(LocalizationConstants.Tables.MonsterName, key);
        public string GetUIWindowTitleByKey(string key) => GetString(LocalizationConstants.Tables.UIWindowTitle, key);
        public string GetUIWindowOptionByKey(string key) => GetString(LocalizationConstants.Tables.UIWindowOption, key);
        public string GetInteractionByKey(string key) => GetString(LocalizationConstants.Tables.UIWindowInteractionDialogue, key);
        public string GetUIWindowTcgBattleHudByKey(string key) => GetString(LocalizationConstants.Tables.UIWindowTcgBattleHud, key);
        public string GetUIWindowPlayerInfoByKey(string key) => GetString(LocalizationConstants.Tables.UIWindowPlayerStatInfo, key);
        public string GetUIWindowShopByKey(string key) => GetString(LocalizationConstants.Tables.UIWindowShop, key);

        public string GetUIWindowPlayerStatResetByKey(string key) => GetString(LocalizationConstants.Tables.UIWindowPlayerStatReset, key);
        
        public string GetSmartInteractionByKey(string key, params object[] arguments) =>
            GetSmartString(LocalizationConstants.Tables.UIWindowInteractionDialogue, key, arguments);
        
        /// <summary>
        /// ItemDescription 테이블의 Smart String(동적 치환) 평가.
        /// </summary>
        public string GetItemDescriptionSmartByKey(string key, params object[] arguments) =>
            GetSmartString(LocalizationConstants.Tables.ItemDescription, key, arguments);

        public string GetItemTypeName(ItemConstants.Type value) =>
            GetItemTaxonomyOrFallback(ItemLocalizationKeys.Type(value), value);

        public string GetItemCategoryName(ItemConstants.Category value) =>
            GetItemTaxonomyOrFallback(ItemLocalizationKeys.Category(value), value);

        public string GetItemSubCategoryName(ItemConstants.SubCategory value) =>
            GetItemTaxonomyOrFallback(ItemLocalizationKeys.SubCategory(value), value);

        public string GetItemClassName(ItemConstants.Class value) =>
            GetItemTaxonomyOrFallback(ItemLocalizationKeys.Class(value), value);

        public string GetItemAntiFlagName(ItemConstants.AntiFlag value) =>
            GetItemTaxonomyOrFallback(ItemLocalizationKeys.AntiFlag(value), value);

        public string GetItemAntiFlagNames(IEnumerable<ItemConstants.AntiFlag> values)
        {
            if (values == null)
                return string.Empty;

            List<string> names = new();
            foreach (ItemConstants.AntiFlag value in values)
            {
                string name = GetItemAntiFlagName(value);
                if (!string.IsNullOrWhiteSpace(name))
                    names.Add(name);
            }

            return string.Join(", ", names);
        }

        private string GetItemTaxonomyOrFallback<TEnum>(string key, TEnum fallback)
            where TEnum : struct, Enum
        {
            if (EqualityComparer<TEnum>.Default.Equals(fallback, default) || string.IsNullOrWhiteSpace(key))
                return string.Empty;

            string text = GetItemTaxonomyByKey(key);
            return string.IsNullOrWhiteSpace(text) ? fallback.ToString() : text;
        }
    }
}
