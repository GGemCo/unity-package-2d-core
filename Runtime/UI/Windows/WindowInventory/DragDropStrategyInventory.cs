using System;
using UnityEngine;

namespace GGemCo2DCore
{
    /// <summary>
    /// 인벤토리 윈도우 - 아이콘 드래그 앤 드랍 관리
    /// </summary>
    public class DragDropStrategyInventory : IDragDropStrategy
    {
        public void HandleDragInIcon(UIWindow window, UIIcon droppedUIIcon, UIIcon targetUIIcon)
        {
            // GcLogger.Log("skill window. OnEndDragInIcon");
            UIWindowInventory uiWindowInventory = window as UIWindowInventory;
            if (uiWindowInventory == null) return;
            UIWindow droppedWindow = droppedUIIcon.window;
            UIWindowConstants.WindowUid droppedWindowUid = droppedUIIcon.windowUid;
            int dropIconSlotIndex = droppedUIIcon.slotIndex;
            int dropIconUid = droppedUIIcon.uid;
            long dropIconInstanceId = droppedUIIcon.instanceId;
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
            UIWindowConstants.WindowUid targetWindowUid = targetUIIcon.windowUid;
            int targetIconSlotIndex = targetUIIcon.slotIndex;
            int targetIconUid = targetUIIcon.uid;
            long targetIconInstanceId = targetUIIcon.instanceId;
            int targetIconCount = targetUIIcon.GetCount();

            // 다른 윈도우에서 인벤토리로 드래그 앤 드랍 했을 때 
            if (droppedWindowUid != targetWindowUid)
            {
                switch (droppedWindowUid)
                {
                    case UIWindowConstants.WindowUid.Stash:
                        SceneGame.Instance.uIWindowManager.MoveIcon(droppedWindowUid, dropIconSlotIndex, UIWindowConstants.WindowUid.Inventory, dropIconCount);
                        break;
                    case UIWindowConstants.WindowUid.QuickSlotSimulation:
                        SceneGame.Instance.uIWindowManager.UnRegisterIcon(droppedWindowUid, dropIconSlotIndex);
                        break;
                    case UIWindowConstants.WindowUid.Equip:
                        // 같은 uid 아이템인지 확인
                        if (droppedUIIcon.uid == targetUIIcon.uid || targetUIIcon.uid <= 0)
                        {
                            var result = uiWindowInventory.EquipData.MinusItem(dropIconSlotIndex, dropIconUid, dropIconCount);
                            droppedWindow.SetIcons(result);

                            result = uiWindowInventory.InventoryData.AddItem(targetIconSlotIndex, new IconPayload(dropIconUid, dropIconCount, dropIconInstanceId));
                            targetWindow.SetIcons(result);
                        }
                        else
                        {
                            // 장비 <-> 인벤토리 swap 은 공통 슬롯 규칙을 모두 통과한 뒤에만 진행합니다.
                            if (!UISlotPlacementValidator.CanSwap(droppedUIIcon, targetUIIcon, out var failMessageKey))
                            {
                                window.ShowSlotAcceptFailure(failMessageKey);
                                droppedUIIcon.HandleInvalidEffect();
                                return;
                            }

                            // 순서 중요
                            // 인벤토리에서 하나 빼고
                            var result = uiWindowInventory.InventoryData.MinusItem(targetIconSlotIndex, targetIconUid, 1);
                            targetWindow.SetIcons(result);
                            
                            // 장비창에 있던것도 빼서 0 을 만듬
                            result = uiWindowInventory.EquipData.MinusItem(dropIconSlotIndex, dropIconUid, 1);
                            droppedWindow.SetIcons(result);
                                
                            // 장비창에 있던것은 인벤토리에 추가한다 
                            result = uiWindowInventory.InventoryData.AddItem(targetIconSlotIndex, new IconPayload(dropIconUid, 1, dropIconInstanceId));
                            targetWindow.SetIcons(result);
                            // 장비창에 하나 넣기
                            result = uiWindowInventory.EquipData.AddItem(dropIconSlotIndex, new IconPayload(targetIconUid, 1, targetIconInstanceId));
                            droppedWindow.SetIcons(result);
                        }
                        break;
                    case UIWindowConstants.WindowUid.None:
                    case UIWindowConstants.WindowUid.Hud:
                    case UIWindowConstants.WindowUid.Inventory:
                    case UIWindowConstants.WindowUid.ItemInfo:
                    case UIWindowConstants.WindowUid.PlayerInfo:
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
                        var info = uiWindowInventory.TableItem.GetDataByUid(targetUIIcon.uid);
                        if (info is { MaxOverlayCount: > 1 })
                        {
                            var result = uiWindowInventory.InventoryData.MergeItem(dropIconSlotIndex, targetIconSlotIndex);
                            droppedWindow.SetIcons(result);
                        }
                        else
                        {
                            if (!UISlotPlacementValidator.CanSwap(droppedUIIcon, targetUIIcon, out var failMessageKey))
                            {
                                window.ShowSlotAcceptFailure(failMessageKey);
                                droppedUIIcon.HandleInvalidEffect();
                                return;
                            }

                            droppedWindow.SetIconCount(dropIconSlotIndex, targetIconUid, targetIconCount, instanceId: targetIconInstanceId);
                            targetWindow.SetIconCount(targetIconSlotIndex, dropIconUid, dropIconCount, instanceId: dropIconInstanceId);
                        }
                    }
                    else
                    {
                        if (!UISlotPlacementValidator.CanSwap(droppedUIIcon, targetUIIcon, out var failMessageKey))
                        {
                            window.ShowSlotAcceptFailure(failMessageKey);
                            droppedUIIcon.HandleInvalidEffect();
                            return;
                        }

                        droppedWindow.SetIconCount(dropIconSlotIndex, targetIconUid, targetIconCount, instanceId: targetIconInstanceId);
                        targetWindow.SetIconCount(targetIconSlotIndex, dropIconUid, dropIconCount, instanceId: dropIconInstanceId);
                    }
                }
            }
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
