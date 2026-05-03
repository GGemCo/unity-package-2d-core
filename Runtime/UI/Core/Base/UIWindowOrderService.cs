using System;
using System.Collections.Generic;

namespace GGemCo2DCore
{
    /// <summary>
    /// TableWindow 정렬값과 외부 등록 규칙을 기준으로 UIWindow sibling 순서를 관리합니다.
    /// </summary>
    internal sealed class UIWindowOrderService
    {
        private readonly UIWindowRegistry _registry;
        private readonly UIWindowTableBinder _tableBinder;
        private readonly Func<List<WindowKey>> _getWindowKeys;
        private readonly List<ExternalWindowRegistration> _externalWindowRegistrations =
            new List<ExternalWindowRegistration>();
        private readonly Dictionary<string, ExternalWindowRegistration> _externalWindowRegistrationMap =
            new Dictionary<string, ExternalWindowRegistration>(StringComparer.Ordinal);
        private int _externalWindowRegistrationSequence;

        /// <summary>
        /// UIWindow 정렬 서비스를 생성합니다.
        /// </summary>
        /// <param name="registry">UID 기준 UIWindow 참조 캐시입니다.</param>
        /// <param name="tableBinder">TableWindow 바인딩 정보입니다.</param>
        /// <param name="getWindowKeys">현재 WindowKey 목록을 반환하는 함수입니다.</param>
        public UIWindowOrderService(
            UIWindowRegistry registry,
            UIWindowTableBinder tableBinder,
            Func<List<WindowKey>> getWindowKeys)
        {
            _registry = registry;
            _tableBinder = tableBinder;
            _getWindowKeys = getWindowKeys;
        }

        /// <summary>
        /// 외부 UIWindow 등록 정보를 보관합니다.
        /// </summary>
        private sealed class ExternalWindowRegistration
        {
            public string key;
            public UIWindow window;
            public UIWindowConstants.WindowUid anchorUid;
            public UIWindowManager.ExternalWindowInsertMode insertMode;
            public int priority;
            public int sequence;
        }

        /// <summary>
        /// TableWindow 정렬에 필요한 core UIWindow 정보를 보관합니다.
        /// </summary>
        private sealed class ManagedWindowOrderInfo
        {
            public UIWindow window;
            public int ordering;
            public int uid;
        }

        /// <summary>
        /// 현재 관리 대상 UIWindow를 최종 sibling 순서대로 반환합니다.
        /// </summary>
        /// <returns>정렬된 UIWindow 목록입니다.</returns>
        public List<UIWindow> GetManagedWindows()
        {
            return BuildManagedWindowOrder();
        }

        /// <summary>
        /// 현재 관리 대상 UIWindow의 Transform sibling index를 정렬 순서에 맞게 갱신합니다.
        /// </summary>
        public void RefreshWindowOrder()
        {
            _registry.Rebuild(_getWindowKeys?.Invoke());

            List<UIWindow> orderedWindows = BuildManagedWindowOrder();
            for (int i = 0; i < orderedWindows.Count; i++)
            {
                UIWindow window = orderedWindows[i];
                if (window == null)
                {
                    continue;
                }

                window.transform.SetSiblingIndex(i);
            }
        }

        /// <summary>
        /// core TableWindow 목록 바깥의 UIWindow를 정렬 목록에 등록합니다.
        /// </summary>
        /// <param name="key">외부 윈도우 등록을 구분하는 고유 key입니다.</param>
        /// <param name="window">등록할 UIWindow입니다.</param>
        /// <param name="anchorUid">Before/After 기준이 되는 core UIWindow UID입니다.</param>
        /// <param name="insertMode">외부 윈도우를 삽입할 위치 규칙입니다.</param>
        /// <param name="priority">동일 규칙 안에서 사용할 우선순위입니다.</param>
        /// <returns>등록에 성공하면 true입니다.</returns>
        public bool RegisterExternalWindow(
            string key,
            UIWindow window,
            UIWindowConstants.WindowUid anchorUid,
            UIWindowManager.ExternalWindowInsertMode insertMode = UIWindowManager.ExternalWindowInsertMode.After,
            int priority = 0)
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

