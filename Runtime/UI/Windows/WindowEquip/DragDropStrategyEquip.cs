using UnityEngine;

namespace GGemCo2DCore
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
            UIWindow droppedWindow = droppedUIIcon.window;
            // UIWindowConstants.WindowUid droppedWindowUid = droppedUIIcon.windowUid;
            int dropIconSlotIndex = droppedUIIcon.slotIndex;
            int dropIconUid = droppedUIIcon.uid;
            long dropIconInstanceId = droppedUIIcon.instanceId;
            // int dropIconCount = droppedUIIcon.GetCount();
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
            // UIWindowConstants.WindowUid targetWindowUid = targetUIIcon.windowUid;
            int targetIconSlotIndex = targetUIIcon.slotIndex;
            int targetIconUid = targetUIIcon.uid;
            long targetIconInstanceId = targetUIIcon.instanceId;
            // int targetIconCount = targetUIIcon.GetCount();

            if (targetIconSlotIndex < window.maxCountIcon)
            {
                // 장비 슬롯은 공통 규칙을 먼저 통과한 뒤, 실제 swap 가능 여부를 추가로 확인합니다.
                if (!UISlotPlacementValidator.CanSwap(droppedUIIcon, targetUIIcon, out var failMessageKey))
                {
                    window.ShowSlotAcceptFailure(failMessageKey);
                    droppedUIIcon.HandleInvalidEffect();
                    return;
                }

                var result = uiWindowEquip.InventoryData.MinusItem(dropIconSlotIndex, dropIconUid, 1);
                droppedWindow.SetIcons(result);
                    
                // 장착된 아이템이 있을 때는 인벤토리 원래 자리로 되돌립니다.
                if (targetIconUid > 0)
                {
                    result = uiWindowEquip.EquipData.MinusItem(targetIconSlotIndex, targetIconUid, 1);
                    targetWindow.SetIcons(result);
                    
                    result = uiWindowEquip.InventoryData.AddItem(dropIconSlotIndex, new IconPayload(targetIconUid, 1, targetIconInstanceId));
                    droppedWindow.SetIcons(result);
                }

                result = uiWindowEquip.EquipData.AddItem(targetIconSlotIndex, new IconPayload(dropIconUid, 1, dropIconInstanceId));
                targetWindow.SetIcons(result);
            }
        }

        public void HandleDragOut(UIWindow window, Vector3 worldPosition, GameObject droppedIcon, GameObject targetIcon,
            Vector3 originalPosition)
        {
        }
    }
}
