using System;
using System.Collections.Generic;
using UnityEngine;

namespace GGemCo2DCore
{
    /// <summary>
    /// UI 윈도우가 활성화된 동안에만 유지할 대표 사운드 UID를 선언합니다.
    /// 수동 등록 UID와 에디터 자동 분석 매니페스트를 합쳐 하나의 UI 윈도우 범위로 관리합니다.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(UIWindowBase))]
    public sealed class UIWindowSoundUsageDeclaration : MonoBehaviour
    {
        [Tooltip("이 UI 윈도우에서만 사용하는 대표 sound UID 목록입니다.")]
        [SerializeField]
        private List<int> soundUids = new List<int>();

        private UIWindowBase _window;
        private SoundScopeLease _lease;
        private int _requestVersion;
        private int _runtimeScopeUid;
        private bool _hasStarted;
        private bool _waitingForInitialVisibility;
        private UIWindowManager _windowManager;

        /// <summary>
        /// 에디터 분석기와 디버그 도구가 확인할 수 있는 수동 등록 대표 사운드 UID 목록입니다.
        /// </summary>
        public IReadOnlyList<int> SoundUids => soundUids;

        /// <summary>
        /// 지정한 UIWindow에 사운드 범위 선언 컴포넌트가 없으면 추가하고 런타임 범위 UID를 연결합니다.
        /// </summary>
        /// <param name="window">사운드 범위를 관리할 UI 윈도우입니다.</param>
        /// <param name="scopeUid">window 테이블에서 사용하는 UI 윈도우 UID입니다.</param>
        /// <returns>기존 또는 새로 추가된 사운드 범위 선언 컴포넌트입니다.</returns>
        public static UIWindowSoundUsageDeclaration EnsureAttached(UIWindowBase window, int scopeUid)
        {
            if (window == null)
                return null;

            UIWindowSoundUsageDeclaration declaration =
                window.GetComponent<UIWindowSoundUsageDeclaration>();
            if (declaration == null)
                declaration = window.gameObject.AddComponent<UIWindowSoundUsageDeclaration>();

            declaration.SetRuntimeScopeUid(scopeUid);
            return declaration;
        }

        /// <summary>
        /// 윈도우 참조를 미리 캐시합니다.
        /// </summary>
        private void Awake()
        {
            _window = GetComponent<UIWindowBase>();
        }

        /// <summary>
        /// UIWindowManager의 초기 레이아웃 활성화가 끝난 뒤 실제 활성 윈도우만 범위를 획득합니다.
        /// </summary>
        private void Start()
        {
            _hasStarted = true;
            _windowManager = ResolveWindowManager();
            if (_windowManager != null && !_windowManager.IsInitialWindowVisibilityApplied)
            {
                _waitingForInitialVisibility = true;
                _windowManager.OnInitialWindowVisibilityApplied += OnInitialWindowVisibilityApplied;
                return;
            }

            AcquireScopeAsync();
        }

        /// <summary>
        /// 시작 이후 비활성 상태에서 다시 열린 윈도우의 사운드 범위를 획득합니다.
        /// </summary>
        private void OnEnable()
        {
            if (_hasStarted && !_waitingForInitialVisibility)
                AcquireScopeAsync();
        }

        /// <summary>
        /// 윈도우가 비활성화되면 아직 완료되지 않은 요청을 무효화하고 범위 참조를 해제합니다.
        /// </summary>
        private void OnDisable()
        {
            ReleaseScope();
        }

        /// <summary>
        /// 윈도우가 파괴될 때 남아 있는 범위 참조를 안전하게 해제합니다.
        /// </summary>
        private void OnDestroy()
        {
            UnsubscribeInitialVisibility();
            ReleaseScope();
        }

        /// <summary>
        /// UIWindowManager의 기본 활성/비활성 적용이 끝난 뒤 실제 활성 윈도우만 사운드 범위를 획득합니다.
        /// </summary>
        /// <param name="manager">초기 표시 상태 적용을 완료한 UIWindowManager입니다.</param>
        private void OnInitialWindowVisibilityApplied(UIWindowManager manager)
        {
            UnsubscribeInitialVisibility();
            if (isActiveAndEnabled)
                AcquireScopeAsync();
        }

        /// <summary>
        /// 현재 윈도우가 속한 UIWindowManager를 SceneGame 또는 부모 계층에서 조회합니다.
        /// </summary>
        /// <returns>연결된 UIWindowManager이며 찾지 못하면 null입니다.</returns>
        private UIWindowManager ResolveWindowManager()
        {
            UIWindowManager sceneManager = SceneGame.Instance != null
                ? SceneGame.Instance.uIWindowManager
                : null;
            return sceneManager != null
                ? sceneManager
                : GetComponentInParent<UIWindowManager>(true);
        }

        /// <summary>
        /// UIWindowManager 초기 표시 완료 이벤트 구독을 한 번만 해제합니다.
        /// </summary>
        private void UnsubscribeInitialVisibility()
        {
            if (_windowManager != null)
                _windowManager.OnInitialWindowVisibilityApplied -= OnInitialWindowVisibilityApplied;

            _waitingForInitialVisibility = false;
        }

