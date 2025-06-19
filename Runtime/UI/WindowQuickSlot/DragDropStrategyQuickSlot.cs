using System;
using GGemCo.Scripts.UI;
using UnityEngine;

namespace GGemCo.Scripts
{
    /// <summary>
    /// 퀵슬롯 윈도우 - 아이콘 드래그 앤 드랍 관리
    /// </summary>
    public class DragDropStrategyQuickSlot : IDragDropStrategy
    {
        public void HandleDragInIcon(UIWindow window, UIIcon droppedUIIcon, UIIcon targetUIIcon)
        {
            UIWindowQuickSlot uiWindowQuickSlot = window as UIWindowQuickSlot;
            if (uiWindowQuickSlot == null) return;
            UIWindow droppedWindow = droppedUIIcon.window;
            UIWindowManager.WindowUid droppedWindowUid = droppedUIIcon.windowUid;
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
            UIWindowManager.WindowUid targetWindowUid = targetUIIcon.windowUid;
            int targetIconSlotIndex = targetUIIcon.slotIndex;
            int targetIconUid = targetUIIcon.uid;
            int targetIconCount = targetUIIcon.GetCount();

            // 다른 윈도우에서 Skill로 드래그 앤 드랍 했을 때 
            if (droppedWindowUid != targetWindowUid)
            {
                switch (droppedWindowUid)
                {
                    case UIWindowManager.WindowUid.Skill:
                        UIWindowSkill uiWindowSkill = droppedWindow as UIWindowSkill;
                        if (uiWindowSkill == null) return;
                        uiWindowSkill.AddToQuickSlot(droppedUIIcon);
                        // if (droppedUIIcon.CheckRequireLevel())
                        // {
                        //     targetWindow.SetIconCount(targetIconSlotIndex, dropIconUid, dropIconCount, dropIconLevel, dropIconIsLearn);
                        // }

                        break;
                    case UIWindowManager.WindowUid.None:
                    case UIWindowManager.WindowUid.Hud:
                    case UIWindowManager.WindowUid.Inventory:
                    case UIWindowManager.WindowUid.ItemInfo:
                    case UIWindowManager.WindowUid.Equip:
                    case UIWindowManager.WindowUid.PlayerInfo:
                    case UIWindowManager.WindowUid.ItemSplit:
                    case UIWindowManager.WindowUid.PlayerBuffInfo:
                    case UIWindowManager.WindowUid.QuickSlot:
                    case UIWindowManager.WindowUid.SkillInfo:
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