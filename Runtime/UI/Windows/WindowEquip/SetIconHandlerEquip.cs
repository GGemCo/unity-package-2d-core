namespace GGemCo2DCore
{
    /// <summary>
    /// 플레이어 장비 윈도우 - 아이콘 관리
    /// </summary>
    public class SetIconHandlerEquip : ISetIconHandler
    {
        public void OnSetIcon(UIWindow window, int slotIndex, int iconUid, int iconCount, int iconLevel, bool isLearned, IconConstants.Type iconType)
        {
            UIIcon icon = window.GetIconByIndex(slotIndex);
            if (icon == null) return;

            // UIIcon에 저장된 instanceId를 함께 저장한다.
            SceneGame.Instance.saveDataManager.Equip.SetItemCount(slotIndex, iconUid, iconCount, icon.instanceId);

            if (SceneGame.Instance.player)
                SceneGame.Instance.player.GetComponent<Player>().EquipItem(slotIndex, iconUid, iconCount, icon.instanceId);
        }
        public void OnDetachIcon(UIWindow window, int slotIndex)
        {
            UIIcon icon = window.GetIconByIndex(slotIndex);
            if (icon == null) return;
            SceneGame.Instance.saveDataManager.Equip.RemoveItemCount(slotIndex);
            if (SceneGame.Instance.player)
                SceneGame.Instance.player.GetComponent<Player>().UnEquipItem(slotIndex);
        }
    }
}