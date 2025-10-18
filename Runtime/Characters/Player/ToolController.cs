using System;
using System.Collections.Generic;
using UnityEngine;

namespace GGemCo2DCore
{
    /// <summary>
    /// 도구 장착 / 해제 관리
    /// 도끼, 곡괭이, 낫, 물뿌리개 등
    /// </summary>
    public class ToolController : MonoBehaviour
    {
        private Player _player;
        private StruckTableItem _currentTool;
        public static event Action<CharacterBase, StruckTableItem> OnPlayerEquipSimulationTool;
        public static event Action<CharacterBase, StruckTableItem> OnPlayerUnEquipSimulationTool;
        
        private TableItem _tableItem;
        private void Awake()
        {
            _currentTool = null;
            _tableItem = TableLoaderManager.Instance.TableItem;
            _player = GetComponent<Player>();
        }
        
        /// <summary>
        /// 도구 착용
        /// </summary>
        /// <param name="itemUid"></param>
        public bool Equip(int itemUid)
        {
            if (_player == null) return false;
            if (itemUid <= 0)
            {
                UnEquip();
                return true;
            }
            StruckTableItem item = _tableItem.GetDataByUid(itemUid);

            if (!item.IsTool() && !item.IsSeed())
            {
                GcLogger.LogError($"tool 이나 seed 아이템이 아닙니다. itemUId: {itemUid}");
                return false;
            }
            _currentTool = item;
            OnPlayerEquipSimulationTool?.Invoke(_player, _currentTool);
            return true;
        }
        /// <summary>
        /// 도구 해제 
        /// </summary>
        public bool UnEquip()
        {
            if (_player == null) return false;
            _currentTool = null;
            
            OnPlayerUnEquipSimulationTool?.Invoke(_player, _currentTool);
            return true;
        }

        public StruckTableItem GetCurrentTool()
        {
            return _currentTool;
        }

        public bool IsEquipSimulationTool()
        {
            if (_currentTool == null) return false;
            return _currentTool.IsTool();
        }
        public bool IsEquipAxe()
        {
            if (_currentTool == null) return false;
            return _currentTool.IsSubCategoryAxe();
        }

        public bool IsEquipHoe()
        {
            if (_currentTool == null) return false;
            return _currentTool.IsSubCategoryHoe();
        }

        public bool IsEquipWatering()
        {
            if (_currentTool == null) return false;
            return _currentTool.IsSubCategoryWatering();
        }
        public bool IsEquipSeed()
        {
            if (_currentTool == null) return false;
            return _currentTool.IsSeed();
        }
    }
}