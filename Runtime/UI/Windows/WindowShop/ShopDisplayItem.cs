namespace GGemCo2DCore
{
    /// <summary>
    /// 상점 상품의 구매 불가 사유로 사용하는 공통 키입니다.
    /// </summary>
    internal static class ShopAvailabilityReason
    {
        internal const string SoldOut = "Shop_SoldOut";
    }

    /// <summary>
    /// Resolved item displayed in the shop UI.
    /// </summary>
    public sealed class ShopDisplayItem
    {
        public StruckTableShopItem Source { get; }
        public int Uid => Source?.Uid ?? 0;
        public int ShopUid => Source?.ShopUid ?? 0;
        public int SlotIndex => Source?.SlotIndex ?? 0;
        public int ItemUid => Source?.ItemUid ?? 0;
        public bool IsEmpty => ItemUid <= 0;

        /// <summary>
        /// 실제 구매 재고가 모두 소진된 상품인지 여부입니다.
        /// 기타 구매 제한 상태와 품절 표시를 구분하기 위해 구매 불가 사유를 함께 확인합니다.
        /// </summary>
        public bool IsSoldOut => DisabledReason == ShopAvailabilityReason.SoldOut;

        public CurrencyConstants.Type CurrencyType => Source?.CurrencyType ?? CurrencyConstants.Type.None;
        public int BaseCurrencyValue => Source?.CurrencyValue ?? 0;
        public int CurrencyValue => Promotion?.FinalCurrencyValue ?? BaseCurrencyValue;
        public bool HasDiscount => Promotion != null && CurrencyValue < BaseCurrencyValue;
        public int MaxBuyCount => Source?.MaxBuyCount ?? 0;
        public int PurchaseLimitCount => Source?.PurchaseLimitCount ?? 0;
        public ShopSoldOutDisplayType SoldOutDisplayType => Source?.SoldOutDisplayType ?? ShopSoldOutDisplayType.Disable;

        /// <summary>
        /// 품절된 상품이 다음 슬롯 추첨에 참여할지 결정하는 정책입니다.
        /// </summary>
        public ShopSoldOutRollPolicy SoldOutRollPolicy =>
            Source?.SoldOutRollPolicy ?? ShopSoldOutRollPolicy.KeepCandidate;

        /// <summary>
        /// 구매 성공 후 아이템을 인벤토리에 넣을지, 즉시 사용할지 결정하는 정책입니다.
        /// </summary>
        public ShopBuyUsePolicy BuyUsePolicy => Source?.BuyUsePolicy ?? ShopBuyUsePolicy.AddToInventory;
        public bool IsBuyable { get; private set; } = true;
        public string DisabledReason { get; private set; }
        public ShopPromotionResult Promotion { get; private set; }

        public ShopDisplayItem(StruckTableShopItem source)
        {
            Source = source;
        }

        public ShopDisplayItem(StruckTableShop source)
            : this(StruckTableShopItem.FromLegacyShopRow(source))
        {
        }

        public void SetAvailability(bool isBuyable, string disabledReason = null)
        {
            IsBuyable = isBuyable;
            DisabledReason = disabledReason;
        }

        public void SetPromotion(ShopPromotionResult promotion)
        {
            Promotion = promotion;
        }
    }
}
