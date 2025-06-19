using System.Collections.Generic;
using UnityEngine;

namespace GGemCo.Scripts
{
    /// <summary>
    /// 장비 착용, 해제 관리
    /// </summary>
    public class EquipController : MonoBehaviour
    {
        private Player player;
        // 현재 장착 중인 아이템
        private readonly Dictionary<int, StruckTableItem> equippedItems = new Dictionary<int, StruckTableItem>();
        
        private delegate void DelegateOnPlayerEquiped(Dictionary<int, StruckTableItem> equippedItems);
        private event DelegateOnPlayerEquiped OnPlayerEquiped;
        private delegate void DelegateOnPlayerUnEquiped(Dictionary<int, StruckTableItem> equippedItems);
        private event DelegateOnPlayerUnEquiped OnPlayerUnEquiped;
        
        TableItem tableItem;

        private void Awake()
        {
            equippedItems.Clear();
            tableItem = TableLoaderManager.Instance.TableItem;
            player = GetComponent<Player>();
            OnPlayerEquiped += player.UpdateStatCache;
            OnPlayerUnEquiped += player.UpdateStatCache;
        }

        /// <summary>
        /// 장비 착용
        /// </summary>
        /// <param name="partIndex">착용 부위</param>
        /// <param name="itemUid"></param>
        public bool EquipItem(int partIndex, int itemUid)
        {
            if (player == null) return false;
            if (itemUid <= 0)
            {
                UnEquipItem(partIndex);
                return true;
            }
            StruckTableItem item = tableItem.GetDataByUid(itemUid);
            if (!equippedItems.TryAdd(partIndex, item))
            {
                equippedItems[partIndex] = item;
            }
            OnPlayerEquiped?.Invoke(equippedItems);
            return true;
        }
        /// <summary>
        /// 장비 해제 
        /// </summary>
        /// <param name="partIndex"></param>
        public bool UnEquipItem(int partIndex)
        {
            if (player == null) return false;
            equippedItems.Remove(partIndex);
            OnPlayerUnEquiped?.Invoke(equippedItems);
            return true;
        }
        /// <summary>
        /// 장착된 모든 아이템 정보 가져오기
        /// </summary>
        /// <returns></returns>
        public Dictionary<int, StruckTableItem> GetEquippedItems()
        {
            return equippedItems;
        }
    }
}