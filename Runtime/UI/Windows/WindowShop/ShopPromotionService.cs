using System.Collections.Generic;
using UnityEngine;

namespace GGemCo2DCore
{
    public sealed class ShopPromotionResult
    {
        public StruckTableShopPromotion Source;
        public int FinalCurrencyValue;
        public int DiscountRate;
    }

    public interface IShopPromotionStrategy
    {
        ShopPromotionStrategyType StrategyType { get; }

        bool TryApply(
            ShopDisplayItem item,
            StruckTableShopPromotion promotion,
            ShopExposureRecord exposureRecord,
            int currentBoughtCount,
            out ShopPromotionResult result,
            out bool resetExposureCycle);
    }

    /// <summary>
    /// Applies configured shop promotions to display items.
    /// </summary>
    public sealed class ShopPromotionService
    {
        private readonly TableShopPromotion _tableShopPromotion;
        private readonly Dictionary<ShopPromotionStrategyType, IShopPromotionStrategy> _strategies =
            new Dictionary<ShopPromotionStrategyType, IShopPromotionStrategy>();

        public ShopPromotionService(TableShopPromotion tableShopPromotion)
        {
            _tableShopPromotion = tableShopPromotion;
            Register(new NthExposureDiscountStrategy());
        }

        public void ApplyPromotions(List<ShopDisplayItem> items, bool countExposure)
        {
            if (items == null || items.Count <= 0 || _tableShopPromotion == null) return;

            var saveDataManager = SceneGame.Instance?.saveDataManager;
            var exposureData = saveDataManager?.ShopExposure;
            var purchaseData = saveDataManager?.ShopPurchase;
            if (exposureData == null || purchaseData == null) return;

            foreach (var item in items)
            {
                if (item == null || item.IsEmpty) continue;
                item.SetPromotion(null);

                int currentBoughtCount = purchaseData.GetBoughtCount(item.Uid);
                ShopExposureRecord exposureRecord = countExposure
                    ? exposureData.AddExposure(item.Uid, currentBoughtCount)
                    : null;

                var promotions = _tableShopPromotion.GetItemsByShopItemUid(item.Uid);
                if (promotions == null || promotions.Count <= 0) continue;
                if (exposureRecord == null) continue;

                bool resetExposureCycle = false;
                foreach (var promotion in promotions)
                {
                    if (promotion == null || !promotion.IsEnabled) continue;
                    if (!_strategies.TryGetValue(promotion.StrategyType, out var strategy)) continue;

                    if (strategy.TryApply(
                            item,
                            promotion,
                            exposureRecord,
                            currentBoughtCount,
                            out var result,
                            out var shouldResetExposureCycle))
                    {
                        item.SetPromotion(result);
                        resetExposureCycle |= shouldResetExposureCycle;
                        break;
                    }

                    resetExposureCycle |= shouldResetExposureCycle;
                }

                if (resetExposureCycle)
                {
                    exposureData.ResetCycle(item.Uid, currentBoughtCount);
                }
            }

            if (countExposure)
            {
                exposureData.SaveIfDirty();
            }
        }

        private void Register(IShopPromotionStrategy strategy)
        {
            if (strategy == null) return;
            _strategies[strategy.StrategyType] = strategy;
        }
    }

    public sealed class NthExposureDiscountStrategy : IShopPromotionStrategy
    {
        public ShopPromotionStrategyType StrategyType => ShopPromotionStrategyType.NthExposureDiscount;

        public bool TryApply(
            ShopDisplayItem item,
            StruckTableShopPromotion promotion,
            ShopExposureRecord exposureRecord,
            int currentBoughtCount,
            out ShopPromotionResult result,
            out bool resetExposureCycle)
        {
            result = null;
            resetExposureCycle = false;
            if (item == null || promotion == null || exposureRecord == null) return false;
            if (promotion.DiscountRate <= 0) return false;
            if (exposureRecord.ExposureCount <= promotion.TriggerExposureCount) return false;

            resetExposureCycle = true;
            bool boughtDuringCycle = currentBoughtCount > exposureRecord.BoughtCountAtCycleStart;
            if (boughtDuringCycle) return false;

            int finalCurrencyValue = Mathf.Max(
                0,
                Mathf.CeilToInt(item.BaseCurrencyValue * (100 - promotion.DiscountRate) / 100f));

            result = new ShopPromotionResult
            {
                Source = promotion,
                FinalCurrencyValue = finalCurrencyValue,
                DiscountRate = promotion.DiscountRate,
            };
            return true;
        }
    }
}