        /// <summary>
        /// UIWindowManager가 확인한 실제 window 테이블 UID를 런타임 범위 식별자로 저장합니다.
        /// 이미 활성화된 컴포넌트의 UID가 변경되면 올바른 범위로 다시 획득합니다.
        /// </summary>
        /// <param name="scopeUid">window 테이블 UID입니다.</param>
        private void SetRuntimeScopeUid(int scopeUid)
        {
            if (scopeUid <= 0 || _runtimeScopeUid == scopeUid)
                return;

            _runtimeScopeUid = scopeUid;
            if (_hasStarted && isActiveAndEnabled)
                AcquireScopeAsync();
        }

        /// <summary>
        /// 수동 선언과 자동 생성 매니페스트의 대표 사운드를 실제 Addressables 키로 해석하여 범위를 획득합니다.
        /// 비활성화 또는 재요청 이후 늦게 완료된 결과는 즉시 해제합니다.
        /// </summary>
        private async void AcquireScopeAsync()
        {
            int requestVersion = ++_requestVersion;
            _lease?.Dispose();
            _lease = null;

            AddressableLoaderSound loader = AddressableLoaderSound.Instance;
            TableLoaderManager tableLoaderManager = TableLoaderManager.Instance;
            if (!isActiveAndEnabled || loader == null || tableLoaderManager == null)
                return;

            IReadOnlyList<int> combinedSoundUids = BuildCombinedSoundUids(tableLoaderManager);
            if (combinedSoundUids.Count == 0)
                return;

            SoundUsageAddressKeyResolver resolver = new SoundUsageAddressKeyResolver(tableLoaderManager);
            IReadOnlyList<string> addressKeys = resolver.ResolveAddressKeys(combinedSoundUids);
            if (addressKeys.Count == 0)
                return;

            SoundScopeLease acquiredLease = null;
            try
            {
                acquiredLease = await loader.AcquireScopeAsync(BuildScopeKey(), addressKeys);
            }
            catch (Exception ex)
            {
                GcLogger.LogWarning(
                    $"[UIWindowSound] 사운드 범위를 획득하지 못했습니다. window={name}, error={ex.Message}");
            }

            if (this == null || !isActiveAndEnabled || requestVersion != _requestVersion)
            {
                acquiredLease?.Dispose();
                return;
            }

            _lease = acquiredLease;
        }

        /// <summary>
        /// Inspector에 등록된 UID와 자동 생성된 UI 윈도우 매니페스트 UID를 중복 없이 합칩니다.
        /// </summary>
        /// <param name="tableLoaderManager">자동 생성 매니페스트를 조회할 테이블 로더입니다.</param>
        /// <returns>현재 UI 윈도우에서 유지해야 하는 대표 sound UID 목록입니다.</returns>
        private IReadOnlyList<int> BuildCombinedSoundUids(TableLoaderManager tableLoaderManager)
        {
            List<int> result = new List<int>();
            HashSet<int> registered = new HashSet<int>();

            if (soundUids != null)
            {
                for (int i = 0; i < soundUids.Count; i++)
                    AppendSoundUid(soundUids[i], result, registered);
            }

            int scopeUid = ResolveScopeUid();
            IReadOnlyList<int> generatedSoundUids = tableLoaderManager?.TableSoundUsageManifest?.GetSoundUids(
                SoundUsageManifestScopeType.UiWindow,
                scopeUid) ?? Array.Empty<int>();

            for (int i = 0; i < generatedSoundUids.Count; i++)
                AppendSoundUid(generatedSoundUids[i], result, registered);

            return result;
        }

        /// <summary>
        /// 유효한 대표 sound UID를 결과 목록에 중복 없이 추가합니다.
        /// </summary>
        /// <param name="soundUid">추가할 대표 sound UID입니다.</param>
        /// <param name="target">UID를 저장할 결과 목록입니다.</param>
        /// <param name="registered">이미 등록된 UID 집합입니다.</param>
        private static void AppendSoundUid(int soundUid, List<int> target, HashSet<int> registered)
        {
            if (soundUid > 0 && registered.Add(soundUid))
                target.Add(soundUid);
        }

        /// <summary>
        /// 현재 윈도우에 연결된 사운드 범위를 한 번만 해제합니다.
        /// </summary>
        private void ReleaseScope()
        {
            _requestVersion++;
            _lease?.Dispose();
            _lease = null;
        }

        /// <summary>
        /// UIWindowManager가 전달한 UID를 우선 사용하고, 없으면 UIWindowBase의 직렬화 UID를 사용합니다.
        /// </summary>
        /// <returns>현재 UI 윈도우의 범위 UID입니다.</returns>
        private int ResolveScopeUid()
        {
            if (_runtimeScopeUid > 0)
                return _runtimeScopeUid;

            return _window != null ? (int)_window.uid : 0;
        }

        /// <summary>
        /// 윈도우 UID가 있으면 UID를, 없으면 오브젝트 이름과 인스턴스 ID를 조합해 고유 범위 키를 생성합니다.
        /// </summary>
        /// <returns>현재 UI 윈도우의 사운드 범위 키입니다.</returns>
        private SoundUsageScopeKey BuildScopeKey()
        {
            int scopeUid = ResolveScopeUid();
            string windowId = scopeUid > 0
                ? scopeUid.ToString()
                : $"{name}.{GetInstanceID()}";
            return SoundUsageScopeKey.UiWindow(windowId);
        }
    }
}
