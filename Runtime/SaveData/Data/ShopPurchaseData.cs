using System.Collections.Generic;

namespace GGemCo2DCore
{
    /// <summary>
    /// shop_item UID별 누적 구매 횟수와 추가 충전 재고를 저장합니다.
    /// </summary>
    public sealed class ShopPurchaseData : DefaultData, ISaveData
    {
        /// <summary>
        /// shop_item UID별 누적 구매 횟수입니다.
        /// </summary>
        public Dictionary<int, int> BoughtCountsByShopItemUid = new Dictionary<int, int>();

        /// <summary>
        /// shop_item UID별 퀘스트 및 게임 진행 보상으로 충전된 누적 재고입니다.
        /// </summary>
        public Dictionary<int, int> RestockedCountsByShopItemUid = new Dictionary<int, int>();

        private TableShopItem _tableShopItem;

        /// <summary>
        /// 상점 구매 데이터를 테이블 및 저장 데이터와 연결합니다.
        /// </summary>
        /// <param name="loader">shop_item 테이블을 제공하는 테이블 로더입니다.</param>
        /// <param name="saveDataContainer">복원할 저장 데이터 컨테이너입니다.</param>
        public void Initialize(TableLoaderManager loader, SaveDataContainer saveDataContainer = null)
        {
            _tableShopItem = loader?.TableShopItem;
            BoughtCountsByShopItemUid =
                saveDataContainer?.ShopPurchaseData?.BoughtCountsByShopItemUid != null
                    ? new Dictionary<int, int>(saveDataContainer.ShopPurchaseData.BoughtCountsByShopItemUid)
                    : new Dictionary<int, int>();
            RestockedCountsByShopItemUid =
                saveDataContainer?.ShopPurchaseData?.RestockedCountsByShopItemUid != null
                    ? new Dictionary<int, int>(saveDataContainer.ShopPurchaseData.RestockedCountsByShopItemUid)
                    : new Dictionary<int, int>();
        }

        /// <summary>
        /// 지정한 shop_item의 누적 구매 횟수를 반환합니다.
        /// </summary>
        /// <param name="shopItemUid">조회할 shop_item UID입니다.</param>
        /// <returns>누적 구매 횟수입니다.</returns>
        public int GetBoughtCount(int shopItemUid)
        {
            if (shopItemUid <= 0) return 0;
            return BoughtCountsByShopItemUid.GetValueOrDefault(shopItemUid);
        }

        /// <summary>
        /// 지정한 shop_item에 누적된 추가 충전 재고를 반환합니다.
        /// </summary>
        /// <param name="shopItemUid">조회할 shop_item UID입니다.</param>
        /// <returns>퀘스트 및 게임 진행 보상으로 충전된 누적 재고입니다.</returns>
        public int GetRestockedCount(int shopItemUid)
        {
            if (shopItemUid <= 0) return 0;
            return RestockedCountsByShopItemUid.GetValueOrDefault(shopItemUid);
        }

        /// <summary>
        /// 기본 구매 한도와 추가 충전 재고에서 누적 구매 횟수를 차감한 남은 재고를 반환합니다.
        /// </summary>
        /// <param name="item">조회할 상점 표시 데이터입니다.</param>
        /// <returns>남은 재고입니다. 구매 제한이 없으면 <see cref="int.MaxValue"/>를 반환합니다.</returns>
        public int GetRemainingCount(ShopDisplayItem item)
        {
            if (item == null || item.PurchaseLimitCount <= 0) return int.MaxValue;

            // 누적 충전량이 큰 저장 데이터에서도 정수 오버플로 없이 남은 재고를 계산합니다.
            long remainingCount =
                (long)item.PurchaseLimitCount +
                GetRestockedCount(item.Uid) -
                GetBoughtCount(item.Uid);
            if (remainingCount <= 0) return 0;
            return remainingCount >= int.MaxValue ? int.MaxValue : (int)remainingCount;
        }

        /// <summary>
        /// 지정한 수량을 현재 상점 재고로 구매할 수 있는지 확인합니다.
        /// </summary>
        /// <param name="item">구매할 상점 표시 데이터입니다.</param>
        /// <param name="count">구매할 수량입니다.</param>
        /// <param name="disabledReason">구매할 수 없을 때 사용할 사유 키입니다.</param>
        /// <returns>구매할 수 있으면 <see langword="true"/>를 반환합니다.</returns>
        public bool CanBuy(ShopDisplayItem item, int count, out string disabledReason)
        {
            disabledReason = null;
            if (item == null) return false;
            if (item.PurchaseLimitCount <= 0) return true;

            if (GetRemainingCount(item) >= count) return true;

            disabledReason = ShopAvailabilityReason.SoldOut;
            return false;
        }

