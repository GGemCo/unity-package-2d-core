namespace GGemCo2DCore
{
    /// <summary>
    /// 퀵슬롯 윈도우 - 아이콘 관리
    /// </summary>
    public class SetIconHandlerQuickSlot : ISetIconHandler
    {
        public void OnSetIcon(UIWindow window, int slotIndex, int iconUid, int iconCount, int iconLevel, bool isLearned, IconConstants.Type iconType)
        {
            UIIcon icon = window.GetIconByIndex(slotIndex);
            if (icon == null) return;

            // 퀵슬롯 아이템은 인벤토리 인스턴스를 다시 찾을 수 있어야 하므로 instanceId 도 함께 저장합니다.
            SceneGame.Instance.saveDataManager.QuickSlot.SetIcon(slotIndex, iconUid, iconCount, iconLevel, isLearned,
                iconType, icon.instanceId);
        }
        public void OnDetachIcon(UIWindow window, int slotIndex)
        {
            UIIcon icon = window.GetIconByIndex(slotIndex);
            if (icon == null) return;
            SceneGame.Instance.saveDataManager.QuickSlot.Remove(slotIndex);
        }
    }
}
