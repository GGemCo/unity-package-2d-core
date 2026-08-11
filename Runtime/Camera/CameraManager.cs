using System;
using UnityEngine;

namespace GGemCo2DCore
{
    /// <summary>
    /// 게임 메인 카메라의 대상 추적, 맵 경계, 연출 효과를 조율하는 매니저입니다.
    /// 실제 계산은 Follow, Bounds, Effects 컨트롤러에 위임합니다.
    /// </summary>
    public class CameraManager : MonoBehaviour
    {
        [Header("Map Bounds")]
        [Tooltip("왼쪽 경계를 제한할지 여부입니다.")]
        public bool useLimitLeft = true;
        [Tooltip("오른쪽 경계를 제한할지 여부입니다.")]
        public bool useLimitRight = true;
        [Tooltip("위쪽 경계를 제한할지 여부입니다.")]
        public bool useLimitTop = true;
        [Tooltip("아래쪽 경계를 제한할지 여부입니다.")]
        public bool useLimitBottom = true;

        [Header("Follow")]
        [Tooltip("대상 추적 기능을 사용할지 여부입니다.")]
        [SerializeField] private bool useTargetFollow = true;
        [Tooltip("맵 로딩 후 플레이어 위치에 카메라를 즉시 맞춘 뒤 추적을 재개할지 여부입니다.")]
        [SerializeField] private bool snapToFollowTargetOnMapLoadComplete = true;
        [Tooltip("대상을 따라가는 속도입니다.")]
        [SerializeField] private float cameraMoveSpeed = 10f;
        [Tooltip("점프/낙하 상태에서 대상의 Y 이동을 카메라에 반영하는 비율입니다.")]
        [Range(0f, 1f)]
        [SerializeField] private float jumpVerticalFollowInfluence = 1f;
        [Tooltip("대상 기준 카메라 기본 오프셋입니다. x가 양수이면 대상의 오른쪽을 더 보여줍니다.")]
        [SerializeField] private Vector2 followOffset = Vector2.zero;

        [Header("Follow Dead Zone")]
        [Tooltip("대상이 Dead Zone 안에 있을 때 카메라가 움직이지 않도록 합니다.")]
        [SerializeField] private bool useFollowDeadZone;
        [Tooltip("카메라 기준 X축 Dead Zone 반경입니다.")]
        [Min(0f)]
        [SerializeField] private float followDeadZoneX;
        [Tooltip("카메라 기준 Y축 Dead Zone 반경입니다.")]
        [Min(0f)]
        [SerializeField] private float followDeadZoneY;

        [Header("Bottom Follow Offset Policy")]
        [Tooltip("아래 경계 제한을 끈 맵에서 Follow Offset Y를 자동 보정할지 결정합니다.")]
        [SerializeField] private CameraBottomFollowOffsetPolicy bottomFollowOffsetPolicy = CameraBottomFollowOffsetPolicy.Manual;
        [Tooltip("자동 보정 시 카메라 하단과 맵 하단 사이에 둘 추가 여백입니다.")]
        [Min(0f)]
        [SerializeField] private float autoBottomEdgePadding;

        private readonly CameraFollowController _followController = new();
        private readonly CameraBoundsController _boundsController = new();
        private readonly CameraEffectController _effectController = new();

        private Camera _currentCamera;
        private Vector3 _basePosition;
        private Vector2 _defaultFollowOffset;
        private bool _defaultUseFollowDeadZone;
        private Vector2 _defaultFollowDeadZone;
        private CameraBottomFollowOffsetPolicy _defaultBottomFollowOffsetPolicy;
        private bool _defaultUseLimitLeft;
        private bool _defaultUseLimitRight;
        private bool _defaultUseLimitTop;
        private bool _defaultUseLimitBottom;
        private bool _pendingAutoBottomOffsetApply;
        private bool _isMapCameraProfileResolved;
        private bool _isFollowPausedByMapLoading;
        private Vector2 _runtimeOverridePreviousFollowOffset;
        private bool _hasRuntimeOverridePreviousFollowOffset;

        /// <summary>
        /// 현재 맵 카메라 프로필 적용이 완료되었을 때 발생합니다.
        /// </summary>
        public event Action<CameraManager> MapCameraProfileResolved;

