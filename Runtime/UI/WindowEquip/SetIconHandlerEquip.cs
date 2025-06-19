namespace GGemCo.Scripts
{
    /// <summary>
    /// 플레이어 장비 윈도우 - 아이콘 관리
    /// </summary>
    public class SetIconHandlerEquip : ISetIconHandler
    {
        public void OnSetIcon(UIWindow window, int slotIndex, int iconUid, int iconCount, int iconLevel, bool isLearned)
        {
            UIIcon icon = window.GetIconByIndex(slotIndex);
            if (icon == null) return;
            
            SceneGame.Instance.player.GetComponent<Player>().EquipItem(slotIndex, iconUid, iconCount);
            SceneGame.Instance.saveDataManager.Equip.SetItemCount(slotIndex, iconUid, iconCount);
        }
        public void OnDetachIcon(UIWindow window, int slotIndex)
        {
            UIIcon icon = window.GetIconByIndex(slotIndex);
            if (icon == null) return;
            SceneGame.Instance.player.GetComponent<Player>().UnEquipItem(slotIndex);
            SceneGame.Instance.saveDataManager.Equip.RemoveItemCount(slotIndex);
        }
    }
}