using System;
using System.Collections;
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
            bool dropIconSelected = droppedUIIcon.IsSelected();
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
            bool targetIconSelected = targetUIIcon.IsSelected();

            if (droppedWindowUid != targetWindowUid)
            {
                switch (droppedWindowUid)
                {
                    case UIWindowConstants.WindowUid.Inventory:
                        SceneGame.Instance.uIWindowManager.MoveIcon(droppedWindowUid, dropIconSlotIndex, targetWindowUid, dropIconCount, targetIconSlotIndex);
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
                    // 같은 아이템일때 
                    if (dropIconUid == targetIconUid)
                    {
                        // 중첩 가능한지 체크
                        var info = uiWindowQuickSlotSimulation.tableItem.GetDataByUid(targetUIIcon.uid);
                        if (info is { MaxOverlayCount: > 1 })
                        {
                            var result = uiWindowQuickSlotSimulation.quickSlotSimulationData.MergeItem(dropIconSlotIndex, targetIconSlotIndex);
                            droppedWindow.SetIcons(result);
                            if (dropIconSelected)
                            {
                                // 합치기 실패했을때, 한쪽이 stack 개수가 max 여서 실패 했을 때, 자리가 바꼈으므로 targetIconSlotIndex 아이콘을 select 한다.
                                if (!result.IsSuccess())
                                {
                                }
                                else
                                {
                                    var dropIcon = droppedWindow.GetIconByIndex(dropIconSlotIndex);
                                    var targetIcon = droppedWindow.GetIconByIndex(targetIconSlotIndex);
                                    // 합쳐도 기존 개수가 남아있으면, 기존 아이콘을 선택한채로 유지한다.
                                    if (dropIcon != null && dropIcon.uid > 0)
                                    {
                                        window.StartCoroutine(UpdateSelected(dropIcon, droppedWindow));
                                    }
                                    // 합치고 기존 개수가 없어지고 타겟 아이콘에 합쳐지면, 타겟 아이콘을 셀렉트 한다.
                                    else if (targetIcon != null && targetIcon.uid > 0)
                                    {
                                        window.StartCoroutine(UpdateSelected(targetIcon, droppedWindow));
                                    }
                                }
                            }
                            else if (targetIconSelected)
                            {
                                // // 합치기 실패했을때, 한쪽이 stack 개수가 max 여서 실패 했을 때
                                // if (!result.IsSuccess())
                                // {
                                // }
                                // else
                                // {
                                //     var dropIcon = droppedWindow.GetIconByIndex(dropIconSlotIndex);
                                //     var targetIcon = droppedWindow.GetIconByIndex(targetIconSlotIndex);
                                //     // 합쳐도 기존 개수가 남아있으면, 기존 아이콘을 선택한채로 유지한다.
                                //     if (targetIcon != null && targetIcon.uid > 0)
                                //     {
                                //         window.StartCoroutine(UpdateSelected(targetIcon, droppedWindow));
                                //     }
                                //     // 합치고 기존 개수가 없어지고 타겟 아이콘에 합쳐지면, 타겟 아이콘을 셀렉트 한다.
                                //     else if (dropIcon != null && dropIcon.uid > 0)
                                //     {
                                //         window.StartCoroutine(UpdateSelected(dropIcon, droppedWindow));
                                //     }
                                // }
                            }
                        }
                        else
                        {
                            var dropIcon = droppedWindow.SetIconCount(dropIconSlotIndex, targetIconUid, targetIconCount);
                            if (targetIconSelected)
                            {
                                window.StartCoroutine(UpdateSelected(dropIcon, droppedWindow));
                            }
                            var targetIcon = targetWindow.SetIconCount(targetIconSlotIndex, dropIconUid, dropIconCount);
                            if (dropIconSelected)
                            {
                                window.StartCoroutine(UpdateSelected(targetIcon, targetWindow));
                            }
                        }
                    }
                    else
                    {
                        var dropIcon = droppedWindow.SetIconCount(dropIconSlotIndex, targetIconUid, targetIconCount);
                        if (targetIconSelected)
                        {
                            window.StartCoroutine(UpdateSelected(dropIcon, droppedWindow));
                        }
                        var targetIcon = targetWindow.SetIconCount(targetIconSlotIndex, dropIconUid, dropIconCount);
                        if (dropIconSelected)
                        {
                            window.StartCoroutine(UpdateSelected(targetIcon, targetWindow));
                        }
                    }
                }
            }
        }

        private IEnumerator UpdateSelected(UIIcon icon, UIWindow window)
        {
            yield return null;
            window.SetSelectedIcon(icon.index);
        }

        public void HandleDragOut(UIWindow window, Vector3 worldPosition, GameObject droppedIcon, GameObject targetIcon,
            Vector3 originalPosition)
        {
            UIIcon icon = droppedIcon.GetComponent<UIIcon>();
            // 맵에 드랍하기
            SceneGame.Instance.ItemManager.MakeDropItem(worldPosition, icon.uid, icon.GetCount());
            // 윈도우에서 아이콘 정보 지워주기 
            icon.window.DetachIcon(icon.slotIndex);
        }
    }
}