        /// <summary>
        /// 현재 맵 카메라 프로필 적용이 완료되었는지 반환합니다.
        /// </summary>
        public bool IsMapCameraProfileResolved => _isMapCameraProfileResolved;

        /// <summary>
        /// 컷신 카메라 독점 제어가 활성화되어 있는지 반환합니다.
        /// </summary>
        public bool IsCutsceneCameraOverrideActive => _effectController.IsOverrideActive;

        /// <summary>
        /// 대상 추적 기능이 켜져 있는지 반환합니다.
        /// </summary>
        public bool IsTargetFollowEnabled => useTargetFollow;

        /// <summary>
        /// 맵 로딩에 의해 대상 추적이 일시 정지되어 있는지 반환합니다.
        /// </summary>
        public bool IsTargetFollowPausedByMapLoading => _isFollowPausedByMapLoading;

        /// <summary>
        /// 게임 기본 Orthographic Size를 반환합니다.
        /// </summary>
        public float OriginalOrthographicSize => _effectController.OriginalOrthographicSize;

        /// <summary>
        /// 대상 기준 카메라 Follow Offset입니다.
        /// </summary>
        public Vector2 FollowOffset
        {
            get => _followController.Offset;
            set
            {
                followOffset = value;
                _followController.Offset = value;
                RequestBottomOffsetApplyIfNeeded();
            }
        }

        /// <summary>
        /// 현재 컷신이 맵 기본 Follow Offset에 추가한 월드 좌표 보정값입니다.
        /// </summary>
        public Vector2 CutsceneFollowOffset => _followController.CutsceneOffset;

        /// <summary>
        /// 카메라 Follow Dead Zone 반경입니다.
        /// </summary>
        public Vector2 FollowDeadZone
        {
            get => _followController.DeadZone;
            set
            {
                followDeadZoneX = Mathf.Max(0f, value.x);
                followDeadZoneY = Mathf.Max(0f, value.y);
                useFollowDeadZone = followDeadZoneX > 0f || followDeadZoneY > 0f;
                _followController.DeadZone = new Vector2(followDeadZoneX, followDeadZoneY);
            }
        }

        private void Awake()
        {
            _currentCamera = GetComponent<Camera>();
            _basePosition = transform.position;
            CaptureDefaults();
            ConfigureControllers();
            _effectController.Initialize(_currentCamera, transform);
            _pendingAutoBottomOffsetApply = false;
            _isMapCameraProfileResolved = false;
            _isFollowPausedByMapLoading = false;
            _runtimeOverridePreviousFollowOffset = followOffset;
            _hasRuntimeOverridePreviousFollowOffset = false;
        }

        private void OnEnable()
        {
            MapManager.OnLoadTilemapCompleteMap += OnLoadTilemapCompleteMap;
            MapManager.OnLoadCompletePlayer += OnLoadCompletePlayer;
        }

        private void OnDisable()
        {
            MapManager.OnLoadTilemapCompleteMap -= OnLoadTilemapCompleteMap;
            MapManager.OnLoadCompletePlayer -= OnLoadCompletePlayer;
        }

        private void Update()
        {
            _effectController.TickZoom();

            if (_effectController.IsOverrideActive)
            {
                _basePosition = _effectController.OverridePosition;
                transform.position = _basePosition;
                return;
            }

            Vector3 nextBasePosition = _basePosition;
            if (CanEvaluateTargetFollow())
            {
                nextBasePosition = _followController.EvaluateBasePosition(_basePosition, Time.deltaTime);
                nextBasePosition = _boundsController.Clamp(nextBasePosition, _currentCamera);
            }

            _basePosition = nextBasePosition;
            _effectController.TickShake();
            transform.position = _basePosition + _effectController.ShakeOffset;
        }

