using System;
using UnityEngine;

namespace GGemCo2DCore
{
    /// <summary>
    /// 카메라 이동량을 기준으로 여러 파랄럭스 레이어를 동기화하는 루트 컴포넌트입니다.
    /// </summary>
    [DefaultExecutionOrder(1000)]
    public class ParallaxRig2D : MonoBehaviour
    {
        [Header("카메라 참조")] [Tooltip("비워두면 SceneGame 의 CameraManager 를 자동으로 찾습니다.")] [SerializeField]
        private CameraManager cameraManager;

        [Tooltip("True 이면 흔들림이 적용되기 전의 카메라 기본 위치를 사용합니다.")] [SerializeField]
        private bool useStableCameraPosition = true;

        [Header("레이어 수집")] [Tooltip("True 이면 자식에 있는 ParallaxLayer2D 를 자동으로 수집합니다.")] [SerializeField]
        private bool autoCollectChildLayers = true;

        [Tooltip("True 이면 비활성 오브젝트까지 포함해서 수집합니다.")] [SerializeField]
        private bool includeInactiveLayers = true;

        [SerializeField] private ParallaxLayer2D[] layers = Array.Empty<ParallaxLayer2D>();

        private Vector3 _baselineCameraPosition;
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

        private void OnLoadCompleteMap(MapTileCommon mapTileCommon, GameObject grid)
        {
            CaptureBaseline();
        }

        private void OnEnable()
        {
            RefreshLayers();
            // CaptureBaseline();
        }

        private void OnDisable()
        {
            ResetLayersToBaseline();
            _hasBaseline = false;
        }

        private void LateUpdate()
        {
            if (!TryResolveReferencePosition(out Vector3 referencePosition))
            {
                return;
            }

            if (!_hasBaseline)
            {
                return;
                // CaptureBaseline(referencePosition);
            }

            Vector3 cameraDelta = referencePosition - _baselineCameraPosition;
            ApplyParallax(cameraDelta);
        }

        /// <summary>
        /// 현재 설정을 기준으로 파랄럭스 레이어 목록을 다시 구성합니다.
        /// </summary>
        private void RefreshLayers()
        {
            if (!autoCollectChildLayers)
            {
                layers ??= Array.Empty<ParallaxLayer2D>();
                return;
            }

            layers = GetComponentsInChildren<ParallaxLayer2D>(includeInactiveLayers);
        }

        /// <summary>
        /// 현재 카메라 위치와 현재 레이어 위치를 기준값으로 다시 저장합니다.
        /// </summary>
        private void CaptureBaseline()
        {
            if (!TryResolveReferencePosition(out Vector3 referencePosition))
            {
                _hasBaseline = false;
                return;
            }

            CaptureBaseline(referencePosition);
        }

        /// <summary>
        /// 저장된 기준 위치로 모든 레이어를 복원합니다.
        /// </summary>
        private void ResetLayersToBaseline()
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
        /// 기준 카메라 위치를 명시적으로 지정하여 다시 저장합니다.
        /// </summary>
        /// <param name="referencePosition">기준으로 사용할 카메라 위치입니다.</param>
        private void CaptureBaseline(Vector3 referencePosition)
        {
            if (layers == null || layers.Length == 0)
            {
                RefreshLayers();
            }

            if (layers == null || layers.Length == 0)
            {
                GcLogger.LogWarning($"ParallaxRig2D has no layers.");
                return;
            }

            for (int i = 0; i < layers.Length; i++)
            {
                if (layers[i] == null)
                {
                    continue;
                }

                layers[i].CaptureBaseline();
            }

            _baselineCameraPosition = referencePosition;
            _hasBaseline = true;
        }

        /// <summary>
        /// 카메라 이동량을 모든 파랄럭스 레이어에 적용합니다.
        /// </summary>
        /// <param name="cameraDelta">기준 시점 대비 카메라 이동량입니다.</param>
        private void ApplyParallax(Vector3 cameraDelta)
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

                layers[i].ApplyParallax(cameraDelta);
            }
        }

        /// <summary>
        /// 현재 파랄럭스 기준 카메라 위치를 반환합니다.
        /// </summary>
        public Vector3 GetBaselineCameraPosition()
        {
            return _baselineCameraPosition;
        }

        /// <summary>
        /// SceneGame 또는 메인 카메라를 사용하여 파랄럭스 계산용 기준 위치를 찾습니다.
        /// </summary>
        /// <param name="referencePosition">계산에 사용할 기준 카메라 위치입니다.</param>
        /// <returns>참조 가능한 카메라가 있으면 True 를 반환합니다.</returns>
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
        /// 명시된 참조가 없을 때 SceneGame 의 CameraManager 를 자동으로 연결합니다.
        /// </summary>
        /// <returns>사용 가능한 CameraManager 인스턴스입니다.</returns>
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
