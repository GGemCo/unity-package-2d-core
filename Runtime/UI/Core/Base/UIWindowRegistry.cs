using System.Collections.Generic;

namespace GGemCo2DCore
{
    /// <summary>
    /// UIWindow 참조를 UID 기준으로 캐시하고 조회하는 책임을 담당합니다.
    /// </summary>
    internal sealed class UIWindowRegistry
    {
        private readonly Dictionary<int, UIWindow> _windowReferenceMap = new Dictionary<int, UIWindow>();
        private UIWindow[] _uiWindows;

        /// <summary>
        /// 현재 캐시에 등록된 UIWindow 참조 개수를 반환합니다.
        /// </summary>
        public int Count => _windowReferenceMap.Count;

        /// <summary>
        /// 배열 기반 UIWindow 참조를 교체하고 UID 캐시를 다시 구성합니다.
        /// </summary>
        /// <param name="uiWindows">외부에서 전달된 UIWindow 배열입니다.</param>
        /// <param name="windowKeys">Inspector에서 연결한 UID별 UIWindow 목록입니다.</param>
        public void SetUIWindows(UIWindow[] uiWindows, List<WindowKey> windowKeys)
        {
            _uiWindows = uiWindows;
            Rebuild(windowKeys);
        }

        /// <summary>
        /// Inspector의 WindowKey 목록과 배열 fallback을 기준으로 UID 캐시를 다시 구성합니다.
        /// </summary>
        /// <param name="windowKeys">Inspector에서 연결한 UID별 UIWindow 목록입니다.</param>
        public void Rebuild(List<WindowKey> windowKeys)
        {
            _windowReferenceMap.Clear();

            if (windowKeys != null)
            {
                for (int i = 0; i < windowKeys.Count; i++)
                {
                    WindowKey windowKey = windowKeys[i];
                    if (windowKey == null || windowKey.uid <= 0 || windowKey.uiWindow == null)
                    {
                        continue;
                    }

                    _windowReferenceMap[windowKey.uid] = windowKey.uiWindow;
                }
            }

            if (_uiWindows == null)
            {
                return;
            }

            for (int i = 0; i < _uiWindows.Length; i++)
            {
                UIWindow uiWindow = _uiWindows[i];
                if (uiWindow == null || _windowReferenceMap.ContainsKey(i))
                {
                    continue;
                }

                _windowReferenceMap.Add(i, uiWindow);
            }
        }

        /// <summary>
        /// 지정한 UID에 해당하는 UIWindow 참조를 반환합니다.
        /// </summary>
        /// <param name="uid">조회할 UIWindow UID입니다.</param>
        /// <returns>캐시에 등록된 UIWindow입니다. 없으면 null을 반환합니다.</returns>
        public UIWindow GetWindowReferenceByUid(int uid)
        {
            if (uid <= 0)
            {
                return null;
            }

            _windowReferenceMap.TryGetValue(uid, out UIWindow uiWindow);
            return uiWindow;
        }

        /// <summary>
        /// 지정한 UID의 WindowKey 항목을 추가하거나 기존 참조를 교체합니다.
        /// </summary>
        /// <param name="windowKeys">Inspector에서 연결한 UID별 UIWindow 목록입니다.</param>
        /// <param name="uid">추가하거나 교체할 UIWindow UID입니다.</param>
        /// <param name="window">UID에 연결할 UIWindow 참조입니다.</param>
        /// <returns>목록이 실제로 변경되었으면 true입니다.</returns>
        public bool UpsertWindowKey(List<WindowKey> windowKeys, int uid, UIWindow window)
        {
            if (windowKeys == null || uid <= 0 || window == null)
            {
                return false;
            }

            for (int i = 0; i < windowKeys.Count; i++)
            {
                WindowKey windowKey = windowKeys[i];
                if (windowKey == null || windowKey.uid != uid)
                {
                    continue;
                }

                if (windowKey.uiWindow == window)
                {
                    return false;
                }

                windowKey.uiWindow = window;
                Rebuild(windowKeys);
                return true;
            }

            windowKeys.Add(new WindowKey
            {
                uid = uid,
                uiWindow = window,
            });
            Rebuild(windowKeys);
            return true;
        }
    }
}
