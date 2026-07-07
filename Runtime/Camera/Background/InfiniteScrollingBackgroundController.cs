using System;
using UnityEngine;

namespace GGemCo2DCore
{
    /// <summary>
    /// 카메라 기준 위치를 따라 무한 반복 배경 레이어들을 갱신합니다.
    /// </summary>
    [DefaultExecutionOrder(1001)]
    public sealed class InfiniteScrollingBackgroundController : MonoBehaviour
    {
        [SerializeField] private CameraManager cameraManager;
        [SerializeField] private bool autoCollectChildLayers = true;
        [SerializeField] private bool includeInactiveLayers = true;
        [SerializeField] private InfiniteScrollingBackgroundLayer[] layers = Array.Empty<InfiniteScrollingBackgroundLayer>();

        private bool _hasBaseline;

        private void Awake()
        {
            RefreshLayers();
            MapManager.OnLoadCompleteMap += OnLoadCompleteMap;
        }

        private void OnDestroy()
        {
            MapManager.OnLoadCompleteMap -= OnLoadCompleteMap;
        }

        private void OnEnable()
        {
            RefreshLayers();
            CaptureBaseline();
        }

        private void LateUpdate()
        {
            if (!TryResolveCameraPosition(out Vector3 cameraPosition))
            {
                return;
            }

            if (!_hasBaseline)
            {
                CaptureBaseline(cameraPosition);
            }

            for (int i = 0; i < layers.Length; i++)
            {
                if (layers[i] == null)
                {
                    continue;
                }

                layers[i].Tick(cameraPosition);
            }
        }

        private void OnLoadCompleteMap(MapTileCommon mapTileCommon, GameObject grid)
        {
            _ = mapTileCommon;
            _ = grid;
            CaptureBaseline();
        }

        /// <summary>
        /// 자식 오브젝트에서 배경 레이어를 다시 수집합니다.
        /// </summary>
        public void RefreshLayers()
        {
            if (!autoCollectChildLayers)
            {
                layers ??= Array.Empty<InfiniteScrollingBackgroundLayer>();
                return;
            }

            layers = GetComponentsInChildren<InfiniteScrollingBackgroundLayer>(includeInactiveLayers);
        }

        /// <summary>
        /// 현재 카메라 기준 위치를 모든 레이어의 기준점으로 저장합니다.
        /// </summary>
        public void CaptureBaseline()
        {
            if (!TryResolveCameraPosition(out Vector3 cameraPosition))
            {
                _hasBaseline = false;
                return;
            }

            CaptureBaseline(cameraPosition);
        }

        private void CaptureBaseline(Vector3 cameraPosition)
        {
            if (layers == null || layers.Length == 0)
            {
                RefreshLayers();
            }

            for (int i = 0; i < layers.Length; i++)
            {
                if (layers[i] == null)
                {
                    continue;
                }

                layers[i].CaptureBaseline(cameraPosition);
            }

            _hasBaseline = true;
        }

        private bool TryResolveCameraPosition(out Vector3 cameraPosition)
        {
            CameraManager resolvedCameraManager = ResolveCameraManager();
            if (resolvedCameraManager != null)
            {
                cameraPosition = resolvedCameraManager.GetBaseWorldPosition();
                return true;
            }

            Camera mainCamera = Camera.main;
            if (mainCamera != null)
            {
                cameraPosition = mainCamera.transform.position;
                return true;
            }

            cameraPosition = Vector3.zero;
            return false;
        }

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
    }
}
