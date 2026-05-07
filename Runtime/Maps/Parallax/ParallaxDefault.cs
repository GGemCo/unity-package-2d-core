using System.Collections.Generic;
using UnityEngine;

namespace GGemCo2DCore
{
    /// <summary>
    /// 배경 이미지 오브젝트를 지정한 방향으로 이동시키고, 필요하면 같은 이미지를 이어 붙여 반복 배경처럼 처리합니다.
    /// </summary>
    public class ParallaxDefault : MonoBehaviour
    {
        [Header("Move")]
        [SerializeField] private float speed = 1f;
        [SerializeField] private Vector3 direction = Vector3.left;

        [Header("Infinite Loop")]
        [SerializeField] private bool useInfiniteLoop = true;
        [SerializeField] private Camera loopCamera;
        [SerializeField, Min(1)] private int preloadCloneCount = 1;
        [SerializeField, Min(0f)] private float segmentSpacing = 0f;
        [SerializeField, Min(0f)] private float recyclePadding = 0f;

        [Header("Debug")]
        [SerializeField] private bool enableDebugLog = false;
        [SerializeField] private Camera debugCamera;
        [SerializeField] private float debugLogInterval = 1f;

        private const float DirectionEpsilon = 0.0001f;

        private readonly List<Transform> _segments = new();
        private readonly List<GameObject> _runtimeClones = new();

        private float _debugElapsedTime;
        private bool _isRuntimeClone;
        private bool _isInfiniteLoopInitialized;

        /// <summary>
        /// 인스펙터에서 반복 배경 설정값이 유효 범위를 벗어나지 않도록 보정합니다.
        /// </summary>
        private void OnValidate()
        {
            preloadCloneCount = Mathf.Max(1, preloadCloneCount);
            segmentSpacing = Mathf.Max(0f, segmentSpacing);
            recyclePadding = Mathf.Max(0f, recyclePadding);
        }

        /// <summary>
        /// 게임 시작 또는 맵 로딩으로 오브젝트가 활성화된 뒤 반복 배경용 복사본을 미리 준비합니다.
        /// </summary>
        private void Start()
        {
            if (!useInfiniteLoop || _isRuntimeClone)
            {
                return;
            }

            InitializeInfiniteLoop();
        }

        /// <summary>
        /// 원본 오브젝트가 제거될 때 런타임에서 만든 복사본을 함께 정리합니다.
        /// </summary>
        private void OnDestroy()
        {
            if (_isRuntimeClone)
            {
                return;
            }

            DestroyRuntimeClones();
        }

        /// <summary>
        /// 매 프레임 패럴랙스 오브젝트를 이동시키고, 반복 배경 옵션이 켜져 있으면 화면 밖으로 나간 조각을 다음 위치로 재배치합니다.
        /// </summary>
        private void Update()
        {
            if (!TryResolveLocalMoveDirection(out Vector3 localMoveDirection))
            {
                UpdateDebugLog();
                return;
            }

            Vector3 moveDelta = localMoveDirection * speed * Time.deltaTime;

            if (useInfiniteLoop && !_isRuntimeClone)
            {
                if (!_isInfiniteLoopInitialized)
                {
                    InitializeInfiniteLoop();
                }

                MoveInfiniteLoopSegments(moveDelta);
                RecyclePassedSegments();
            }
            else
            {
                transform.Translate(moveDelta);
            }

            UpdateDebugLog();
        }

        /// <summary>
        /// 현재 방향 값을 기준으로 런타임 반복 배경에 사용할 원본과 복사본 목록을 구성합니다.
        /// </summary>
        private void InitializeInfiniteLoop()
        {
            if (_isInfiniteLoopInitialized || _isRuntimeClone)
            {
                return;
            }

            _segments.Clear();
            _segments.Add(transform);

            if (!TryResolveWorldMoveAxis(out Vector3 moveAxis))
            {
                _isInfiniteLoopInitialized = true;
                return;
            }

            Vector3 spawnAxis = -moveAxis;
            for (int i = 0; i < preloadCloneCount; i++)
            {
                CreateRuntimeClone(spawnAxis);
            }

            _isInfiniteLoopInitialized = true;
        }

