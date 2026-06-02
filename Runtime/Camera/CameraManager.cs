using System;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

namespace GGemCo2DCore
{
    public enum CameraShakeChannel
    {
        Default = 0,
        AnimationEvent = 1,
        Cutscene = 2,
        SkillDamage = 3,
    }

    /// <summary>
    /// 카메라 Shake 파형의 시작 위상을 결정하는 정책입니다.
    /// </summary>
    public enum CameraShakePhaseMode
    {
        /// <summary>
        /// 매 재생마다 임의 위상에서 시작합니다.
        /// </summary>
        Random = 0,

        /// <summary>
        /// 프리셋 또는 요청에 지정된 고정 위상에서 시작합니다.
        /// </summary>
        Fixed = 1,
    }

    /// <summary>
    /// 맵 로드 시점에 카메라의 Y 오프셋을 자동 보정할지 결정하는 정책입니다.
    /// </summary>
    public enum CameraBottomFollowOffsetPolicy
    {
        /// <summary>
        /// 인스펙터 또는 코드에서 설정한 값을 그대로 사용합니다.
        /// </summary>
        Manual = 0,

        /// <summary>
        /// 맵 하단 경계에 카메라 하단이 맞도록 followOffset.y를 자동 계산합니다.
        /// </summary>
        AutoAlignToMapBottomOnMapLoad = 1,
    }

    public struct CameraShakeRequest
    {
        public float Duration;
        public float LeftStrength;
        public float RightStrength;
        public float DownStrength;
        public float UpStrength;
        public int RepeatCount;
        public CameraShakeChannel Channel;
        public bool UseUnscaledTime;
        public CameraShakePhaseMode PhaseMode;
        public float FixedPhaseRadians;

        public bool IsValid
        {
            get
            {
                if (Duration <= 0f)
                {
                    return false;
                }

                if (RepeatCount <= 0)
                {
                    return false;
                }

                return LeftStrength > 0f || RightStrength > 0f || DownStrength > 0f || UpStrength > 0f;
            }
        }

        public static CameraShakeRequest CreateSymmetric(
            float duration,
            float magnitude,
            int repeatCount,
            CameraShakeChannel channel,
            bool useUnscaledTime = false)
        {
            return new CameraShakeRequest
            {
                Duration = duration,
                LeftStrength = magnitude,
                RightStrength = magnitude,
                DownStrength = magnitude,
                UpStrength = magnitude,
                RepeatCount = Mathf.Max(1, repeatCount),
                Channel = channel,
                UseUnscaledTime = useUnscaledTime,
                PhaseMode = CameraShakePhaseMode.Random,
                FixedPhaseRadians = 0f,
            };
        }
    }

    public class CameraManager : MonoBehaviour
    {
        private sealed class ActiveCameraShake
        {
            public CameraShakeRequest Request;
            public float Elapsed;
            public float PhaseOffset;
        }

        [Tooltip("왼쪽 경계 제한 여부")]
        public bool useLimitLeft = true;
        [Tooltip("오른쪽 경계 제한 여부")]
        public bool useLimitRight = true;
        [Tooltip("위쪽 경계 제한 여부")]
        public bool useLimitTop = true;
        [Tooltip("아래쪽 경계 제한 여부")]
        public bool useLimitBottom = true;
        [Tooltip("타겟을 따라다니는 속도")]
        [SerializeField] private float cameraMoveSpeed;

        [Header("Vertical Follow")]
        [Tooltip("점프 상태일 때 카메라가 타겟의 Y 이동량을 얼마나 따라갈지 결정합니다. 1이면 기존과 동일하고, 0.5면 절반만 따라갑니다.")]
        [Range(0f, 1f)]
        [SerializeField] private float jumpVerticalFollowInfluence = 1f;

        [Header("Camera Offset")]
        [Tooltip("타겟(플레이어) 기준 카메라 기본 오프셋(월드 단위). 예) x>0이면 캐릭터 오른쪽을 더 보여줍니다.")]
        [SerializeField]
        private Vector2 followOffset = Vector2.zero;

