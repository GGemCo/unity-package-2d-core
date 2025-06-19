using GGemCo.Scripts.UI;
using UnityEngine;

namespace GGemCo.Scripts
{
    /// <summary>
    /// 아이템 분해 윈도우 - 아이콘 드래그 앤 드랍 관리
    /// </summary>
    public class DragDropStrategyItemSalvage : IDragDropStrategy
    {
        public void HandleDragInIcon(UIWindow window, UIIcon droppedUIIcon, UIIcon targetUIIcon)
        {
            UIWindowItemSalvage uiWindowItemSalvage = window as UIWindowItemSalvage;
            if (uiWindowItemSalvage == null) return;
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

            // 분해 결과 slot 에 드래그 했을 때 return 처리 
            if (targetIconSlotIndex >= uiWindowItemSalvage.salvageIconCount)
            {
                return;
            }
            
            // 인벤토리에서 상점으로 드래그 앤 드랍 했을 때만 처리한다 
            if (droppedWindowUid == UIWindowManager.WindowUid.Inventory && targetIconSlotIndex < uiWindowItemSalvage.maxCountIcon)
            {
                // 분해할 수 있는 아이템 인지 체크
                if (droppedUIIcon.IsAntiFlag(ItemConstants.AntiFlag.Salvage))
                {
                    SceneGame.Instance.systemMessageManager.ShowMessageWarning("분해할 수 없는 아이템 입니다.");
                    return;
                }
                // 분해 할 수 있는 개수가 넘어가지 않았는지 체크
                if (uiWindowItemSalvage.CheckSalvagePossibleCount() == false)
                {
                    SceneGame.Instance.systemMessageManager.ShowMessageWarning("더 이상 아이템을 등록할 수 없습니다.");
                    return;
                }
                SceneGame.Instance.uIWindowManager.RegisterIcon(droppedWindowUid, dropIconSlotIndex,
                    UIWindowManager.WindowUid.ItemSalvage, dropIconCount);
            }
        }

        public void HandleDragOut(UIWindow window, Vector3 worldPosition, GameObject droppedIcon, GameObject targetIcon,
            Vector3 originalPosition)
        {
        }
    }
}