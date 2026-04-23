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

        public void Initialize(TableLoaderManager loader, SaveDataContainer saveDataContainer = null)
        {
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

        protected override int GetMaxSlotCount()
        {
            return 0;
        }
    }
}