        /// <summary>
        /// 인스펙터 기본 설정값을 저장합니다.
        /// 맵별 카메라 프로필을 해제할 때 이 값으로 복원합니다.
        /// </summary>
        private void CaptureDefaults()
        {
            _defaultFollowOffset = followOffset;
            _defaultUseFollowDeadZone = useFollowDeadZone;
            _defaultFollowDeadZone = new Vector2(followDeadZoneX, followDeadZoneY);
            _defaultBottomFollowOffsetPolicy = bottomFollowOffsetPolicy;
            _defaultUseLimitLeft = useLimitLeft;
            _defaultUseLimitRight = useLimitRight;
            _defaultUseLimitTop = useLimitTop;
            _defaultUseLimitBottom = useLimitBottom;
        }

        /// <summary>
        /// 현재 직렬화 필드 값을 하위 컨트롤러에 반영합니다.
        /// </summary>
        private void ConfigureControllers()
        {
            _followController.Configure(
                cameraMoveSpeed,
                followOffset,
                useFollowDeadZone,
                new Vector2(followDeadZoneX, followDeadZoneY),
                jumpVerticalFollowInfluence);

            _boundsController.ConfigureLimits(useLimitLeft, useLimitRight, useLimitTop, useLimitBottom);
        }

        /// <summary>
        /// 현재 프레임에 대상 추적 계산을 실행할 수 있는지 확인합니다.
        /// </summary>
        /// <returns>대상 추적을 실행할 수 있으면 true를 반환합니다.</returns>
        private bool CanEvaluateTargetFollow()
        {
            return useTargetFollow
                   && !_isFollowPausedByMapLoading
                   && _followController.HasFollowTarget
                   && _boundsController.HasMapSize;
        }

        /// <summary>
        /// 맵 경계 크기를 변경합니다.
        /// </summary>
        /// <param name="pWidth">맵 월드 폭입니다.</param>
        /// <param name="pHeight">맵 월드 높이입니다.</param>
        public void ChangeMapSize(float pWidth, float pHeight)
        {
            _boundsController.ChangeMapSize(pWidth, pHeight);
            RequestBottomOffsetApplyIfNeeded();
        }

        /// <summary>
        /// 현재 맵 테이블의 카메라 오버라이드 값을 적용합니다.
        /// 지정되지 않은 항목은 인스펙터 기본값으로 복원합니다.
        /// </summary>
        /// <param name="mapData">현재 로드 중인 맵 테이블 데이터입니다.</param>
        public void ApplyMapCameraOverrides(StruckTableMap mapData)
        {
            MarkMapCameraProfileUnresolved();

            Vector2 resolvedFollowOffset = _defaultFollowOffset;
            bool resolvedUseFollowDeadZone = _defaultUseFollowDeadZone;
            Vector2 resolvedFollowDeadZone = _defaultFollowDeadZone;
            CameraBottomFollowOffsetPolicy resolvedBottomPolicy = _defaultBottomFollowOffsetPolicy;
            bool resolvedUseLimitLeft = _defaultUseLimitLeft;
            bool resolvedUseLimitRight = _defaultUseLimitRight;
            bool resolvedUseLimitTop = _defaultUseLimitTop;
            bool resolvedUseLimitBottom = _defaultUseLimitBottom;

            if (mapData != null)
            {
                if (mapData.UseCameraFollowOffset)
                {
                    resolvedFollowOffset = mapData.CameraFollowOffset;
                }

                if (mapData.UseCameraFollowDeadZone)
                {
                    resolvedFollowDeadZone = mapData.CameraFollowDeadZone;
                    resolvedUseFollowDeadZone = resolvedFollowDeadZone.sqrMagnitude > 0f;
                }

                if (mapData.UseCameraBottomFollowOffsetPolicy)
                {
                    resolvedBottomPolicy = mapData.BottomFollowOffsetPolicy;
                }

                if (mapData.UseParallax)
                {
                    // Parallax 맵은 배경 이동 여백을 확보해야 하므로 카메라 경계 제한을 해제합니다.
                    resolvedUseLimitLeft = false;
                    resolvedUseLimitRight = false;
                    resolvedUseLimitTop = false;
                    resolvedUseLimitBottom = false;
                }
            }

            followOffset = resolvedFollowOffset;
            useFollowDeadZone = resolvedUseFollowDeadZone;
            followDeadZoneX = Mathf.Max(0f, resolvedFollowDeadZone.x);
            followDeadZoneY = Mathf.Max(0f, resolvedFollowDeadZone.y);
            bottomFollowOffsetPolicy = resolvedBottomPolicy;
            useLimitLeft = resolvedUseLimitLeft;
            useLimitRight = resolvedUseLimitRight;
            useLimitTop = resolvedUseLimitTop;
            useLimitBottom = resolvedUseLimitBottom;
            ConfigureControllers();
            _pendingAutoBottomOffsetApply = false;
            RequestBottomOffsetApplyIfNeeded();
        }

