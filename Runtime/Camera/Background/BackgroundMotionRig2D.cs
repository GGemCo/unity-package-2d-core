using System;
using System.Collections.Generic;
using UnityEngine;

namespace GGemCo2DCore
{
    /// <summary>
    /// 카메라 기준 위치를 조회하여 배경 레이어의 원근감, 자동 스크롤, 무한 반복 갱신을 총괄합니다.
    /// </summary>
    [DefaultExecutionOrder(1000)]
    [DisallowMultipleComponent]
    public sealed class BackgroundMotionRig2D : MonoBehaviour
    {
        [Header("카메라")]
        [Tooltip("비워두면 SceneGame의 CameraManager를 자동으로 찾습니다.")]
        [SerializeField] private CameraManager cameraManager;

        [Tooltip("카메라 흔들림이 적용되기 전의 안정된 기준 위치를 사용합니다.")]
        [SerializeField] private bool useStableCameraPosition = true;

        [Header("레이어 수집")]
        [Tooltip("자식 BackgroundLayer2D를 자동으로 수집합니다.")]
        [SerializeField] private bool autoCollectChildLayers = true;

        [Tooltip("비활성 자식 레이어도 수집 대상에 포함합니다.")]
        [SerializeField] private bool includeInactiveLayers = true;

        [SerializeField] private BackgroundLayer2D[] layers = Array.Empty<BackgroundLayer2D>();

        private readonly List<BackgroundLayer2D> _layerBuffer = new();
        private MapTileCommon _currentMapTileCommon;
        private bool _hasBaseline;
        private bool _hasLoggedLegacyConflict;

        private void Awake()
        {
            DisableLegacyControllers();
            RefreshLayers();
            MapManager.OnLoadCompleteMap += OnLoadCompleteMap;
        }

        private void OnEnable()
        {
            RefreshLayers();
            CaptureBaseline();
        }

        private void OnDisable()
        {
            ResetLayersToBaseline();
            _hasBaseline = false;
        }

        private void OnDestroy()
        {
            MapManager.OnLoadCompleteMap -= OnLoadCompleteMap;
        }

        private void LateUpdate()
        {
            if (!TryResolveReferencePosition(out Vector3 referencePosition))
            {
                return;
            }

            if (!_hasBaseline)
            {
                CaptureBaseline(referencePosition);
            }

            float deltaTime = Time.deltaTime;
            for (int i = 0; i < layers.Length; i++)
            {
                BackgroundLayer2D layer = layers[i];
                if (layer == null || !layer.isActiveAndEnabled)
                {
                    continue;
                }

                layer.Tick(referencePosition, deltaTime);
            }
        }

        /// <summary>
        /// 현재 Rig 아래의 배경 레이어 목록을 다시 수집합니다.
        /// 런타임 반복 세그먼트에 포함된 비활성 복제 컨트롤러는 수집 대상에서 제외합니다.
        /// </summary>
        public void RefreshLayers()
        {
            if (!autoCollectChildLayers)
            {
                layers ??= Array.Empty<BackgroundLayer2D>();
                return;
            }

            _layerBuffer.Clear();
            GetComponentsInChildren(includeInactiveLayers, _layerBuffer);

            int validCount = 0;
            for (int i = 0; i < _layerBuffer.Count; i++)
            {
                BackgroundLayer2D layer = _layerBuffer[i];
                if (layer == null || layer.IsRuntimeClone)
                {
                    continue;
                }

                _layerBuffer[validCount++] = layer;
            }

            if (layers == null || layers.Length != validCount)
            {
                layers = new BackgroundLayer2D[validCount];
            }

            for (int i = 0; i < validCount; i++)
            {
                layers[i] = _layerBuffer[i];
            }

            _layerBuffer.Clear();
        }

        /// <summary>
        /// 현재 카메라와 각 레이어의 위치를 새로운 기준점으로 저장합니다.
        /// </summary>
        public void CaptureBaseline()
        {
            if (!TryResolveReferencePosition(out Vector3 referencePosition))
            {
                _hasBaseline = false;
                return;
            }

            CaptureBaseline(referencePosition);
        }

        /// <summary>
        /// 모든 레이어를 마지막으로 저장한 기준 위치로 복원합니다.
        /// </summary>
        public void ResetLayersToBaseline()
        {
            if (layers == null)
            {
                return;
            }

            for (int i = 0; i < layers.Length; i++)
            {
                if (layers[i] == null)
                {
                    continue;
                }

                layers[i].ResetToBaseline();
            }
        }