        [Header("Follow Dead Zone")]
        [Tooltip("타겟이 데드존 안에 있을 때 카메라가 움직이지 않도록 할지 여부입니다.")]
        [SerializeField]
        private bool useFollowDeadZone = false;

        [Tooltip("카메라 기준 위치에서 X축으로 허용할 데드존 반경입니다. 0이면 X축 데드존을 사용하지 않습니다.")]
        [Min(0f)]
        [SerializeField]
        private float followDeadZoneX = 0f;

        [Tooltip("카메라 기준 위치에서 Y축으로 허용할 데드존 반경입니다. 0이면 Y축 데드존을 사용하지 않습니다.")]
        [Min(0f)]
        [SerializeField]
        private float followDeadZoneY = 0f;

        [Header("Bottom Follow Offset Policy")]
        [Tooltip("아래 경계 제한(useLimitBottom)이 꺼진 경우, 맵 로드 시 followOffset.y 자동 보정 정책을 적용합니다.")]
        [SerializeField]
        private CameraBottomFollowOffsetPolicy bottomFollowOffsetPolicy = CameraBottomFollowOffsetPolicy.Manual;

        [Tooltip("자동 보정 시 맵 하단 경계와 카메라 하단 사이의 추가 여백(월드 단위)입니다.")]
        [Min(0f)]
        [SerializeField]
        private float autoBottomEdgePadding = 0f;

        private readonly List<ActiveCameraShake> _activeShakes = new();

        private float _originalOrthographicSize;
        private Vector3 _originCameraPosition;
        private Camera _currentCamera;

        private Vector3 _cameraPosition; // (legacy) runtime override
        private Vector3 _basePosition;
        private Vector3 _shakeOffset;
        private Vector2 _center;
        private Vector2 _mapSize;
        private Vector2 _monsterSpawnPositionBoxSize;
        private Transform _followTarget;
        private ICameraVerticalFollowStateSource _verticalFollowStateSource;
        private bool _hasVerticalFollowAnchor;
        private float _verticalFollowAnchorTargetY;
        private bool _pendingAutoBottomOffsetApply;
        private Vector2 _defaultFollowOffset;
        private bool _defaultUseFollowDeadZone;
        private Vector2 _defaultFollowDeadZone;
        private CameraBottomFollowOffsetPolicy _defaultBottomFollowOffsetPolicy;

        private float _width;
        private float _height;

        // 줌 관련 처리
        private bool _isZooming;
        private float _zoomTimer;
        private float _zoomDuration;
        private float _zoomStartSize;
        private float _zoomEndSize;
        private Easing.EaseType _zoomEasing;
        private bool _zoomUseUnscaledTime;
        private bool _isMapCameraProfileResolved;

        /// <summary>
        /// 현재 맵 카메라 프로필(점프 추적 영향도/Follow Offset/하단 오프셋 정책)의
        /// 런타임 적용이 완료되었을 때 발생합니다.
        /// </summary>
        public event Action<CameraManager> MapCameraProfileResolved;

        /// <summary>
        /// 현재 맵 카메라 프로필의 런타임 적용 완료 여부를 반환합니다.
        /// </summary>
        public bool IsMapCameraProfileResolved => _isMapCameraProfileResolved;

        /// <summary>
        /// 게임 기본 카메라 orthographicSize(원본 값)를 반환합니다.
        /// </summary>
        public float OriginalOrthographicSize => _originalOrthographicSize;

        private void Awake()
        {
            _isZooming = false;
            _zoomTimer = 0;
            _zoomDuration = 0;
            _zoomStartSize = 0;
            _zoomEndSize = 0;
            _zoomEasing = Easing.EaseType.Linear;
            _zoomUseUnscaledTime = false;
            _defaultFollowOffset = followOffset;
            _defaultUseFollowDeadZone = useFollowDeadZone;
            _defaultFollowDeadZone = new Vector2(followDeadZoneX, followDeadZoneY);
            _defaultBottomFollowOffsetPolicy = bottomFollowOffsetPolicy;
            _originCameraPosition = Vector3.zero;
            _cameraPosition = new Vector3(followOffset.x, followOffset.y, 0f);
            _basePosition = transform.position;
            _shakeOffset = Vector3.zero;

            _currentCamera = GetComponent<Camera>();
            _originalOrthographicSize = _currentCamera.orthographicSize;
            _height = _originalOrthographicSize;
            _width = _height * Screen.width / Screen.height;
            _pendingAutoBottomOffsetApply = false;
            _isMapCameraProfileResolved = false;
        }