        /// <summary>
        /// 원본 오브젝트를 복사하고, 이동 방향의 반대편 끝에 붙여 다음 배경 조각으로 사용합니다.
        /// </summary>
        /// <param name="spawnAxis">새 배경 조각을 배치할 월드 방향입니다.</param>
        private void CreateRuntimeClone(Vector3 spawnAxis)
        {
            GameObject clone = Instantiate(gameObject, transform.parent, false);
            clone.name = $"{name}_Loop_{_runtimeClones.Count + 1}";

            if (clone.TryGetComponent(out ParallaxDefault cloneParallax))
            {
                cloneParallax._isRuntimeClone = true;
                cloneParallax.enabled = false;
            }

            Transform cloneTransform = clone.transform;
            if (!PlaceSegmentAfterLast(cloneTransform, spawnAxis))
            {
                Destroy(clone);
                return;
            }

            _runtimeClones.Add(clone);
            _segments.Add(cloneTransform);
        }

        /// <summary>
        /// 반복 배경에 포함된 모든 조각을 같은 이동량만큼 이동시킵니다.
        /// </summary>
        /// <param name="moveDelta">이번 프레임에 적용할 로컬 이동량입니다.</param>
        private void MoveInfiniteLoopSegments(Vector3 moveDelta)
        {
            for (int i = _segments.Count - 1; i >= 0; i--)
            {
                Transform segment = _segments[i];
                if (segment == null)
                {
                    _segments.RemoveAt(i);
                    continue;
                }

                segment.Translate(moveDelta);
            }
        }

        /// <summary>
        /// 카메라 화면을 완전히 벗어난 배경 조각을 찾아 가장 뒤쪽 배경 다음 위치로 재배치합니다.
        /// </summary>
        private void RecyclePassedSegments()
        {
            if (_segments.Count <= 1 || !TryResolveWorldMoveAxis(out Vector3 moveAxis))
            {
                return;
            }

            Camera targetCamera = ResolveLoopCamera();
            if (targetCamera == null)
            {
                return;
            }

            if (!TryGetCameraProjection(targetCamera, moveAxis, out _, out float cameraForwardEdge))
            {
                return;
            }

            Vector3 spawnAxis = -moveAxis;
            for (int i = 0; i < _segments.Count; i++)
            {
                Transform segment = _segments[i];
                if (segment == null)
                {
                    continue;
                }

                if (!TryGetSegmentProjection(segment, moveAxis, out float segmentBackEdge, out _))
                {
                    continue;
                }

                if (segmentBackEdge <= cameraForwardEdge + recyclePadding)
                {
                    continue;
                }

                PlaceSegmentAfterLast(segment, spawnAxis);
            }
        }

        /// <summary>
        /// 지정한 배경 조각을 현재 가장 뒤쪽 조각 다음에 빈틈없이 배치합니다.
        /// </summary>
        /// <param name="segment">재배치할 배경 조각입니다.</param>
        /// <param name="spawnAxis">배경이 새로 이어질 월드 방향입니다.</param>
        /// <returns>배치 기준이 되는 렌더러 영역을 계산했으면 true를 반환합니다.</returns>
        private bool PlaceSegmentAfterLast(Transform segment, Vector3 spawnAxis)
        {
            if (segment == null)
            {
                return false;
            }

            if (!TryGetLastSegmentEdge(spawnAxis, segment, out float lastEdge))
            {
                return false;
            }

            if (!TryGetSegmentProjection(segment, spawnAxis, out float segmentStartEdge, out _))
            {
                return false;
            }

            float moveDistance = lastEdge + segmentSpacing - segmentStartEdge;
            segment.position += spawnAxis * moveDistance;
            return true;
        }

        /// <summary>
        /// 현재 배경 조각 중 새 배경을 이어 붙일 기준이 되는 가장 뒤쪽 끝 좌표를 계산합니다.
        /// </summary>
        /// <param name="axis">끝 좌표를 비교할 월드 방향입니다.</param>
        /// <param name="excludeSegment">이번에 이동시킬 조각은 기준 계산에서 제외합니다.</param>
        /// <param name="lastEdge">계산된 가장 뒤쪽 끝 좌표입니다.</param>
        /// <returns>기준 조각을 찾았으면 true를 반환합니다.</returns>
        private bool TryGetLastSegmentEdge(Vector3 axis, Transform excludeSegment, out float lastEdge)
        {
            lastEdge = float.NegativeInfinity;
            bool hasSegment = false;

            for (int i = 0; i < _segments.Count; i++)
            {
                Transform segment = _segments[i];
                if (segment == null || segment == excludeSegment)
                {
                    continue;
                }

                if (!TryGetSegmentProjection(segment, axis, out _, out float segmentEndEdge))
                {
                    continue;
                }

                if (segmentEndEdge > lastEdge)
                {
                    lastEdge = segmentEndEdge;
                }

                hasSegment = true;
            }

            return hasSegment;
        }

