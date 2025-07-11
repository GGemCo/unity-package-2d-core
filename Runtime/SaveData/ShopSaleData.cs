﻿
namespace GGemCo2DCore
{
    /// <summary>
    /// 상점에 판매할 때 사용 - 저장하지 않음
    /// </summary>
    public class ShopSaleData : ItemStorageData
    {
        public void Initialize(TableLoaderManager loader, SaveDataContainer saveDataContainer = null)
        {
            ItemCounts.Clear();
        }

        protected override int GetMaxSlotCount()
        {
            return SceneGame.Instance.uIWindowManager
                .GetUIWindowByUid<UIWindowShopSale>(UIWindowConstants.WindowUid.ShopSale)?.maxCountIcon ?? 0;
        }
    }
}