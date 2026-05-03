using System.Collections.Generic;

namespace GGemCo2DCore
{
    /// <summary>
    /// UIWindow 표시 상태를 스택으로 저장하고 마지막 저장 상태부터 복원합니다.
    /// </summary>
    internal sealed class UIWindowVisibilityStateStack
    {
        private readonly Stack<VisibilityStateEntry> _visibilityStateStack = new Stack<VisibilityStateEntry>();

        /// <summary>
        /// 현재 저장된 표시 상태 스냅샷 개수를 반환합니다.
        /// </summary>
        public int Count => _visibilityStateStack.Count;

        /// <summary>
        /// 개별 UIWindow의 표시 상태를 보관합니다.
        /// </summary>
        private sealed class WindowVisibilityStateItem
        {
            public UIWindow window;
            public bool visible;
        }

        /// <summary>
        /// 한 번의 Push에서 저장한 UIWindow 표시 상태와 복원 모드를 보관합니다.
        /// </summary>
        private sealed class VisibilityStateEntry
        {
            public List<WindowVisibilityStateItem> state;
            public UIWindowConstants.UIWindowVisibilityApplyMode restoreMode;
        }

        /// <summary>
        /// 지정한 UIWindow들의 현재 표시 상태를 스택에 저장합니다.
        /// </summary>
        /// <param name="windows">표시 상태를 저장할 UIWindow 목록입니다.</param>
        /// <param name="restoreMode">Pop 시 사용할 표시 상태 복원 모드입니다.</param>
        /// <returns>저장된 스냅샷이 있으면 true입니다.</returns>
        public bool Push(
            IEnumerable<UIWindow> windows,
            UIWindowConstants.UIWindowVisibilityApplyMode restoreMode)
        {
            List<WindowVisibilityStateItem> snapshot = CaptureVisibilityStateItems(windows);
            if (snapshot == null || snapshot.Count <= 0)
            {
                return false;
            }

            _visibilityStateStack.Push(new VisibilityStateEntry
            {
                state = snapshot,
                restoreMode = restoreMode,
            });
            return true;
        }

        /// <summary>
        /// 스택에 저장된 가장 마지막 UIWindow 표시 상태를 복원합니다.
        /// </summary>
        /// <returns>복원할 스냅샷이 있어 복원했으면 true입니다.</returns>
        public bool Pop()
        {
            if (_visibilityStateStack.Count <= 0)
            {
                return false;
            }

            VisibilityStateEntry entry = _visibilityStateStack.Pop();
            RestoreVisibilityStateItems(entry.state, entry.restoreMode);
            return true;
        }

        /// <summary>
        /// 저장된 UIWindow 표시 상태 스택을 모두 비웁니다.
        /// </summary>
        public void Clear()
        {
            _visibilityStateStack.Clear();
        }

        /// <summary>
        /// 단일 UIWindow에 지정한 표시 상태 적용 모드를 실행합니다.
        /// </summary>
        /// <param name="window">표시 상태를 변경할 UIWindow입니다.</param>
        /// <param name="show">표시하면 true, 숨기면 false입니다.</param>
        /// <param name="mode">표시 상태 적용 모드입니다.</param>
        public static void ApplyVisibility(
            UIWindow window,
            bool show,
            UIWindowConstants.UIWindowVisibilityApplyMode mode)
        {
            if (window == null)
            {
                return;
            }

            switch (mode)
            {
                case UIWindowConstants.UIWindowVisibilityApplyMode.ImmediateSilent:
                    window.SetVisibleImmediate(show, invokeOnShow: false, followLinkedWindows: false);
                    break;
                case UIWindowConstants.UIWindowVisibilityApplyMode.Normal:
                default:
                    window.Show(show);
                    break;
            }
        }

        /// <summary>
        /// 지정한 UIWindow들의 현재 표시 상태를 중복 없이 캡처합니다.
        /// </summary>
        /// <param name="windows">표시 상태를 캡처할 UIWindow 목록입니다.</param>
        /// <returns>표시 상태 스냅샷 목록입니다.</returns>
        private static List<WindowVisibilityStateItem> CaptureVisibilityStateItems(IEnumerable<UIWindow> windows)
        {
            List<WindowVisibilityStateItem> result = new List<WindowVisibilityStateItem>();
            if (windows == null)
            {
                return result;
            }

            HashSet<UIWindow> addedWindows = new HashSet<UIWindow>();
            foreach (UIWindow window in windows)
            {
                if (window == null || !addedWindows.Add(window))
                {
                    continue;
                }

                result.Add(new WindowVisibilityStateItem
                {
                    window = window,
                    visible = window.gameObject.activeSelf,
                });
            }

            return result;
        }

        /// <summary>
        /// 저장된 표시 상태 스냅샷을 지정한 모드로 복원합니다.
        /// </summary>
        /// <param name="state">복원할 표시 상태 목록입니다.</param>
        /// <param name="mode">표시 상태 적용 모드입니다.</param>
        private static void RestoreVisibilityStateItems(
            IEnumerable<WindowVisibilityStateItem> state,
            UIWindowConstants.UIWindowVisibilityApplyMode mode)
        {
            if (state == null)
            {
                return;
            }

            foreach (WindowVisibilityStateItem item in state)
            {
                if (item == null || item.window == null)
                {
                    continue;
                }

                ApplyVisibility(item.window, item.visible, mode);
            }
        }
    }
}
