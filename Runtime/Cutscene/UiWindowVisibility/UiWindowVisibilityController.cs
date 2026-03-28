using System.Collections;
using System.Collections.Generic;

namespace GGemCo2DCore
{
    /// <summary>
    /// UIWindowManager에 등록된 UI 윈도우의 표시 상태를 컷신 이벤트로 제어합니다.
    /// </summary>
    public sealed class UiWindowVisibilityController : CutsceneDefaultController, ICutsceneController
    {
        private UiWindowVisibilityData _data;
        private UIWindowManager _windowManager;
        private Dictionary<UIWindowConstants.WindowUid, bool> _snapshot;
        private bool _hasSnapshot;

        public UiWindowVisibilityController(CutsceneManager manager)
        {
            CutsceneManager = manager;
        }

        public IEnumerator Ready(CutsceneEvent evt)
        {
            yield return null;
        }

        public void Trigger(CutsceneEvent evt)
        {
            if (evt.type != CutsceneEventType.UiWindowVisibility)
            {
                return;
            }

            _data = evt.uiWindowVisibility ?? new UiWindowVisibilityData();
            _windowManager = SceneGame.Instance != null ? SceneGame.Instance.uIWindowManager : null;
            if (_windowManager == null)
            {
                GcLogger.LogError("UiWindowVisibilityController: UIWindowManager를 찾을 수 없습니다.");
                return;
            }

            var targetWindows = ResolveTargetWindows(_windowManager, _data);
            if (targetWindows.Count <= 0)
            {
                return;
            }

            _snapshot = _windowManager.CaptureVisibilityState(targetWindows);
            _hasSnapshot = _snapshot != null && _snapshot.Count > 0;
            _windowManager.SetWindowsVisible(targetWindows, _data.show);
        }

        public void Update()
        {
        }

        public void Stop()
        {
            if (_data != null && _data.restoreOnStop)
            {
                RestoreSnapshot();
            }
        }

        public void End()
        {
            if (_data != null && _data.restoreOnCutsceneEnd)
            {
                RestoreSnapshot();
            }
        }

        private void RestoreSnapshot()
        {
            if (!_hasSnapshot || _windowManager == null || _snapshot == null)
            {
                return;
            }

            _windowManager.RestoreVisibilityState(_snapshot);
            _hasSnapshot = false;
        }

        private static List<UIWindowConstants.WindowUid> ResolveTargetWindows(UIWindowManager windowManager, UiWindowVisibilityData data)
        {
            var managedWindows = windowManager.GetManagedWindowUids();
            if (managedWindows == null || managedWindows.Count <= 0)
            {
                return new List<UIWindowConstants.WindowUid>();
            }

            switch (data.mode)
            {
                case UiWindowVisibilityMode.IncludeOnly:
                    return FilterExistingWindowUids(windowManager, data.targetWindows);

                case UiWindowVisibilityMode.AllExcept:
                {
                    var excepts = new HashSet<UIWindowConstants.WindowUid>(FilterExistingWindowUids(windowManager, data.exceptWindows));
                    var result = new List<UIWindowConstants.WindowUid>();
                    foreach (var windowUid in managedWindows)
                    {
                        if (!excepts.Contains(windowUid))
                        {
                            result.Add(windowUid);
                        }
                    }

                    return result;
                }

                case UiWindowVisibilityMode.All:
                default:
                    return managedWindows;
            }
        }

        private static List<UIWindowConstants.WindowUid> FilterExistingWindowUids(UIWindowManager windowManager, List<UIWindowConstants.WindowUid> source)
        {
            var result = new List<UIWindowConstants.WindowUid>();
            if (source == null || source.Count <= 0)
            {
                return result;
            }

            var seen = new HashSet<UIWindowConstants.WindowUid>();
            foreach (var windowUid in source)
            {
                if (windowUid == UIWindowConstants.WindowUid.None || seen.Contains(windowUid))
                {
                    continue;
                }

                if (!windowManager.HasManagedWindow(windowUid))
                {
                    continue;
                }

                seen.Add(windowUid);
                result.Add(windowUid);
            }

            return result;
        }
    }
}
