using GGemCo.Scripts.UI;
using UnityEngine;

namespace GGemCo.Scripts
{
    /// <summary>
    /// 플레이어 장비 윈도우 - 아이콘 드래그 앤 드랍 관리
    /// </summary>
    public class DragDropStrategyEquip : IDragDropStrategy
    {
        public void HandleDragInIcon(UIWindow window, UIIcon droppedUIIcon, UIIcon targetUIIcon)
        {
            UIWindowEquip uiWindowEquip = window as UIWindowEquip;
            if (uiWindowEquip == null) return;
            UIIconItem uiIconItem = droppedUIIcon as UIIconItem;
            if (uiIconItem == null) return;
            UIWindow droppedWindow = droppedUIIcon.window;
            UIWindowManager.WindowUid droppedWindowUid = droppedUIIcon.windowUid;
            int dropIconSlotIndex = droppedUIIcon.slotIndex;
            int dropIconUid = droppedUIIcon.uid;
            int dropIconCount = droppedUIIcon.GetCount();
            if (dropIconUid <= 0)
            {
                return;
            }
            
            // 드래그앤 드랍 한 곳에 아무것도 없을때 
            if (targetUIIcon == null)
            {
                return;
            }
            UIWindow targetWindow = targetUIIcon.window;
            UIWindowManager.WindowUid targetWindowUid = targetUIIcon.windowUid;
            int targetIconSlotIndex = targetUIIcon.slotIndex;
            int targetIconUid = targetUIIcon.uid;
            int targetIconCount = targetUIIcon.GetCount();

            if (targetIconSlotIndex < window.maxCountIcon)
            {
                // 장착 아이템 인지 체크
                // 착용할 수 있는 부위인지 체크
                if (uiIconItem.IsTypeEquip() && uiIconItem.IsEquipParts(targetIconSlotIndex))
                {
                    var result = uiWindowEquip.InventoryData.MinusItem(dropIconSlotIndex, dropIconUid, 1);
                    droppedWindow.SetIcons(result);
                        
                    // 장착된 아이템이 있을 때
                    if (targetIconUid > 0)
                    {
                        result = uiWindowEquip.EquipData.MinusItem(targetIconSlotIndex, targetIconUid, 1);
                        targetWindow.SetIcons(result);
                        
                        result = uiWindowEquip.InventoryData.AddItem(dropIconSlotIndex, targetIconUid, 1);
                        droppedWindow.SetIcons(result);
                    }
                    result = uiWindowEquip.EquipData.AddItem(targetIconSlotIndex, dropIconUid, 1);
                    targetWindow.SetIcons(result);
                }
                else
                {
                    SceneGame.Instance.systemMessageManager.ShowMessageWarning("해당 슬롯에는 착용할 수 없는 아이템 입니다.");
                }
            }
        }

        public void HandleDragOut(UIWindow window, Vector3 worldPosition, GameObject droppedIcon, GameObject targetIcon,
            Vector3 originalPosition)
        {
        }
    }
}