        /// <summary>
        /// 구매가 완료된 shop_item의 누적 구매 횟수를 증가시킵니다.
        /// </summary>
        /// <param name="item">구매가 완료된 상점 표시 데이터입니다.</param>
        /// <param name="count">구매한 수량입니다.</param>
        public void AddBoughtCount(ShopDisplayItem item, int count)
        {
            if (item == null || item.Uid <= 0 || item.PurchaseLimitCount <= 0 || count <= 0) return;

            int currentCount = GetBoughtCount(item.Uid);
            int nextCount = count >= int.MaxValue - currentCount
                ? int.MaxValue
                : currentCount + count;
            BoughtCountsByShopItemUid[item.Uid] = nextCount;
            SaveDatas();
            ShopAvailabilityService.Instance.NotifyChanged();
        }

        /// <summary>
        /// 지정한 shop_item에 구매 가능한 재고를 누적하여 충전합니다.
        /// 구매 전에 지급된 재고도 보존되며, 이후 구매할 때 한 개씩 차감됩니다.
        /// </summary>
        /// <param name="shopItemUid">재고를 충전할 shop_item UID입니다.</param>
        /// <param name="amount">추가할 재고 수량입니다.</param>
        /// <returns>재고가 정상적으로 충전되면 <see langword="true"/>를 반환합니다.</returns>
        public bool GrantStock(int shopItemUid, int amount)
        {
            if (shopItemUid <= 0 || amount <= 0 || _tableShopItem == null)
            {
                return false;
            }

            StruckTableShopItem item = _tableShopItem.GetDataByUid(shopItemUid);
            if (item == null || item.PurchaseLimitCount <= 0)
            {
                GcLogger.LogError(
                    $"재고 충전이 가능한 shop_item 정보를 찾을 수 없습니다. shopItemUid: {shopItemUid}");
                return false;
            }

            int currentCount = GetRestockedCount(shopItemUid);
            int nextCount = amount >= int.MaxValue - currentCount
                ? int.MaxValue
                : currentCount + amount;
            if (nextCount == currentCount)
            {
                return false;
            }

            RestockedCountsByShopItemUid[shopItemUid] = nextCount;
            SaveAndNotifyChanged();
            return true;
        }

        /// <summary>
        /// 지정한 상점 표시 데이터의 누적 구매 횟수를 초기화합니다.
        /// 추가 충전 재고는 유지합니다.
        /// </summary>
        /// <param name="item">구매 횟수를 초기화할 상점 표시 데이터입니다.</param>
        /// <returns>구매 횟수가 제거되면 <see langword="true"/>를 반환합니다.</returns>
        public bool ClearBoughtCount(ShopDisplayItem item)
        {
            if (item == null) return false;
            return ClearBoughtCount(item.Uid);
        }

        /// <summary>
        /// 지정한 shop_item의 누적 구매 횟수를 초기화합니다.
        /// 추가 충전 재고는 유지합니다.
        /// </summary>
        /// <param name="shopItemUid">구매 횟수를 초기화할 shop_item UID입니다.</param>
        /// <returns>구매 횟수가 제거되면 <see langword="true"/>를 반환합니다.</returns>
        public bool ClearBoughtCount(int shopItemUid)
        {
            if (!RemoveBoughtCount(shopItemUid)) return false;

            SaveAndNotifyChanged();
            return true;
        }

        /// <summary>
        /// 지정한 상점에 속한 모든 shop_item의 누적 구매 횟수를 초기화합니다.
        /// 추가 충전 재고는 유지합니다.
        /// </summary>
        /// <param name="shopUid">구매 횟수를 초기화할 상점 UID입니다.</param>
        /// <returns>하나 이상의 구매 횟수가 제거되면 <see langword="true"/>를 반환합니다.</returns>
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

        /// <summary>
        /// 모든 shop_item의 누적 구매 횟수를 초기화합니다.
        /// 추가 충전 재고는 유지합니다.
        /// </summary>
        /// <returns>하나 이상의 구매 횟수가 제거되면 <see langword="true"/>를 반환합니다.</returns>
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

        /// <summary>
        /// 변경된 구매 데이터를 저장하고 상점 구매 가능 상태 변경을 알립니다.
        /// </summary>
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
