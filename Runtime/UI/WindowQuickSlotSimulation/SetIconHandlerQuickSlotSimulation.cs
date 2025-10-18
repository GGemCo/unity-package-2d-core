
namespace GGemCo2DCore
{
    /// <summary>
    /// 시뮬레이션용 퀵슬롯 윈도우 - 아이콘 관리
    /// </summary>
    public class SetIconHandlerQuickSlotSimulation : ISetIconHandler
    {
        public void OnSetIcon(UIWindow window, int slotIndex, int iconUid, int iconCount, int iconLevel, bool isLearned)
        {
            var quickSlotSimulationData = SceneGame.Instance.saveDataManager.QuickSlotSimulation;
            UIIcon icon = window.GetIconByIndex(slotIndex);
            if (icon == null) return;
            quickSlotSimulationData.SetItemCount(slotIndex, icon.uid, icon.GetCount());
        }

        public void OnDetachIcon(UIWindow window, int slotIndex)
        {
            var quickSlotSimulationData = SceneGame.Instance.saveDataManager.QuickSlotSimulation;
            UIIcon icon = window.GetIconByIndex(slotIndex);
            if (icon == null) return;
            // 아이콘 정보 지워주기
            quickSlotSimulationData.RemoveItemCount(slotIndex);
            
            if (!icon.IsSelected()) return;
            // 선택되어있던 툴일 경우
            // 선택 표시 지워주기
            window.RemoveSelectedIcon();
            // 툴 장착 해제하기
            var player = SceneGame.Instance.player?.GetComponent<Player>();
            if (player == null) return;
            player.UnEquipTool();
        }
    }
}