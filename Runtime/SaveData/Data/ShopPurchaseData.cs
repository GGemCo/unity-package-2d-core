using System.Collections.Generic;
using UnityEngine;

namespace GGemCo2DCore
{
    /// <summary>
    /// Persistent purchase counts keyed by shop_item Uid.
    /// </summary>
    public sealed class ShopPurchaseData : DefaultData, ISaveData
    {
        public Dictionary<int, int> BoughtCountsByShopItemUid = new Dictionary<int, int>();
        private TableShopItem _tableShopItem;

        public void Initialize(TableLoaderManager loader, SaveDataContainer saveDataContainer = null)
        {
            _tableShopItem = loader?.TableShopItem;
            BoughtCountsByShopItemUid =
                saveDataContainer?.ShopPurchaseData?.BoughtCountsByShopItemUid != null
                    ? new Dictionary<int, int>(saveDataContainer.ShopPurchaseData.BoughtCountsByShopItemUid)
                    : new Dictionary<int, int>();
        }

        public int GetBoughtCount(int shopItemUid)
        {
            if (shopItemUid <= 0) return 0;
            return BoughtCountsByShopItemUid.GetValueOrDefault(shopItemUid);
        }

        public int GetRemainingCount(ShopDisplayItem item)
        {
            if (item == null || item.PurchaseLimitCount <= 0) return int.MaxValue;
            return Mathf.Max(0, item.PurchaseLimitCount - GetBoughtCount(item.Uid));
        }

        public bool CanBuy(ShopDisplayItem item, int count, out string disabledReason)
        {
            disabledReason = null;
            if (item == null) return false;
            if (item.PurchaseLimitCount <= 0) return true;

            if (GetRemainingCount(item) >= count) return true;

            disabledReason = "Shop_SoldOut";
            return false;
        }

        public void AddBoughtCount(ShopDisplayItem item, int count)
        {
            if (item == null || item.Uid <= 0 || item.PurchaseLimitCount <= 0 || count <= 0) return;

            int nextCount = GetBoughtCount(item.Uid) + count;
            BoughtCountsByShopItemUid[item.Uid] = nextCount;
            SaveDatas();
            ShopAvailabilityService.Instance.NotifyChanged();
        }

        public bool ClearBoughtCount(ShopDisplayItem item)
        {
            if (item == null) return false;
            return ClearBoughtCount(item.Uid);
        }

        public bool ClearBoughtCount(int shopItemUid)
        {
            if (!RemoveBoughtCount(shopItemUid)) return false;

            SaveAndNotifyChanged();
            return true;
        }

        public bool ClearBoughtCountsByShopUid(int shopUid)
        {
            if (shopUid <= 0 || _tableShopItem == null) return false;

            var items = _tableShopItem.GetItemsByShopUid(shopUid);
            if (items == null || items.Count <= 0) return false;

            bool changed = false;
            foreach (var item in items)
            {
                if (item == null) continue;
                changed |= RemoveBoughtCount(item.Uid);
            }

            if (!changed) return false;

            SaveAndNotifyChanged();
            return true;
        }

        public bool ClearAllBoughtCounts()
        {
            if (BoughtCountsByShopItemUid.Count <= 0) return false;

            BoughtCountsByShopItemUid.Clear();
            SaveAndNotifyChanged();
            return true;
        }

        private bool RemoveBoughtCount(int shopItemUid)
        {
            return shopItemUid > 0 && BoughtCountsByShopItemUid.Remove(shopItemUid);
        }

        private void SaveAndNotifyChanged()
        {
            SaveDatas();
            ShopAvailabilityService.Instance.NotifyChanged();
        }

        protected override int GetMaxSlotCount()
        {
            return 0;
        }
    }
}
