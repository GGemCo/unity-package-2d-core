namespace GGemCo.Scripts
{
    /// <summary>
    /// 아이템 나누기 윈도우 - 아이콘 관리
    /// </summary>
    public class SetIconHandlerItemSplit : ISetIconHandler
    {
        public void OnSetIcon(UIWindow window, int slotIndex, int iconUid, int iconCount, int iconLevel, bool isLearned)
        {
            UIWindowItemSplit uiWindowItemSplit = window as UIWindowItemSplit;
            if (uiWindowItemSplit == null) return;
            uiWindowItemSplit.UpdateInfo(iconUid, iconCount);
        }
        public void OnDetachIcon(UIWindow window, int slotIndex)
        {
        }
    }
}