        /// <summary>
        /// 맵 로드 완료 이벤트를 구독합니다.
        /// </summary>
        private void OnEnable()
        {
            MapManager.OnLoadTilemapCompleteMap += OnLoadTilemapCompleteMap;
        }

        /// <summary>
        /// 맵 로드 완료 이벤트 구독을 해제합니다.
        /// </summary>
        private void OnDisable()
        {
            MapManager.OnLoadTilemapCompleteMap -= OnLoadTilemapCompleteMap;
        }

        private void Update()
        {
            LimitCameraArea();
            UpdateZoom();
        }

        /// <summary>
        /// 카메라의 타겟 추적 위치를 계산하고 데드존, 맵 경계 제한, 흔들림 오프셋을 순서대로 적용합니다.
        /// </summary>
        private void LimitCameraArea()
        {
            UpdateShakeOffset();

            if (_followTarget == null || _mapSize.x == 0f)
            {
                transform.position = _basePosition + _shakeOffset;
                return;
            }

            // 플레이어를 따라가는 카메라 위치 계산
            Vector3 targetPos = _followTarget.position + _cameraPosition;
            targetPos.y = EvaluateVerticalFollowTargetY(targetPos.y);
            targetPos = ApplyFollowDeadZone(_basePosition, targetPos);
            targetPos = Vector3.Lerp(_basePosition, targetPos, Time.deltaTime * cameraMoveSpeed);

            float clampX = targetPos.x;
            float clampY = targetPos.y;

            // 카메라 half extents (Orthographic)
            float halfH = _currentCamera.orthographicSize;
            float halfW = halfH * _currentCamera.aspect;

            // --- 좌우 제한 ---
            if (useLimitLeft || useLimitRight)
            {
                float minX = halfW;
                float maxX = _mapSize.x - halfW;

                // 맵이 화면보다 작아 clamp 구간이 뒤집히는 경우 -> 중앙 고정
                if (maxX < minX)
                {
                    clampX = _mapSize.x * 0.5f;
                }
                else
                {
                    if (useLimitLeft) clampX = Mathf.Max(clampX, minX);
                    if (useLimitRight) clampX = Mathf.Min(clampX, maxX);
                }
            }

            // --- 상하 제한 ---
            if (useLimitBottom || useLimitTop)
            {
                float minY = halfH;
                float maxY = _mapSize.y - halfH;

                if (maxY < minY)
                {
                    clampY = _mapSize.y * 0.5f;
                }
                else
                {
                    if (useLimitBottom) clampY = Mathf.Max(clampY, minY);
                    if (useLimitTop) clampY = Mathf.Min(clampY, maxY);
                }
            }

            // 최종 위치 적용 (Shake는 "기본 위치"를 기준으로 오프셋으로만 적용)
            _basePosition = new Vector3(clampX, clampY, -10f);
            transform.position = _basePosition + _shakeOffset;
        }

