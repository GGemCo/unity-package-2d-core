
namespace GGemCo.Scripts
{
    /// <summary>
    /// 아이템 분해하기 윈도우 - 아이콘 관리
    /// </summary>
    public class SetIconHandlerItemSalvage : ISetIconHandler
    {
        public void OnSetIcon(UIWindow window, int slotIndex, int iconUid, int iconCount, int iconLevel, bool isLearned)
        {
            UIIcon icon = window.GetIconByIndex(slotIndex);
            if (icon != null)
            {
                icon.SetDrag(false);
            }
            UIWindowItemSalvage uiWindowItemSalvage = window as UIWindowItemSalvage;
            if (uiWindowItemSalvage == null) return;
            uiWindowItemSalvage.UpdateResultInfos();
        }
        public void OnDetachIcon(UIWindow window, int slotIndex)
        {
            UIWindowItemSalvage uiWindowItemSalvage = window as UIWindowItemSalvage;
            if (uiWindowItemSalvage == null) return;
            uiWindowItemSalvage.UpdateResultInfos();
        }
    }
}