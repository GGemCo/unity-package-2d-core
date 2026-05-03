using System;
using System.Collections.Generic;

namespace GGemCo2DCore
{
    /// <summary>
    /// UIWindow 표시, 숨김, 일괄 복원 정책을 담당합니다.
    /// </summary>
    internal sealed class UIWindowVisibilityService
    {
        private readonly Func<UIWindowConstants.WindowUid, UIWindow> _getWindowByUid;
        private readonly Func<List<UIWindow>> _getManagedWindows;
        private readonly UIWindowVisibilityStateStack _stateStack;

        /// <summary>
        /// UIWindow 표시 상태 서비스를 생성합니다.
        /// </summary>
        /// <param name="getWindowByUid">UID로 UIWindow를 조회하는 함수입니다.</param>
        /// <param name="getManagedWindows">현재 관리 중인 UIWindow 목록을 반환하는 함수입니다.</param>
        /// <param name="stateStack">표시 상태 스택입니다.</param>
        public UIWindowVisibilityService(
            Func<UIWindowConstants.WindowUid, UIWindow> getWindowByUid,
            Func<List<UIWindow>> getManagedWindows,
            UIWindowVisibilityStateStack stateStack)
        {
            _getWindowByUid = getWindowByUid;
            _getManagedWindows = getManagedWindows;
            _stateStack = stateStack;
        }

        /// <summary>
        /// 지정한 UIWindow의 표시 상태를 기본 모드로 변경합니다.
        /// </summary>
        /// <param name="uid">표시 상태를 변경할 UIWindow UID입니다.</param>
        /// <param name="show">표시하면 true, 숨기면 false입니다.</param>
        public void ShowWindow(UIWindowConstants.WindowUid uid, bool show)
        {
            ShowWindow(uid, show, UIWindowConstants.UIWindowVisibilityApplyMode.Normal);
        }

        /// <summary>
        /// 지정한 UIWindow의 표시 상태를 지정한 모드로 변경합니다.
        /// </summary>
        /// <param name="uid">표시 상태를 변경할 UIWindow UID입니다.</param>
        /// <param name="show">표시하면 true, 숨기면 false입니다.</param>
        /// <param name="mode">표시 상태 적용 모드입니다.</param>
        public void ShowWindow(
            UIWindowConstants.WindowUid uid,
            bool show,
            UIWindowConstants.UIWindowVisibilityApplyMode mode)
        {
            UIWindow uiWindow = _getWindowByUid?.Invoke(uid);
            if (uiWindow == null)
            {
                GcLogger.LogError($"{nameof(UIWindow)} 컴포넌트가 없습니다. uid:" + uid);
                return;
            }

            UIWindowVisibilityStateStack.ApplyVisibility(uiWindow, show, mode);
        }

        /// <summary>
        /// 지정한 UIWindow가 현재 표시 중인지 확인합니다.
        /// </summary>
        /// <param name="windowUid">확인할 UIWindow UID입니다.</param>
        /// <returns>활성 상태이면 true입니다.</returns>
        public bool IsShowByWindowUid(UIWindowConstants.WindowUid windowUid)
        {
            UIWindow uiWindow = _getWindowByUid?.Invoke(windowUid);
            return uiWindow != null && uiWindow.gameObject.activeSelf;
        }

        /// <summary>
        /// 지정한 UIWindow UID 목록의 현재 표시 상태를 캡처합니다.
        /// </summary>
        /// <param name="windowUids">표시 상태를 캡처할 UIWindow UID 목록입니다.</param>
        /// <returns>UID별 표시 상태입니다.</returns>
        public Dictionary<UIWindowConstants.WindowUid, bool> CaptureVisibilityState(
            IEnumerable<UIWindowConstants.WindowUid> windowUids)
        {
            Dictionary<UIWindowConstants.WindowUid, bool> result =
                new Dictionary<UIWindowConstants.WindowUid, bool>();
            if (windowUids == null)
            {
                return result;
            }

            foreach (UIWindowConstants.WindowUid windowUid in windowUids)
            {
                if (windowUid == UIWindowConstants.WindowUid.None || result.ContainsKey(windowUid))
                {
                    continue;
                }

                UIWindow uiWindow = _getWindowByUid?.Invoke(windowUid);
                if (uiWindow == null)
                {
                    continue;
                }

                result[windowUid] = uiWindow.gameObject.activeSelf;
            }

            return result;
        }