        /// <summary>
        /// 현재 타겟의 상태에 따라 세로 추적 목표 Y를 계산합니다.
        /// 점프 상태가 아닐 때는 원본 목표 Y를 그대로 사용합니다.
        /// </summary>
        /// <param name="targetY">타겟과 카메라 오프셋을 더한 원본 목표 Y입니다.</param>
        /// <returns>점프 세로 추적 영향도가 반영된 목표 Y입니다.</returns>
        private float EvaluateVerticalFollowTargetY(float targetY)
        {
            if (_verticalFollowStateSource == null || !_verticalFollowStateSource.IsVerticalFollowInfluenceActive)
            {
                _hasVerticalFollowAnchor = false;
                return targetY;
            }

            if (Mathf.Approximately(jumpVerticalFollowInfluence, 1f))
            {
                if (!_hasVerticalFollowAnchor)
                {
                    _verticalFollowAnchorTargetY = targetY;
                    _hasVerticalFollowAnchor = true;
                }

                return targetY;
            }

            if (!_hasVerticalFollowAnchor)
            {
                _verticalFollowAnchorTargetY = targetY;
                _hasVerticalFollowAnchor = true;
            }

            float deltaY = targetY - _verticalFollowAnchorTargetY;
            return _verticalFollowAnchorTargetY + (deltaY * jumpVerticalFollowInfluence);
        }

        /// <summary>
        /// 타겟 목표 위치가 카메라 데드존 안에 있으면 현재 기준 위치를 유지하고,
        /// 데드존을 벗어난 축만 초과분만큼 보정합니다.
        /// </summary>
        /// <param name="currentBasePosition">흔들림이 적용되기 전의 현재 카메라 기준 위치입니다.</param>
        /// <param name="targetPosition">타겟 위치, Follow Offset, 세로 추적 정책이 반영된 목표 위치입니다.</param>
        /// <returns>데드존 정책이 반영된 카메라 목표 위치입니다.</returns>
        private Vector3 ApplyFollowDeadZone(Vector3 currentBasePosition, Vector3 targetPosition)
        {
            if (!useFollowDeadZone)
            {
                return targetPosition;
            }

            float deadZoneX = Mathf.Max(0f, followDeadZoneX);
            float deadZoneY = Mathf.Max(0f, followDeadZoneY);

            if (deadZoneX <= 0f && deadZoneY <= 0f)
            {
                return targetPosition;
            }

            Vector3 resolvedPosition = currentBasePosition;

            if (deadZoneX <= 0f)
            {
                resolvedPosition.x = targetPosition.x;
            }
            else
            {
                float deltaX = targetPosition.x - currentBasePosition.x;
                if (deltaX > deadZoneX)
                {
                    resolvedPosition.x = targetPosition.x - deadZoneX;
                }
                else if (deltaX < -deadZoneX)
                {
                    resolvedPosition.x = targetPosition.x + deadZoneX;
                }
            }

            if (deadZoneY <= 0f)
            {
                resolvedPosition.y = targetPosition.y;
            }
            else
            {
                float deltaY = targetPosition.y - currentBasePosition.y;
                if (deltaY > deadZoneY)
                {
                    resolvedPosition.y = targetPosition.y - deadZoneY;
                }
                else if (deltaY < -deadZoneY)
                {
                    resolvedPosition.y = targetPosition.y + deadZoneY;
                }
            }

            resolvedPosition.z = targetPosition.z;
            return resolvedPosition;
        }

        /// <summary>
        /// 현재 따라가는 타겟에서 세로 추적 상태 제공자를 다시 찾고,
        /// 점프 세로 추적 기준점을 초기화합니다.
        /// </summary>
        private void RefreshVerticalFollowStateSource()
        {
            _verticalFollowStateSource = null;
            _hasVerticalFollowAnchor = false;
            _verticalFollowAnchorTargetY = 0f;

            if (_followTarget == null)
            {
                return;
            }

            MonoBehaviour[] behaviours = _followTarget.GetComponents<MonoBehaviour>();
            for (int i = 0; i < behaviours.Length; i++)
            {
                if (behaviours[i] is ICameraVerticalFollowStateSource stateSource)
                {
                    _verticalFollowStateSource = stateSource;
                    return;
                }
            }
        }

        private void UpdateZoom()
        {
            if (!_isZooming)
            {
                return;
            }

            float deltaTime = _zoomUseUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
            _zoomTimer += deltaTime;

            float duration = Mathf.Max(_zoomDuration, 0.0001f);
            float t = Mathf.Clamp01(_zoomTimer / duration);
            float easedT = Easing.Apply(t, _zoomEasing);
            float zoom = Mathf.Lerp(_zoomStartSize, _zoomEndSize, easedT);
            _currentCamera.orthographicSize = zoom;

            _height = zoom;
            _width = _height * Screen.width / Screen.height;
            if (t >= 1f)
            {
                _isZooming = false;
            }
        }