        /// <summary>
        /// 맵 로딩 완료 시 현재 맵 문맥과 배경 레이어의 기준점을 갱신합니다.
        /// </summary>
        /// <param name="mapTileCommon">현재 로드된 맵의 공통 루트입니다.</param>
        /// <param name="grid">현재 로드된 Grid 오브젝트입니다.</param>
        private void OnLoadCompleteMap(MapTileCommon mapTileCommon, GameObject grid)
        {
            _ = grid;
            _currentMapTileCommon = mapTileCommon;
            RefreshLayers();
            CaptureBaseline();
        }

        /// <summary>
        /// 명시한 카메라 위치로 모든 레이어의 기준점을 저장합니다.
        /// </summary>
        /// <param name="referencePosition">원근감 계산에 사용할 기준 카메라 위치입니다.</param>
        private void CaptureBaseline(Vector3 referencePosition)
        {
            ResolveCurrentMapTileCommon();

            for (int i = 0; i < layers.Length; i++)
            {
                BackgroundLayer2D layer = layers[i];
                if (layer == null || layer.IsRuntimeClone)
                {
                    continue;
                }

                layer.SetMapContext(_currentMapTileCommon);
                layer.CaptureBaseline(referencePosition);
            }

            _hasBaseline = true;
        }

        /// <summary>
        /// 카메라 흔들림 정책을 고려하여 배경 계산에 사용할 기준 위치를 조회합니다.
        /// </summary>
        /// <param name="referencePosition">조회된 카메라 기준 위치입니다.</param>
        /// <returns>사용 가능한 카메라를 찾았으면 <see langword="true"/>입니다.</returns>
        private bool TryResolveReferencePosition(out Vector3 referencePosition)
        {
            CameraManager resolvedCameraManager = ResolveCameraManager();
            if (resolvedCameraManager != null)
            {
                referencePosition = useStableCameraPosition
                    ? resolvedCameraManager.GetBaseWorldPosition()
                    : resolvedCameraManager.transform.position;
                return true;
            }

            Camera mainCamera = Camera.main;
            if (mainCamera != null)
            {
                referencePosition = mainCamera.transform.position;
                return true;
            }

            referencePosition = Vector3.zero;
            return false;
        }

        /// <summary>
        /// 명시적 참조 또는 SceneGame을 통해 CameraManager를 조회합니다.
        /// </summary>
        /// <returns>사용 가능한 CameraManager이며, 찾지 못하면 null입니다.</returns>
        private CameraManager ResolveCameraManager()
        {
            if (cameraManager != null)
            {
                return cameraManager;
            }

            if (SceneGame.Instance != null)
            {
                cameraManager = SceneGame.Instance.cameraManager;
            }

            return cameraManager;
        }

        /// <summary>
        /// 현재 활성 맵의 MapTileCommon을 조회하여 캐릭터 유지 범위 계산에 사용합니다.
        /// </summary>
        private void ResolveCurrentMapTileCommon()
        {
            if (_currentMapTileCommon != null)
            {
                return;
            }

            MapManager mapManager = SceneGame.Instance != null
                ? SceneGame.Instance.mapManager
                : null;
            Transform currentMap = mapManager != null
                ? mapManager.GetCurrentMap()
                : null;

            _currentMapTileCommon = currentMap != null
                ? currentMap.GetComponent<MapTileCommon>()
                : null;
        }

        /// <summary>
        /// 신규 Rig와 동일한 오브젝트에 남아 있는 레거시 배경 컨트롤러를 중지하여 이중 위치 갱신을 방지합니다.
        /// </summary>
        private void DisableLegacyControllers()
        {
            bool hasConflict = false;

            if (TryGetComponent(out ParallaxRig2D legacyParallaxRig) && legacyParallaxRig.enabled)
            {
                legacyParallaxRig.enabled = false;
                hasConflict = true;
            }

            if (TryGetComponent(out InfiniteScrollingBackgroundController legacyInfiniteController) &&
                legacyInfiniteController.enabled)
            {
                legacyInfiniteController.enabled = false;
                hasConflict = true;
            }

            if (hasConflict && !_hasLoggedLegacyConflict)
            {
                _hasLoggedLegacyConflict = true;
                GcLogger.LogWarning(
                    $"[BackgroundMotionRig2D] {name}: 레거시 배경 Rig를 중지하고 신규 통합 Rig가 위치 갱신을 전담합니다.");
            }
        }
    }
}
