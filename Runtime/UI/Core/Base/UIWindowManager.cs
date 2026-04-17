using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace GGemCo2DCore
{
    /// <summary>
    /// 윈도우 관리 매니저
    /// </summary>
    public class UIWindowManager : MonoBehaviour
    {
        [Header("기본속성")] 
        [Tooltip("아이콘 선택 이미지")]
        public GameObject prefabIconOver;
        private Image _imageIconOver;
        public GameObject prefabIconSelected;
        private Image _imageIconSelected;
        
        [Tooltip("윈도우 리스트")]
        [SerializeField] private UIWindow[] uiWindows;
        public void SetUIWindow(UIWindow[] prefabs) => uiWindows = prefabs;
        
        private readonly Dictionary<int, StruckTableWindow> _struckTableWindows = new Dictionary<int, StruckTableWindow>();
        private readonly List<ExternalWindowRegistration> _externalWindowRegistrations = new List<ExternalWindowRegistration>();
        private readonly Dictionary<string, ExternalWindowRegistration> _externalWindowRegistrationMap =
            new Dictionary<string, ExternalWindowRegistration>(StringComparer.Ordinal);
        private int _externalWindowRegistrationSequence;

        /// <summary>
        /// 일시적으로 UI 표시 상태를 저장하기 위한 스택입니다.
        /// 중첩된 연출에서도 마지막으로 저장한 상태부터 역순으로 복원합니다.
        /// </summary>
        private readonly Stack<VisibilityStateEntry> _visibilityStateStack = new Stack<VisibilityStateEntry>();

        public enum ExternalWindowInsertMode
        {
            Before = 0,
            After = 1,
            First = 2,
            Last = 3,
        }

        private sealed class ExternalWindowRegistration
        {
            public string key;
            public UIWindow window;
            public UIWindowConstants.WindowUid anchorUid;
            public ExternalWindowInsertMode insertMode;
            public int priority;
            public int sequence;
        }

        private sealed class WindowVisibilityStateItem
        {
            public UIWindow window;
            public bool visible;
        }

        private sealed class VisibilityStateEntry
        {
            public List<WindowVisibilityStateItem> state;
            public UIWindowConstants.UIWindowVisibilityApplyMode restoreMode;
        }

        private void Awake()
        {
            _struckTableWindows.Clear();
            
            InitializationTableInfo();
        }

        private void Start()
        {
            MakeIconOver();
            MakeIconSelected();
        }

        private void MakeIconOver()
        {
            if (prefabIconOver == null) return;
            _imageIconOver = Instantiate(prefabIconOver, SceneGame.Instance.canvasUI.transform)?.GetComponent<Image>();
            if (_imageIconOver == null) return;
            _imageIconOver.gameObject.SetActive(false);
        }

        private void MakeIconSelected()
        {
            if (prefabIconSelected == null) return;
            _imageIconSelected = Instantiate(prefabIconSelected, SceneGame.Instance.canvasUI.transform)?.GetComponent<Image>();
            if (_imageIconSelected == null) return;
            _imageIconSelected.gameObject.SetActive(false);
        }

        /// <summary>
        /// 각 윈도우에 table 정보 연결하기
        /// </summary>
        private void InitializationTableInfo()
        {
            if (TableLoaderManager.Instance == null) return;
            if (uiWindows == null || uiWindows.Length <= 0) return;

            TableWindow tableWindow = TableLoaderManager.Instance.TableWindow;
            var tables = tableWindow.GetDatas();
            if (tables == null) return;
            // datas를 Ordering 컬럼 기준으로 정렬된 새로운 Dictionary 만들기
            var orderedDatas = tables
                .OrderBy(kv => kv.Value.Ordering) // Ordering 값 기준으로 정렬
                .ToDictionary(kv => kv.Key, kv => kv.Value);

            foreach (var table in orderedDatas)
            {
                int uid = table.Key;
                if (uid == 0) continue;
                StruckTableWindow info = tableWindow.GetDataByUid(uid);
                if (info == null || info.Uid <= 0) continue;
                if (uid >= uiWindows.Length || !uiWindows[uid]) continue;
                UIWindow window = uiWindows[uid].gameObject.GetComponent<UIWindow>();
                if (window == null)
                {
                    GcLogger.LogError($"{nameof(UIWindow)} 스크립트가 없습니다.");
                    continue;
                }

                if (!info.UseInGame)
                {
                    window.gameObject.SetActive(false);
                    continue;
                }

                window.SetTableWindow(info);
                _struckTableWindows[info.Uid] = info;
            }

            RefreshWindowOrder();
        }
        private sealed class ManagedWindowOrderInfo
        {
            public UIWindow window;
            public int ordering;
            public int uid;
        }

        private List<ManagedWindowOrderInfo> GetCoreManagedWindowOrderInfos()
        {
            var result = new List<ManagedWindowOrderInfo>();
            foreach (var pair in _struckTableWindows)
            {
                int uid = pair.Key;
                if (uid <= 0) continue;
                if (uiWindows == null || uid >= uiWindows.Length) continue;

                UIWindow window = uiWindows[uid];
                if (window == null) continue;

                result.Add(new ManagedWindowOrderInfo
                {
                    window = window,
                    ordering = pair.Value.Ordering,
                    uid = uid,
                });
            }

            result.Sort((a, b) =>
            {
                int compare = a.ordering.CompareTo(b.ordering);
                if (compare != 0) return compare;
                return a.uid.CompareTo(b.uid);
            });
            return result;
        }

        private List<ExternalWindowRegistration> GetOrderedExternalRegistrations()
        {
            var result = new List<ExternalWindowRegistration>();
            for (int i = 0; i < _externalWindowRegistrations.Count; i++)
            {
                var registration = _externalWindowRegistrations[i];
                if (registration == null || registration.window == null)
                {
                    continue;
                }

                result.Add(registration);
            }

            result.Sort((a, b) =>
            {
                int compare = a.priority.CompareTo(b.priority);
                if (compare != 0) return compare;
                return a.sequence.CompareTo(b.sequence);
            });
            return result;
        }

        private static void AppendRegistrations(List<UIWindow> target, List<ExternalWindowRegistration> registrations)
        {
            if (target == null || registrations == null) return;

            for (int i = 0; i < registrations.Count; i++)
            {
                var registration = registrations[i];
                if (registration == null || registration.window == null) continue;
                if (target.Contains(registration.window)) continue;
                target.Add(registration.window);
            }
        }

        private List<UIWindow> BuildManagedWindowOrder()
        {
            var orderedCoreWindows = GetCoreManagedWindowOrderInfos();
            var externalRegistrations = GetOrderedExternalRegistrations();
            var firstRegistrations = new List<ExternalWindowRegistration>();
            var lastRegistrations = new List<ExternalWindowRegistration>();
            var beforeRegistrationsByAnchor = new Dictionary<int, List<ExternalWindowRegistration>>();
            var afterRegistrationsByAnchor = new Dictionary<int, List<ExternalWindowRegistration>>();

            for (int i = 0; i < externalRegistrations.Count; i++)
            {
                var registration = externalRegistrations[i];
                if (registration == null || registration.window == null)
                {
                    continue;
                }

                switch (registration.insertMode)
                {
                    case ExternalWindowInsertMode.First:
                        firstRegistrations.Add(registration);
                        break;
                    case ExternalWindowInsertMode.Before:
                    {
                        int anchorUid = (int)registration.anchorUid;
                        if (!_struckTableWindows.ContainsKey(anchorUid))
                        {
                            lastRegistrations.Add(registration);
                            break;
                        }

                        if (!beforeRegistrationsByAnchor.TryGetValue(anchorUid, out var beforeList))
                        {
                            beforeList = new List<ExternalWindowRegistration>();
                            beforeRegistrationsByAnchor.Add(anchorUid, beforeList);
                        }
                        beforeList.Add(registration);
                        break;
                    }
                    case ExternalWindowInsertMode.After:
                    {
                        int anchorUid = (int)registration.anchorUid;
                        if (!_struckTableWindows.ContainsKey(anchorUid))
                        {
                            lastRegistrations.Add(registration);
                            break;
                        }

                        if (!afterRegistrationsByAnchor.TryGetValue(anchorUid, out var afterList))
                        {
                            afterList = new List<ExternalWindowRegistration>();
                            afterRegistrationsByAnchor.Add(anchorUid, afterList);
                        }
                        afterList.Add(registration);
                        break;
                    }
                    case ExternalWindowInsertMode.Last:
                    default:
                        lastRegistrations.Add(registration);
                        break;
                }
            }

            var result = new List<UIWindow>();
            AppendRegistrations(result, firstRegistrations);

            for (int i = 0; i < orderedCoreWindows.Count; i++)
            {
                var coreInfo = orderedCoreWindows[i];
                if (coreInfo == null || coreInfo.window == null)
                {
                    continue;
                }

                if (beforeRegistrationsByAnchor.TryGetValue(coreInfo.uid, out var beforeRegistrations))
                {
                    AppendRegistrations(result, beforeRegistrations);
                }

                if (!result.Contains(coreInfo.window))
                {
                    result.Add(coreInfo.window);
                }

                if (afterRegistrationsByAnchor.TryGetValue(coreInfo.uid, out var afterRegistrations))
                {
                    AppendRegistrations(result, afterRegistrations);
                }
            }

            AppendRegistrations(result, lastRegistrations);
            return result;
        }

        public void RefreshWindowOrder()
        {
            var orderedWindows = BuildManagedWindowOrder();
            for (int i = 0; i < orderedWindows.Count; i++)
            {
                var window = orderedWindows[i];
                if (window == null) continue;
                window.transform.SetSiblingIndex(i);
            }
        }

        public bool RegisterExternalWindow(string key, UIWindow window, UIWindowConstants.WindowUid anchorUid,
            ExternalWindowInsertMode insertMode = ExternalWindowInsertMode.After, int priority = 0)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                GcLogger.LogError("외부 UIWindow 등록 key 값이 비어 있습니다.");
                return false;
            }

            if (window == null)
            {
                GcLogger.LogError($"외부 UIWindow 등록 대상이 없습니다. key:{key}");
                return false;
            }

            if (!_externalWindowRegistrationMap.TryGetValue(key, out var registration))
            {
                registration = new ExternalWindowRegistration
                {
                    key = key,
                    sequence = _externalWindowRegistrationSequence++,
                };
                _externalWindowRegistrationMap.Add(key, registration);
                _externalWindowRegistrations.Add(registration);
            }

            registration.window = window;
            registration.anchorUid = anchorUid;
            registration.insertMode = insertMode;
            registration.priority = priority;

            RefreshWindowOrder();
            return true;
        }

        public bool UnregisterExternalWindow(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                return false;
            }

            if (!_externalWindowRegistrationMap.TryGetValue(key, out var registration))
            {
                return false;
            }

            _externalWindowRegistrationMap.Remove(key);
            _externalWindowRegistrations.Remove(registration);
            RefreshWindowOrder();
            return true;
        }

        /// <summary>
        /// 윈도우 보임/안보임 처리 
        /// </summary>
        /// <param name="uid"></param>
        /// <param name="show"></param>
        public void ShowWindow(UIWindowConstants.WindowUid uid, bool show)
        {
            ShowWindow(uid, show, UIWindowConstants.UIWindowVisibilityApplyMode.Normal);
        }

        /// <summary>
        /// 윈도우 보임/안보임 처리 시 적용 모드를 지정합니다.
        /// </summary>
        public void ShowWindow(UIWindowConstants.WindowUid uid, bool show, UIWindowConstants.UIWindowVisibilityApplyMode mode)
        {
            UIWindow uiWindow = GetUIWindowByUid<UIWindow>(uid);
            if (uiWindow == null) {
                GcLogger.LogError($"{nameof(UIWindow)} 컴포넌트가 없습니다. uid:"+uid);
                return;
            }

            switch (mode)
            {
                case UIWindowConstants.UIWindowVisibilityApplyMode.ImmediateSilent:
                    uiWindow.SetVisibleImmediate(show, invokeOnShow: false, followLinkedWindows: false);
                    break;
                case UIWindowConstants.UIWindowVisibilityApplyMode.Normal:
                default:
                    uiWindow.Show(show);
                    break;
            }
        }
        /// <summary>
        /// 특정 윈도우에서 아이콘 가져오기
        /// </summary>
        /// <param name="srcWindowUid"></param>
        /// <param name="srcIndex"></param>
        /// <returns></returns>
        private UIIcon GetIconByWindowUid(UIWindowConstants.WindowUid srcWindowUid, int srcIndex)
        {
            UIWindow uiWindow = GetUIWindowByUid<UIWindow>(srcWindowUid);
            if (uiWindow == null)
            {
                GcLogger.LogError($"{nameof(UIWindow)} 컴포넌트가 없습니다. uid:"+srcWindowUid);
                return null;
            }
            return uiWindow.GetIconByIndex(srcIndex);
        }
        /// <summary>
        /// UIWindow 찾기 
        /// </summary>
        /// <param name="windowUid"></param>
        /// <returns></returns>
        public T GetUIWindowByUid<T>(UIWindowConstants.WindowUid windowUid) where T : UIWindow
        {
            int uid = (int)windowUid;
            if (uiWindows.Length <= uid) return null;
            StruckTableWindow info = _struckTableWindows.GetValueOrDefault(uid);
            if (info == null) return null;
            if (!info.UseInGame) return null;
            UIWindow uiWindow = uiWindows[uid];
            if (uiWindow == null)
            {
                GcLogger.LogError($"{nameof(UIWindow)} 컴포넌트가 없습니다. uid:"+windowUid);
                return null;
            }

            return uiWindow as T;
        }
        /// <summary>
        /// 특정 윈도우에서 아이콘 지우기
        /// </summary>
        /// <param name="windowUid"></param>
        /// <param name="slotIndex"></param>
        public void RemoveIcon(UIWindowConstants.WindowUid windowUid, int slotIndex)
        {
            UIWindow uiWindow = GetUIWindowByUid<UIWindow>(windowUid);
            if (uiWindow == null)
            {
                GcLogger.LogError($"{nameof(UIWindow)} 컴포넌트가 없습니다. uid:"+windowUid);
                return;
            }
            uiWindow.DetachIcon(slotIndex);
        }
        /// <summary>
        /// 윈도우가 활성화 되어있는지 체크
        /// </summary>
        /// <param name="windowUid"></param>
        /// <returns></returns>
        public bool IsShowByWindowUid(UIWindowConstants.WindowUid windowUid)
        {
            UIWindow uiWindow = GetUIWindowByUid<UIWindow>(windowUid);
            if (uiWindow == null) return false;
            return uiWindow.gameObject.activeSelf;
        }


        /// <summary>
        /// 관리 중인 윈도우인지 여부를 반환합니다.
        /// </summary>
        public bool HasManagedWindow(UIWindowConstants.WindowUid windowUid)
        {
            return GetUIWindowByUid<UIWindow>(windowUid) != null;
        }

        /// <summary>
        /// 현재 관리 중인 윈도우 UID 목록을 반환합니다.
        /// </summary>
        public List<UIWindowConstants.WindowUid> GetManagedWindowUids()
        {
            var result = new List<UIWindowConstants.WindowUid>();
            foreach (var uid in _struckTableWindows.Keys)
            {
                if (uid <= 0) continue;
                var windowUid = (UIWindowConstants.WindowUid)uid;
                if (GetUIWindowByUid<UIWindow>(windowUid) == null) continue;
                result.Add(windowUid);
            }

            return result;
        }

        /// <summary>
        /// 현재 관리 중인 모든 윈도우를 정렬 순서 기준으로 반환합니다.
        /// Core window 와 외부 등록 window 를 모두 포함합니다.
        /// </summary>
        public List<UIWindow> GetManagedWindows()
        {
            return BuildManagedWindowOrder();
        }

        /// <summary>
        /// 지정한 윈도우들의 현재 표시 상태를 캡처합니다.
        /// </summary>
        public Dictionary<UIWindowConstants.WindowUid, bool> CaptureVisibilityState(IEnumerable<UIWindowConstants.WindowUid> windowUids)
        {
            var result = new Dictionary<UIWindowConstants.WindowUid, bool>();
            if (windowUids == null)
            {
                return result;
            }

            foreach (var windowUid in windowUids)
            {
                if (windowUid == UIWindowConstants.WindowUid.None || result.ContainsKey(windowUid))
                {
                    continue;
                }

                var uiWindow = GetUIWindowByUid<UIWindow>(windowUid);
                if (uiWindow == null)
                {
                    continue;
                }

                result[windowUid] = uiWindow.gameObject.activeSelf;
            }

            return result;
        }

        private static List<WindowVisibilityStateItem> CaptureVisibilityStateItems(IEnumerable<UIWindow> windows)
        {
            var result = new List<WindowVisibilityStateItem>();
            if (windows == null)
            {
                return result;
            }

            var addedWindows = new HashSet<UIWindow>();
            foreach (var window in windows)
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

        private static void RestoreVisibilityStateItems(IEnumerable<WindowVisibilityStateItem> state,
            UIWindowConstants.UIWindowVisibilityApplyMode mode)
        {
            if (state == null)
            {
                return;
            }

            foreach (var item in state)
            {
                if (item == null || item.window == null)
                {
                    continue;
                }

                switch (mode)
                {
                    case UIWindowConstants.UIWindowVisibilityApplyMode.ImmediateSilent:
                        item.window.SetVisibleImmediate(item.visible, invokeOnShow: false, followLinkedWindows: false);
                        break;
                    case UIWindowConstants.UIWindowVisibilityApplyMode.Normal:
                    default:
                        item.window.Show(item.visible);
                        break;
                }
            }
        }

        public Dictionary<UIWindow, bool> CaptureVisibilityState(IEnumerable<UIWindow> windows)
        {
            var result = new Dictionary<UIWindow, bool>();
            var items = CaptureVisibilityStateItems(windows);
            for (int i = 0; i < items.Count; i++)
            {
                var item = items[i];
                if (item == null || item.window == null) continue;
                result[item.window] = item.visible;
            }

            return result;
        }

        /// <summary>
        /// 저장된 윈도우 표시 상태를 복원합니다.
        /// </summary>
        public void RestoreVisibilityState(IReadOnlyDictionary<UIWindowConstants.WindowUid, bool> state)
        {
            RestoreVisibilityState(state, UIWindowConstants.UIWindowVisibilityApplyMode.Normal);
        }

        /// <summary>
        /// 저장된 윈도우 표시 상태를 지정한 적용 모드로 복원합니다.
        /// </summary>
        public void RestoreVisibilityState(IReadOnlyDictionary<UIWindowConstants.WindowUid, bool> state, UIWindowConstants.UIWindowVisibilityApplyMode mode)
        {
            if (state == null)
            {
                return;
            }

            foreach (var pair in state)
            {
                ShowWindow(pair.Key, pair.Value, mode);
            }
        }

        /// <summary>
        /// 지정한 윈도우들의 현재 표시 상태를 스택에 저장합니다.
        /// 이후 <see cref="PopVisibilityState"/> 호출 시 마지막으로 저장한 상태부터 역순으로 복원됩니다.
        /// </summary>
        /// <returns>실제로 저장된 스냅샷이 있으면 true, 아니면 false입니다.</returns>
        public bool PushVisibilityState(IEnumerable<UIWindowConstants.WindowUid> windowUids, UIWindowConstants.UIWindowVisibilityApplyMode restoreMode = UIWindowConstants.UIWindowVisibilityApplyMode.Normal)
        {
            if (windowUids == null)
            {
                return false;
            }

            var windows = new List<UIWindow>();
            foreach (var windowUid in windowUids)
            {
                var uiWindow = GetUIWindowByUid<UIWindow>(windowUid);
                if (uiWindow == null)
                {
                    continue;
                }

                windows.Add(uiWindow);
            }

            return PushVisibilityState(windows, restoreMode);
        }

        public bool PushVisibilityState(IEnumerable<UIWindow> windows,
            UIWindowConstants.UIWindowVisibilityApplyMode restoreMode = UIWindowConstants.UIWindowVisibilityApplyMode.Normal)
        {
            var snapshot = CaptureVisibilityStateItems(windows);
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
        /// 스택에 저장된 가장 마지막 윈도우 표시 상태를 복원합니다.
        /// </summary>
        /// <returns>복원에 성공하면 true, 복원할 스냅샷이 없으면 false입니다.</returns>
        public bool PopVisibilityState()
        {
            if (_visibilityStateStack.Count <= 0)
            {
                return false;
            }

            var entry = _visibilityStateStack.Pop();
            RestoreVisibilityStateItems(entry.state, entry.restoreMode);
            return true;
        }

        /// <summary>
        /// 현재 저장된 윈도우 표시 상태 스택을 모두 비웁니다.
        /// </summary>
        public void ClearVisibilityStateStack()
        {
            _visibilityStateStack.Clear();
        }

        /// <summary>
        /// 저장된 윈도우 표시 상태 스택 개수를 반환합니다.
        /// </summary>
        public int GetVisibilityStateStackCount()
        {
            return _visibilityStateStack.Count;
        }

        /// <summary>
        /// 지정한 윈도우들을 일괄 표시/숨김 처리합니다.
        /// </summary>
        public void SetWindowsVisible(IEnumerable<UIWindowConstants.WindowUid> windowUids, bool show)
        {
            SetWindowsVisible(windowUids, show, UIWindowConstants.UIWindowVisibilityApplyMode.Normal);
        }

        /// <summary>
        /// 지정한 윈도우들을 일괄 표시/숨김 처리합니다.
        /// </summary>
        public void SetWindowsVisible(IEnumerable<UIWindowConstants.WindowUid> windowUids, bool show, UIWindowConstants.UIWindowVisibilityApplyMode mode)
        {
            if (windowUids == null)
            {
                return;
            }

            foreach (var windowUid in windowUids)
            {
                ShowWindow(windowUid, show, mode);
            }
        }

        public void SetWindowsVisible(IEnumerable<UIWindow> windows, bool show,
            UIWindowConstants.UIWindowVisibilityApplyMode mode = UIWindowConstants.UIWindowVisibilityApplyMode.Normal)
        {
            if (windows == null)
            {
                return;
            }

            foreach (var window in windows)
            {
                if (window == null)
                {
                    continue;
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
        }

        /// <summary>
        /// fromWindowUid 의 fromIndex 에 있는 아이템을 toWindowUid 로 toCount 개수 옮기기.
        /// toIndex 있으면 해당 위치로 옮기기 
        /// </summary>
        /// <param name="fromWindowUid"></param>
        /// <param name="fromIndex"></param>
        /// <param name="toWindowUid"></param>
        /// <param name="toCount"></param>
        /// <param name="toIndex"></param>
        public void MoveIcon(UIWindowConstants.WindowUid fromWindowUid, int fromIndex, UIWindowConstants.WindowUid toWindowUid, int toCount, int toIndex = -1)
        {
            UIWindow fromWindow = GetUIWindowByUid<UIWindow>(fromWindowUid);
            UIWindow toWindow = GetUIWindowByUid<UIWindow>(toWindowUid);
            if (fromWindow == null || toWindow == null)
            {
                GcLogger.LogError("from window 또는 to window 값이 잘 못 되었습니다. from window:"+fromWindowUid+"/to window:"+toWindowUid);
                return;
            }
            UIIcon fromIcon = fromWindow.GetIconByIndex(fromIndex);
            if (fromIcon == null) return;
            int fromIconUid = fromIcon.uid;
            long fromIconInstanceId = fromIcon.instanceId;
            
            fromWindow.SetIconCount(fromIndex, fromIcon.uid, fromIcon.GetCount() - toCount, instanceId:fromIconInstanceId);
            // 특정 슬롯으로 이동
            if (toIndex >= 0)
            {
                // 그 위치에 아이콘이 있으면 되돌려준다
                var toIcon = toWindow.GetIconByIndex(toIndex);
                if (toIcon != null && toIcon.uid > 0 && toIcon.GetCount() > 0)
                {
                    fromWindow.SetIconCount(toIcon.uid, toIcon.GetCount(), instanceId:toIcon.instanceId);
                }
                toWindow.SetIconCount(toIndex, fromIconUid, toCount, instanceId:fromIconInstanceId);
            }
            // 비어있는 슬롯으로 이동
            else
            {
                toWindow.SetIconCount(fromIconUid, toCount, instanceId:fromIconInstanceId);
            }
        }
        /// <summary>
        /// 등록 해제하기
        /// </summary>
        /// <param name="fromWindowUid"></param>
        /// <param name="fromIndex"></param>
        /// <param name="toWindowUid"></param>
        public void UnRegisterIcon(UIWindowConstants.WindowUid fromWindowUid, int fromIndex, UIWindowConstants.WindowUid toWindowUid = UIWindowConstants.WindowUid.Inventory)
        {
            UIWindow fromWindow = GetUIWindowByUid<UIWindow>(fromWindowUid);
            UIWindow toWindow = GetUIWindowByUid<UIWindow>(toWindowUid);
            if (fromWindow == null || toWindow == null)
            {
                GcLogger.LogError("from window 또는 to window 값이 잘 못 되었습니다. from window:"+fromWindowUid+"/to window:"+toWindowUid);
                return;
            }
            UIIcon fromIcon = fromWindow.GetIconByIndex(fromIndex);
            if (fromIcon == null) return;
            var info = fromIcon.GetParentInfo();
            UIWindowConstants.WindowUid parentWindowUid = info.Item1;
            if (parentWindowUid == UIWindowConstants.WindowUid.None) return;
            int parentIconIndex = info.Item2;
            UIWindow parent = GetUIWindowByUid<UIWindow>(parentWindowUid);
            var parentIcon = parent.GetIconByIndex(parentIconIndex);
            if (parentIcon != null)
            {
                parentIcon.SetIconLock(false);
            }
            
            fromWindow.DetachIcon(fromIndex);
            
        }
        /// <summary>
        /// 아이콘 등록하기
        /// </summary>
        /// <param name="fromWindowUid"></param>
        /// <param name="fromIndex"></param>
        /// <param name="toWindowUid"></param>
        /// <param name="toCount"></param>
        /// <param name="toIndex"></param>
        public void RegisterIcon(UIWindowConstants.WindowUid fromWindowUid, int fromIndex, UIWindowConstants.WindowUid toWindowUid, int toCount, int toIndex = -1)
        {
            UIWindow fromWindow = GetUIWindowByUid<UIWindow>(fromWindowUid);
            UIWindow toWindow = GetUIWindowByUid<UIWindow>(toWindowUid);
            if (fromWindow == null || toWindow == null)
            {
                GcLogger.LogError("from window 또는 to window 값이 잘 못 되었습니다. from window:"+fromWindowUid+"/to window:"+toWindowUid);
                return;
            }
            UIIcon fromIcon = fromWindow.GetIconByIndex(fromIndex);
            if (fromIcon == null) return;

            fromIcon.SetIconLock(true);
            int itemUid = fromIcon.uid;
            long itemInstanceId = fromIcon.instanceId;
            
            if (toIndex >= 0)
            {
                // 그 위치에 아이콘이 있으면 되돌려준다
                var icon = toWindow.GetIconByIndex(toIndex);
                if (icon != null && icon.uid > 0 && icon.GetCount() > 0)
                {
                    fromWindow.SetIconCount(icon.uid, icon.GetCount(), instanceId: itemInstanceId);
                }
                UIIcon uiIcon = toWindow.SetIconCountReturnIcon(toIndex, itemUid, toCount, instanceId: itemInstanceId);
                if (uiIcon != null)
                {
                    uiIcon.SetParentInfo(fromWindowUid, fromIndex);
                }
            }
            else
            {
                UIIcon uiIcon = toWindow.SetIconCountReturnIcon(itemUid, toCount, instanceId: itemInstanceId);
                if (uiIcon != null)
                {
                    uiIcon.SetParentInfo(fromWindowUid, fromIndex);
                }
            }
        }
        /// <summary>
        /// 모든 윈도우 닫기
        /// </summary>
        /// <param name="exceptWindowUids">제외할 윈도우 uid</param>
        public void CloseAll(List<UIWindowConstants.WindowUid> exceptWindowUids = null)
        {
            var managedWindows = GetManagedWindows();
            for (int i = 0; i < managedWindows.Count; i++)
            {
                var window = managedWindows[i];
                if (window == null) continue;
                if (window.GetDefaultActive()) continue;
                if (!window.gameObject.activeSelf) continue;
                if (exceptWindowUids is { Count: > 0 } && exceptWindowUids.Contains(window.uid)) continue;
                window.Show(false);
            }
        }
        /// <summary>
        /// UIWindow 스크립트 추가하기
        /// </summary>
        /// <param name="className"></param>
        /// <returns></returns>
        private UIWindow AddUIComponent(string className)
        {
            if (className == "") return null;
            GameObject go = GameObject.Find(className);
            if (go == null)
            {
                GcLogger.LogError($"{className} 게임 오브젝트를 찾지 못 했습니다.");
                return null;
            }

            // 문자열 → Type
            Type type = Type.GetType($"{ConfigDefine.NameSDK}.Scripts.{className}");
            if (type == null)
            {
                GcLogger.LogError($"{className} 스크립트를 찾지 못 했습니다. 네임스페이스 설정을 확인해주세요.");
                return null;
            }

            // AddComponent(Type)
            if (go.GetComponent(type) == null)
            {
                // go.AddComponent(type);
                GcLogger.LogError($"{className} 컴포넌트가 없습니다.");
                return null;
            }
            else
            {
                // GcLogger.Log($"{className} already has component {className}");
            }
            return go.GetComponent<UIWindow>();
        }
        private void OnDestroy()
        {
            
        }

        public void ShowOverIconImage(bool show, Vector2? position = null, Vector2? slotSize = null)
        {
            if (_imageIconOver == null)
                return;

            _imageIconOver.gameObject.SetActive(show);

            if (!show) return;

            // position이 null이면 기존 위치 유지
            if (position.HasValue)
                _imageIconOver.rectTransform.position = position.Value;

            // size가 null이면 기존 사이즈 유지
            if (slotSize.HasValue)
                _imageIconOver.rectTransform.sizeDelta = slotSize.Value;
        }

        public void ShowSelectIconImage(bool show, Vector2? position = null, Vector2? slotSize = null)
        {
            if (_imageIconSelected == null)
            {
                // GcLogger.LogError($"{nameof(prefabIconSelected)}가 등록되지 않았습니다.");
                return;
            }
            _imageIconSelected.gameObject.SetActive(show);
            if (show)
            {
                // position이 null이면 기존 위치 유지
                if (position.HasValue)
                    _imageIconSelected.rectTransform.position = position.Value;

                // size가 null이면 기존 사이즈 유지
                if (slotSize.HasValue)
                    _imageIconSelected.rectTransform.sizeDelta = slotSize.Value;
            }
        }
    }
}