        /// <summary>
        /// 대상 추적 기능의 사용 여부를 변경합니다.
        /// 기능을 끄면 현재 FollowTarget은 유지하지만 추적 계산은 멈춥니다.
        /// </summary>
        /// <param name="enabled">대상 추적 기능 사용 여부입니다.</param>
        public void SetTargetFollowEnabled(bool enabled)
        {
            useTargetFollow = enabled;
            if (!useTargetFollow)
            {
                _isFollowPausedByMapLoading = false;
            }
        }

        /// <summary>
        /// 맵 로딩 시작 시 호출되어 카메라 연출 효과를 중단하고 대상 추적을 일시 정지합니다.
        /// </summary>
        public void HandleMapLoadStarted()
        {
            StopAllCameraEffectsForMapLoading();
            PauseTargetFollowForMapLoading();
        }

        /// <summary>
        /// 맵 로딩 중 남아 있으면 안 되는 카메라 효과를 모두 정리합니다.
        /// </summary>
        public void StopAllCameraEffectsForMapLoading()
        {
            _effectController.StopAllShakes();
            _effectController.StopZoom();
            _effectController.ClearOverride();
            ClearCutsceneFollowOffset();
            _pendingAutoBottomOffsetApply = false;
            transform.position = _basePosition;
        }

        /// <summary>
        /// 대상 추적 기능이 켜져 있으면 맵 로딩 동안 추적 계산을 일시 정지합니다.
        /// </summary>
        public void PauseTargetFollowForMapLoading()
        {
            _isFollowPausedByMapLoading = useTargetFollow;
        }

        /// <summary>
        /// 플레이어가 시작 위치로 이동된 뒤 대상 추적을 재개합니다.
        /// </summary>
        public void HandlePlayerLoadedForMap()
        {
            ResumeTargetFollowAfterMapLoading();
        }

        /// <summary>
        /// 맵 로딩으로 일시 정지한 대상 추적을 플레이어 기준으로 재개합니다.
        /// </summary>
        public void ResumeTargetFollowAfterMapLoading()
        {
            if (!useTargetFollow)
            {
                _isFollowPausedByMapLoading = false;
                return;
            }

            SetFollowPlayer();
            _isFollowPausedByMapLoading = false;

            if (snapToFollowTargetOnMapLoadComplete)
            {
                SnapToFollowTarget();
            }
        }

        /// <summary>
        /// 현재 추적 대상과 Follow Offset을 기준으로 카메라 기본 위치를 즉시 맞춥니다.
        /// </summary>
        private void SnapToFollowTarget()
        {
            if (_followController.FollowTarget == null)
            {
                return;
            }

            Vector3 targetPosition = _followController.FollowTarget.position
                                     + new Vector3(_followController.Offset.x, _followController.Offset.y, 0f);
            targetPosition.z = transform.position.z;
            _basePosition = _boundsController.Clamp(targetPosition, _currentCamera);
            transform.position = _basePosition;
        }

        /// <summary>
        /// 컷신 카메라 독점 제어를 시작하고 일반 추적/경계/효과 계산을 차단합니다.
        /// </summary>
        /// <param name="owner">독점 제어를 요청한 소유자입니다.</param>
        /// <param name="position">시작 위치입니다.</param>
        /// <returns>독점 제어가 시작되면 true를 반환합니다.</returns>
        public bool BeginCutsceneCameraOverride(object owner, Vector2 position)
        {
            bool started = _effectController.BeginOverride(owner, position);
            if (started)
            {
                _pendingAutoBottomOffsetApply = false;
                _basePosition = _effectController.OverridePosition;
                transform.position = _basePosition;
            }

            return started;
        }

