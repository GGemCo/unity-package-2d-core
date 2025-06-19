using System.Collections.Generic;

namespace GGemCo.Scripts
{
    /// <summary>
    /// 세이브 데이터 - 장착 아이템 정보
    /// </summary>
    public class EquipData : ItemStorageData
    {
        public void Initialize(TableLoaderManager loader, SaveDataContainer saveDataContainer = null)
        {
            ItemCounts.Clear();
            if (saveDataContainer?.EquipData != null)
            {
                ItemCounts = new Dictionary<int, SaveDataIcon>(saveDataContainer.EquipData.ItemCounts);
            }
        }

        protected override int GetMaxSlotCount()
        {
            return SceneGame.Instance.uIWindowManager
                .GetUIWindowByUid<UIWindowEquip>(UIWindowManager.WindowUid.Equip)?.maxCountIcon ?? 0;
        }
    }
}