        private void UpdateShakeOffset()
        {
            if (_activeShakes.Count == 0)
            {
                _shakeOffset = Vector3.zero;
                return;
            }

            Vector3 totalOffset = Vector3.zero;

            for (int i = _activeShakes.Count - 1; i >= 0; i--)
            {
                ActiveCameraShake shake = _activeShakes[i];
                float deltaTime = shake.Request.UseUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
                shake.Elapsed += deltaTime;

                if (shake.Elapsed >= shake.Request.Duration)
                {
                    _activeShakes.RemoveAt(i);
                    continue;
                }

                totalOffset += EvaluateShakeOffset(shake);
            }

            _shakeOffset = totalOffset;
        }

        private static Vector3 EvaluateShakeOffset(ActiveCameraShake shake)
        {
            float normalized = Mathf.Clamp01(shake.Elapsed / shake.Request.Duration);
            float attenuation = 1f - normalized;

            float phase = shake.PhaseOffset + (normalized * shake.Request.RepeatCount * Mathf.PI * 2f);
            float signedX = Mathf.Sin(phase);
            float signedY = Mathf.Cos(phase + (Mathf.PI * 0.5f));

            float amplitudeX = signedX >= 0f ? shake.Request.RightStrength : shake.Request.LeftStrength;
            float amplitudeY = signedY >= 0f ? shake.Request.UpStrength : shake.Request.DownStrength;

            return new Vector3(
                signedX * amplitudeX * attenuation,
                signedY * amplitudeY * attenuation,
                0f);
        }

        /// <summary>
        /// 카메라 흔들림 효과 주기
        /// </summary>
        public void StartShake(float shakeDuration, float shakeMagnitude)
        {
            if (shakeDuration <= 0f || shakeMagnitude <= 0f)
            {
                return;
            }

            PlayShake(CameraShakeRequest.CreateSymmetric(
                shakeDuration,
                shakeMagnitude,
                3,
                CameraShakeChannel.Default));
        }

        /// <summary>
        /// 방향별 카메라 흔들림 효과를 재생합니다.
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
            PlayShake(new CameraShakeRequest
            {
                Duration = duration,
                LeftStrength = Mathf.Max(0f, leftStrength),
                RightStrength = Mathf.Max(0f, rightStrength),
                DownStrength = Mathf.Max(0f, downStrength),
                UpStrength = Mathf.Max(0f, upStrength),
                RepeatCount = Mathf.Max(1, repeatCount),
                Channel = channel,
                UseUnscaledTime = useUnscaledTime,
            });
        }

        public void PlayShake(CameraShakeRequest request)
        {
            if (!request.IsValid)
            {
                return;
            }

            _activeShakes.Add(new ActiveCameraShake
            {
                Request = request,
                Elapsed = 0f,
                PhaseOffset = ResolveShakePhaseOffset(request),
            });
        }

        /// <summary>
        /// Shake 요청의 시작 위상 정책에 따라 실제 파형 시작 위상을 계산합니다.
        /// </summary>
        /// <param name="request">재생할 Shake 요청 데이터입니다.</param>
        /// <returns>사인/코사인 파형 계산에 사용할 라디안 단위 시작 위상입니다.</returns>
        private static float ResolveShakePhaseOffset(CameraShakeRequest request)
        {
            if (request.PhaseMode == CameraShakePhaseMode.Fixed)
            {
                // 고정 위상은 0~2π 범위로 보정해 큰 값이나 음수 입력도 같은 파형 위치로 해석합니다.
                return Mathf.Repeat(request.FixedPhaseRadians, Mathf.PI * 2f);
            }

            return Random.Range(0f, Mathf.PI * 2f);
        }

        public void PlayShake(CameraShakePreset preset, CameraShakeChannel channel = CameraShakeChannel.Default)
        {
            if (preset == null)
            {
                return;
            }

            PlayShake(preset.ToRequest(channel));
        }

