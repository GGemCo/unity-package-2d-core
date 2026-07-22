using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

namespace GGemCo2DCore
{
    /// <summary>
    /// 배경 이미지 오브젝트를 지정한 방향으로 이동시키고, 필요하면 같은 이미지를 이어 붙여 반복 배경처럼 처리합니다.
    /// </summary>
    public class ParallaxDefault : MonoBehaviour
    {
        [Header("Move")]
        [Tooltip("배경의 이동 속도입니다. direction을 정규화한 뒤 초당 로컬 유닛으로 적용합니다.")]
        [SerializeField] private float speed = 1f;
        [Tooltip("배경이 이동할 로컬 방향입니다. 벡터의 크기는 이동 속도에 영향을 주지 않습니다.")]
        [SerializeField] private Vector3 direction = Vector3.left;

        [Header("Infinite Loop")]
        [Tooltip("배경 조각을 이어 붙이고 화면을 벗어난 조각을 재배치하여 무한 반복할지 여부입니다.")]
        [SerializeField] private bool useInfiniteLoop = true;
        [Tooltip("반복 범위를 판정할 카메라입니다. 비워두면 SceneGame의 메인 카메라, 그다음 Camera.main을 사용합니다.")]
        [SerializeField] private Camera loopCamera;
        [Tooltip("시작할 때 GGemCoSettings.autoMoveStartDirection의 반대편에 미리 생성할 반복 배경 복제본 수입니다.")]
        [SerializeField, Min(0)] private int preloadCloneCountOppositeAutoMoveStartDirection = 1;
        [Tooltip("시작할 때 GGemCoSettings.autoMoveStartDirection 쪽에 미리 생성할 반복 배경 복제본 수입니다.")]
        [FormerlySerializedAs("preloadCloneCountMoveDirection")]
        [SerializeField, Min(0)] private int preloadCloneCountAutoMoveStartDirection;
        [Tooltip("이어지는 배경 조각 사이에 추가할 간격입니다. 이동 축 기준 월드 유닛으로 적용합니다.")]
        [SerializeField, Min(0f)] private float segmentSpacing = 0f;
        [Tooltip("배경 조각이 유지 범위를 완전히 벗어난 뒤 재배치되기까지 허용할 여유 거리입니다.")]
        [SerializeField, Min(0f)] private float recyclePadding = 0f;
        [Tooltip("카메라와 배경의 상대 이동 방향을 기준으로 배경 조각의 재배치 방향을 결정할지 여부입니다.")]
        [SerializeField] private bool useCameraRelativeLoopDirection = true;
        [Tooltip("카메라 기준 상대 이동 방향이 반대로 바뀌었다고 판단할 최소 이동량입니다.")]
        [SerializeField, Min(0f)] private float cameraRelativeDirectionThreshold = 0.001f;

        [Header("Actor Residency")]
        [Tooltip("현재 맵의 캐릭터 위치까지 배경 유지 범위에 포함하여 캐릭터 주변 조각의 조기 재배치를 방지할지 여부입니다.")]
        [SerializeField] private bool useActorResidencyAnchors = true;
        [Tooltip("각 캐릭터 위치를 기준으로 배경 유지 범위에 추가할 여유 거리입니다.")]
        [SerializeField, Min(0f)] private float actorResidencyPadding = 2f;
        [Tooltip("비활성화된 캐릭터의 위치도 배경 유지 범위 계산에 포함할지 여부입니다.")]
        [SerializeField] private bool includeInactiveActorResidencyAnchors;

        [Header("Debug")]
        [Tooltip("지정한 주기마다 월드 이동 속도와 화면 픽셀 이동 속도를 Console에 출력할지 여부입니다.")]
        [SerializeField] private bool enableDebugLog = false;
        [Tooltip("화면 픽셀 이동 속도를 계산할 카메라입니다. 비워두면 Camera.main을 사용합니다.")]
        [SerializeField] private Camera debugCamera;
        [Tooltip("디버그 이동 정보를 출력하는 시간 간격입니다(초).")]
        [SerializeField] private float debugLogInterval = 1f;

        private const float DirectionEpsilon = 0.0001f;

        private readonly List<Transform> _segments = new();
        private readonly List<GameObject> _runtimeClones = new();
        private readonly List<Transform> _actorResidencyAnchors = new();

