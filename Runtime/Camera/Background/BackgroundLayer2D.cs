using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

namespace GGemCo2DCore
{
    /// <summary>
    /// 무한 반복 배경이 이어지는 축을 정의합니다.
    /// </summary>
    public enum BackgroundRepeatAxis
    {
        /// <summary>
        /// 로컬 X축을 기준으로 배경을 반복합니다.
        /// </summary>
        Horizontal = 0,

        /// <summary>
        /// 로컬 Y축을 기준으로 배경을 반복합니다.
        /// </summary>
        Vertical = 1,
    }

    /// <summary>
    /// 하나의 배경 레이어에서 원근감, 자동 스크롤, 무한 반복 위치를 합성하여 적용합니다.
    /// 이 컴포넌트가 레이어 세그먼트 Transform을 변경하는 유일한 주체입니다.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class BackgroundLayer2D : MonoBehaviour
    {
        [Header("원근감")]
        [Tooltip("카메라 이동에 따른 원근감 오프셋을 적용합니다.")]
        [SerializeField] private bool useParallax;

        [SerializeField] private bool useHorizontalParallax = true;
        [SerializeField] private bool useVerticalParallax = true;

        [Tooltip("1이면 카메라를 완전히 따라 화면 위치가 유지되고, 0이면 월드 위치를 유지합니다.")]
        [SerializeField] private float horizontalCameraInfluence = 0.75f;

        [Tooltip("1이면 카메라를 완전히 따라 화면 위치가 유지되고, 0이면 월드 위치를 유지합니다.")]
        [SerializeField] private float verticalCameraInfluence = 0.5f;

        [Header("자동 스크롤")]
        [Tooltip("시간에 따라 배경을 자동으로 이동합니다.")]
        [SerializeField] private bool useAutoScroll = true;

        [Tooltip("배경이 이동할 로컬 방향입니다. 크기는 속도에 영향을 주지 않습니다.")]
        [SerializeField] private Vector3 direction = Vector3.left;

        [Tooltip("초당 이동할 월드 거리입니다.")]
        [SerializeField, Min(0f)] private float speed = 1f;

        [Header("무한 반복")]
        [Tooltip("카메라와 캐릭터 유지 범위를 벗어난 세그먼트를 반대편으로 재배치합니다.")]
        [FormerlySerializedAs("useInfiniteLoop")]
        [SerializeField] private bool useInfiniteRepeat;

        [SerializeField] private BackgroundRepeatAxis repeatAxis = BackgroundRepeatAxis.Horizontal;

        [Tooltip("기준 세그먼트의 반복 축 음의 방향에 미리 배치할 복제본 수입니다.")]
        [FormerlySerializedAs("preloadCloneCountOppositeAutoMoveStartDirection")]
        [SerializeField, Min(0)] private int preloadSegmentCountBefore = 1;

        [Tooltip("기준 세그먼트의 반복 축 양의 방향에 미리 배치할 복제본 수입니다.")]
        [FormerlySerializedAs("preloadCloneCountAutoMoveStartDirection")]
        [SerializeField, Min(0)] private int preloadSegmentCountAfter = 1;

        [Tooltip("이어지는 세그먼트 사이에 추가할 월드 간격입니다.")]
        [SerializeField, Min(0f)] private float segmentSpacing;

        [Tooltip("카메라 또는 캐릭터 유지 범위를 완전히 벗어난 뒤 재활용하기 위한 추가 여백입니다.")]
        [SerializeField, Min(0f)] private float recyclePadding;

        [Tooltip("카메라와 배경의 상대 이동 방향을 사용하여 재활용 방향을 결정합니다.")]
        [FormerlySerializedAs("useCameraRelativeLoopDirection")]
        [SerializeField] private bool useCameraRelativeRepeatDirection = true;

        [Tooltip("상대 이동 방향이 반대로 바뀌었다고 판단할 최소 이동량입니다.")]
        [SerializeField, Min(0f)] private float cameraRelativeDirectionThreshold = 0.001f;

        [Header("캐릭터 유지 범위")]
        [Tooltip("현재 맵의 플레이어, 몬스터, NPC 위치까지 세그먼트 유지 범위에 포함합니다.")]
        [SerializeField] private bool useActorResidencyAnchors = true;

        [SerializeField, Min(0f)] private float actorResidencyPadding = 2f;
        [SerializeField] private bool includeInactiveActorResidencyAnchors;

        private const float DirectionEpsilon = 0.0001f;

        private sealed class SegmentState
        {
            public Transform Transform;
            public Renderer[] Renderers;
            public Vector3 BaselinePosition;
        }

        private readonly List<SegmentState> _segments = new();
        private readonly List<GameObject> _runtimeClones = new();
        private readonly List<Transform> _actorResidencyAnchors = new();
        private readonly List<SegmentState> _recycleCandidates = new();
        private readonly List<float> _recycleCandidateEdges = new();
        private readonly List<Renderer> _rendererBuffer = new();

        private MapTileCommon _mapTileCommon;
        private Camera _renderCamera;
        private Vector3 _baselineCameraPosition;
        private Vector3 _previousCameraPosition;
        private Vector3 _autoScrollOffset;
        private Vector3 _appliedCompositeOffset;
        private bool _isRuntimeClone;
        private bool _isInitialized;
        private bool _hasBaseline;
        private bool _hasPreviousCameraPosition;
        private bool _hasLoggedMissingRenderer;

        /// <summary>
        /// 무한 반복 과정에서 생성된 표시 전용 복제본인지 여부입니다.
        /// </summary>
        public bool IsRuntimeClone => _isRuntimeClone;

        private void OnValidate()
        {
            speed = Mathf.Max(0f, speed);
            preloadSegmentCountBefore = Mathf.Max(0, preloadSegmentCountBefore);
            preloadSegmentCountAfter = Mathf.Max(0, preloadSegmentCountAfter);
            segmentSpacing = Mathf.Max(0f, segmentSpacing);
            recyclePadding = Mathf.Max(0f, recyclePadding);
            cameraRelativeDirectionThreshold = Mathf.Max(0f, cameraRelativeDirectionThreshold);
            actorResidencyPadding = Mathf.Max(0f, actorResidencyPadding);

            if (useInfiniteRepeat && preloadSegmentCountBefore == 0 && preloadSegmentCountAfter == 0)
            {
                // 반복 기능은 최소 하나의 복제 세그먼트가 있어야 화면 밖 세그먼트를 교대할 수 있습니다.
                preloadSegmentCountBefore = 1;
            }
        }

        private void OnDestroy()
        {
            if (_isRuntimeClone)
            {
                return;
            }

            DestroyRuntimeClones();
        }

        /// <summary>
        /// 캐릭터 위치를 포함한 반복 유지 범위 계산에 사용할 현재 맵 문맥을 지정합니다.
        /// </summary>
        /// <param name="mapTileCommon">현재 로드된 맵의 공통 루트입니다.</param>
        public void SetMapContext(MapTileCommon mapTileCommon)
        {
            _mapTileCommon = mapTileCommon;
        }

        /// <summary>
        /// 현재 카메라와 모든 세그먼트 위치를 새로운 기준점으로 저장합니다.
        /// </summary>
        /// <param name="cameraPosition">원근감과 상대 이동 계산에 사용할 기준 카메라 위치입니다.</param>
        public void CaptureBaseline(Vector3 cameraPosition)
        {
            if (_isRuntimeClone)
            {
                return;
            }

            EnsureInitialized();

            _baselineCameraPosition = cameraPosition;
            _previousCameraPosition = cameraPosition;
            _autoScrollOffset = Vector3.zero;
            _appliedCompositeOffset = Vector3.zero;
            _hasPreviousCameraPosition = true;

            for (int i = 0; i < _segments.Count; i++)
            {
                SegmentState segment = _segments[i];
                if (segment?.Transform == null)
                {
                    continue;
                }

                segment.BaselinePosition = segment.Transform.position;
            }

            _hasBaseline = true;
        }

        /// <summary>
        /// 카메라 위치와 경과 시간을 기준으로 원근감, 자동 이동, 반복 재배치를 한 번 계산합니다.
        /// </summary>
        /// <param name="cameraPosition">현재 프레임의 안정된 카메라 기준 위치입니다.</param>
        /// <param name="deltaTime">현재 프레임의 경과 시간입니다.</param>
        public void Tick(Vector3 cameraPosition, float deltaTime)
        {
            if (_isRuntimeClone)
            {
                return;
            }

            if (!_hasBaseline)
            {
                CaptureBaseline(cameraPosition);
            }

            Vector3 cameraFrameDelta = ResolveCameraFrameDelta(cameraPosition);
            AccumulateAutoScrollOffset(deltaTime);

            Vector3 targetCompositeOffset = _autoScrollOffset + ResolveParallaxOffset(cameraPosition);
            Vector3 compositeFrameDelta = targetCompositeOffset - _appliedCompositeOffset;
            ApplyCompositeDelta(compositeFrameDelta);
            _appliedCompositeOffset = targetCompositeOffset;

            if (useInfiniteRepeat)
            {
                RecyclePassedSegments(cameraPosition, cameraFrameDelta, compositeFrameDelta);
            }
        }

        /// <summary>
        /// 모든 세그먼트를 마지막으로 저장한 기준 위치로 복원하고 누적 이동량을 초기화합니다.
        /// </summary>
        public void ResetToBaseline()
        {
            if (!_hasBaseline || _isRuntimeClone)
            {
                return;
            }

            for (int i = 0; i < _segments.Count; i++)
            {
                SegmentState segment = _segments[i];
                if (segment?.Transform == null)
                {
                    continue;
                }

                segment.Transform.position = segment.BaselinePosition;
            }

            _autoScrollOffset = Vector3.zero;
            _appliedCompositeOffset = Vector3.zero;
            _hasPreviousCameraPosition = false;
            _hasBaseline = false;
        }

        /// <summary>
        /// 기준 세그먼트와 필요한 런타임 복제본을 최초 한 번 구성합니다.
        /// </summary>
        private void EnsureInitialized()
        {
            if (_isInitialized || _isRuntimeClone)
            {
                return;
            }

            _segments.Clear();
            _segments.Add(CreateSegmentState(gameObject));

            if (useInfiniteRepeat)
            {
                Vector3 repeatWorldAxis = ResolveRepeatWorldAxis();
                CreateRuntimeSegments(-repeatWorldAxis, preloadSegmentCountBefore);
                CreateRuntimeSegments(repeatWorldAxis, preloadSegmentCountAfter);
            }

            _isInitialized = true;
        }

        /// <summary>
        /// 오브젝트와 하위 Renderer를 캐시한 세그먼트 상태를 생성합니다.
        /// Renderer 조회는 초기화 시에만 수행하여 반복 갱신 중 할당을 방지합니다.
        /// </summary>
        /// <param name="segmentObject">세그먼트로 사용할 오브젝트입니다.</param>
        /// <returns>Transform과 Renderer 캐시를 포함한 세그먼트 상태입니다.</returns>
        private SegmentState CreateSegmentState(GameObject segmentObject)
        {
            _rendererBuffer.Clear();
            segmentObject.GetComponentsInChildren(false, _rendererBuffer);
            Renderer[] renderers = _rendererBuffer.ToArray();
            _rendererBuffer.Clear();

            if (renderers.Length == 0 && !_hasLoggedMissingRenderer)
            {
                _hasLoggedMissingRenderer = true;
                GcLogger.LogWarning($"[BackgroundLayer2D] {name}: 반복 크기를 계산할 Renderer가 없습니다.");
            }

            return new SegmentState
            {
                Transform = segmentObject.transform,
                Renderers = renderers,
                BaselinePosition = segmentObject.transform.position,
            };
        }

        /// <summary>
        /// 지정한 방향에 필요한 수만큼 표시 전용 반복 세그먼트를 생성합니다.
        /// </summary>
        /// <param name="spawnAxis">새 세그먼트를 이어 붙일 월드 방향입니다.</param>
        /// <param name="count">생성할 세그먼트 수입니다.</param>
        private void CreateRuntimeSegments(Vector3 spawnAxis, int count)
        {
            for (int i = 0; i < count; i++)
            {
                GameObject clone = Instantiate(gameObject, transform.parent, false);
                clone.name = $"{name}_Repeat_{_runtimeClones.Count + 1}";

                if (clone.TryGetComponent(out BackgroundLayer2D cloneLayer))
                {
                    cloneLayer._isRuntimeClone = true;
                    cloneLayer.enabled = false;
                }

                SegmentState cloneSegment = CreateSegmentState(clone);
                _segments.Add(cloneSegment);
                if (!PlaceSegmentAfterLast(cloneSegment, spawnAxis))
                {
                    _segments.Remove(cloneSegment);
                    Destroy(clone);
                    continue;
                }

                _runtimeClones.Add(clone);
            }
        }

        /// <summary>
        /// 자동 스크롤이 활성화된 경우 이번 프레임의 이동량을 누적합니다.
        /// </summary>
        /// <param name="deltaTime">현재 프레임의 경과 시간입니다.</param>
        private void AccumulateAutoScrollOffset(float deltaTime)
        {
            if (!useAutoScroll || speed <= 0f || !TryResolveAutoScrollWorldDirection(out Vector3 worldDirection))
            {
                return;
            }

            _autoScrollOffset += worldDirection * speed * Mathf.Max(0f, deltaTime);
        }

        /// <summary>
        /// 기준 카메라 위치 대비 현재 카메라 이동량을 원근감 영향 비율로 변환합니다.
        /// </summary>
        /// <param name="cameraPosition">현재 카메라 기준 위치입니다.</param>
        /// <returns>현재 프레임에 적용할 누적 원근감 오프셋입니다.</returns>
        private Vector3 ResolveParallaxOffset(Vector3 cameraPosition)
        {
            if (!useParallax)
            {
                return Vector3.zero;
            }

            Vector3 cameraDelta = cameraPosition - _baselineCameraPosition;
            return new Vector3(
                useHorizontalParallax ? cameraDelta.x * horizontalCameraInfluence : 0f,
                useVerticalParallax ? cameraDelta.y * verticalCameraInfluence : 0f,
                0f);
        }

        /// <summary>
        /// 합성된 프레임 이동량을 원본과 모든 반복 세그먼트에 동일하게 적용합니다.
        /// </summary>
        /// <param name="compositeFrameDelta">자동 스크롤과 원근감이 합쳐진 이번 프레임 이동량입니다.</param>
        private void ApplyCompositeDelta(Vector3 compositeFrameDelta)
        {
            if (compositeFrameDelta.sqrMagnitude <= DirectionEpsilon * DirectionEpsilon)
            {
                return;
            }

            for (int i = _segments.Count - 1; i >= 0; i--)
            {
                SegmentState segment = _segments[i];
                if (segment?.Transform == null)
                {
                    _segments.RemoveAt(i);
                    continue;
                }

                segment.Transform.position += compositeFrameDelta;
            }
        }

        /// <summary>
        /// 카메라와 배경의 상대 이동 방향을 기준으로 유지 범위를 벗어난 세그먼트를 반대편으로 옮깁니다.
        /// </summary>
        /// <param name="cameraPosition">현재 카메라 기준 위치입니다.</param>
        /// <param name="cameraFrameDelta">이전 프레임 대비 카메라 이동량입니다.</param>
        /// <param name="backgroundFrameDelta">이번 프레임의 합성 배경 이동량입니다.</param>
        private void RecyclePassedSegments(
            Vector3 cameraPosition,
            Vector3 cameraFrameDelta,
            Vector3 backgroundFrameDelta)
        {
            if (_segments.Count <= 1)
            {
                return;
            }

            Camera targetCamera = _renderCamera != null
                ? _renderCamera
                : _renderCamera = ResolveRenderCamera();
            if (targetCamera == null)
            {
                return;
            }

            Vector3 repeatWorldAxis = ResolveRepeatWorldAxis();
            Vector3 recycleAxis = ResolveRecycleAxis(
                repeatWorldAxis,
                cameraFrameDelta,
                backgroundFrameDelta);

            if (!TryGetResidencyProjection(
                    targetCamera,
                    cameraPosition,
                    recycleAxis,
                    out _,
                    out float residencyForwardEdge))
            {
                return;
            }

            CollectRecycleCandidates(recycleAxis, residencyForwardEdge + recyclePadding);
            RecycleCandidates(-recycleAxis);
        }

        /// <summary>
        /// 화면과 캐릭터 유지 범위를 완전히 벗어난 세그먼트를 재활용 후보로 수집합니다.
        /// </summary>
        /// <param name="recycleAxis">화면 밖으로 빠져나가는 방향입니다.</param>
        /// <param name="recycleBoundary">재활용을 시작할 투영 경계입니다.</param>
        private void CollectRecycleCandidates(Vector3 recycleAxis, float recycleBoundary)
        {
            _recycleCandidates.Clear();
            _recycleCandidateEdges.Clear();

            for (int i = 0; i < _segments.Count; i++)
            {
                SegmentState segment = _segments[i];
                if (!TryGetSegmentProjection(segment, recycleAxis, out float backEdge, out _))
                {
                    continue;
                }

                if (backEdge <= recycleBoundary)
                {
                    continue;
                }

                _recycleCandidates.Add(segment);
                _recycleCandidateEdges.Add(backEdge);
            }
        }

        /// <summary>
        /// 가장 멀리 벗어난 후보부터 반대편 마지막 세그먼트 뒤에 이어 붙입니다.
        /// </summary>
        /// <param name="spawnAxis">재활용 세그먼트를 배치할 월드 방향입니다.</param>
        private void RecycleCandidates(Vector3 spawnAxis)
        {
            while (_recycleCandidates.Count > 0)
            {
                int candidateIndex = FindFurthestRecycleCandidateIndex();
                if (candidateIndex < 0)
                {
                    return;
                }

                SegmentState segment = _recycleCandidates[candidateIndex];
                PlaceSegmentAfterLast(segment, spawnAxis);
                _recycleCandidates.RemoveAt(candidateIndex);
                _recycleCandidateEdges.RemoveAt(candidateIndex);
            }
        }

        /// <summary>
        /// 재활용 후보 중 현재 유지 범위에서 가장 멀리 떨어진 항목의 인덱스를 찾습니다.
        /// </summary>
        /// <returns>후보 인덱스이며, 후보가 없으면 -1입니다.</returns>
        private int FindFurthestRecycleCandidateIndex()
        {
            int furthestIndex = -1;
            float furthestEdge = float.NegativeInfinity;

            for (int i = 0; i < _recycleCandidateEdges.Count; i++)
            {
                if (_recycleCandidateEdges[i] <= furthestEdge)
                {
                    continue;
                }

                furthestEdge = _recycleCandidateEdges[i];
                furthestIndex = i;
            }

            return furthestIndex;
        }

        /// <summary>
        /// 세그먼트를 지정한 방향의 가장 마지막 세그먼트 뒤에 빈틈없이 배치합니다.
        /// </summary>
        /// <param name="segment">배치할 세그먼트입니다.</param>
        /// <param name="spawnAxis">세그먼트를 이어 붙일 월드 방향입니다.</param>
        /// <returns>유효한 Renderer 경계를 사용하여 배치했으면 <see langword="true"/>입니다.</returns>
        private bool PlaceSegmentAfterLast(SegmentState segment, Vector3 spawnAxis)
        {
            if (segment?.Transform == null ||
                !TryGetLastSegmentEdge(spawnAxis, segment, out float lastEdge) ||
                !TryGetSegmentProjection(segment, spawnAxis, out float segmentStartEdge, out _))
            {
                return false;
            }

            float moveDistance = lastEdge + segmentSpacing - segmentStartEdge;
            segment.Transform.position += spawnAxis * moveDistance;
            return true;
        }

        /// <summary>
        /// 지정한 방향에서 제외 대상을 뺀 가장 마지막 세그먼트의 끝 경계를 찾습니다.
        /// </summary>
        /// <param name="axis">끝 경계를 비교할 정규화된 월드 축입니다.</param>
        /// <param name="excludeSegment">경계 계산에서 제외할 배치 대상입니다.</param>
        /// <param name="lastEdge">조회된 가장 마지막 끝 경계입니다.</param>
        /// <returns>비교 가능한 다른 세그먼트를 찾았으면 <see langword="true"/>입니다.</returns>
        private bool TryGetLastSegmentEdge(
            Vector3 axis,
            SegmentState excludeSegment,
            out float lastEdge)
        {
            lastEdge = float.NegativeInfinity;
            bool hasSegment = false;

            for (int i = 0; i < _segments.Count; i++)
            {
                SegmentState segment = _segments[i];
                if (segment == excludeSegment ||
                    !TryGetSegmentProjection(segment, axis, out _, out float endEdge))
                {
                    continue;
                }

                lastEdge = Mathf.Max(lastEdge, endEdge);
                hasSegment = true;
            }

            return hasSegment;
        }

        /// <summary>
        /// 캐시된 Renderer Bounds를 지정한 월드 축에 투영합니다.
        /// </summary>
        /// <param name="segment">투영할 세그먼트 상태입니다.</param>
        /// <param name="axis">투영에 사용할 정규화된 월드 축입니다.</param>
        /// <param name="min">Renderer Bounds의 최소 투영값입니다.</param>
        /// <param name="max">Renderer Bounds의 최대 투영값입니다.</param>
        /// <returns>활성 Renderer를 하나 이상 계산했으면 <see langword="true"/>입니다.</returns>
        private static bool TryGetSegmentProjection(
            SegmentState segment,
            Vector3 axis,
            out float min,
            out float max)
        {
            min = float.PositiveInfinity;
            max = float.NegativeInfinity;

            if (segment?.Transform == null || segment.Renderers == null)
            {
                return false;
            }

            for (int i = 0; i < segment.Renderers.Length; i++)
            {
                Renderer targetRenderer = segment.Renderers[i];
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
        /// 카메라 화면과 현재 맵 캐릭터 위치를 합친 세그먼트 유지 범위를 계산합니다.
        /// </summary>
        /// <param name="targetCamera">뷰포트 경계를 계산할 렌더 카메라입니다.</param>
        /// <param name="cameraPosition">흔들림이 제외된 안정된 카메라 기준 위치입니다.</param>
        /// <param name="axis">유지 범위를 투영할 월드 축입니다.</param>
        /// <param name="min">계산된 최소 유지 경계입니다.</param>
        /// <param name="max">계산된 최대 유지 경계입니다.</param>
        /// <returns>유효한 카메라 유지 범위를 계산했으면 <see langword="true"/>입니다.</returns>
        private bool TryGetResidencyProjection(
            Camera targetCamera,
            Vector3 cameraPosition,
            Vector3 axis,
            out float min,
            out float max)
        {
            if (!TryGetCameraProjection(targetCamera, cameraPosition, axis, out min, out max))
            {
                return false;
            }

            AppendActorResidencyProjection(axis, ref min, ref max);
            return min <= max;
        }

        /// <summary>
        /// 카메라 뷰포트 네 모서리를 배경 반복 축에 투영합니다.
        /// </summary>
        /// <param name="targetCamera">뷰포트 경계를 계산할 렌더 카메라입니다.</param>
        /// <param name="cameraPosition">흔들림이 제외된 안정된 카메라 기준 위치입니다.</param>
        /// <param name="axis">뷰포트 경계를 투영할 월드 축입니다.</param>
        /// <param name="min">카메라 화면의 최소 투영값입니다.</param>
        /// <param name="max">카메라 화면의 최대 투영값입니다.</param>
        /// <returns>유효한 화면 경계를 계산했으면 <see langword="true"/>입니다.</returns>
        private bool TryGetCameraProjection(
            Camera targetCamera,
            Vector3 cameraPosition,
            Vector3 axis,
            out float min,
            out float max)
        {
            min = float.PositiveInfinity;
            max = float.NegativeInfinity;

            float cameraDistance = Vector3.Dot(
                transform.position - targetCamera.transform.position,
                targetCamera.transform.forward);
            if (cameraDistance <= 0f)
            {
                cameraDistance = targetCamera.nearClipPlane;
            }

            ProjectCameraViewportPoint(targetCamera, axis, 0f, 0f, cameraDistance, ref min, ref max);
            ProjectCameraViewportPoint(targetCamera, axis, 0f, 1f, cameraDistance, ref min, ref max);
            ProjectCameraViewportPoint(targetCamera, axis, 1f, 0f, cameraDistance, ref min, ref max);
            ProjectCameraViewportPoint(targetCamera, axis, 1f, 1f, cameraDistance, ref min, ref max);

            // 안정된 카메라 위치와 실제 렌더 카메라 위치의 흔들림 차이는 반복 경계에 반영하지 않습니다.
            Vector3 stableOffset = cameraPosition - targetCamera.transform.position;
            float stableProjection = Vector3.Dot(stableOffset, axis);
            min += stableProjection;
            max += stableProjection;
            return min <= max;
        }

        /// <summary>
        /// 뷰포트 좌표를 월드 좌표로 변환한 뒤 지정한 축에 투영합니다.
        /// </summary>
        private static void ProjectCameraViewportPoint(
            Camera targetCamera,
            Vector3 axis,
            float viewportX,
            float viewportY,
            float cameraDistance,
            ref float min,
            ref float max)
        {
            Vector3 worldPoint = targetCamera.ViewportToWorldPoint(
                new Vector3(viewportX, viewportY, cameraDistance));
            float projection = Vector3.Dot(worldPoint, axis);
            min = Mathf.Min(min, projection);
            max = Mathf.Max(max, projection);
        }

        /// <summary>
        /// 현재 맵 캐릭터 위치를 세그먼트 유지 범위에 추가합니다.
        /// </summary>
        /// <param name="axis">캐릭터 위치를 투영할 월드 축입니다.</param>
        /// <param name="min">현재까지 계산된 최소 유지 경계입니다.</param>
        /// <param name="max">현재까지 계산된 최대 유지 경계입니다.</param>
        private void AppendActorResidencyProjection(Vector3 axis, ref float min, ref float max)
        {
            if (!useActorResidencyAnchors || _mapTileCommon == null)
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
        /// 카메라와 배경의 상대 이동량을 사용하여 화면 밖으로 빠져나가는 방향을 결정합니다.
        /// </summary>
        /// <param name="repeatWorldAxis">설정된 반복 축의 월드 방향입니다.</param>
        /// <param name="cameraFrameDelta">이전 프레임 대비 카메라 이동량입니다.</param>
        /// <param name="backgroundFrameDelta">이번 프레임의 합성 배경 이동량입니다.</param>
        /// <returns>세그먼트가 화면 밖으로 빠져나가는 것으로 판정할 방향입니다.</returns>
        private Vector3 ResolveRecycleAxis(
            Vector3 repeatWorldAxis,
            Vector3 cameraFrameDelta,
            Vector3 backgroundFrameDelta)
        {
            if (!useCameraRelativeRepeatDirection)
            {
                return ResolveDefaultRecycleAxis(repeatWorldAxis);
            }

            Vector3 relativeDelta = backgroundFrameDelta - cameraFrameDelta;
            float relativeAmount = Vector3.Dot(relativeDelta, repeatWorldAxis);
            if (relativeAmount < -cameraRelativeDirectionThreshold)
            {
                return -repeatWorldAxis;
            }

            if (relativeAmount > cameraRelativeDirectionThreshold)
            {
                return repeatWorldAxis;
            }

            return ResolveDefaultRecycleAxis(repeatWorldAxis);
        }

        /// <summary>
        /// 상대 이동량이 충분하지 않을 때 자동 스크롤 방향 또는 반복 축을 기본 재활용 방향으로 사용합니다.
        /// </summary>
        /// <param name="repeatWorldAxis">설정된 반복 축의 월드 방향입니다.</param>
        /// <returns>자동 이동 정책을 반영한 기본 재활용 방향입니다.</returns>
        private Vector3 ResolveDefaultRecycleAxis(Vector3 repeatWorldAxis)
        {
            if (useAutoScroll && TryResolveAutoScrollWorldDirection(out Vector3 scrollDirection))
            {
                return Vector3.Dot(scrollDirection, repeatWorldAxis) < 0f
                    ? -repeatWorldAxis
                    : repeatWorldAxis;
            }

            return repeatWorldAxis;
        }

        /// <summary>
        /// 설정된 로컬 반복 축을 정규화된 월드 방향으로 변환합니다.
        /// </summary>
        /// <returns>정규화된 반복 월드 방향입니다.</returns>
        private Vector3 ResolveRepeatWorldAxis()
        {
            Vector3 localAxis = repeatAxis == BackgroundRepeatAxis.Horizontal
                ? Vector3.right
                : Vector3.up;
            Vector3 worldAxis = transform.TransformDirection(localAxis);
            return worldAxis.sqrMagnitude > DirectionEpsilon
                ? worldAxis.normalized
                : localAxis;
        }

        /// <summary>
        /// 설정된 로컬 자동 이동 방향을 정규화된 월드 방향으로 변환합니다.
        /// </summary>
        /// <param name="worldDirection">정규화된 자동 이동 월드 방향입니다.</param>
        /// <returns>유효한 이동 방향을 계산했으면 <see langword="true"/>입니다.</returns>
        private bool TryResolveAutoScrollWorldDirection(out Vector3 worldDirection)
        {
            if (direction.sqrMagnitude <= DirectionEpsilon)
            {
                worldDirection = Vector3.zero;
                return false;
            }

            worldDirection = transform.TransformDirection(direction.normalized);
            if (worldDirection.sqrMagnitude <= DirectionEpsilon)
            {
                worldDirection = Vector3.zero;
                return false;
            }

            worldDirection.Normalize();
            return true;
        }

        /// <summary>
        /// 이전 프레임 대비 카메라 이동량을 계산하고 현재 위치를 다음 프레임 기준으로 저장합니다.
        /// </summary>
        /// <param name="cameraPosition">현재 프레임의 카메라 기준 위치입니다.</param>
        /// <returns>이전 프레임 대비 카메라 이동량입니다.</returns>
        private Vector3 ResolveCameraFrameDelta(Vector3 cameraPosition)
        {
            if (!_hasPreviousCameraPosition)
            {
                _previousCameraPosition = cameraPosition;
                _hasPreviousCameraPosition = true;
                return Vector3.zero;
            }

            Vector3 cameraDelta = cameraPosition - _previousCameraPosition;
            _previousCameraPosition = cameraPosition;
            return cameraDelta;
        }

        /// <summary>
        /// 반복 경계 계산에 사용할 실제 렌더 카메라를 조회합니다.
        /// </summary>
        /// <returns>SceneGame의 메인 카메라 또는 Camera.main이며, 찾지 못하면 null입니다.</returns>
        private static Camera ResolveRenderCamera()
        {
            if (SceneGame.Instance != null && SceneGame.Instance.mainCamera != null)
            {
                return SceneGame.Instance.mainCamera;
            }

            return Camera.main;
        }

        /// <summary>
        /// 이 레이어가 생성한 런타임 반복 세그먼트를 모두 제거하고 내부 캐시를 초기화합니다.
        /// </summary>
        private void DestroyRuntimeClones()
        {
            for (int i = _runtimeClones.Count - 1; i >= 0; i--)
            {
                if (_runtimeClones[i] != null)
                {
                    Destroy(_runtimeClones[i]);
                }
            }

            _runtimeClones.Clear();
            _segments.Clear();
            _recycleCandidates.Clear();
            _recycleCandidateEdges.Clear();
            _isInitialized = false;
            _hasBaseline = false;
        }
    }
}
