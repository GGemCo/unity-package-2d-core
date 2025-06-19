namespace GGemCo.Scripts
{
    /// <summary>
    /// 아이템 판매 윈도우 - 아이콘 관리
    /// </summary>
    public class SetIconHandlerShopSale : ISetIconHandler
    {
        public void OnSetIcon(UIWindow window, int slotIndex, int iconUid, int iconCount, int iconLevel, bool isLearned)
        {
            UIIcon icon = window.GetIconByIndex(slotIndex);
            if (icon != null)
            {
                icon.SetDrag(false);
            }
            UIWindowShopSale uiWindowShopSale = window as UIWindowShopSale;
            uiWindowShopSale?.ReCalculateSaleItem();
        }
        public void OnDetachIcon(UIWindow window, int slotIndex)
        {
            UIWindowShopSale uiWindowShopSale = window as UIWindowShopSale;
            uiWindowShopSale?.ReCalculateSaleItem();
        }
    }
}