        /// <summary>
        /// 지정한 조각에 포함된 렌더러들의 월드 Bounds를 특정 축에 투영해 시작점과 끝점을 계산합니다.
        /// </summary>
        /// <param name="segment">투영할 배경 조각입니다.</param>
        /// <param name="axis">투영 기준이 되는 월드 방향입니다.</param>
        /// <param name="min">축 기준 최소 투영값입니다.</param>
        /// <param name="max">축 기준 최대 투영값입니다.</param>
        /// <returns>활성 렌더러를 하나 이상 찾았으면 true를 반환합니다.</returns>
        private static bool TryGetSegmentProjection(Transform segment, Vector3 axis, out float min, out float max)
        {
            min = float.PositiveInfinity;
            max = float.NegativeInfinity;

            if (segment == null)
            {
                return false;
            }

            Renderer[] renderers = segment.GetComponentsInChildren<Renderer>(false);
            for (int i = 0; i < renderers.Length; i++)
            {
                Renderer targetRenderer = renderers[i];
                if (targetRenderer == null || !targetRenderer.enabled)
                {
                    continue;
                }

                Bounds bounds = targetRenderer.bounds;
                float center = Vector3.Dot(bounds.center, axis);
                float extent =
                    Mathf.Abs(axis.x) * bounds.extents.x +
                    Mathf.Abs(axis.y) * bounds.extents.y +
                    Mathf.Abs(axis.z) * bounds.extents.z;

                min = Mathf.Min(min, center - extent);
                max = Mathf.Max(max, center + extent);
            }

            return min <= max;
        }

        /// <summary>
        /// 카메라 화면의 네 모서리를 특정 축에 투영해 카메라가 차지하는 구간을 계산합니다.
        /// </summary>
        /// <param name="targetCamera">기준으로 사용할 카메라입니다.</param>
        /// <param name="axis">투영 기준이 되는 월드 방향입니다.</param>
        /// <param name="min">축 기준 최소 투영값입니다.</param>
        /// <param name="max">축 기준 최대 투영값입니다.</param>
        /// <returns>카메라 정보를 계산할 수 있으면 true를 반환합니다.</returns>
        private bool TryGetCameraProjection(Camera targetCamera, Vector3 axis, out float min, out float max)
        {
            min = float.PositiveInfinity;
            max = float.NegativeInfinity;

            if (targetCamera == null)
            {
                return false;
            }

            float cameraDistance = Vector3.Dot(transform.position - targetCamera.transform.position, targetCamera.transform.forward);
            if (cameraDistance <= 0f)
            {
                cameraDistance = targetCamera.nearClipPlane;
            }

            ProjectCameraViewportPoint(targetCamera, axis, new Vector3(0f, 0f, cameraDistance), ref min, ref max);
            ProjectCameraViewportPoint(targetCamera, axis, new Vector3(0f, 1f, cameraDistance), ref min, ref max);
            ProjectCameraViewportPoint(targetCamera, axis, new Vector3(1f, 0f, cameraDistance), ref min, ref max);
            ProjectCameraViewportPoint(targetCamera, axis, new Vector3(1f, 1f, cameraDistance), ref min, ref max);

            return min <= max;
        }

        /// <summary>
        /// 카메라 뷰포트 좌표 하나를 월드 좌표로 변환한 뒤 지정 축에 투영합니다.
        /// </summary>
        /// <param name="targetCamera">기준으로 사용할 카메라입니다.</param>
        /// <param name="axis">투영 기준이 되는 월드 방향입니다.</param>
        /// <param name="viewportPoint">투영할 뷰포트 좌표입니다.</param>
        /// <param name="min">현재까지의 최소 투영값입니다.</param>
        /// <param name="max">현재까지의 최대 투영값입니다.</param>
        private static void ProjectCameraViewportPoint(
            Camera targetCamera,
            Vector3 axis,
            Vector3 viewportPoint,
            ref float min,
            ref float max)
        {
            Vector3 worldPoint = targetCamera.ViewportToWorldPoint(viewportPoint);
            float projection = Vector3.Dot(worldPoint, axis);
            min = Mathf.Min(min, projection);
            max = Mathf.Max(max, projection);
        }

