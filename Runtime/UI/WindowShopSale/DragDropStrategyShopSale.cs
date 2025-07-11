﻿using UnityEngine;

namespace GGemCo2DCore
{
    /// <summary>
    /// 아이템 판매 윈도우 - 아이콘 드래그 앤 드랍 관리
    /// </summary>
    public class DragDropStrategyShopSale : IDragDropStrategy
    {
        public void HandleDragInIcon(UIWindow window, UIIcon droppedUIIcon, UIIcon targetUIIcon)
        {
            UIWindowShopSale uiwindowShopSale = window as UIWindowShopSale;
            if (uiwindowShopSale == null) return;
            // UIWindow droppedWindow = droppedUIIcon.window;
            UIWindowConstants.WindowUid droppedWindowUid = droppedUIIcon.windowUid;
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
            // UIWindow targetWindow = targetUIIcon.window;
            // UIWindowConstants.WindowUid targetWindowUid = targetUIIcon.windowUid;
            int targetIconSlotIndex = targetUIIcon.slotIndex;
            // int targetIconUid = targetUIIcon.uid;
            // int targetIconCount = targetUIIcon.GetCount();

            // 인벤토리에서 상점으로 드래그 앤 드랍 했을 때만 처리한다 
            if (droppedWindowUid == UIWindowConstants.WindowUid.Inventory && targetIconSlotIndex < uiwindowShopSale.maxCountIcon)
            {
                // 판매할 수 있는 아이템 인지 체크
                if (droppedUIIcon.IsAntiFlag(ItemConstants.AntiFlag.ShopSale))
                {
                    SceneGame.Instance.systemMessageManager.ShowMessageWarning("This item cannot be sold.");//"해당 아이템은 판매할 수 없는 아이템 입니다."
                }
                else
                {
                    SceneGame.Instance.uIWindowManager.RegisterIcon(droppedWindowUid, dropIconSlotIndex,
                        UIWindowConstants.WindowUid.ShopSale, dropIconCount);
                }
            }
        }

        public void HandleDragOut(UIWindow window, Vector3 worldPosition, GameObject droppedIcon, GameObject targetIcon,
            Vector3 originalPosition)
        {
        }
    }
}