        public void StopShake(CameraShakeChannel channel)
        {
            for (int i = _activeShakes.Count - 1; i >= 0; i--)
            {
                if (_activeShakes[i].Request.Channel == channel)
                {
                    _activeShakes.RemoveAt(i);
                }
            }

            if (_activeShakes.Count == 0)
            {
                _shakeOffset = Vector3.zero;
            }
        }

        public void StopAllShakes()
        {
            _activeShakes.Clear();
            _shakeOffset = Vector3.zero;
        }

        /// <summary>
        /// 맵 경계선 사이즈 변경하기
        /// </summary>
        public void ChangeMapSize(float pWidth, float pHeight)
        {
            _mapSize.x = pWidth;
            _mapSize.y = pHeight;
            RequestBottomOffsetApplyIfNeeded();
        }

        /// <summary>
        /// 현재 맵 테이블의 카메라 오버라이드 값을 적용합니다.
        /// 오버라이드가 지정되지 않은 항목은 인스펙터 기본값으로 복원합니다.
        /// </summary>
        /// <param name="mapData">현재 로드 중인 맵 테이블 데이터입니다.</param>
        public void ApplyMapCameraOverrides(StruckTableMap mapData)
        {
            MarkMapCameraProfileUnresolved();
            Vector2 resolvedFollowOffset = _defaultFollowOffset;
            bool resolvedUseFollowDeadZone = _defaultUseFollowDeadZone;
            Vector2 resolvedFollowDeadZone = _defaultFollowDeadZone;
            CameraBottomFollowOffsetPolicy resolvedBottomPolicy = _defaultBottomFollowOffsetPolicy;

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
            }

