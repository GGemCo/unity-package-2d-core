using System;
using System.Collections.Generic;
using UnityEngine;

namespace GGemCo2DCore
{
    /// <summary>
    /// 장비 착용, 해제 관리
    /// </summary>
    public class EquipController : MonoBehaviour
    {
        private Player _player;
        // 현재 장착 중인 아이템
        private readonly Dictionary<int, StruckTableItem> _equippedItems = new Dictionary<int, StruckTableItem>();
        
        public static event Action<CharacterBase, Dictionary<int, StruckTableItem>> OnPlayerEquiped;
        public static event Action<CharacterBase, Dictionary<int, StruckTableItem>> OnPlayerUnEquiped;

        private TableItem _tableItem;

        private void Awake()
        {
            _equippedItems.Clear();
            _tableItem = TableLoaderManager.Instance.TableItem;
            _player = GetComponent<Player>();
            OnPlayerEquiped += _player.UpdateStatCache;
            OnPlayerUnEquiped += _player.UpdateStatCache;
        }

        private void OnDestroy()
        {
            OnPlayerEquiped -= _player.UpdateStatCache;
            OnPlayerUnEquiped -= _player.UpdateStatCache;
        }

        /// <summary>
        /// 장비 착용
        /// </summary>
        /// <param name="partIndex">착용 부위</param>
        /// <param name="itemUid"></param>
        public bool EquipItem(int partIndex, int itemUid)
        {
            if (_player == null) return false;
            if (itemUid <= 0)
            {
                UnEquipItem(partIndex);
                return true;
            }
            StruckTableItem item = _tableItem.GetDataByUid(itemUid);
            if (!_equippedItems.TryAdd(partIndex, item))
            {
                _equippedItems[partIndex] = item;
            }
            OnPlayerEquiped?.Invoke(_player, _equippedItems);
            return true;
        }
        /// <summary>
        /// 장비 해제 
        /// </summary>
        /// <param name="partIndex"></param>
        public bool UnEquipItem(int partIndex)
        {
            if (_player == null) return false;
            _equippedItems.Remove(partIndex);
            OnPlayerUnEquiped?.Invoke(_player, _equippedItems);
            return true;
        }
        /// <summary>
        /// 장착된 모든 아이템 정보 가져오기
        /// </summary>
        /// <returns></returns>
        public Dictionary<int, StruckTableItem> GetEquippedItems()
        {
            return _equippedItems;
        }
    }
}