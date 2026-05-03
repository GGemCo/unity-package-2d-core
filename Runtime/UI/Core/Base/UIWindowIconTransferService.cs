using System;

namespace GGemCo2DCore
{
    /// <summary>
    /// 서로 다른 UIWindow 사이의 UIIcon 이동, 등록, 해제를 담당합니다.
    /// </summary>
    internal sealed class UIWindowIconTransferService
    {
        private readonly Func<UIWindowConstants.WindowUid, UIWindow> _getWindowByUid;

        /// <summary>
        /// 아이콘 이동 서비스를 생성합니다.
        /// </summary>
        /// <param name="getWindowByUid">UID로 UIWindow를 조회하는 함수입니다.</param>
        public UIWindowIconTransferService(Func<UIWindowConstants.WindowUid, UIWindow> getWindowByUid)
        {
            _getWindowByUid = getWindowByUid;
        }

        /// <summary>
        /// 지정한 UIWindow 슬롯의 아이콘을 제거합니다.
        /// </summary>
        /// <param name="windowUid">아이콘을 제거할 UIWindow UID입니다.</param>
        /// <param name="slotIndex">아이콘을 제거할 슬롯 인덱스입니다.</param>
        public void RemoveIcon(UIWindowConstants.WindowUid windowUid, int slotIndex)
        {
            UIWindow uiWindow = _getWindowByUid?.Invoke(windowUid);
            if (uiWindow == null)
            {
                GcLogger.LogError($"{nameof(UIWindow)} 컴포넌트가 없습니다. uid:" + windowUid);
                return;
            }

            uiWindow.DetachIcon(slotIndex);
        }

        /// <summary>
        /// 한 UIWindow 슬롯의 아이콘 수량 일부 또는 전체를 다른 UIWindow 슬롯으로 이동합니다.
        /// </summary>
        /// <param name="fromWindowUid">이동할 아이콘이 있는 UIWindow UID입니다.</param>
        /// <param name="fromIndex">이동할 아이콘이 있는 슬롯 인덱스입니다.</param>
        /// <param name="toWindowUid">아이콘을 받을 UIWindow UID입니다.</param>
        /// <param name="toCount">이동할 아이콘 수량입니다.</param>
        /// <param name="toIndex">대상 슬롯 인덱스입니다. -1이면 자동으로 빈 슬롯을 찾습니다.</param>
        public void MoveIcon(
            UIWindowConstants.WindowUid fromWindowUid,
            int fromIndex,
            UIWindowConstants.WindowUid toWindowUid,
            int toCount,
            int toIndex = -1)
        {
            UIWindow fromWindow = _getWindowByUid?.Invoke(fromWindowUid);
            UIWindow toWindow = _getWindowByUid?.Invoke(toWindowUid);
            if (fromWindow == null || toWindow == null)
            {
                GcLogger.LogError("from window 또는 to window 값이 잘 못 되었습니다. from window:" +
                                  fromWindowUid + "/to window:" + toWindowUid);
                return;
            }

            UIIcon fromIcon = fromWindow.GetIconByIndex(fromIndex);
            if (fromIcon == null || fromIcon.uid <= 0 || fromIcon.GetCount() <= 0)
            {
                return;
            }

            int fromIconUid = fromIcon.uid;
            long fromIconInstanceId = fromIcon.instanceId;
            int targetSlotIndex = toIndex >= 0 ? toIndex : toWindow.FindFirstAcceptableEmptySlot(fromIcon);
            if (targetSlotIndex < 0)
            {
                toWindow.ShowSlotAcceptFailure("Window_NoEmptySpace");
                return;
            }

            UIIcon targetIcon = toWindow.GetIconByIndex(targetSlotIndex);
            if (targetIcon != null && !UISlotPlacementValidator.CanSwap(fromIcon, targetIcon, out string failMessageKey))
            {
                toWindow.ShowSlotAcceptFailure(failMessageKey);
                fromIcon.HandleInvalidEffect();
                return;
            }

            if (targetIcon == null && !toWindow.CanAcceptIcon(fromIcon, targetSlotIndex, out failMessageKey))
            {
                toWindow.ShowSlotAcceptFailure(failMessageKey);
                fromIcon.HandleInvalidEffect();
                return;
            }

            fromWindow.SetIconCount(fromIndex, fromIcon.uid, fromIcon.GetCount() - toCount, instanceId: fromIconInstanceId);

            UIIcon toIcon = toWindow.GetIconByIndex(targetSlotIndex);
            if (toIcon != null && toIcon.uid > 0 && toIcon.GetCount() > 0)
            {
                fromWindow.SetIconCount(toIcon.uid, toIcon.GetCount(), instanceId: toIcon.instanceId);
            }

            toWindow.SetIconCount(targetSlotIndex, fromIconUid, toCount, instanceId: fromIconInstanceId);
        }

