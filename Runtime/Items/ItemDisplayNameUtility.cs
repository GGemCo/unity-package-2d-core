namespace GGemCo2DCore
{
    /// <summary>
    /// 아이템 이름 표시 규칙을 한 곳에서 관리합니다.
    /// </summary>
    public static class ItemDisplayNameUtility
    {
        public static string GetDisplayName(StruckTableItem item, LocalizationManager localization = null)
        {
            if (item == null)
                return string.Empty;

            string itemName = ResolveBaseName(item, localization);
            return AppendUidIfEnabled(itemName, item.Uid);
        }

        public static string GetDisplayName(int itemUid, LocalizationManager localization = null)
        {
            if (itemUid <= 0 || TableLoaderManager.Instance == null)
                return string.Empty;

            return GetDisplayName(TableLoaderManager.Instance.GetItemData(itemUid, false), localization);
        }

        private static string ResolveBaseName(StruckTableItem item, LocalizationManager localization)
        {
            string itemName = item.Name;
            if (localization == null)
                return itemName;

            string localized = localization.GetItemNameByKey(item.Uid.ToString());
            if (string.IsNullOrWhiteSpace(localized))
                return itemName;

            if (item.Upgrade > 0)
                return $"{localized} +{item.Upgrade}";

            return localized;
        }

        private static string AppendUidIfEnabled(string itemName, int itemUid)
        {
            if (!ShouldShowUid() || itemUid <= 0)
                return itemName;

            if (string.IsNullOrWhiteSpace(itemName))
                return $"[{itemUid}]";

            return $"{itemName} [{itemUid}]";
        }

        private static bool ShouldShowUid()
        {
            var itemSettings = AddressableLoaderSettings.Instance != null
                ? AddressableLoaderSettings.Instance.itemSettings
                : null;

            return itemSettings != null && itemSettings.EnableItemUid;
        }
    }
}