        /// <summary>
        /// 컷신 카메라 독점 제어 중 사용할 위치를 갱신합니다.
        /// </summary>
        public bool SetCutsceneCameraOverridePosition(object owner, Vector2 position)
        {
            bool updated = _effectController.SetOverridePosition(owner, position);
            if (updated)
            {
                _basePosition = _effectController.OverridePosition;
                transform.position = _basePosition;
            }

            return updated;
        }

        /// <summary>
        /// 컷신 카메라 독점 제어를 종료합니다.
        /// </summary>
        public bool EndCutsceneCameraOverride(object owner)
        {
            bool ended = _effectController.EndOverride(owner);
            if (ended)
            {
                _basePosition = transform.position;
            }

            return ended;
        }

        /// <summary>
        /// 카메라를 지정한 월드 좌표로 즉시 이동합니다.
        /// </summary>
        public void MoveCameraPosition(float x, float y)
        {
            _basePosition = new Vector3(x + _followController.Offset.x, y + _followController.Offset.y, -10f);
            transform.position = _basePosition;
        }

        /// <summary>
        /// Follow Offset 값을 런타임에서 임시 변경합니다.
        /// </summary>
        public void ChangeCameraPositionValue(float x, float y)
        {
            _runtimeOverridePreviousFollowOffset = _followController.Offset;
            _hasRuntimeOverridePreviousFollowOffset = true;
            _followController.Offset = new Vector2(x, y);
        }

        /// <summary>
        /// 임시 변경 전 Follow Offset 값으로 되돌립니다.
        /// </summary>
        public void ResetCameraPositionValue()
        {
            if (!_hasRuntimeOverridePreviousFollowOffset)
            {
                return;
            }

            _followController.Offset = _runtimeOverridePreviousFollowOffset;
            _hasRuntimeOverridePreviousFollowOffset = false;
        }

        /// <summary>
        /// 카메라 추적 대상을 제거합니다.
        /// </summary>
        public void RemoveFollowTarget()
        {
            _followController.RemoveTarget();
        }

        /// <summary>
        /// 카메라 추적 대상을 변경합니다.
        /// 추적 기능이 꺼져 있어도 대상 참조는 저장해 두며, 실제 추적 계산만 중단됩니다.
        /// </summary>
        /// <param name="target">추적할 대상입니다. null이면 플레이어를 기본 대상으로 시도합니다.</param>
        public void SetFollowTarget(Transform target)
        {
            Transform resolvedTarget = target;
            if (resolvedTarget == null && SceneGame.Instance != null && SceneGame.Instance.player != null)
            {
                resolvedTarget = SceneGame.Instance.player.transform;
            }

            _followController.SetTarget(resolvedTarget);
            RequestBottomOffsetApplyIfNeeded();
        }

        /// <summary>
        /// 맵 기본 Follow Offset을 유지한 채 컷신 전용 추가 Offset을 적용합니다.
        /// 연속된 Camera Change Target 이벤트에서는 이전 값을 누적하지 않고 새 값으로 교체합니다.
        /// </summary>
        /// <param name="offset">추적 대상 위치에 추가할 월드 좌표 보정값입니다.</param>
        public void SetCutsceneFollowOffset(Vector2 offset)
        {
            if (!IsFinite(offset.x) || !IsFinite(offset.y))
            {
                GcLogger.LogWarning($"[{nameof(CameraManager)}] 유효하지 않은 컷신 카메라 Offset을 0으로 보정합니다. offset: {offset}");
                offset = Vector2.zero;
            }

            _followController.SetCutsceneOffset(offset);
        }

        /// <summary>
        /// 컷신 전용 추가 Offset을 제거하고 맵 기본 Follow Offset만 사용하도록 복원합니다.
        /// </summary>
        public void ClearCutsceneFollowOffset()
        {
            _followController.ClearCutsceneOffset();
        }

        /// <summary>
        /// 카메라 좌표 계산에 사용할 부동소수점 값이 유효한지 확인합니다.
        /// </summary>
        /// <param name="value">검사할 값입니다.</param>
        /// <returns>NaN 또는 무한대가 아니면 <see langword="true"/>입니다.</returns>
        private static bool IsFinite(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }

