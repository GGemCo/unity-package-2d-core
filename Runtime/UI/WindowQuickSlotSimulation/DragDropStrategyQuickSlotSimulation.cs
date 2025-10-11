using System;
using UnityEngine;

namespace GGemCo2DCore
{
    /// <summary>
    /// 시뮬레이션용 퀵슬롯 윈도우 - 아이콘 드래그 앤 드랍 관리
    /// </summary>
    public class DragDropStrategyQuickSlotSimulation : IDragDropStrategy
    {
        public void HandleDragInIcon(UIWindow window, UIIcon droppedUIIcon, UIIcon targetUIIcon)
        {
            UIWindowQuickSlotSimulation uiWindowQuickSlotSimulation = window as UIWindowQuickSlotSimulation;
            if (uiWindowQuickSlotSimulation == null) return;
            UIWindow droppedWindow = droppedUIIcon.window;
            UIWindowConstants.WindowUid droppedWindowUid = droppedUIIcon.windowUid;
            int dropIconSlotIndex = droppedUIIcon.slotIndex;
            int dropIconUid = droppedUIIcon.uid;
            int dropIconCount = droppedUIIcon.GetCount();
            int dropIconLevel = droppedUIIcon.GetLevel();
            bool dropIconIsLearn = droppedUIIcon.IsLearn();
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
            UIWindowConstants.WindowUid targetWindowUid = targetUIIcon.windowUid;
            int targetIconSlotIndex = targetUIIcon.slotIndex;
            int targetIconUid = targetUIIcon.uid;
            int targetIconCount = targetUIIcon.GetCount();

            if (droppedWindowUid != targetWindowUid)
            {
                switch (droppedWindowUid)
                {
                    case UIWindowConstants.WindowUid.Inventory:
                        SceneGame.Instance.uIWindowManager.LinkIcon(droppedWindowUid, dropIconSlotIndex, targetWindowUid, dropIconCount, targetIconSlotIndex);
                        break;
                    case UIWindowConstants.WindowUid.Skill:
                    case UIWindowConstants.WindowUid.None:
                    case UIWindowConstants.WindowUid.Hud:
                    case UIWindowConstants.WindowUid.ItemInfo:
                    case UIWindowConstants.WindowUid.Equip:
                    case UIWindowConstants.WindowUid.PlayerInfo:
                    case UIWindowConstants.WindowUid.ItemSplit:
                    case UIWindowConstants.WindowUid.PlayerBuffInfo:
                    case UIWindowConstants.WindowUid.QuickSlot:
                    case UIWindowConstants.WindowUid.SkillInfo:
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
            else
            {
                if (targetIconSlotIndex < window.maxCountIcon)
                {
                }
            }
        }

        public void HandleDragOut(UIWindow window, Vector3 worldPosition, GameObject droppedIcon, GameObject targetIcon,
            Vector3 originalPosition)
        {
            window.DetachIcon(droppedIcon.GetComponent<UIIcon>().slotIndex);
        }
    }
}