        private float _debugElapsedTime;
        private bool _isRuntimeClone;
        private bool _isInfiniteLoopInitialized;
        private bool _hasPreviousLoopCameraPosition;
        private Camera _previousLoopCamera;
        private Vector3 _previousLoopCameraPosition;
        private MapTileCommon _mapTileCommon;

        /// <summary>
        /// 새로 추가하거나 Reset한 컴포넌트가 자동 이동 시작 방향 양쪽에 반복 배경을 하나씩 준비하도록 기본값을 설정합니다.
        /// </summary>
        private void Reset()
        {
            preloadCloneCountOppositeAutoMoveStartDirection = 1;
            preloadCloneCountAutoMoveStartDirection = 1;
        }

        /// <summary>
        /// 인스펙터에서 반복 배경 설정값이 유효 범위를 벗어나지 않도록 보정합니다.
        /// </summary>
        private void OnValidate()
        {
            preloadCloneCountOppositeAutoMoveStartDirection =
                Mathf.Max(0, preloadCloneCountOppositeAutoMoveStartDirection);
            preloadCloneCountAutoMoveStartDirection = Mathf.Max(0, preloadCloneCountAutoMoveStartDirection);
            if (useInfiniteLoop &&
                preloadCloneCountOppositeAutoMoveStartDirection == 0 &&
                preloadCloneCountAutoMoveStartDirection == 0)
            {
                // 반복 배경에 복제본이 하나도 없으면 재배치가 동작하지 않으므로 시작 방향 반대편에 최소 한 개를 유지합니다.
                preloadCloneCountOppositeAutoMoveStartDirection = 1;
            }

            segmentSpacing = Mathf.Max(0f, segmentSpacing);
            recyclePadding = Mathf.Max(0f, recyclePadding);
            cameraRelativeDirectionThreshold = Mathf.Max(0f, cameraRelativeDirectionThreshold);
            actorResidencyPadding = Mathf.Max(0f, actorResidencyPadding);
        }

        /// <summary>
        /// 현재 맵 로딩 이벤트를 구독하고, 이미 로드된 맵이 있으면 Parallax 유지 앵커 조회 대상으로 캐싱합니다.
        /// </summary>
        private void OnEnable()
        {
            if (_isRuntimeClone)
            {
                return;
            }

            MapManager.OnLoadCompleteMap += OnLoadCompleteMap;
            ResolveCurrentMapTileCommon();
        }

        /// <summary>
        /// 현재 맵 로딩 이벤트 구독을 해제합니다.
        /// </summary>
        private void OnDisable()
        {
            MapManager.OnLoadCompleteMap -= OnLoadCompleteMap;
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

                Camera targetCamera = ResolveLoopCamera();
                Vector3 cameraDelta = ResolveLoopCameraDelta(targetCamera);
                MoveInfiniteLoopSegments(moveDelta);
                RecyclePassedSegments(targetCamera, cameraDelta);
            }
            else
            {
                transform.Translate(moveDelta);
            }

            UpdateDebugLog();
        }

        /// <summary>
        /// 전역 자동 이동 시작 방향을 기준으로 런타임 반복 배경에 사용할 원본과 복사본 목록을 구성합니다.
        /// 설정 로더가 아직 준비되지 않았으면 잘못된 방향으로 복제하지 않고 다음 Update에서 초기화를 다시 시도합니다.
        /// </summary>
        private void InitializeInfiniteLoop()
        {
            if (_isInfiniteLoopInitialized || _isRuntimeClone)
            {
                return;
            }

            _segments.Clear();
            _segments.Add(transform);

            if (!TryResolveWorldMoveAxis(out _))
            {
                _isInfiniteLoopInitialized = true;
                return;
            }

            if (!TryResolveAutoMoveStartAxis(out Vector3 autoMoveStartAxis))
            {
                return;
            }

            CreateRuntimeClones(-autoMoveStartAxis, preloadCloneCountOppositeAutoMoveStartDirection);
            CreateRuntimeClones(autoMoveStartAxis, preloadCloneCountAutoMoveStartDirection);

            _isInfiniteLoopInitialized = true;
        }

        /// <summary>
        /// 로드된 메인 설정의 자동 이동 시작 방향을 월드 X축 방향으로 변환합니다.
        /// </summary>
        /// <param name="startAxis">왼쪽이면 월드 X축 음의 방향, 오른쪽이면 양의 방향입니다.</param>
        /// <returns>메인 설정이 준비되어 방향을 확인할 수 있으면 <see langword="true"/>를 반환합니다.</returns>
        private static bool TryResolveAutoMoveStartAxis(out Vector3 startAxis)
        {
            GGemCoSettings settings = AddressableLoaderSettings.Instance != null
                ? AddressableLoaderSettings.Instance.settings
                : null;

            if (settings == null)
            {
                startAxis = Vector3.zero;
                return false;
            }

            startAxis = settings.autoMoveStartDirection == AutoMoveDirection.Left
                ? Vector3.left
                : Vector3.right;
            return true;
        }

