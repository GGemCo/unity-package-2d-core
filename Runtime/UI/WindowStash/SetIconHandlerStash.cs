namespace GGemCo.Scripts
{
    /// <summary>
    /// 창고 윈도우 - 아이콘 관리
    /// </summary>
    public class SetIconHandlerStash : ISetIconHandler
    {
        public void OnSetIcon(UIWindow window, int slotIndex, int iconUid, int iconCount, int iconLevel, bool isLearned)
        {
            UIIcon icon = window.GetIconByIndex(slotIndex);
            if (icon != null)
            {
                SceneGame.Instance.saveDataManager.Stash.SetItemCount(slotIndex, iconUid, iconCount);
            }
        }
        public void OnDetachIcon(UIWindow window, int slotIndex)
        {
            SceneGame.Instance.saveDataManager.Stash.RemoveItemCount(slotIndex);
        }
    }
}