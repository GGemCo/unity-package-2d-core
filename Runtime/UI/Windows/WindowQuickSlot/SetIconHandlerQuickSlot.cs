namespace GGemCo2DCore
{
    /// <summary>
    /// 퀵슬롯 윈도우 - 아이콘 관리
    /// </summary>
    public class SetIconHandlerQuickSlot : ISetIconHandler
    {
        public void OnSetIcon(UIWindow window, int slotIndex, int iconUid, int iconCount, int iconLevel, bool isLearned)
        {
            UIIcon icon = window.GetIconByIndex(slotIndex);
            if (icon == null) return;
            var type = icon.GetIconType();
            if (type == IconConstants.Type.Item)
            {
                SceneGame.Instance.saveDataManager.QuickSlot.SetItem(slotIndex, iconUid, iconCount, icon.instanceId);
            }
            else
            {
                // 기본은 Skill 로 저장 (기존 동작)
                SceneGame.Instance.saveDataManager.QuickSlot.SetSkill(slotIndex, iconUid, iconCount, iconLevel, isLearned);
            }
        }
        public void OnDetachIcon(UIWindow window, int slotIndex)
        {
            UIIcon icon = window.GetIconByIndex(slotIndex);
            if (icon == null) return;
            SceneGame.Instance.saveDataManager.QuickSlot.Remove(slotIndex);
        }
    }
}