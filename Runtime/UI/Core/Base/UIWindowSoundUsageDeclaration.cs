using System;
using System.Collections.Generic;
using UnityEngine;

namespace GGemCo2DCore
{
    /// <summary>
    /// UI 윈도우가 활성화된 동안에만 유지할 대표 사운드 UID를 선언합니다.
    /// 윈도우 프리팹 루트에 추가하면 활성화 시 범위를 획득하고 비활성화 시 자동 해제합니다.
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
        private bool _hasStarted;

        /// <summary>
        /// 에디터 분석기와 디버그 도구가 확인할 수 있는 선언된 대표 사운드 UID 목록입니다.
        /// </summary>
        public IReadOnlyList<int> SoundUids => soundUids;

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
            AcquireScopeAsync();
        }

        /// <summary>
        /// 시작 이후 비활성 상태에서 다시 열린 윈도우의 사운드 범위를 획득합니다.
        /// </summary>
        private void OnEnable()
        {
            if (_hasStarted)
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
            ReleaseScope();
        }

        /// <summary>
        /// 선언된 대표 사운드를 실제 Addressables 키로 해석하여 UI 윈도우 범위를 비동기로 획득합니다.
        /// 비활성화 또는 재요청 이후 늦게 완료된 결과는 즉시 해제합니다.
        /// </summary>
        private async void AcquireScopeAsync()
        {
            int requestVersion = ++_requestVersion;
            _lease?.Dispose();
            _lease = null;

            AddressableLoaderSound loader = AddressableLoaderSound.Instance;
            TableLoaderManager tableLoaderManager = TableLoaderManager.Instance;
            if (!isActiveAndEnabled || loader == null || tableLoaderManager == null || soundUids == null || soundUids.Count == 0)
                return;

            SoundUsageAddressKeyResolver resolver = new SoundUsageAddressKeyResolver(tableLoaderManager);
            IReadOnlyList<string> addressKeys = resolver.ResolveAddressKeys(soundUids);
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
        /// 현재 윈도우에 연결된 사운드 범위를 한 번만 해제합니다.
        /// </summary>
        private void ReleaseScope()
        {
            _requestVersion++;
            _lease?.Dispose();
            _lease = null;
        }

        /// <summary>
        /// 윈도우 UID가 있으면 UID를, 없으면 오브젝트 이름과 인스턴스 ID를 조합해 고유 범위 키를 생성합니다.
        /// </summary>
        /// <returns>현재 UI 윈도우의 사운드 범위 키입니다.</returns>
        private SoundUsageScopeKey BuildScopeKey()
        {
            string windowId = _window != null && (int)_window.uid > 0
                ? ((int)_window.uid).ToString()
                : $"{name}.{GetInstanceID()}";
            return SoundUsageScopeKey.UiWindow(windowId);
        }
    }
}