        /// <summary>
        /// 지정한 방향에 필요한 수만큼 반복 배경 복제본을 이어 붙입니다.
        /// </summary>
        /// <param name="spawnAxis">복제본을 이어 붙일 월드 방향입니다.</param>
        /// <param name="cloneCount">생성할 복제본 수입니다.</param>
        private void CreateRuntimeClones(Vector3 spawnAxis, int cloneCount)
        {
            for (int i = 0; i < cloneCount; i++)
            {
                CreateRuntimeClone(spawnAxis);
            }
        }

        /// <summary>
        /// 원본 오브젝트를 복사하고, 지정한 방향의 마지막 조각 뒤에 붙여 반복 배경으로 사용합니다.
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
        /// <param name="targetCamera">반복 배경 판정에 사용할 카메라입니다.</param>
        /// <param name="cameraDelta">이전 프레임 대비 카메라 월드 이동량입니다.</param>
        private void RecyclePassedSegments(Camera targetCamera, Vector3 cameraDelta)
        {
            if (_segments.Count <= 1 || !TryResolveWorldMoveAxis(out Vector3 moveAxis))
            {
                return;
            }

            if (targetCamera == null)
            {
                return;
            }

            Vector3 recycleAxis = ResolveRecycleAxis(moveAxis, cameraDelta);
            if (!TryGetLoopResidencyProjection(targetCamera, recycleAxis, out _, out float residencyForwardEdge))
            {
                return;
            }

            Vector3 spawnAxis = -recycleAxis;
            for (int i = 0; i < _segments.Count; i++)
            {
                Transform segment = _segments[i];
                if (segment == null)
                {
                    continue;
                }

                if (!TryGetSegmentProjection(segment, recycleAxis, out float segmentBackEdge, out _))
                {
                    continue;
                }

                if (segmentBackEdge <= residencyForwardEdge + recyclePadding)
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
        /// 반복 배경 유지에 사용할 전체 기준 범위를 계산합니다.
        /// 기본 카메라 화면 범위에 현재 맵의 물리 캐릭터 앵커 범위를 합쳐,
        /// 플레이어와 멀어진 캐릭터가 딛고 있는 세그먼트가 즉시 재배치되지 않도록 보호합니다.
        /// </summary>
        /// <param name="targetCamera">반복 배경 판정에 사용할 카메라입니다.</param>
        /// <param name="axis">투영 기준이 되는 월드 방향입니다.</param>
        /// <param name="min">카메라와 캐릭터 앵커를 포함한 최소 투영값입니다.</param>
        /// <param name="max">카메라와 캐릭터 앵커를 포함한 최대 투영값입니다.</param>
        /// <returns>기준 범위를 계산할 수 있으면 <see langword="true"/>를 반환합니다.</returns>
        private bool TryGetLoopResidencyProjection(Camera targetCamera, Vector3 axis, out float min, out float max)
        {
            if (!TryGetCameraProjection(targetCamera, axis, out min, out max))
            {
                return false;
            }

            AppendActorResidencyProjection(axis, ref min, ref max);
            return min <= max;
        }

        /// <summary>
        /// 현재 맵의 물리 캐릭터 앵커를 투영 범위에 추가합니다.
        /// </summary>
        /// <param name="axis">투영 기준이 되는 월드 방향입니다.</param>
        /// <param name="min">현재까지의 최소 투영값입니다.</param>
        /// <param name="max">현재까지의 최대 투영값입니다.</param>
        private void AppendActorResidencyProjection(Vector3 axis, ref float min, ref float max)
        {
            if (!useActorResidencyAnchors)
            {
                return;
            }

            ResolveCurrentMapTileCommon();
            if (_mapTileCommon == null)
            {
                return;
            }

            _actorResidencyAnchors.Clear();
            _mapTileCommon.AppendParallaxActorAnchors(
                _actorResidencyAnchors,
                includeInactiveActorResidencyAnchors);

            for (int i = 0; i < _actorResidencyAnchors.Count; i++)
            {
                Transform actorAnchor = _actorResidencyAnchors[i];
                if (actorAnchor == null)
                {
                    continue;
                }

                float projection = Vector3.Dot(actorAnchor.position, axis);
                min = Mathf.Min(min, projection - actorResidencyPadding);
                max = Mathf.Max(max, projection + actorResidencyPadding);
            }
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
        /// 맵 로딩 완료 시 Parallax 유지 앵커를 조회할 현재 맵을 갱신합니다.
        /// </summary>
        /// <param name="mapTileCommon">현재 로드가 완료된 맵 타일 루트입니다.</param>
        /// <param name="grid">현재 맵이 배치된 Grid 오브젝트입니다.</param>
        private void OnLoadCompleteMap(MapTileCommon mapTileCommon, GameObject grid)
        {
            _ = grid;
            _mapTileCommon = mapTileCommon;
        }

        /// <summary>
        /// 현재 로드된 맵 타일 루트를 찾아 캐싱합니다.
        /// 이벤트 수신 전에 활성화된 Parallax 오브젝트도 앵커 기반 유지 판정을 사용할 수 있도록 보완합니다.
        /// </summary>
        private void ResolveCurrentMapTileCommon()
        {
            if (_mapTileCommon != null)
            {
                return;
            }

            MapManager mapManager = SceneGame.Instance != null
                ? SceneGame.Instance.mapManager
                : null;
            Transform currentMap = mapManager != null
                ? mapManager.GetCurrentMap()
                : null;

            _mapTileCommon = currentMap != null
                ? currentMap.GetComponent<MapTileCommon>()
                : null;
        }

        /// <summary>
        /// 현재 프레임 카메라 위치와 이전 프레임 카메라 위치의 차이를 계산하고, 다음 프레임 비교를 위해 현재 위치를 저장합니다.
        /// </summary>
        /// <param name="targetCamera">반복 배경 판정에 사용할 카메라입니다.</param>
        /// <returns>이전 프레임 대비 카메라 월드 이동량입니다.</returns>
        private Vector3 ResolveLoopCameraDelta(Camera targetCamera)
        {
            if (targetCamera == null)
            {
                _previousLoopCamera = null;
                _hasPreviousLoopCameraPosition = false;
                return Vector3.zero;
            }

            Vector3 currentCameraPosition = targetCamera.transform.position;
            if (!_hasPreviousLoopCameraPosition || _previousLoopCamera != targetCamera)
            {
                _previousLoopCamera = targetCamera;
                _previousLoopCameraPosition = currentCameraPosition;
                _hasPreviousLoopCameraPosition = true;
                return Vector3.zero;
            }

            Vector3 cameraDelta = currentCameraPosition - _previousLoopCameraPosition;
            _previousLoopCameraPosition = currentCameraPosition;
            return cameraDelta;
        }

        /// <summary>
        /// 배경 자체의 월드 이동량과 카메라 이동량을 비교해 화면 기준으로 배경이 빠져나가는 방향을 계산합니다.
        /// </summary>
        /// <param name="moveAxis">direction 필드에서 계산한 월드 이동 축입니다.</param>
        /// <param name="cameraDelta">이전 프레임 대비 카메라 월드 이동량입니다.</param>
        /// <returns>화면 기준으로 배경 조각이 빠져나가는 월드 방향입니다.</returns>
        private Vector3 ResolveRecycleAxis(Vector3 moveAxis, Vector3 cameraDelta)
        {
            if (!useCameraRelativeLoopDirection)
            {
                return moveAxis;
            }

            Vector3 backgroundDelta = moveAxis * speed * Time.deltaTime;
            Vector3 relativeDelta = backgroundDelta - cameraDelta;
            float relativeMoveAmount = Vector3.Dot(relativeDelta, moveAxis);

            if (relativeMoveAmount < -cameraRelativeDirectionThreshold)
            {
                return -moveAxis;
            }

            return moveAxis;
        }

        /// <summary>
        /// 반복 배경 판정에 사용할 카메라를 가져옵니다.
        /// </summary>
        /// <returns>명시된 카메라가 있으면 해당 카메라를, 없으면 메인 카메라를 반환합니다.</returns>
        private Camera ResolveLoopCamera()
        {
            if (loopCamera != null)
            {
                return loopCamera;
            }

            if (SceneGame.Instance != null && SceneGame.Instance.mainCamera != null)
            {
                return SceneGame.Instance.mainCamera;
            }

            return Camera.main;
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
