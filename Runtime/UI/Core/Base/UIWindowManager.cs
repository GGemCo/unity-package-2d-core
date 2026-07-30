using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GGemCo2DCore
{
    /// <summary>
    /// UIWindow 시스템의 외부 진입점입니다.
    /// 세부 책임은 registry, table binder, order, visibility, slot activation, icon service로 위임합니다.
    /// </summary>
    public class UIWindowManager : MonoBehaviour
    {
        [Header("기본속성")]
        [Tooltip("아이콘 마우스 오버시 보여줄 이미지")]
        public GameObject prefabIconOver;

        [Tooltip("선택 되었을 때 보여줄 이미지, 이펙트")]
        [SerializeField] private GameObject prefabIconSelected;

        [Tooltip("보여줄 이미지 사이즈 고정 여부. False 경우, 해당 윈도우의 Slot Size로 적용됨.")]
        [SerializeField] private bool isSelectedIconSizeFixed;

        [Tooltip("선택 아이콘 이미지에 Animation2dController가 있을 때 사용할 기본 애니메이션 설정입니다.")]
        [SerializeField] private UISelectedIconAnimationSettings defaultSelectedIconAnimation =
            new UISelectedIconAnimationSettings();

        [Header("개별 UI 윈도우 연결")]
        public List<WindowKey> windowKeys = new List<WindowKey>();

        private UIWindow[] _uiWindows;
        private readonly UIWindowRegistry _windowRegistry = new UIWindowRegistry();
        private UIWindowTableBinder _windowTableBinder;
        private UIWindowOrderService _windowOrderService;
        private UIWindowVisibilityStateStack _visibilityStateStack;
        private UIWindowVisibilityService _windowVisibilityService;
        private UIWindowInitialVisibilityService _initialVisibilityService;
        private UIWindowSlotActivationService _slotActivationService;
        private UIWindowIconTransferService _iconTransferService;
        private UIWindowIconVisualPresenter _iconVisualPresenter;

        /// <summary>
        /// 모든 UIWindow의 초기 표시 상태와 기본 비활성 처리가 끝났을 때 발생합니다.
        /// </summary>
        public event Action<UIWindowManager> OnInitialWindowVisibilityApplied;

        /// <summary>
        /// UIWindowManager의 초기 표시 상태 적용이 완료되었는지 여부입니다.
        /// </summary>
        public bool IsInitialWindowVisibilityApplied { get; private set; }

        /// <summary>
        /// 외부 UIWindow를 core UIWindow 정렬 목록에 삽입할 위치 규칙입니다.
        /// </summary>
        public enum ExternalWindowInsertMode
        {
            Before = 0,
            After = 1,
            First = 2,
            Last = 3,
        }

        /// <summary>
        /// 외부에서 전달된 UIWindow 배열을 저장하고 참조 캐시를 다시 구성합니다.
        /// </summary>
        /// <param name="prefabs">UID fallback으로 사용할 UIWindow 배열입니다.</param>
        public void SetUIWindow(UIWindow[] prefabs)
        {
            EnsureServices();
            _uiWindows = prefabs;
            _windowRegistry.SetUIWindows(_uiWindows, windowKeys);
        }

        /// <summary>
        /// UIWindowManager가 사용하는 내부 서비스들을 초기화하고 TableWindow 정보를 연결합니다.
        /// </summary>
        private void Awake()
        {
            EnsureServices();
            RebuildWindowReferenceMap();
            InitializationTableInfo();
        }

        /// <summary>
        /// 아이콘 hover/선택 표시용 공통 이미지를 생성합니다.
        /// </summary>
        private void Start()
        {
            ConfigureIconVisualPresenter();
            MakeIconOver();
            MakeIconSelected();
            StartCoroutine(ApplyInitialWindowVisibilityRoutine());
        }

        /// <summary>
        /// 표시 억제 해제 후 보류된 UIWindow 표시 요청을 적용합니다.
        /// 컷신 컨트롤러의 Stop/End와 스냅샷 복원 처리가 끝난 뒤 실행되도록 LateUpdate에서 처리합니다.
        /// </summary>
        private void LateUpdate()
        {
            EnsureServices();
            _windowVisibilityService.FlushDeferredVisibilityRequests();
        }

        /// <summary>
        /// 기본 비활성 UIWindow의 초기 Transform/Layout 갱신을 끝낸 뒤 외부 구독자에게 완료 시점을 알립니다.
        /// </summary>
        /// <returns>초기 UI 표시 상태 적용을 지연 처리하는 코루틴입니다.</returns>
        private IEnumerator ApplyInitialWindowVisibilityRoutine()
        {
            yield return _initialVisibilityService.ApplyDefaultInactiveAfterInitialLayout();

            IsInitialWindowVisibilityApplied = true;
            OnInitialWindowVisibilityApplied?.Invoke(this);
        }

        /// <summary>
        /// 내부 서비스 객체를 지연 생성합니다.
        /// </summary>
        private void EnsureServices()
        {
            if (_windowTableBinder != null)
            {
                return;
            }

            _windowTableBinder = new UIWindowTableBinder(_windowRegistry);
            _windowOrderService = new UIWindowOrderService(_windowRegistry, _windowTableBinder, () => windowKeys);
            _visibilityStateStack = new UIWindowVisibilityStateStack();
            _windowVisibilityService = new UIWindowVisibilityService(
                GetUIWindowByUidSilently,
                GetManagedWindows,
                _visibilityStateStack);
            _initialVisibilityService = new UIWindowInitialVisibilityService(GetManagedWindows);
            _slotActivationService = new UIWindowSlotActivationService(
                windowUid => GetUIWindowByUid<UIWindow>(windowUid),
                GetManagedWindows);
            _iconTransferService = new UIWindowIconTransferService(GetUIWindowByUidSilently);
            _iconVisualPresenter = new UIWindowIconVisualPresenter();
        }

        /// <summary>
        /// 내부 UI 서비스가 자체 실패 정책을 적용할 수 있도록 UIWindow를 오류 로그 없이 조회합니다.
        /// </summary>
        /// <param name="windowUid">조회할 UIWindow UID입니다.</param>
        /// <returns>사용 가능한 UIWindow이며, 찾지 못하면 <see langword="null"/>입니다.</returns>
        private UIWindow GetUIWindowByUidSilently(UIWindowConstants.WindowUid windowUid)
        {
            return TryGetUIWindowByUid(windowUid, out UIWindow uiWindow)
                ? uiWindow
                : null;
        }

        /// <summary>
        /// 선택/hover 아이콘 Presenter가 최신 serialized 설정을 사용하도록 동기화합니다.
        /// </summary>
        private void ConfigureIconVisualPresenter()
        {
            EnsureServices();
            _iconVisualPresenter.Configure(
                prefabIconOver,
                prefabIconSelected,
                isSelectedIconSizeFixed,
                defaultSelectedIconAnimation);
        }

        /// <summary>
        /// hover 아이콘 이미지를 생성합니다.
        /// </summary>
        private void MakeIconOver()
        {
            ConfigureIconVisualPresenter();
            _iconVisualPresenter.MakeIconOver();
        }

        /// <summary>
        /// 선택 아이콘 이미지를 생성합니다.
        /// </summary>
        private void MakeIconSelected()
        {
            ConfigureIconVisualPresenter();
            _iconVisualPresenter.MakeIconSelected();
        }

        /// <summary>
        /// UID 기준 UIWindow 참조 캐시를 다시 구성합니다.
        /// </summary>
        private void RebuildWindowReferenceMap()
        {
            EnsureServices();
            _windowRegistry.Rebuild(windowKeys);
        }

        /// <summary>
        /// TableWindow 데이터를 각 UIWindow에 연결하고 정렬 순서를 갱신합니다.
        /// </summary>
        private void InitializationTableInfo()
        {
            EnsureServices();
            _windowTableBinder.Initialize(windowKeys);
            RefreshWindowOrder();
            EnsureWindowSoundUsageDeclarations();
            _initialVisibilityService.PrepareDefaultInactiveWindows();
        }

        /// <summary>
        /// TableWindow에 정상 연결된 모든 UIWindow에 범위 사운드 관리 컴포넌트를 보장합니다.
        /// 프리팹에 컴포넌트가 없어도 자동 생성 매니페스트를 런타임에서 사용할 수 있게 합니다.
        /// </summary>
        private void EnsureWindowSoundUsageDeclarations()
        {
            foreach (int uid in _windowTableBinder.WindowUids)
            {
                if (uid <= 0)
                    continue;

                UIWindow window = _windowRegistry.GetWindowReferenceByUid(uid);
                if (window == null)
                    continue;

                UIWindowSoundUsageDeclaration.EnsureAttached(window, uid);
            }
        }

        /// <summary>
        /// 지정한 UID에 해당하는 UIWindow 참조를 추가하거나 교체합니다.
        /// </summary>
        /// <param name="uid">추가하거나 교체할 UIWindow UID입니다.</param>
        /// <param name="window">UID에 연결할 UIWindow 참조입니다.</param>
        /// <returns>목록이 실제로 변경되었으면 true입니다.</returns>
        public bool UpsertWindowKey(int uid, UIWindow window)
        {
            EnsureServices();
            if (windowKeys == null)
            {
                windowKeys = new List<WindowKey>();
            }

            bool changed = _windowRegistry.UpsertWindowKey(windowKeys, uid, window);
            if (Application.isPlaying && window != null && uid > 0)
                UIWindowSoundUsageDeclaration.EnsureAttached(window, uid);

            return changed;
        }

        /// <summary>
        /// 관리 중인 UIWindow의 Transform sibling 순서를 TableWindow ordering 기준으로 갱신합니다.
        /// </summary>
        public void RefreshWindowOrder()
        {
            EnsureServices();
            _windowOrderService.RefreshWindowOrder();
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
            ExternalWindowInsertMode insertMode = ExternalWindowInsertMode.After,
            int priority = 0)
        {
            EnsureServices();
            return _windowOrderService.RegisterExternalWindow(key, window, anchorUid, insertMode, priority);
        }

        /// <summary>
        /// key로 등록된 외부 UIWindow를 정렬 목록에서 제거합니다.
        /// </summary>
        /// <param name="key">제거할 외부 윈도우 등록 key입니다.</param>
        /// <returns>등록 항목이 제거되었으면 true입니다.</returns>
        public bool UnregisterExternalWindow(string key)
        {
            EnsureServices();
            return _windowOrderService.UnregisterExternalWindow(key);
        }

        /// <summary>
        /// 지정한 UIWindow의 표시 상태를 기본 모드로 변경합니다.
        /// </summary>
        /// <param name="uid">표시 상태를 변경할 UIWindow UID입니다.</param>
        /// <param name="show">표시하면 true, 숨기면 false입니다.</param>
        public void ShowWindow(UIWindowConstants.WindowUid uid, bool show)
        {
            EnsureServices();
            _windowVisibilityService.ShowWindow(uid, show);
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
            EnsureServices();
            _windowVisibilityService.ShowWindow(uid, show, mode);
        }

        /// <summary>
        /// 지정한 UIWindow의 표시 상태를 기본 모드로 변경합니다.
        /// 현재 표시 억제 중이면 요청을 보류하고, 억제 해제 후 LateUpdate에서 자동 적용합니다.
        /// </summary>
        /// <param name="uid">표시 상태를 변경할 UIWindow UID입니다.</param>
        /// <param name="show">표시하면 true, 숨기면 false입니다.</param>
        public void ShowWindowWhenAllowed(UIWindowConstants.WindowUid uid, bool show)
        {
            EnsureServices();
            _windowVisibilityService.ShowWindowWhenAllowed(uid, show);
        }

        /// <summary>
        /// 지정한 UIWindow의 표시 상태를 지정한 모드로 변경합니다.
        /// 현재 표시 억제 중이면 요청을 보류하고, 억제 해제 후 LateUpdate에서 자동 적용합니다.
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
            EnsureServices();
            _windowVisibilityService.ShowWindowWhenAllowed(uid, show, mode, owner);
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
            EnsureServices();
            return _windowVisibilityService.CancelDeferredWindowVisibilityRequest(uid, owner);
        }

        /// <summary>
        /// 지정한 소유자가 등록한 모든 보류 표시 요청을 취소합니다.
        /// 맵 전환, 씬 종료, 루틴 중단처럼 요청 주체가 더 이상 유효하지 않을 때 사용합니다.
        /// </summary>
        /// <param name="owner">취소할 요청 소유자입니다.</param>
        /// <returns>취소한 보류 요청 개수입니다.</returns>
        public int CancelDeferredWindowVisibilityRequests(object owner)
        {
            EnsureServices();
            return _windowVisibilityService.CancelDeferredWindowVisibilityRequests(owner);
        }

        /// <summary>
        /// 보류 중인 모든 UIWindow 표시 요청을 취소합니다.
        /// 씬 종료처럼 기존 UI 표시 요청이 더 이상 의미 없을 때 사용합니다.
        /// </summary>
        public void ClearDeferredWindowVisibilityRequests()
        {
            EnsureServices();
            _windowVisibilityService.ClearDeferredWindowVisibilityRequests();
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
            EnsureServices();
            return _windowVisibilityService.HasDeferredWindowVisibilityRequest(uid, owner);
        }

        /// <summary>
        /// 지정한 UIWindow 슬롯이 저장 데이터에 의해 활성화되어 있는지 확인합니다.
        /// </summary>
        /// <param name="windowUid">확인할 UIWindow UID입니다.</param>
        /// <param name="slotIndex">확인할 슬롯 인덱스입니다.</param>
        /// <returns>저장된 활성 슬롯이면 true입니다.</returns>
        public bool IsWindowSlotActivated(UIWindowConstants.WindowUid windowUid, int slotIndex)
        {
            EnsureServices();
            return _slotActivationService.IsWindowSlotActivated(windowUid, slotIndex);
        }

        /// <summary>
        /// 지정한 UIWindow 슬롯의 저장 활성 상태를 변경합니다.
        /// </summary>
        /// <param name="windowUid">대상 UIWindow UID입니다.</param>
        /// <param name="slotIndex">대상 슬롯 인덱스입니다.</param>
        /// <param name="activated">저장 활성 여부입니다.</param>
        /// <returns>저장 상태가 실제로 변경되었으면 true입니다.</returns>
        public bool SetWindowSlotActivated(
            UIWindowConstants.WindowUid windowUid,
            int slotIndex,
            bool activated)
        {
            EnsureServices();
            return _slotActivationService.SetWindowSlotActivated(windowUid, slotIndex, activated);
        }

        /// <summary>
        /// 지정한 UIWindow 슬롯을 저장 활성 상태로 변경합니다.
        /// </summary>
        /// <param name="windowUid">대상 UIWindow UID입니다.</param>
        /// <param name="slotIndex">대상 슬롯 인덱스입니다.</param>
        /// <returns>저장 상태가 새로 변경되었으면 true입니다.</returns>
        public bool ActivateWindowSlot(UIWindowConstants.WindowUid windowUid, int slotIndex)
        {
            EnsureServices();
            return _slotActivationService.ActivateWindowSlot(windowUid, slotIndex);
        }

        /// <summary>
        /// 지정한 UIWindow 슬롯의 저장 활성 상태를 해제합니다.
        /// </summary>
        /// <param name="windowUid">대상 UIWindow UID입니다.</param>
        /// <param name="slotIndex">대상 슬롯 인덱스입니다.</param>
        /// <returns>저장 상태가 새로 변경되었으면 true입니다.</returns>
        public bool DeactivateWindowSlot(UIWindowConstants.WindowUid windowUid, int slotIndex)
        {
            EnsureServices();
            return _slotActivationService.DeactivateWindowSlot(windowUid, slotIndex);
        }

        /// <summary>
        /// 저장 활성 정보 복원 이후 모든 관리 UIWindow의 비활성 표시를 다시 반영합니다.
        /// </summary>
        public void RefreshWindowSlotActivationStates()
        {
            EnsureServices();
            _slotActivationService.RefreshWindowSlotActivationStates();
        }

        /// <summary>
        /// UID에 해당하는 UIWindow를 지정 타입으로 조회합니다.
        /// </summary>
        /// <typeparam name="T">반환받을 UIWindow 파생 타입입니다.</typeparam>
        /// <param name="windowUid">조회할 UIWindow UID입니다.</param>
        /// <returns>조회된 UIWindow입니다. 없거나 타입이 다르면 null입니다.</returns>
        public T GetUIWindowByUid<T>(UIWindowConstants.WindowUid windowUid) where T : UIWindow
        {
            EnsureServices();

            int uid = (int)windowUid;
            if (uid <= 0)
            {
                return null;
            }

            if (!_windowTableBinder.TryGetWindowInfo(uid, out StruckTableWindow info))
            {
                return null;
            }

            if (!info.UseInGame)
            {
                return null;
            }

            UIWindow uiWindow = _windowRegistry.GetWindowReferenceByUid(uid);
            if (uiWindow == null)
            {
                GcLogger.LogError($"{nameof(UIWindow)} 컴포넌트가 없습니다. uid:" + windowUid);
                return null;
            }

            return uiWindow as T;
        }

        /// <summary>
        /// UID에 해당하는 UIWindow를 오류 로그 없이 지정 타입으로 조회합니다.
        /// 선택적 UIWindow 확인이나 씬 종료 정리처럼 참조가 이미 파괴될 수 있는 경로에서 사용합니다.
        /// </summary>
        /// <typeparam name="T">반환받을 UIWindow 파생 타입입니다.</typeparam>
        /// <param name="windowUid">조회할 UIWindow UID입니다.</param>
        /// <param name="uiWindow">조회에 성공한 UIWindow입니다.</param>
        /// <returns>사용 가능한 지정 타입의 UIWindow를 찾았으면 <see langword="true"/>입니다.</returns>
        public bool TryGetUIWindowByUid<T>(
            UIWindowConstants.WindowUid windowUid,
            out T uiWindow) where T : UIWindow
        {
            EnsureServices();
            uiWindow = null;

            int uid = (int)windowUid;
            if (uid <= 0 ||
                !_windowTableBinder.TryGetWindowInfo(uid, out StruckTableWindow info) ||
                !info.UseInGame)
            {
                return false;
            }

            UIWindow window = _windowRegistry.GetWindowReferenceByUid(uid);
            if (window == null)
            {
                return false;
            }

            uiWindow = window as T;
            return uiWindow != null;
        }

        /// <summary>
        /// 지정한 UIWindow 슬롯의 아이콘을 제거합니다.
        /// </summary>
        /// <param name="windowUid">아이콘을 제거할 UIWindow UID입니다.</param>
        /// <param name="slotIndex">아이콘을 제거할 슬롯 인덱스입니다.</param>
        public void RemoveIcon(UIWindowConstants.WindowUid windowUid, int slotIndex)
        {
            EnsureServices();
            _iconTransferService.RemoveIcon(windowUid, slotIndex);
        }

        /// <summary>
        /// 지정한 UIWindow가 현재 표시 중인지 확인합니다.
        /// </summary>
        /// <param name="windowUid">확인할 UIWindow UID입니다.</param>
        /// <returns>활성 상태이면 true입니다.</returns>
        public bool IsShowByWindowUid(UIWindowConstants.WindowUid windowUid)
        {
            EnsureServices();
            return _windowVisibilityService.IsShowByWindowUid(windowUid);
        }

        /// <summary>
        /// 지정한 UID가 현재 관리 중인 UIWindow인지 확인합니다.
        /// </summary>
        /// <param name="windowUid">확인할 UIWindow UID입니다.</param>
        /// <returns>관리 중인 UIWindow이면 true입니다.</returns>
        public bool HasManagedWindow(UIWindowConstants.WindowUid windowUid)
        {
            return TryGetUIWindowByUid(windowUid, out UIWindow _);
        }

        /// <summary>
        /// 현재 관리 중인 UIWindow UID 목록을 반환합니다.
        /// </summary>
        /// <returns>관리 중인 UIWindow UID 목록입니다.</returns>
        public List<UIWindowConstants.WindowUid> GetManagedWindowUids()
        {
            EnsureServices();

            List<UIWindowConstants.WindowUid> result = new List<UIWindowConstants.WindowUid>();
            foreach (int uid in _windowTableBinder.WindowUids)
            {
                if (uid <= 0)
                {
                    continue;
                }

                UIWindowConstants.WindowUid windowUid = (UIWindowConstants.WindowUid)uid;
                if (GetUIWindowByUid<UIWindow>(windowUid) == null)
                {
                    continue;
                }

                result.Add(windowUid);
            }

            return result;
        }

        /// <summary>
        /// 현재 관리 중인 모든 UIWindow를 정렬 순서 기준으로 반환합니다.
        /// </summary>
        /// <returns>정렬된 UIWindow 목록입니다.</returns>
        public List<UIWindow> GetManagedWindows()
        {
            EnsureServices();
            return _windowOrderService.GetManagedWindows();
        }

        /// <summary>
        /// 지정한 UIWindow UID 목록의 현재 표시 상태를 캡처합니다.
        /// </summary>
        /// <param name="windowUids">표시 상태를 캡처할 UIWindow UID 목록입니다.</param>
        /// <returns>UID별 표시 상태입니다.</returns>
        public Dictionary<UIWindowConstants.WindowUid, bool> CaptureVisibilityState(
            IEnumerable<UIWindowConstants.WindowUid> windowUids)
        {
            EnsureServices();
            return _windowVisibilityService.CaptureVisibilityState(windowUids);
        }

        /// <summary>
        /// 지정한 UIWindow 목록의 현재 표시 상태를 캡처합니다.
        /// </summary>
        /// <param name="windows">표시 상태를 캡처할 UIWindow 목록입니다.</param>
        /// <returns>UIWindow별 표시 상태입니다.</returns>
        public Dictionary<UIWindow, bool> CaptureVisibilityState(IEnumerable<UIWindow> windows)
        {
            EnsureServices();
            return _windowVisibilityService.CaptureVisibilityState(windows);
        }

        /// <summary>
        /// 저장된 UID별 표시 상태를 기본 모드로 복원합니다.
        /// </summary>
        /// <param name="state">복원할 UID별 표시 상태입니다.</param>
        public void RestoreVisibilityState(IReadOnlyDictionary<UIWindowConstants.WindowUid, bool> state)
        {
            EnsureServices();
            _windowVisibilityService.RestoreVisibilityState(state);
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
            EnsureServices();
            _windowVisibilityService.RestoreVisibilityState(state, mode);
        }

        /// <summary>
        /// 지정한 UIWindow UID 목록의 현재 표시 상태를 스택에 저장합니다.
        /// </summary>
        /// <param name="windowUids">표시 상태를 저장할 UIWindow UID 목록입니다.</param>
        /// <param name="restoreMode">Pop 시 사용할 표시 상태 복원 모드입니다.</param>
        /// <returns>저장된 스냅샷이 있으면 true입니다.</returns>
        public bool PushVisibilityState(
            IEnumerable<UIWindowConstants.WindowUid> windowUids,
            UIWindowConstants.UIWindowVisibilityApplyMode restoreMode =
                UIWindowConstants.UIWindowVisibilityApplyMode.Normal)
        {
            EnsureServices();
            return _windowVisibilityService.PushVisibilityState(windowUids, restoreMode);
        }

        /// <summary>
        /// 지정한 UIWindow 목록의 현재 표시 상태를 스택에 저장합니다.
        /// </summary>
        /// <param name="windows">표시 상태를 저장할 UIWindow 목록입니다.</param>
        /// <param name="restoreMode">Pop 시 사용할 표시 상태 복원 모드입니다.</param>
        /// <returns>저장된 스냅샷이 있으면 true입니다.</returns>
        public bool PushVisibilityState(
            IEnumerable<UIWindow> windows,
            UIWindowConstants.UIWindowVisibilityApplyMode restoreMode =
                UIWindowConstants.UIWindowVisibilityApplyMode.Normal)
        {
            EnsureServices();
            return _windowVisibilityService.PushVisibilityState(windows, restoreMode);
        }

        /// <summary>
        /// 스택에 저장된 가장 마지막 표시 상태를 복원합니다.
        /// </summary>
        /// <returns>복원할 스냅샷이 있으면 true입니다.</returns>
        public bool PopVisibilityState()
        {
            EnsureServices();
            return _windowVisibilityService.PopVisibilityState();
        }

        /// <summary>
        /// 표시 상태 스택을 모두 비웁니다.
        /// </summary>
        public void ClearVisibilityStateStack()
        {
            EnsureServices();
            _windowVisibilityService.ClearVisibilityStateStack();
        }

        /// <summary>
        /// 현재 표시 상태 스택에 저장된 스냅샷 개수를 반환합니다.
        /// </summary>
        /// <returns>저장된 표시 상태 스냅샷 개수입니다.</returns>
        public int GetVisibilityStateStackCount()
        {
            EnsureServices();
            return _windowVisibilityService.GetVisibilityStateStackCount();
        }

        /// <summary>
        /// 지정한 UIWindow UID 목록에 표시 억제 토큰을 발급합니다.
        /// 억제 중인 창은 다른 런타임 로직이 표시를 요청해도 토큰 해제 전까지 다시 켜지지 않습니다.
        /// </summary>
        /// <param name="windowUids">표시 요청을 억제할 UIWindow UID 목록입니다.</param>
        /// <returns>해제에 사용할 토큰입니다. 억제 대상이 없으면 0을 반환합니다.</returns>
        public int AcquireVisibilitySuppression(IEnumerable<UIWindowConstants.WindowUid> windowUids)
        {
            EnsureServices();
            return _windowVisibilityService.AcquireVisibilitySuppression(windowUids);
        }

        /// <summary>
        /// 지정한 표시 억제 토큰을 해제합니다.
        /// 같은 창을 여러 토큰이 억제 중이면 마지막 토큰이 해제된 뒤 표시 요청이 허용됩니다.
        /// </summary>
        /// <param name="token">해제할 표시 억제 토큰입니다.</param>
        /// <returns>토큰을 찾아 해제했으면 true입니다.</returns>
        public bool ReleaseVisibilitySuppression(int token)
        {
            EnsureServices();
            return _windowVisibilityService.ReleaseVisibilitySuppression(token);
        }

        /// <summary>
        /// 지정한 UIWindow UID가 현재 표시 억제 대상인지 확인합니다.
        /// </summary>
        /// <param name="windowUid">확인할 UIWindow UID입니다.</param>
        /// <returns>표시 요청이 억제 중이면 true입니다.</returns>
        public bool IsWindowVisibilitySuppressed(UIWindowConstants.WindowUid windowUid)
        {
            EnsureServices();
            return _windowVisibilityService.IsWindowVisibilitySuppressed(windowUid);
        }

        /// <summary>
        /// 지정한 UIWindow UID 목록을 기본 모드로 일괄 표시하거나 숨깁니다.
        /// </summary>
        /// <param name="windowUids">표시 상태를 변경할 UIWindow UID 목록입니다.</param>
        /// <param name="show">표시하면 true, 숨기면 false입니다.</param>
        public void SetWindowsVisible(IEnumerable<UIWindowConstants.WindowUid> windowUids, bool show)
        {
            EnsureServices();
            _windowVisibilityService.SetWindowsVisible(windowUids, show);
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
            EnsureServices();
            _windowVisibilityService.SetWindowsVisible(windowUids, show, mode);
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
            UIWindowConstants.UIWindowVisibilityApplyMode mode =
                UIWindowConstants.UIWindowVisibilityApplyMode.Normal)
        {
            EnsureServices();
            _windowVisibilityService.SetWindowsVisible(windows, show, mode);
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
            EnsureServices();
            _iconTransferService.MoveIcon(fromWindowUid, fromIndex, toWindowUid, toCount, toIndex);
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
            EnsureServices();
            _iconTransferService.UnRegisterIcon(fromWindowUid, fromIndex, toWindowUid);
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
            EnsureServices();
            _iconTransferService.RegisterIcon(fromWindowUid, fromIndex, toWindowUid, toCount, toIndex);
        }

        /// <summary>
        /// 기본 활성 UIWindow와 예외 UID를 제외한 모든 관리 UIWindow를 닫습니다.
        /// </summary>
        /// <param name="exceptWindowUids">닫지 않을 UIWindow UID 목록입니다.</param>
        public void CloseAll(List<UIWindowConstants.WindowUid> exceptWindowUids = null)
        {
            EnsureServices();
            _windowVisibilityService.CloseAll(exceptWindowUids);
        }

        /// <summary>
        /// hover 이미지의 표시 상태, 위치, 크기를 갱신합니다.
        /// </summary>
        /// <param name="show">hover 이미지를 표시하면 true입니다.</param>
        /// <param name="position">표시할 월드 좌표입니다. null이면 기존 위치를 유지합니다.</param>
        /// <param name="slotSize">표시할 크기입니다. null이면 기존 크기를 유지합니다.</param>
        public void ShowOverIconImage(bool show, Vector2? position = null, Vector2? slotSize = null)
        {
            ConfigureIconVisualPresenter();
            _iconVisualPresenter.ShowOverIconImage(show, position, slotSize);
        }

        /// <summary>
        /// 선택 이미지의 표시 상태와 위치, 크기, Sprite, 애니메이션을 갱신합니다.
        /// </summary>
        /// <param name="show">선택 이미지를 표시하면 true입니다.</param>
        /// <param name="position">선택 이미지가 표시될 월드 좌표입니다. null이면 기존 위치를 유지합니다.</param>
        /// <param name="slotSize">선택 이미지 크기입니다. null이면 기존 크기를 유지합니다.</param>
        /// <param name="spriteOverride">선택 이미지에 사용할 Sprite입니다. null이면 프리팹 기본 Sprite를 사용합니다.</param>
        /// <param name="prefabOverride">선택 이미지에 사용할 Prefab입니다. null이면 기본 Prefab을 사용합니다.</param>
        /// <param name="animationOverride">선택 이미지 애니메이션 설정입니다. null이면 기본 설정을 사용합니다.</param>
        /// <param name="parentOverride">선택 이미지 오브젝트를 붙일 부모 Transform입니다. null이면 메인 캔버스를 사용합니다.</param>
        public void ShowSelectIconImage(
            bool show,
            Vector2? position = null,
            Vector2? slotSize = null,
            Sprite spriteOverride = null,
            GameObject prefabOverride = null,
            UISelectedIconAnimationSettings animationOverride = null,
            Transform parentOverride = null)
        {
            ConfigureIconVisualPresenter();
            _iconVisualPresenter.ShowSelectIconImage(
                show,
                position,
                slotSize,
                spriteOverride,
                prefabOverride,
                animationOverride,
                parentOverride);
        }

        /// <summary>
        /// 현재 활성 선택 이미지 오브젝트를 반환합니다.
        /// 선택 이미지 표시 후 추가 후처리가 필요한 윈도우에서 사용합니다.
        /// </summary>
        /// <returns>현재 활성 선택 이미지 오브젝트입니다. 활성 선택 이미지가 없으면 null을 반환합니다.</returns>
        public GameObject GetActiveSelectedIconImageObject()
        {
            ConfigureIconVisualPresenter();
            return _iconVisualPresenter.GetActiveSelectedIconImageObject();
        }
    }
}
