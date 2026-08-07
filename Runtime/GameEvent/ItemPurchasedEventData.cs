namespace GGemCo2DCore
{
    /// <summary>
    /// 아이템 구매 완료 이벤트에 전달되는 구매 결과 데이터입니다.
    /// </summary>
    public readonly struct ItemPurchasedEventData
    {
        /// <summary>
        /// 실제로 구매한 Item 테이블 UID입니다.
        /// </summary>
        public readonly int ItemUid;

        /// <summary>
        /// 구매에 사용한 shop_item 테이블 UID입니다.
        /// 레거시 구매 경로처럼 상품 행을 식별할 수 없으면 0입니다.
        /// </summary>
        public readonly int ShopItemUid;

        /// <summary>
        /// 구매가 발생한 Shop 테이블 UID입니다.
        /// 레거시 구매 경로처럼 상점을 식별할 수 없으면 0입니다.
        /// </summary>
        public readonly int ShopUid;

        /// <summary>
        /// 구매가 완료된 아이템 수량입니다.
        /// </summary>
        public readonly int Count;

        /// <summary>
        /// 구매에 사용한 재화 타입입니다.
        /// </summary>
        public readonly CurrencyConstants.Type CurrencyType;

        /// <summary>
        /// 아이템 한 개를 구매할 때 사용한 재화 수량입니다.
        /// </summary>
        public readonly int UnitPrice;

        /// <summary>
        /// 해당 구매에서 실제로 사용한 전체 재화 수량입니다.
        /// </summary>
        public readonly long TotalPrice;

        /// <summary>
        /// 구매 후 아이템을 처리한 정책입니다.
        /// </summary>
        public readonly ShopBuyUsePolicy BuyUsePolicy;

        /// <summary>
        /// 아이템 구매 완료 이벤트 데이터를 생성합니다.
        /// </summary>
        /// <param name="itemUid">구매한 Item 테이블 UID입니다.</param>
        /// <param name="count">구매한 아이템 수량입니다.</param>
        /// <param name="shopItemUid">구매에 사용한 shop_item 테이블 UID입니다.</param>
        /// <param name="shopUid">구매가 발생한 Shop 테이블 UID입니다.</param>
        /// <param name="buyUsePolicy">구매 후 아이템 처리 정책입니다.</param>
        /// <param name="currencyType">구매에 사용한 재화 타입입니다.</param>
        /// <param name="unitPrice">아이템 한 개의 구매 가격입니다.</param>
        public ItemPurchasedEventData(
            int itemUid,
            int count,
            int shopItemUid = 0,
            int shopUid = 0,
            ShopBuyUsePolicy buyUsePolicy = ShopBuyUsePolicy.AddToInventory,
            CurrencyConstants.Type currencyType = CurrencyConstants.Type.None,
            int unitPrice = 0)
        {
            ItemUid = itemUid;
            ShopItemUid = shopItemUid;
            ShopUid = shopUid;
            Count = count;
            CurrencyType = currencyType;
            UnitPrice = unitPrice;
            TotalPrice = (long)unitPrice * count;
            BuyUsePolicy = buyUsePolicy;
        }
    }
}
