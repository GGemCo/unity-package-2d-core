namespace GGemCo2DCore
{
    /// <summary>
    /// Resolved item displayed in the shop UI.
    /// </summary>
    public sealed class ShopDisplayItem
    {
        public StruckTableShop Source { get; }
        public int ShopUid => Source?.Uid ?? 0;
        public int SlotIndex => Source?.SlotIndex ?? 0;
        public int ItemUid => Source?.ItemUid ?? 0;
        public CurrencyConstants.Type CurrencyType => Source?.CurrencyType ?? CurrencyConstants.Type.None;
        public int CurrencyValue => Source?.CurrencyValue ?? 0;
        public int MaxBuyCount => Source?.MaxBuyCount ?? 0;
        public bool IsBuyable { get; private set; } = true;
        public string DisabledReason { get; private set; }

        public ShopDisplayItem(StruckTableShop source)
        {
            Source = source;
        }

        public void SetAvailability(bool isBuyable, string disabledReason = null)
        {
            IsBuyable = isBuyable;
            DisabledReason = disabledReason;
        }
    }
}