        /// <summary>
        /// 반복 배경 판정에 사용할 카메라를 가져옵니다.
        /// </summary>
        /// <returns>명시된 카메라가 있으면 해당 카메라를, 없으면 메인 카메라를 반환합니다.</returns>
        private Camera ResolveLoopCamera()
        {
            return loopCamera != null ? loopCamera : SceneGame.Instance.mainCamera;
        }

        /// <summary>
        /// 설정된 방향 값을 로컬 이동 방향으로 변환합니다.
        /// </summary>
        /// <param name="moveDirection">정규화된 로컬 이동 방향입니다.</param>
        /// <returns>유효한 방향 값이면 true를 반환합니다.</returns>
        private bool TryResolveLocalMoveDirection(out Vector3 moveDirection)
        {
            if (direction.sqrMagnitude <= DirectionEpsilon)
            {
                moveDirection = Vector3.zero;
                return false;
            }

            moveDirection = direction.normalized;
            return true;
        }

        /// <summary>
        /// 설정된 방향 값을 월드 기준 이동 축으로 변환합니다.
        /// </summary>
        /// <param name="moveAxis">정규화된 월드 이동 축입니다.</param>
        /// <returns>유효한 이동 축이면 true를 반환합니다.</returns>
        private bool TryResolveWorldMoveAxis(out Vector3 moveAxis)
        {
            if (!TryResolveLocalMoveDirection(out Vector3 localMoveDirection))
            {
                moveAxis = Vector3.zero;
                return false;
            }

            moveAxis = transform.TransformDirection(localMoveDirection);
            if (moveAxis.sqrMagnitude <= DirectionEpsilon)
            {
                moveAxis = Vector3.zero;
                return false;
            }

            moveAxis.Normalize();
            return true;
        }

        /// <summary>
        /// 런타임에서 생성한 복사본을 제거하고 내부 목록을 초기 상태로 되돌립니다.
        /// </summary>
        private void DestroyRuntimeClones()
        {
            for (int i = _runtimeClones.Count - 1; i >= 0; i--)
            {
                GameObject clone = _runtimeClones[i];
                if (clone == null)
                {
                    continue;
                }

                Destroy(clone);
            }

            _runtimeClones.Clear();
            _segments.Clear();
            _isInfiniteLoopInitialized = false;
        }

        /// <summary>
        /// 디버그 옵션이 켜져 있으면 지정한 주기마다 현재 이동 정보를 출력합니다.
        /// </summary>
        private void UpdateDebugLog()
        {
            if (!enableDebugLog)
            {
                return;
            }

            _debugElapsedTime += Time.deltaTime;
            if (_debugElapsedTime < debugLogInterval)
            {
                return;
            }

            _debugElapsedTime = 0f;
            LogDebugInfo();
        }

        /// <summary>
        /// 현재 설정 기준의 초당 월드 이동량(unit/s)과
        /// Orthographic 카메라 기준 화면 픽셀 이동량(px/s)을 로그로 출력합니다.
        /// </summary>
        [ContextMenu("Log Debug Info")]
        private void LogDebugInfo()
        {
            float worldUnitsPerSecond = direction.sqrMagnitude > DirectionEpsilon ? speed : 0f;
            Camera targetCamera = debugCamera != null ? debugCamera : Camera.main;

            if (targetCamera == null)
            {
                Debug.Log(
                    $"[ParallaxDefault] {name} | " +
                    $"world: {worldUnitsPerSecond:F3} unit/s | " +
                    $"camera 가 없어 pixel 값을 계산할 수 없습니다.",
                    this);
                return;
            }

            if (!targetCamera.orthographic)
            {
                Debug.Log(
                    $"[ParallaxDefault] {name} | " +
                    $"world: {worldUnitsPerSecond:F3} unit/s | " +
                    $"Perspective 카메라는 화면 위치에 따라 픽셀 비율이 달라지므로 고정 px/s 계산이 어렵습니다.",
                    this);
                return;
            }

            float pixelsPerUnitOnScreen = Screen.height / (targetCamera.orthographicSize * 2f);
            float pixelsPerSecond = worldUnitsPerSecond * pixelsPerUnitOnScreen;

            Debug.Log(
                $"[ParallaxDefault] {name} | " +
                $"world: {worldUnitsPerSecond:F3} unit/s | " +
                $"screen: {pixelsPerSecond:F3} px/s | " +
                $"ppuOnScreen: {pixelsPerUnitOnScreen:F3} px/unit | " +
                $"screenHeight: {Screen.height} | " +
                $"orthoSize: {targetCamera.orthographicSize:F3}",
                this);
        }
    }
}
