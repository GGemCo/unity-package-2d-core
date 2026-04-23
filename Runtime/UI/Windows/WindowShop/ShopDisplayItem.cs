namespace GGemCo2DCore
{
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
        public CurrencyConstants.Type CurrencyType => Source?.CurrencyType ?? CurrencyConstants.Type.None;
        public int CurrencyValue => Source?.CurrencyValue ?? 0;
        public int MaxBuyCount => Source?.MaxBuyCount ?? 0;
        public int PurchaseLimitCount => Source?.PurchaseLimitCount ?? 0;
        public ShopSoldOutDisplayType SoldOutDisplayType => Source?.SoldOutDisplayType ?? ShopSoldOutDisplayType.Disable;
        public bool IsBuyable { get; private set; } = true;
        public string DisabledReason { get; private set; }

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
    }
}
