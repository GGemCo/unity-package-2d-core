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
            long dropIconInstanceId = droppedUIIcon.instanceId;
            IconConstants.Type dropIconType = droppedUIIcon.GetIconType();
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
            int targetIconLevel = targetUIIcon.GetLevel();
            bool targetIconIsLearn = targetUIIcon.IsLearn();
            long targetIconInstanceId = targetUIIcon.instanceId;
            IconConstants.Type targetIconType = targetUIIcon.GetIconType();

            // 다른 윈도우에서 Skill로 드래그 앤 드랍 했을 때 
            if (droppedWindowUid != targetWindowUid)
            {
                if (QuickSlotDragStrategyRegistry.TryGet(droppedUIIcon.windowUid, out var strategy))
                {
                    strategy.HandleDragInIcon(window, droppedUIIcon, targetUIIcon);
                    return;
                }
                switch (droppedWindowUid)
                {
                    case UIWindowConstants.WindowUid.Skill:
                    case UIWindowConstants.WindowUid.SkillPassive:
                    case UIWindowConstants.WindowUid.Inventory:
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
                    // 퀵슬롯 내부 이동은 실제 slot 데이터 교체이므로 양방향 배치 가능 여부를 확인합니다.
                    if (!UISlotPlacementValidator.CanSwap(droppedUIIcon, targetUIIcon, out var failMessageKey))
                    {
                        window.ShowSlotAcceptFailure(failMessageKey);
                        droppedUIIcon.HandleInvalidEffect();
                        return;
                    }

                    if (targetIconUid <= 0)
                    {
                        droppedWindow.DetachIcon(dropIconSlotIndex);
                        targetWindow.SetIconCount(targetIconSlotIndex, dropIconUid, dropIconCount, dropIconLevel,
                            dropIconIsLearn, dropIconInstanceId, dropIconType);
                        return;
                    }

                    droppedWindow.SetIconCount(dropIconSlotIndex, targetIconUid, targetIconCount, targetIconLevel,
                        targetIconIsLearn, targetIconInstanceId, targetIconType);
                    targetWindow.SetIconCount(targetIconSlotIndex, dropIconUid, dropIconCount, dropIconLevel,
                        dropIconIsLearn, dropIconInstanceId, dropIconType);
                }
            }
        }

        public void HandleDragOut(UIWindow window, Vector3 worldPosition, GameObject droppedIcon, GameObject targetIcon,
            Vector3 originalPosition)
        {
            var droppedUIIcon = droppedIcon.GetComponent<UIIcon>();
            if (!droppedUIIcon) return;

            UIWindowConstants.WindowUid windowUid = UIWindowConstants.WindowUid.None;
            var iconType = droppedUIIcon.GetIconType();
            switch (iconType)
            {
                case IconConstants.Type.Item:
                    windowUid = UIWindowConstants.WindowUid.Inventory;
                    break;
                case IconConstants.Type.Skill:
                    windowUid = UIWindowConstants.WindowUid.Skill;
                    break;
                case IconConstants.Type.SkillPassive:
                    windowUid = UIWindowConstants.WindowUid.SkillPassive;
                    break;
                default:
                    break;
            }
            // 아이콘 타입을 위에서 가져온 후 삭제
            window.DetachIcon(droppedUIIcon.slotIndex);
            if (!QuickSlotDragStrategyRegistry.TryGet(windowUid, out var strategy)) return;
            strategy.HandleDragOut(window, worldPosition, droppedIcon, targetIcon, originalPosition);
        }
    }
}