        /// <summary>
        /// 지정한 UIWindow 목록의 현재 표시 상태를 캡처합니다.
        /// </summary>
        /// <param name="windows">표시 상태를 캡처할 UIWindow 목록입니다.</param>
        /// <returns>UIWindow별 표시 상태입니다.</returns>
        public Dictionary<UIWindow, bool> CaptureVisibilityState(IEnumerable<UIWindow> windows)
        {
            Dictionary<UIWindow, bool> result = new Dictionary<UIWindow, bool>();
            if (windows == null)
            {
                return result;
            }

            foreach (UIWindow window in windows)
            {
                if (window == null || result.ContainsKey(window))
                {
                    continue;
                }

                result[window] = window.gameObject.activeSelf;
            }

            return result;
        }

        /// <summary>
        /// 저장된 UID별 표시 상태를 기본 모드로 복원합니다.
        /// </summary>
        /// <param name="state">복원할 UID별 표시 상태입니다.</param>
        public void RestoreVisibilityState(IReadOnlyDictionary<UIWindowConstants.WindowUid, bool> state)
        {
            RestoreVisibilityState(state, UIWindowConstants.UIWindowVisibilityApplyMode.Normal);
        }

        /// <summary>
        /// 저장된 UID별 표시 상태를 지정한 모드로 복원합니다.
        /// </summary>
        /// <param name="state">복원할 UID별 표시 상태입니다.</param>
        /// <param name="mode">표시 상태 적용 모드입니다.</param>
        public void RestoreVisibilityState(
            IReadOnlyDictionary<UIWindowConstants.WindowUid, bool> state,
            UIWindowConstants.UIWindowVisibilityApplyMode mode)
        {
            if (state == null)
            {
                return;
            }

            foreach (KeyValuePair<UIWindowConstants.WindowUid, bool> pair in state)
            {
                ShowWindow(pair.Key, pair.Value, mode);
            }
        }

        /// <summary>
        /// 지정한 UIWindow UID 목록의 현재 표시 상태를 스택에 저장합니다.
        /// </summary>
        /// <param name="windowUids">표시 상태를 저장할 UIWindow UID 목록입니다.</param>
        /// <param name="restoreMode">Pop 시 사용할 표시 상태 복원 모드입니다.</param>
        /// <returns>저장된 스냅샷이 있으면 true입니다.</returns>
        public bool PushVisibilityState(
            IEnumerable<UIWindowConstants.WindowUid> windowUids,
            UIWindowConstants.UIWindowVisibilityApplyMode restoreMode)
        {
            if (windowUids == null)
            {
                return false;
            }

            List<UIWindow> windows = new List<UIWindow>();
            foreach (UIWindowConstants.WindowUid windowUid in windowUids)
            {
                UIWindow uiWindow = _getWindowByUid?.Invoke(windowUid);
                if (uiWindow == null)
                {
                    continue;
                }

                windows.Add(uiWindow);
            }

            return PushVisibilityState(windows, restoreMode);
        }

        /// <summary>
        /// 지정한 UIWindow 목록의 현재 표시 상태를 스택에 저장합니다.
        /// </summary>
        /// <param name="windows">표시 상태를 저장할 UIWindow 목록입니다.</param>
        /// <param name="restoreMode">Pop 시 사용할 표시 상태 복원 모드입니다.</param>
        /// <returns>저장된 스냅샷이 있으면 true입니다.</returns>
        public bool PushVisibilityState(
            IEnumerable<UIWindow> windows,
            UIWindowConstants.UIWindowVisibilityApplyMode restoreMode)
        {
            return _stateStack != null && _stateStack.Push(windows, restoreMode);
        }

