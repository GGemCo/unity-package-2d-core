using GGemCo.Scripts.UI;
using UnityEngine;

namespace GGemCo.Scripts
{
    /// <summary>
    /// 아이템 강화 윈도우 - 아이콘 드래그 앤 드랍 관리
    /// </summary>
    public class DragDropStrategyItemUpgrade : IDragDropStrategy
    {
        public void HandleDragInIcon(UIWindow window, UIIcon droppedUIIcon, UIIcon targetUIIcon)
        {
            UIWindowItemUpgrade uiWindowItemUpgrade = window as UIWindowItemUpgrade;
            if (uiWindowItemUpgrade == null) return;
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

            // 결과 slot 에 드래그 했을 때 return 처리 
            if (targetIconSlotIndex == uiWindowItemUpgrade.GetResultIconSlotIndex())
            {
                return;
            }
            // 인벤토리에서 상점으로 드래그 앤 드랍 했을 때만 처리한다 
            if (droppedWindowUid == UIWindowManager.WindowUid.Inventory && targetIconSlotIndex < uiWindowItemUpgrade.maxCountIcon)
            {
                // 분해할 수 있는 아이템 인지 체크
                if (droppedUIIcon.IsAntiFlag(ItemConstants.AntiFlag.Upgrade))
                {
                    SceneGame.Instance.systemMessageManager.ShowMessageWarning("강화 할 수 없는 아이템 입니다.");
                    return;
                }
                // 기존 register 된 아이콘이 있으면 un register 해주기
                var registerIcon = uiWindowItemUpgrade.GetIconByIndex(uiWindowItemUpgrade.GetSourceIconSlotIndex());
                if (registerIcon != null && registerIcon.uid > 0)
                {
                    SceneGame.Instance.uIWindowManager.UnRegisterIcon(UIWindowManager.WindowUid.ItemUpgrade, 0);
                }
                SceneGame.Instance.uIWindowManager.RegisterIcon(UIWindowManager.WindowUid.Inventory, dropIconSlotIndex, UIWindowManager.WindowUid.ItemUpgrade, 1);
            }
        }

        public void HandleDragOut(UIWindow window, Vector3 worldPosition, GameObject droppedIcon, GameObject targetIcon,
            Vector3 originalPosition)
        {
        }
    }
}