using System;
using UnityEngine;

namespace GGemCo2DCore
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

            // 다른 윈도우에서 Skill로 드래그 앤 드랍 했을 때 
            if (droppedWindowUid != targetWindowUid)
            {
                switch (droppedWindowUid)
                {
                    case UIWindowConstants.WindowUid.Skill:
                    {
                        // Skill 패키지 타입을 직접 참조하지 않고, UIIcon 의 공용 정보(uid/level/isLearn)만 저장한다.
                        ApplyToQuickSlot(uiWindowQuickSlot, targetIconSlotIndex,
                            QuickSlotContentKind.Skill, dropIconUid, dropIconCount, dropIconLevel, dropIconIsLearn, droppedUIIcon.instanceId);
                        break;
                    }
                    case UIWindowConstants.WindowUid.Inventory:
                    {
                        // 인벤토리 아이템 → 퀵슬롯
                        ApplyToQuickSlot(uiWindowQuickSlot, targetIconSlotIndex,
                            QuickSlotContentKind.Item, dropIconUid, dropIconCount, 0, false, droppedUIIcon.instanceId);
                        break;
                    }

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


        private static void ApplyToQuickSlot(
            UIWindowQuickSlot window,
            int targetSlotIndex,
            QuickSlotContentKind kind,
            int uid,
            int count,
            int level,
            bool isLearn,
            long instanceId)
        {
            if (window == null) return;

            // 1) 아이콘(UI) 반영
            var icon = window.GetIconByIndex(targetSlotIndex);
            if (icon is UIIconQuickSlot quickSlotIcon)
            {
                if (kind == QuickSlotContentKind.None || uid <= 0 || count <= 0)
                    quickSlotIcon.ClearEntry();
                else
                    quickSlotIcon.ApplyEntry(kind, uid, count, level, isLearn, instanceId);
            }
            else
            {
                // 구버전 아이콘 프리팹 호환
                icon?.ChangeInfoByUid(uid, count, level, isLearn, 0, instanceId);
            }

            // 2) 저장 반영 (Core는 Skill 패키지를 몰라도 됨)
            var quickSlot = SceneGame.Instance?.saveDataManager?.QuickSlot;
            if (quickSlot == null) return;

            switch (kind)
            {
                case QuickSlotContentKind.Skill:
                    quickSlot.SetSkill(targetSlotIndex, uid, count, level, isLearn);
                    break;
                case QuickSlotContentKind.Item:
                    quickSlot.SetItem(targetSlotIndex, uid, count, instanceId);
                    break;
                default:
                    quickSlot.Remove(targetSlotIndex);
                    break;
            }
        }
    }
}