        /// <summary>
        /// 스택에 저장된 가장 마지막 표시 상태를 복원합니다.
        /// </summary>
        /// <returns>복원할 스냅샷이 있으면 true입니다.</returns>
        public bool PopVisibilityState()
        {
            return _stateStack != null && _stateStack.Pop();
        }

        /// <summary>
        /// 표시 상태 스택을 모두 비웁니다.
        /// </summary>
        public void ClearVisibilityStateStack()
        {
            _stateStack?.Clear();
        }

        /// <summary>
        /// 현재 표시 상태 스택에 저장된 스냅샷 개수를 반환합니다.
        /// </summary>
        /// <returns>저장된 표시 상태 스냅샷 개수입니다.</returns>
        public int GetVisibilityStateStackCount()
        {
            return _stateStack?.Count ?? 0;
        }

        /// <summary>
        /// 지정한 UIWindow UID 목록을 기본 모드로 일괄 표시하거나 숨깁니다.
        /// </summary>
        /// <param name="windowUids">표시 상태를 변경할 UIWindow UID 목록입니다.</param>
        /// <param name="show">표시하면 true, 숨기면 false입니다.</param>
        public void SetWindowsVisible(IEnumerable<UIWindowConstants.WindowUid> windowUids, bool show)
        {
            SetWindowsVisible(windowUids, show, UIWindowConstants.UIWindowVisibilityApplyMode.Normal);
        }

        /// <summary>
        /// 지정한 UIWindow UID 목록을 지정한 모드로 일괄 표시하거나 숨깁니다.
        /// </summary>
        /// <param name="windowUids">표시 상태를 변경할 UIWindow UID 목록입니다.</param>
        /// <param name="show">표시하면 true, 숨기면 false입니다.</param>
        /// <param name="mode">표시 상태 적용 모드입니다.</param>
        public void SetWindowsVisible(
            IEnumerable<UIWindowConstants.WindowUid> windowUids,
            bool show,
            UIWindowConstants.UIWindowVisibilityApplyMode mode)
        {
            if (windowUids == null)
            {
                return;
            }

            foreach (UIWindowConstants.WindowUid windowUid in windowUids)
            {
                ShowWindow(windowUid, show, mode);
            }
        }

        /// <summary>
        /// 지정한 UIWindow 목록을 지정한 모드로 일괄 표시하거나 숨깁니다.
        /// </summary>
        /// <param name="windows">표시 상태를 변경할 UIWindow 목록입니다.</param>
        /// <param name="show">표시하면 true, 숨기면 false입니다.</param>
        /// <param name="mode">표시 상태 적용 모드입니다.</param>
        public void SetWindowsVisible(
            IEnumerable<UIWindow> windows,
            bool show,
            UIWindowConstants.UIWindowVisibilityApplyMode mode = UIWindowConstants.UIWindowVisibilityApplyMode.Normal)
        {
            if (windows == null)
            {
                return;
            }

            foreach (UIWindow window in windows)
            {
                UIWindowVisibilityStateStack.ApplyVisibility(window, show, mode);
            }
        }

        /// <summary>
        /// 기본 활성 UIWindow와 예외 UID를 제외한 모든 관리 UIWindow를 닫습니다.
        /// </summary>
        /// <param name="exceptWindowUids">닫지 않을 UIWindow UID 목록입니다.</param>
        public void CloseAll(List<UIWindowConstants.WindowUid> exceptWindowUids = null)
        {
            List<UIWindow> managedWindows = _getManagedWindows?.Invoke();
            if (managedWindows == null)
            {
                return;
            }

            for (int i = 0; i < managedWindows.Count; i++)
            {
                UIWindow window = managedWindows[i];
                if (window == null)
                {
                    continue;
                }

                if (window.GetDefaultActive() || !window.gameObject.activeSelf)
                {
                    continue;
                }

                if (exceptWindowUids is { Count: > 0 } && exceptWindowUids.Contains(window.uid))
                {
                    continue;
                }

                window.Show(false);
            }
        }
    }
}