        /// <summary>
        /// 지정한 대상이 현재 게임 카메라의 정상 추적 대상인지 확인합니다.
        /// </summary>
        public bool CanGameplayFollowTarget(Transform target)
        {
            if (!isActiveAndEnabled || target == null || !target.gameObject.activeInHierarchy)
            {
                return false;
            }

            if (!useTargetFollow || _isFollowPausedByMapLoading)
            {
                return false;
            }

            if (_effectController.IsOverrideActive || !_isMapCameraProfileResolved)
            {
                return false;
            }

            if (!_followController.IsFollowing(target))
            {
                return false;
            }

            if (_currentCamera == null || !_currentCamera.isActiveAndEnabled || !_currentCamera.orthographic)
            {
                return false;
            }

            if (!_boundsController.HasMapSize)
            {
                return false;
            }

            return cameraMoveSpeed > 0.0001f;
        }

        /// <summary>
        /// 흔들림이 적용되기 전 기본 위치를 기준으로 Orthographic 화면의 월드 Rect를 계산합니다.
        /// </summary>
        public bool TryGetBaseViewportWorldRect(out Rect worldRect)
        {
            return _boundsController.TryGetViewportWorldRect(_currentCamera, _basePosition, out worldRect);
        }

        private void OnLoadTilemapCompleteMap(MapTileCommon mapTileCommon, GameObject grid)
        {
            _ = mapTileCommon;
            _ = grid;
            RequestBottomOffsetApplyIfNeeded();
        }

        private void OnLoadCompletePlayer(MapTileCommon mapTileCommon, GameObject grid)
        {
            _ = mapTileCommon;
            _ = grid;
            HandlePlayerLoadedForMap();
        }

        /// <summary>
        /// 자동 하단 정렬 정책이 적용 가능한지 확인하고 필요하면 보정을 예약합니다.
        /// </summary>
        private void RequestBottomOffsetApplyIfNeeded()
        {
            if (bottomFollowOffsetPolicy != CameraBottomFollowOffsetPolicy.AutoAlignToMapBottomOnMapLoad)
            {
                _pendingAutoBottomOffsetApply = false;
                MarkMapCameraProfileResolved();
                return;
            }

            if (useLimitBottom)
            {
                _pendingAutoBottomOffsetApply = false;
                MarkMapCameraProfileResolved();
                return;
            }

            _pendingAutoBottomOffsetApply = true;
            TryApplyBottomOffsetIfPending();
        }

        /// <summary>
        /// 예약된 하단 자동 정렬 보정을 실제 Follow Offset에 적용합니다.
        /// </summary>
        private void TryApplyBottomOffsetIfPending()
        {
            if (!_pendingAutoBottomOffsetApply)
            {
                return;
            }

            if (_followController.FollowTarget == null || _currentCamera == null || !_boundsController.HasMapSize)
            {
                return;
            }

            MapManager mapManager = SceneGame.Instance != null ? SceneGame.Instance.mapManager : null;
            if (mapManager == null || !mapManager.TryGetCurrentMapBottomY(out float mapBottomY))
            {
                return;
            }

            float desiredCameraCenterY = mapBottomY + _currentCamera.orthographicSize + autoBottomEdgePadding;
            float newFollowOffsetY = desiredCameraCenterY - _followController.FollowTarget.position.y;
            followOffset.y = newFollowOffsetY;
            _followController.SetOffsetY(newFollowOffsetY);
            _pendingAutoBottomOffsetApply = false;
            MarkMapCameraProfileResolved();
        }

        /// <summary>
        /// 플레이어를 추적 대상으로 설정합니다.
        /// </summary>
        public void SetFollowPlayer()
        {
            if (SceneGame.Instance == null || SceneGame.Instance.player == null)
            {
                return;
            }

            SetFollowTarget(SceneGame.Instance.player.transform);
        }

        /// <summary>
        /// Orthographic Size 보간을 시작합니다.
        /// </summary>
        public void StartZoom(
            float endSize,
            float duration = 1f,
            Easing.EaseType easeType = Easing.EaseType.EaseOutQuad,
            bool useUnscaledTime = false,
            bool changeOriginalSize = false)
        {
            _effectController.StartZoom(endSize, duration, easeType, useUnscaledTime, changeOriginalSize);
        }

