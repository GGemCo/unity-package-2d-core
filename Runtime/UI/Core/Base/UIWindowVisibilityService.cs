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
        private readonly Dictionary<int, List<UIWindowConstants.WindowUid>> _suppressedWindowsByToken =
            new Dictionary<int, List<UIWindowConstants.WindowUid>>();
        private readonly Dictionary<UIWindowConstants.WindowUid, int> _suppressedWindowRefCounts =
            new Dictionary<UIWindowConstants.WindowUid, int>();
        private readonly Dictionary<UIWindowConstants.WindowUid, DeferredVisibilityRequest> _deferredVisibilityRequests =
            new Dictionary<UIWindowConstants.WindowUid, DeferredVisibilityRequest>();
        private int _nextSuppressionToken = 1;

        /// <summary>
        /// 표시 억제 중 들어온 UIWindow 표시 요청을 저장하는 내부 요청 정보입니다.
        /// 억제 해제 후 같은 UID에 대한 마지막 요청만 적용하여 중복 표시를 방지합니다.
        /// </summary>
        private sealed class DeferredVisibilityRequest
        {
            /// <summary>
            /// 표시 상태를 변경할 UIWindow UID입니다.
            /// </summary>
            public UIWindowConstants.WindowUid WindowUid { get; }

            /// <summary>
            /// 표시하면 true, 숨기면 false입니다.
            /// </summary>
            public bool Show { get; }

            /// <summary>
            /// 표시 상태 적용 모드입니다.
            /// </summary>
            public UIWindowConstants.UIWindowVisibilityApplyMode Mode { get; }

            /// <summary>
            /// 요청을 등록한 소유자입니다. 맵 전환이나 씬 종료 시 소유자 단위 취소에 사용합니다.
            /// </summary>
            public object Owner { get; }

            /// <summary>
            /// 지연 표시 요청 정보를 생성합니다.
            /// </summary>
            /// <param name="windowUid">표시 상태를 변경할 UIWindow UID입니다.</param>
            /// <param name="show">표시하면 true, 숨기면 false입니다.</param>
            /// <param name="mode">표시 상태 적용 모드입니다.</param>
            /// <param name="owner">요청을 등록한 소유자입니다.</param>
            public DeferredVisibilityRequest(
                UIWindowConstants.WindowUid windowUid,
                bool show,
                UIWindowConstants.UIWindowVisibilityApplyMode mode,
                object owner)
            {
                WindowUid = windowUid;
                Show = show;
                Mode = mode;
                Owner = owner;
            }
        }

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
            TryApplyWindowVisibility(uid, show, mode, deferIfSuppressed: false, owner: null);
        }

        /// <summary>
        /// 지정한 UIWindow의 표시 상태를 기본 모드로 변경합니다.
        /// 표시 요청이 현재 억제 중이면 요청을 버리지 않고 억제 해제 후 적용되도록 보류합니다.
        /// </summary>
        /// <param name="uid">표시 상태를 변경할 UIWindow UID입니다.</param>
        /// <param name="show">표시하면 true, 숨기면 false입니다.</param>
        public void ShowWindowWhenAllowed(UIWindowConstants.WindowUid uid, bool show)
        {
            ShowWindowWhenAllowed(uid, show, UIWindowConstants.UIWindowVisibilityApplyMode.Normal, owner: null);
        }

        /// <summary>
        /// 지정한 UIWindow의 표시 상태를 지정한 모드로 변경합니다.
        /// 표시 요청이 현재 억제 중이면 요청을 버리지 않고 억제 해제 후 적용되도록 보류합니다.
        /// </summary>
        /// <param name="uid">표시 상태를 변경할 UIWindow UID입니다.</param>
        /// <param name="show">표시하면 true, 숨기면 false입니다.</param>
        /// <param name="mode">표시 상태 적용 모드입니다.</param>
        /// <param name="owner">요청을 등록한 소유자입니다. null이면 소유자 없이 등록합니다.</param>
        public void ShowWindowWhenAllowed(
            UIWindowConstants.WindowUid uid,
            bool show,
            UIWindowConstants.UIWindowVisibilityApplyMode mode,
            object owner = null)
        {
            TryApplyWindowVisibility(uid, show, mode, deferIfSuppressed: true, owner: owner);
        }

        /// <summary>
        /// 보류 중인 UIWindow 표시 요청 중 현재 억제가 해제된 요청을 적용합니다.
        /// UIWindowManager의 LateUpdate에서 호출하여 컷신 스냅샷 복원 이후 안전하게 처리합니다.
        /// </summary>
        /// <returns>하나 이상의 보류 요청을 적용했으면 true입니다.</returns>
        public bool FlushDeferredVisibilityRequests()
        {
            if (_deferredVisibilityRequests.Count <= 0)
            {
                return false;
            }

            List<UIWindowConstants.WindowUid> readyWindowUids = new List<UIWindowConstants.WindowUid>();
            foreach (KeyValuePair<UIWindowConstants.WindowUid, DeferredVisibilityRequest> pair in _deferredVisibilityRequests)
            {
                DeferredVisibilityRequest request = pair.Value;
                if (request == null || !request.Show || !IsWindowVisibilitySuppressed(pair.Key))
                {
                    readyWindowUids.Add(pair.Key);
                }
            }

            bool applied = false;
            for (int i = 0; i < readyWindowUids.Count; i++)
            {
                UIWindowConstants.WindowUid windowUid = readyWindowUids[i];
                if (!_deferredVisibilityRequests.TryGetValue(windowUid, out DeferredVisibilityRequest request))
                {
                    continue;
                }

                _deferredVisibilityRequests.Remove(windowUid);
                if (request == null)
                {
                    continue;
                }

                applied |= TryApplyWindowVisibility(
                    request.WindowUid,
                    request.Show,
                    request.Mode,
                    deferIfSuppressed: false,
                    owner: request.Owner);
            }

            return applied;
        }

        /// <summary>
        /// 지정한 UIWindow UID의 보류 중인 표시 요청을 취소합니다.
        /// owner를 전달하면 같은 소유자가 등록한 요청일 때만 취소합니다.
        /// </summary>
        /// <param name="uid">취소할 UIWindow UID입니다.</param>
        /// <param name="owner">요청 소유자입니다. null이면 UID가 같은 요청을 소유자와 무관하게 취소합니다.</param>
        /// <returns>보류 요청을 취소했으면 true입니다.</returns>
        public bool CancelDeferredWindowVisibilityRequest(UIWindowConstants.WindowUid uid, object owner = null)
        {
            if (uid == UIWindowConstants.WindowUid.None ||
                !_deferredVisibilityRequests.TryGetValue(uid, out DeferredVisibilityRequest request))
            {
                return false;
            }

            if (owner != null && !ReferenceEquals(request.Owner, owner))
            {
                return false;
            }

            _deferredVisibilityRequests.Remove(uid);
            return true;
        }

        /// <summary>
        /// 지정한 소유자가 등록한 모든 보류 표시 요청을 취소합니다.
        /// 맵 전환, 씬 종료, 루틴 중단처럼 요청 주체가 더 이상 유효하지 않을 때 사용합니다.
        /// </summary>
        /// <param name="owner">취소할 요청 소유자입니다.</param>
        /// <returns>취소한 보류 요청 개수입니다.</returns>
        public int CancelDeferredWindowVisibilityRequests(object owner)
        {
            if (owner == null || _deferredVisibilityRequests.Count <= 0)
            {
                return 0;
            }

            List<UIWindowConstants.WindowUid> removeWindowUids = new List<UIWindowConstants.WindowUid>();
            foreach (KeyValuePair<UIWindowConstants.WindowUid, DeferredVisibilityRequest> pair in _deferredVisibilityRequests)
            {
                if (pair.Value != null && ReferenceEquals(pair.Value.Owner, owner))
                {
                    removeWindowUids.Add(pair.Key);
                }
            }

            for (int i = 0; i < removeWindowUids.Count; i++)
            {
                _deferredVisibilityRequests.Remove(removeWindowUids[i]);
            }

            return removeWindowUids.Count;
        }

        /// <summary>
        /// 보류 중인 모든 UIWindow 표시 요청을 취소합니다.
        /// 씬 종료처럼 기존 UI 표시 요청이 더 이상 의미 없을 때 사용합니다.
        /// </summary>
        public void ClearDeferredWindowVisibilityRequests()
        {
            _deferredVisibilityRequests.Clear();
        }

        /// <summary>
        /// 지정한 UIWindow UID에 보류 중인 표시 요청이 있는지 확인합니다.
        /// owner를 전달하면 같은 소유자의 요청만 확인합니다.
        /// </summary>
        /// <param name="uid">확인할 UIWindow UID입니다.</param>
        /// <param name="owner">요청 소유자입니다. null이면 소유자와 무관하게 확인합니다.</param>
        /// <returns>보류 요청이 있으면 true입니다.</returns>
        public bool HasDeferredWindowVisibilityRequest(UIWindowConstants.WindowUid uid, object owner = null)
        {
            if (uid == UIWindowConstants.WindowUid.None ||
                !_deferredVisibilityRequests.TryGetValue(uid, out DeferredVisibilityRequest request))
            {
                return false;
            }

            return owner == null || ReferenceEquals(request.Owner, owner);
        }

        /// <summary>
        /// UIWindow 표시 상태 변경을 실제 적용하거나, 표시 억제 중인 요청을 보류합니다.
        /// 일반 ShowWindow 호출은 기존 동작을 유지하기 위해 보류하지 않고 반환합니다.
        /// </summary>
        /// <param name="uid">표시 상태를 변경할 UIWindow UID입니다.</param>
        /// <param name="show">표시하면 true, 숨기면 false입니다.</param>
        /// <param name="mode">표시 상태 적용 모드입니다.</param>
        /// <param name="deferIfSuppressed">표시 억제 중이면 요청을 보류할지 여부입니다.</param>
        /// <param name="owner">요청을 등록한 소유자입니다.</param>
        /// <returns>표시 상태를 즉시 적용했으면 true입니다.</returns>
        private bool TryApplyWindowVisibility(
            UIWindowConstants.WindowUid uid,
            bool show,
            UIWindowConstants.UIWindowVisibilityApplyMode mode,
            bool deferIfSuppressed,
            object owner)
        {
            if (uid == UIWindowConstants.WindowUid.None)
            {
                return false;
            }

            UIWindow uiWindow = _getWindowByUid?.Invoke(uid);
            if (uiWindow == null)
            {
                GcLogger.LogError($"{nameof(UIWindow)} 컴포넌트가 없습니다. uid:" + uid);
                return false;
            }

            if (show && IsWindowVisibilitySuppressed(uid))
            {
                if (deferIfSuppressed)
                {
                    _deferredVisibilityRequests[uid] = new DeferredVisibilityRequest(uid, show, mode, owner);
                }

                return false;
            }

            _deferredVisibilityRequests.Remove(uid);
            UIWindowVisibilityStateStack.ApplyVisibility(uiWindow, show, mode);
            return true;
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
        /// 지정한 UIWindow UID 목록에 표시 억제 토큰을 발급합니다.
        /// 억제 중인 창은 외부에서 표시 요청이 들어와도 토큰이 해제될 때까지 다시 켜지지 않습니다.
        /// </summary>
        /// <param name="windowUids">표시 요청을 억제할 UIWindow UID 목록입니다.</param>
        /// <returns>해제에 사용할 토큰입니다. 억제 대상이 없으면 0을 반환합니다.</returns>
        public int AcquireVisibilitySuppression(IEnumerable<UIWindowConstants.WindowUid> windowUids)
        {
            List<UIWindowConstants.WindowUid> normalizedWindowUids = NormalizeSuppressionTargets(windowUids);
            if (normalizedWindowUids.Count <= 0)
            {
                return 0;
            }

            int token = _nextSuppressionToken++;
            if (_nextSuppressionToken <= 0)
            {
                _nextSuppressionToken = 1;
            }

            _suppressedWindowsByToken[token] = normalizedWindowUids;
            for (int i = 0; i < normalizedWindowUids.Count; i++)
            {
                UIWindowConstants.WindowUid windowUid = normalizedWindowUids[i];
                _suppressedWindowRefCounts.TryGetValue(windowUid, out int count);
                _suppressedWindowRefCounts[windowUid] = count + 1;
            }

            return token;
        }

        /// <summary>
        /// 지정한 표시 억제 토큰을 해제합니다.
        /// 같은 창을 여러 토큰이 억제 중이면 마지막 토큰이 해제될 때 표시 요청이 다시 허용됩니다.
        /// </summary>
        /// <param name="token">해제할 표시 억제 토큰입니다.</param>
        /// <returns>토큰을 찾아 해제했으면 true입니다.</returns>
        public bool ReleaseVisibilitySuppression(int token)
        {
            if (token == 0 || !_suppressedWindowsByToken.TryGetValue(token, out List<UIWindowConstants.WindowUid> windowUids))
            {
                return false;
            }

            _suppressedWindowsByToken.Remove(token);
            for (int i = 0; i < windowUids.Count; i++)
            {
                UIWindowConstants.WindowUid windowUid = windowUids[i];
                if (!_suppressedWindowRefCounts.TryGetValue(windowUid, out int count))
                {
                    continue;
                }

                count--;
                if (count <= 0)
                {
                    _suppressedWindowRefCounts.Remove(windowUid);
                    continue;
                }

                _suppressedWindowRefCounts[windowUid] = count;
            }

            return true;
        }

        /// <summary>
        /// 지정한 UIWindow UID가 현재 표시 억제 대상인지 확인합니다.
        /// </summary>
        /// <param name="windowUid">확인할 UIWindow UID입니다.</param>
        /// <returns>표시 요청이 억제 중이면 true입니다.</returns>
        public bool IsWindowVisibilitySuppressed(UIWindowConstants.WindowUid windowUid)
        {
            return windowUid != UIWindowConstants.WindowUid.None &&
                   _suppressedWindowRefCounts.ContainsKey(windowUid);
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
                if (show && window != null && IsWindowVisibilitySuppressed(window.uid))
                {
                    continue;
                }

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

        /// <summary>
        /// 표시 억제 대상 목록에서 None과 중복 값을 제거합니다.
        /// </summary>
        /// <param name="windowUids">정규화할 UIWindow UID 목록입니다.</param>
        /// <returns>중복 없이 정리된 표시 억제 대상 목록입니다.</returns>
        private static List<UIWindowConstants.WindowUid> NormalizeSuppressionTargets(
            IEnumerable<UIWindowConstants.WindowUid> windowUids)
        {
            List<UIWindowConstants.WindowUid> result = new List<UIWindowConstants.WindowUid>();
            if (windowUids == null)
            {
                return result;
            }

            HashSet<UIWindowConstants.WindowUid> seen = new HashSet<UIWindowConstants.WindowUid>();
            foreach (UIWindowConstants.WindowUid windowUid in windowUids)
            {
                if (windowUid == UIWindowConstants.WindowUid.None || !seen.Add(windowUid))
                {
                    continue;
                }

                result.Add(windowUid);
            }

            return result;
        }
    }
}