            if (!_externalWindowRegistrationMap.TryGetValue(key, out ExternalWindowRegistration registration))
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

        /// <summary>
        /// key로 등록된 외부 UIWindow를 정렬 목록에서 제거합니다.
        /// </summary>
        /// <param name="key">제거할 외부 윈도우 등록 key입니다.</param>
        /// <returns>등록 항목이 제거되었으면 true입니다.</returns>
        public bool UnregisterExternalWindow(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                return false;
            }

            if (!_externalWindowRegistrationMap.TryGetValue(key, out ExternalWindowRegistration registration))
            {
                return false;
            }

            _externalWindowRegistrationMap.Remove(key);
            _externalWindowRegistrations.Remove(registration);
            return true;
        }

        /// <summary>
        /// TableWindow에 등록된 core UIWindow를 테이블 ordering 기준으로 정렬합니다.
        /// </summary>
        /// <returns>정렬 정보 목록입니다.</returns>
        private List<ManagedWindowOrderInfo> GetCoreManagedWindowOrderInfos()
        {
            List<ManagedWindowOrderInfo> result = new List<ManagedWindowOrderInfo>();
            foreach (KeyValuePair<int, StruckTableWindow> pair in _tableBinder.TableWindows)
            {
                int uid = pair.Key;
                if (uid <= 0)
                {
                    continue;
                }

                UIWindow window = _registry.GetWindowReferenceByUid(uid);
                if (window == null)
                {
                    continue;
                }

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
                if (compare != 0)
                {
                    return compare;
                }

                return a.uid.CompareTo(b.uid);
            });
            return result;
        }

        /// <summary>
        /// 외부 등록 UIWindow를 priority와 등록 순서 기준으로 정렬합니다.
        /// </summary>
        /// <returns>정렬된 외부 등록 목록입니다.</returns>
        private List<ExternalWindowRegistration> GetOrderedExternalRegistrations()
        {
            List<ExternalWindowRegistration> result = new List<ExternalWindowRegistration>();
            for (int i = 0; i < _externalWindowRegistrations.Count; i++)
            {
                ExternalWindowRegistration registration = _externalWindowRegistrations[i];
                if (registration == null || registration.window == null)
                {
                    continue;
                }

                result.Add(registration);
            }

            result.Sort((a, b) =>
            {
                int compare = a.priority.CompareTo(b.priority);
                if (compare != 0)
                {
                    return compare;
                }

                return a.sequence.CompareTo(b.sequence);
            });
            return result;
        }

        /// <summary>
        /// 중복을 제외하고 외부 등록 UIWindow를 대상 목록 뒤에 붙입니다.
        /// </summary>
        /// <param name="target">UIWindow를 추가할 대상 목록입니다.</param>
        /// <param name="registrations">추가할 외부 등록 목록입니다.</param>
        private static void AppendRegistrations(
            List<UIWindow> target,
            List<ExternalWindowRegistration> registrations)
        {
            if (target == null || registrations == null)
            {
                return;
            }

            for (int i = 0; i < registrations.Count; i++)
            {
                ExternalWindowRegistration registration = registrations[i];
                if (registration == null || registration.window == null || target.Contains(registration.window))
                {
                    continue;
                }

                target.Add(registration.window);
            }
        }

        /// <summary>
        /// core UIWindow와 외부 등록 UIWindow를 하나의 최종 정렬 목록으로 조합합니다.
        /// </summary>
        /// <returns>최종 sibling 순서에 맞춘 UIWindow 목록입니다.</returns>
        private List<UIWindow> BuildManagedWindowOrder()
        {
            List<ManagedWindowOrderInfo> orderedCoreWindows = GetCoreManagedWindowOrderInfos();
            List<ExternalWindowRegistration> externalRegistrations = GetOrderedExternalRegistrations();
            List<ExternalWindowRegistration> firstRegistrations = new List<ExternalWindowRegistration>();
            List<ExternalWindowRegistration> lastRegistrations = new List<ExternalWindowRegistration>();
            Dictionary<int, List<ExternalWindowRegistration>> beforeRegistrationsByAnchor =
                new Dictionary<int, List<ExternalWindowRegistration>>();
            Dictionary<int, List<ExternalWindowRegistration>> afterRegistrationsByAnchor =
                new Dictionary<int, List<ExternalWindowRegistration>>();

            for (int i = 0; i < externalRegistrations.Count; i++)
            {
                ExternalWindowRegistration registration = externalRegistrations[i];
                if (registration == null || registration.window == null)
                {
                    continue;
                }

                switch (registration.insertMode)
                {
                    case UIWindowManager.ExternalWindowInsertMode.First:
                        firstRegistrations.Add(registration);
                        break;
                    case UIWindowManager.ExternalWindowInsertMode.Before:
                        AddAnchoredRegistration(
                            registration,
                            beforeRegistrationsByAnchor,
                            lastRegistrations);
                        break;
                    case UIWindowManager.ExternalWindowInsertMode.After:
                        AddAnchoredRegistration(
                            registration,
                            afterRegistrationsByAnchor,
                            lastRegistrations);
                        break;
                    case UIWindowManager.ExternalWindowInsertMode.Last:
                    default:
                        lastRegistrations.Add(registration);
                        break;
                }
            }

            List<UIWindow> result = new List<UIWindow>();
            AppendRegistrations(result, firstRegistrations);

            for (int i = 0; i < orderedCoreWindows.Count; i++)
            {
                ManagedWindowOrderInfo coreInfo = orderedCoreWindows[i];
                if (coreInfo == null || coreInfo.window == null)
                {
                    continue;
                }

                if (beforeRegistrationsByAnchor.TryGetValue(coreInfo.uid, out List<ExternalWindowRegistration> beforeRegistrations))
                {
                    AppendRegistrations(result, beforeRegistrations);
                }

                if (!result.Contains(coreInfo.window))
                {
                    result.Add(coreInfo.window);
                }

                if (afterRegistrationsByAnchor.TryGetValue(coreInfo.uid, out List<ExternalWindowRegistration> afterRegistrations))
                {
                    AppendRegistrations(result, afterRegistrations);
                }
            }

            AppendRegistrations(result, lastRegistrations);
            return result;
        }

        /// <summary>
        /// anchor UID가 유효하면 anchor별 등록 목록에 추가하고, 유효하지 않으면 마지막 등록 목록으로 보냅니다.
        /// </summary>
        /// <param name="registration">분류할 외부 윈도우 등록 정보입니다.</param>
        /// <param name="registrationsByAnchor">anchor UID별 등록 목록입니다.</param>
        /// <param name="lastRegistrations">anchor를 찾지 못한 등록을 모을 마지막 목록입니다.</param>
        private void AddAnchoredRegistration(
            ExternalWindowRegistration registration,
            Dictionary<int, List<ExternalWindowRegistration>> registrationsByAnchor,
            List<ExternalWindowRegistration> lastRegistrations)
        {
            int anchorUid = (int)registration.anchorUid;
            if (!_tableBinder.TableWindows.ContainsKey(anchorUid))
            {
                lastRegistrations.Add(registration);
                return;
            }

            if (!registrationsByAnchor.TryGetValue(anchorUid, out List<ExternalWindowRegistration> anchoredRegistrations))
            {
                anchoredRegistrations = new List<ExternalWindowRegistration>();
                registrationsByAnchor.Add(anchorUid, anchoredRegistrations);
            }

            anchoredRegistrations.Add(registration);
        }
    }
}
