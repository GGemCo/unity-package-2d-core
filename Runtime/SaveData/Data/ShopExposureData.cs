using System.Collections.Generic;

namespace GGemCo2DCore
{
    public sealed class ShopExposureRecord
    {
        public int ExposureCount;
        public int BoughtCountAtCycleStart;
    }

    /// <summary>
    /// Persistent exposure counters keyed by shop_item Uid.
    /// </summary>
    public sealed class ShopExposureData : DefaultData, ISaveData
    {
        public Dictionary<int, ShopExposureRecord> RecordsByShopItemUid =
            new Dictionary<int, ShopExposureRecord>();

        private bool _dirty;

        public void Initialize(TableLoaderManager loader, SaveDataContainer saveDataContainer = null)
        {
            RecordsByShopItemUid =
                saveDataContainer?.ShopExposureData?.RecordsByShopItemUid != null
                    ? new Dictionary<int, ShopExposureRecord>(saveDataContainer.ShopExposureData.RecordsByShopItemUid)
                    : new Dictionary<int, ShopExposureRecord>();
        }

        public ShopExposureRecord AddExposure(int shopItemUid, int currentBoughtCount)
        {
            if (shopItemUid <= 0) return null;

            var record = GetOrCreate(shopItemUid, currentBoughtCount);
            record.ExposureCount++;
            _dirty = true;
            return record;
        }

        public void ResetCycle(int shopItemUid, int currentBoughtCount)
        {
            if (shopItemUid <= 0) return;

            var record = GetOrCreate(shopItemUid, currentBoughtCount);
            record.ExposureCount = 0;
            record.BoughtCountAtCycleStart = currentBoughtCount;
            _dirty = true;
        }

        public void SaveIfDirty()
        {
            if (!_dirty) return;
            _dirty = false;
            SaveDatas();
        }

        private ShopExposureRecord GetOrCreate(int shopItemUid, int currentBoughtCount)
        {
            if (!RecordsByShopItemUid.TryGetValue(shopItemUid, out var record) || record == null)
            {
                record = new ShopExposureRecord
                {
                    ExposureCount = 0,
                    BoughtCountAtCycleStart = currentBoughtCount,
                };
                RecordsByShopItemUid[shopItemUid] = record;
                _dirty = true;
            }

            return record;
        }

        protected override int GetMaxSlotCount()
        {
            return 0;
        }
    }
}