            followOffset = resolvedFollowOffset;
            useFollowDeadZone = resolvedUseFollowDeadZone;
            followDeadZoneX = Mathf.Max(0f, resolvedFollowDeadZone.x);
            followDeadZoneY = Mathf.Max(0f, resolvedFollowDeadZone.y);
            bottomFollowOffsetPolicy = resolvedBottomPolicy;
            _cameraPosition.x = resolvedFollowOffset.x;
            _cameraPosition.y = resolvedFollowOffset.y;
            _hasVerticalFollowAnchor = false;
            _pendingAutoBottomOffsetApply = false;
            RequestBottomOffsetApplyIfNeeded();
        }

        /// <summary>
        /// 카메라 강제로 이동시키기
        /// </summary>
        public void MoveCameraPosition(float x, float y)
        {
            transform.position = new Vector3(x, y, -10f) + _cameraPosition;
            _basePosition = transform.position;
        }

        /// <summary>
        /// 플레이어 기준에서의 카메라 위치 값 바꾸기
        /// </summary>
        public void ChangeCameraPositionValue(float x, float y)
        {
            _originCameraPosition.x = _cameraPosition.x;
            _originCameraPosition.y = _cameraPosition.y;
            _cameraPosition.x = x;
            _cameraPosition.y = y;
        }

        public void ResetCameraPositionValue()
        {
            _cameraPosition.x = _originCameraPosition.x;
            _cameraPosition.y = _originCameraPosition.y;
        }

        /// <summary>
        /// 카메라가 따라가는 캐릭터 지우기
        /// </summary>
        public void RemoveFollowTarget()
        {
            _followTarget = null;
            RefreshVerticalFollowStateSource();
        }

        /// <summary>
        /// 따라가는 캐릭터 변경
        /// </summary>
        public void SetFollowTarget(Transform target)
        {
            _followTarget = target == null ? SceneGame.Instance.player.transform : target;
            RefreshVerticalFollowStateSource();
            RequestBottomOffsetApplyIfNeeded();
        }

        /// <summary>
        /// 타일맵 로드 완료 이벤트를 수신하면 자동 바텀 정렬 적용을 재시도합니다.
        /// 실제 맵 하단 경계값은 MapManager에서 계산한 월드 경계를 사용합니다.
        /// </summary>
        /// <param name="mapTileCommon">현재 로드된 맵 루트 컴포넌트입니다.</param>
        /// <param name="grid">맵이 배치된 Grid 오브젝트입니다.</param>
        private void OnLoadTilemapCompleteMap(MapTileCommon mapTileCommon, GameObject grid)
        {
            _ = mapTileCommon;
            _ = grid;
            RequestBottomOffsetApplyIfNeeded();
        }

        /// <summary>
        /// 자동 바텀 정렬 정책이 적용 가능한 상태인지 확인하고,
        /// 적용 대상이면 followOffset.y 보정을 요청합니다.
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
        /// 대기 중인 자동 바텀 정렬 보정을 실제로 적용합니다.
        /// 맵 하단이 화면 하단과 맞도록 타겟 기준 Y 오프셋을 계산합니다.
        /// </summary>
        private void TryApplyBottomOffsetIfPending()
        {
            if (!_pendingAutoBottomOffsetApply)
            {
                return;
            }

            if (_followTarget == null || _currentCamera == null)
            {
                return;
            }

            if (_mapSize.x <= 0f || _mapSize.y <= 0f)
            {
                return;
            }

            MapManager mapManager = SceneGame.Instance != null ? SceneGame.Instance.mapManager : null;
            if (mapManager == null || !mapManager.TryGetCurrentMapBottomY(out float mapBottomY))
            {
                return;
            }

            float desiredCameraCenterY = mapBottomY + _currentCamera.orthographicSize + autoBottomEdgePadding;
            float newFollowOffsetY = desiredCameraCenterY - _followTarget.position.y;

            followOffset.y = newFollowOffsetY;
            _cameraPosition.y = newFollowOffsetY;
            _hasVerticalFollowAnchor = false;
            _pendingAutoBottomOffsetApply = false;
            MarkMapCameraProfileResolved();
        }

        /// <summary>
        /// player 따라가도록 설정
        /// </summary>
        public void SetFollowPlayer()
        {
            if (SceneGame.Instance == null || SceneGame.Instance.player == null) return;
            SetFollowTarget(SceneGame.Instance.player.transform);
        }

        /// <summary>
        /// orthographicSize 변경하기
        /// </summary>
        public void StartZoom(
            float endSize,
            float duration = 1f,
            Easing.EaseType easeType = Easing.EaseType.EaseOutQuad,
            bool useUnscaledTime = false,
            bool changeOriginalSize = false)
        {
            if (_currentCamera == null || !_currentCamera.orthographic)
            {
                return;
            }

            _zoomTimer = 0f;
            _zoomStartSize = _currentCamera.orthographicSize;
            _zoomEndSize = endSize;
            _zoomDuration = Mathf.Max(duration, 0.0001f);
            _zoomEasing = easeType;
            _zoomUseUnscaledTime = useUnscaledTime;
            _isZooming = true;
            if (changeOriginalSize)
            {
                ChangeOriginalOrthographicSize(endSize);
            }

            if (duration <= 0f)
            {
                UpdateZoom();
            }
        }

        /// <summary>
        /// orthographicSize 초기화
        /// </summary>
        private void ReSetZoom()
        {
            _zoomTimer = 0;
            _zoomStartSize = _currentCamera.orthographicSize;
            _zoomEndSize = _originalOrthographicSize;
            _zoomDuration = 1f;
            _zoomEasing = Easing.EaseType.EaseOutQuad;
            _zoomUseUnscaledTime = false;
            _isZooming = true;
        }

        /// <summary>
        /// 연출 종료시 호출
        /// </summary>
        public void ReSetByCutscene()
        {
            SetFollowPlayer();
            StopShake(CameraShakeChannel.Cutscene);
            ReSetZoom();
        }

        public void StopZoom(bool snapToTarget = false)
        {
            if (!_isZooming)
            {
                return;
            }

            if (snapToTarget && _currentCamera != null)
            {
                _currentCamera.orthographicSize = _zoomEndSize;
                _height = _zoomEndSize;
                _width = _height * Screen.width / Screen.height;
            }

            _isZooming = false;
        }

        /// <summary>
        /// 카메라의 orthographicSize를 즉시 적용합니다.
        /// Intro 시작 연출처럼 "프레임 보간 없이 즉시 스냅"이 필요한 경우 사용합니다.
        /// </summary>
        /// <param name="size">즉시 적용할 orthographicSize 값입니다.</param>
        /// <param name="changeOriginalSize">
        /// true이면 기본(원본) 카메라 사이즈도 함께 갱신합니다.
        /// false이면 현재 프레임의 표시 사이즈만 변경하고 원본 값은 유지합니다.
        /// </param>
        public void SetOrthographicSizeImmediate(float size, bool changeOriginalSize = false)
        {
            if (_currentCamera == null || !_currentCamera.orthographic)
            {
                return;
            }

            float safeSize = Mathf.Max(size, 0.0001f);
            _currentCamera.orthographicSize = safeSize;
            _height = safeSize;
            _width = _height * Screen.width / Screen.height;

            if (changeOriginalSize)
            {
                _originalOrthographicSize = safeSize;
            }
        }

        /// <summary>
        /// 타겟(플레이어) 기준 카메라 기본 오프셋(월드 단위).
        /// Inspector에서 설정한 값은 시작 시 기본값으로 사용되며, 흔들림(Shake)은 이 기본 위치를 기준으로 적용됩니다.
        /// </summary>
        public Vector2 FollowOffset
        {
            get => new Vector2(_cameraPosition.x, _cameraPosition.y);
            set => _cameraPosition = new Vector3(value.x, value.y, 0f);
        }

        /// <summary>
        /// 카메라 Follow Dead Zone 반경을 반환하거나 설정합니다.
        /// 두 축이 모두 0 이하이면 데드존 추적을 비활성화합니다.
        /// </summary>
        public Vector2 FollowDeadZone
        {
            get => new Vector2(followDeadZoneX, followDeadZoneY);
            set
            {
                followDeadZoneX = Mathf.Max(0f, value.x);
                followDeadZoneY = Mathf.Max(0f, value.y);
                useFollowDeadZone = followDeadZoneX > 0f || followDeadZoneY > 0f;
            }
        }

        public void SetCameraMoveSpeed(float speed)
        {
            cameraMoveSpeed = speed;
        }

        /// <summary>
        /// 카메라 위치 가져오기 (Z 값은 제외)
        /// </summary>
        public Vector2 GetPositionCenter()
        {
            return transform.position;
        }

        /// <summary>
        /// 흔들림이 적용되기 전의 카메라 기본 월드 위치를 반환합니다.
        /// </summary>
        public Vector3 GetBaseWorldPosition()
        {
            return _basePosition;
        }

        /// <summary>
        /// 현재 카메라에 적용 중인 흔들림 오프셋을 반환합니다.
        /// </summary>
        public Vector3 GetShakeOffset()
        {
            return _shakeOffset;
        }

        public void ChangeOriginalOrthographicSize(float size)
        {
            _originalOrthographicSize = size;
        }

        /// <summary>
        /// 맵 카메라 프로필 적용 상태를 "미완료"로 전환합니다.
        /// 새 맵 로드/오버라이드 반영 시점에 호출됩니다.
        /// </summary>
        private void MarkMapCameraProfileUnresolved()
        {
            _isMapCameraProfileResolved = false;
        }

        /// <summary>
        /// 맵 카메라 프로필 적용 상태를 "완료"로 전환하고 완료 이벤트를 발행합니다.
        /// </summary>
        private void MarkMapCameraProfileResolved()
        {
            if (_isMapCameraProfileResolved)
            {
                return;
            }

            _isMapCameraProfileResolved = true;
            MapCameraProfileResolved?.Invoke(this);
        }

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            // Gizmos.color = Color.red;
            // Gizmos.DrawWireCube(center, mapSize * 2);
            //
            // Gizmos.color = Color.blue;
            // Gizmos.DrawWireCube(center, monsterSpawnPositionBoxSize * 2);
        }
#endif
    }
}
