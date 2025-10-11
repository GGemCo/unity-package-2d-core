namespace GGemCo2DCore
{
    /// <summary>
    /// 시뮬레이션용 퀵슬롯 윈도우 - 아이콘 관리
    /// </summary>
    public class SetIconHandlerQuickSlotSimulation : ISetIconHandler
    {
        public void OnSetIcon(UIWindow window, int slotIndex, int iconUid, int iconCount, int iconLevel, bool isLearned)
        {
            UIIcon icon = window.GetIconByIndex(slotIndex);
            if (icon == null) return;
            SceneGame.Instance.saveDataManager.QuickSlotSimulation.SetSkill(slotIndex, iconUid, iconCount, iconLevel, isLearned);
        }
        public void OnDetachIcon(UIWindow window, int slotIndex)
        {
            UIIcon icon = window.GetIconByIndex(slotIndex);
            if (icon == null) return;
            SceneGame.Instance.saveDataManager.QuickSlotSimulation.RemoveSkill(slotIndex);
        }
    }
}