        /// <summary>
        /// 컷신 종료 시 카메라를 일반 게임플레이 상태로 복구합니다.
        /// </summary>
        public void ReSetByCutscene()
        {
            _effectController.ClearOverride();
            ClearCutsceneFollowOffset();
            SetFollowPlayer();
            _effectController.StopShake(CameraShakeChannel.Cutscene);
            _effectController.ResetZoom();
            _basePosition = transform.position;
        }

        /// <summary>
        /// 진행 중인 줌을 중단합니다.
        /// </summary>
        public void StopZoom(bool snapToTarget = false)
        {
            _effectController.StopZoom(snapToTarget);
        }

        /// <summary>
        /// Orthographic Size를 즉시 적용합니다.
        /// </summary>
        public void SetOrthographicSizeImmediate(float size, bool changeOriginalSize = false)
        {
            _effectController.SetOrthographicSizeImmediate(size, changeOriginalSize);
        }

        /// <summary>
        /// 카메라 흔들림을 시작합니다.
        /// </summary>
        public void StartShake(float shakeDuration, float shakeMagnitude)
        {
            _effectController.StartShake(shakeDuration, shakeMagnitude);
        }

        /// <summary>
        /// 방향별 세기를 지정한 카메라 흔들림을 시작합니다.
        /// </summary>
        public void StartShake(
            float duration,
            float leftStrength,
            float rightStrength,
            float downStrength,
            float upStrength,
            int repeatCount,
            CameraShakeChannel channel = CameraShakeChannel.Default,
            bool useUnscaledTime = false)
        {
            _effectController.StartShake(
                duration,
                leftStrength,
                rightStrength,
                downStrength,
                upStrength,
                repeatCount,
                channel,
                useUnscaledTime);
        }

        /// <summary>
        /// 카메라 흔들림 요청을 재생합니다.
        /// </summary>
        public void PlayShake(CameraShakeRequest request)
        {
            _effectController.PlayShake(request);
        }

        /// <summary>
        /// 프리셋 기반 카메라 흔들림을 재생합니다.
        /// </summary>
        public void PlayShake(CameraShakePreset preset, CameraShakeChannel channel = CameraShakeChannel.Default)
        {
            _effectController.PlayShake(preset, channel);
        }

        /// <summary>
        /// 지정 채널의 카메라 흔들림을 중단합니다.
        /// </summary>
        public void StopShake(CameraShakeChannel channel)
        {
            _effectController.StopShake(channel);
        }

        /// <summary>
        /// 모든 카메라 흔들림을 중단합니다.
        /// </summary>
        public void StopAllShakes()
        {
            _effectController.StopAllShakes();
        }

        /// <summary>
        /// 카메라 추적 속도를 변경합니다.
        /// </summary>
        public void SetCameraMoveSpeed(float speed)
        {
            cameraMoveSpeed = Mathf.Max(0f, speed);
            _followController.MoveSpeed = cameraMoveSpeed;
        }

        /// <summary>
        /// 현재 카메라 위치를 반환합니다.
        /// </summary>
        public Vector2 GetPositionCenter()
        {
            return transform.position;
        }

        /// <summary>
        /// 흔들림이 적용되기 전 카메라 기본 월드 위치를 반환합니다.
        /// </summary>
        public Vector3 GetBaseWorldPosition()
        {
            return _basePosition;
        }

        /// <summary>
        /// 현재 적용 중인 흔들림 오프셋을 반환합니다.
        /// </summary>
        public Vector3 GetShakeOffset()
        {
            return _effectController.ShakeOffset;
        }

        /// <summary>
        /// 게임 기본 Orthographic Size를 변경합니다.
        /// </summary>
        public void ChangeOriginalOrthographicSize(float size)
        {
            _effectController.ChangeOriginalOrthographicSize(size);
        }

        private void MarkMapCameraProfileUnresolved()
        {
            _isMapCameraProfileResolved = false;
        }

        private void MarkMapCameraProfileResolved()
        {
            if (_isMapCameraProfileResolved)
            {
                return;
            }

            _isMapCameraProfileResolved = true;
            MapCameraProfileResolved?.Invoke(this);
        }
    }
}