        /// <summary>
        /// 등록형 UIWindow에 들어간 아이콘을 해제하고 원본 부모 아이콘의 잠금을 풉니다.
        /// </summary>
        /// <param name="fromWindowUid">등록을 해제할 UIWindow UID입니다.</param>
        /// <param name="fromIndex">등록을 해제할 슬롯 인덱스입니다.</param>
        /// <param name="toWindowUid">기본 반환 대상 UIWindow UID입니다.</param>
        public void UnRegisterIcon(
            UIWindowConstants.WindowUid fromWindowUid,
            int fromIndex,
            UIWindowConstants.WindowUid toWindowUid = UIWindowConstants.WindowUid.Inventory)
        {
            UIWindow fromWindow = _getWindowByUid?.Invoke(fromWindowUid);
            UIWindow toWindow = _getWindowByUid?.Invoke(toWindowUid);
            if (fromWindow == null || toWindow == null)
            {
                GcLogger.LogError("from window 또는 to window 값이 잘 못 되었습니다. from window:" +
                                  fromWindowUid + "/to window:" + toWindowUid);
                return;
            }

            UIIcon fromIcon = fromWindow.GetIconByIndex(fromIndex);
            if (fromIcon == null)
            {
                return;
            }

            (UIWindowConstants.WindowUid parentWindowUid, int parentIconIndex) = fromIcon.GetParentInfo();
            if (parentWindowUid == UIWindowConstants.WindowUid.None)
            {
                return;
            }

            UIWindow parent = _getWindowByUid?.Invoke(parentWindowUid);
            UIIcon parentIcon = parent?.GetIconByIndex(parentIconIndex);
            parentIcon?.SetIconLock(false);

            fromWindow.DetachIcon(fromIndex);
        }

        /// <summary>
        /// 한 UIWindow의 아이콘을 다른 UIWindow에 등록하고 원본 아이콘을 잠금 처리합니다.
        /// </summary>
        /// <param name="fromWindowUid">등록할 아이콘이 있는 UIWindow UID입니다.</param>
        /// <param name="fromIndex">등록할 아이콘이 있는 슬롯 인덱스입니다.</param>
        /// <param name="toWindowUid">등록 대상 UIWindow UID입니다.</param>
        /// <param name="toCount">등록할 아이콘 수량입니다.</param>
        /// <param name="toIndex">대상 슬롯 인덱스입니다. -1이면 자동으로 빈 슬롯을 찾습니다.</param>
        public void RegisterIcon(
            UIWindowConstants.WindowUid fromWindowUid,
            int fromIndex,
            UIWindowConstants.WindowUid toWindowUid,
            int toCount,
            int toIndex = -1)
        {
            UIWindow fromWindow = _getWindowByUid?.Invoke(fromWindowUid);
            UIWindow toWindow = _getWindowByUid?.Invoke(toWindowUid);
            if (fromWindow == null || toWindow == null)
            {
                GcLogger.LogError("from window 또는 to window 값이 잘 못 되었습니다. from window:" +
                                  fromWindowUid + "/to window:" + toWindowUid);
                return;
            }

            UIIcon fromIcon = fromWindow.GetIconByIndex(fromIndex);
            if (fromIcon == null || fromIcon.uid <= 0 || fromIcon.GetCount() <= 0)
            {
                return;
            }

            int targetSlotIndex = toIndex >= 0 ? toIndex : toWindow.FindFirstAcceptableEmptySlot(fromIcon);
            if (targetSlotIndex < 0)
            {
                toWindow.ShowSlotAcceptFailure("Window_NoEmptySpace");
                fromIcon.HandleInvalidEffect();
                return;
            }

            if (!toWindow.CanAcceptIcon(fromIcon, targetSlotIndex, out string failMessageKey))
            {
                toWindow.ShowSlotAcceptFailure(failMessageKey);
                fromIcon.HandleInvalidEffect();
                return;
            }

            fromIcon.SetIconLock(true);
            int itemUid = fromIcon.uid;
            long itemInstanceId = fromIcon.instanceId;

            UIIcon icon = toWindow.GetIconByIndex(targetSlotIndex);
            if (icon != null && icon.uid > 0 && icon.GetCount() > 0)
            {
                fromWindow.SetIconCount(icon.uid, icon.GetCount(), instanceId: icon.instanceId);
            }

            UIIcon uiIcon = toWindow.SetIconCountReturnIcon(
                targetSlotIndex,
                itemUid,
                toCount,
                instanceId: itemInstanceId);
            uiIcon?.SetParentInfo(fromWindowUid, fromIndex);
        }
    }
}
