using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

namespace GGemCo2DCore
{
    /// <summary>
    /// 카메라 흔들림, 줌, 컷신 독점 제어 같은 연출 효과를 관리합니다.
    /// </summary>
    public sealed class CameraEffectController
    {
        private sealed class ActiveCameraShake
        {
            public CameraShakeRequest Request;
            public float Elapsed;
            public float PhaseOffset;
        }

        private readonly List<ActiveCameraShake> _activeShakes = new();
        private Camera _camera;
        private Transform _cameraTransform;
        private float _originalOrthographicSize;
        private Vector3 _shakeOffset;
        private bool _isZooming;
        private float _zoomTimer;
        private float _zoomDuration;
        private float _zoomStartSize;
        private float _zoomEndSize;
        private Easing.EaseType _zoomEasing;
        private bool _zoomUseUnscaledTime;
        private CameraZoomOwner _zoomOwner;
        private object _zoomSource;
        private float _zoomRestoreSize;
        private bool _hasZoomOwnership;
        private object _overrideOwner;
        private bool _isOverrideActive;
        private Vector3 _overridePosition;

        /// <summary>
        /// 흔들림이 적용되기 전 원본 Orthographic Size입니다.
        /// </summary>
        public float OriginalOrthographicSize => _originalOrthographicSize;

        /// <summary>
        /// 현재 컷신 독점 제어가 활성화되어 있는지 반환합니다.
        /// </summary>
        public bool IsOverrideActive => _isOverrideActive;

        /// <summary>
        /// 컷신 독점 제어 중인 카메라 위치입니다.
        /// </summary>
        public Vector3 OverridePosition => _overridePosition;

        /// <summary>
        /// 현재 프레임에 적용할 흔들림 오프셋입니다.
        /// </summary>
        public Vector3 ShakeOffset => _shakeOffset;

        /// <summary>
        /// 컨트롤러가 사용할 카메라 참조를 초기화합니다.
        /// </summary>
        /// <param name="camera">대상 카메라입니다.</param>
        /// <param name="cameraTransform">대상 카메라 Transform입니다.</param>
        public void Initialize(Camera camera, Transform cameraTransform)
        {
            _camera = camera;
            _cameraTransform = cameraTransform;
            _originalOrthographicSize = camera != null ? camera.orthographicSize : 0f;
            _shakeOffset = Vector3.zero;
            _isZooming = false;
            _zoomTimer = 0f;
            _zoomDuration = 0f;
            _zoomStartSize = 0f;
            _zoomEndSize = 0f;
            _zoomEasing = Easing.EaseType.Linear;
            _zoomUseUnscaledTime = false;
            ClearZoomOwnership();
            _overrideOwner = null;
            _isOverrideActive = false;
            _overridePosition = cameraTransform != null ? cameraTransform.position : Vector3.zero;
        }

        /// <summary>
        /// 카메라 줌 상태를 갱신합니다.
        /// </summary>
        public void TickZoom()
        {
            if (!_isZooming || _camera == null)
            {
                return;
            }

            float deltaTime = _zoomUseUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
            _zoomTimer += deltaTime;

            float duration = Mathf.Max(_zoomDuration, 0.0001f);
            float t = Mathf.Clamp01(_zoomTimer / duration);
            float easedT = Easing.Apply(t, _zoomEasing);
            _camera.orthographicSize = Mathf.Lerp(_zoomStartSize, _zoomEndSize, easedT);

            if (t >= 1f)
            {
                _isZooming = false;
            }
        }

        /// <summary>
        /// 카메라 흔들림 상태를 갱신합니다.
        /// </summary>
        public void TickShake()
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

        /// <summary>
        /// 일반 카메라 흔들림을 시작합니다.
        /// </summary>
        public void StartShake(float duration, float magnitude)
        {
            if (duration <= 0f || magnitude <= 0f)
            {
                return;
            }

            PlayShake(CameraShakeRequest.CreateSymmetric(duration, magnitude, 3, CameraShakeChannel.Default));
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
            PlayShake(new CameraShakeRequest
            {
                ShakeType = CameraShakeType.Common,
                Duration = duration,
                Strength = Mathf.Max(Mathf.Max(leftStrength, rightStrength), Mathf.Max(downStrength, upStrength)),
                AxisStrength = Vector2.one,
                LeftStrength = Mathf.Max(0f, leftStrength),
                RightStrength = Mathf.Max(0f, rightStrength),
                DownStrength = Mathf.Max(0f, downStrength),
                UpStrength = Mathf.Max(0f, upStrength),
                RepeatCount = Mathf.Max(1, repeatCount),
                RandomStartPhase = true,
                Direction = Vector2.right,
                Channel = channel,
                UseUnscaledTime = useUnscaledTime,
                DecayMode = CameraShakeDecayMode.Linear,
            });
        }

        /// <summary>
        /// 지정된 흔들림 요청을 재생합니다.
        /// </summary>
        /// <param name="request">재생할 흔들림 요청입니다.</param>
        public void PlayShake(CameraShakeRequest request)
        {
            if (_isOverrideActive || !request.IsValid)
            {
                return;
            }

            _activeShakes.Add(new ActiveCameraShake
            {
                Request = request,
                Elapsed = 0f,
                PhaseOffset = request.RandomStartPhase ? Random.Range(0f, Mathf.PI * 2f) : 0f,
            });
        }

        /// <summary>
        /// 프리셋 기반 흔들림을 재생합니다.
        /// </summary>
        public void PlayShake(CameraShakePreset preset, CameraShakeChannel channel = CameraShakeChannel.Default)
        {
            if (preset == null)
            {
                return;
            }

            PlayShake(preset.ToRequest(channel));
        }

        /// <summary>
        /// 지정된 채널의 카메라 흔들림을 모두 중단합니다.
        /// </summary>
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

        /// <summary>
        /// 모든 카메라 흔들림을 중단합니다.
        /// </summary>
        public void StopAllShakes()
        {
            _activeShakes.Clear();
            _shakeOffset = Vector3.zero;
        }

        /// <summary>
        /// Orthographic Size 보간을 시작합니다.
        /// </summary>
        public void StartZoom(
            float endSize,
            float duration,
            Easing.EaseType easeType,
            bool useUnscaledTime,
            bool changeOriginalSize)
        {
            ClearZoomOwnership();
            StartZoomInternal(endSize, duration, easeType, useUnscaledTime, changeOriginalSize);
        }

        /// <summary>
        /// 소유권과 교체 정책을 확인한 뒤 카메라 줌을 시작합니다.
        /// 같은 출처가 연속으로 요청하면 최초 요청 직전 크기를 복귀 기준으로 유지합니다.
        /// </summary>
        /// <param name="request">실행할 카메라 줌 요청입니다.</param>
        /// <returns>요청이 수락되어 줌을 적용했으면 <see langword="true"/>입니다.</returns>
        public bool TryStartZoom(CameraZoomRequest request)
        {
            if (!request.IsValid || _isOverrideActive || _camera == null || !_camera.orthographic)
            {
                return false;
            }

            if (request.Owner != CameraZoomOwner.Default && request.Source == null)
            {
                return false;
            }

            if (!CanAcceptZoom(request))
            {
                return false;
            }

            bool isSameSource = _hasZoomOwnership &&
                                _zoomOwner == request.Owner &&
                                ReferenceEquals(_zoomSource, request.Source);
            bool canInheritRestoreSize = _hasZoomOwnership && _zoomOwner == request.Owner;
            if (!isSameSource && !canInheritRestoreSize)
            {
                _zoomRestoreSize = _camera.orthographicSize;
            }

            // 같은 우선순위 계층의 스킬끼리 줌을 교체할 때는 최초 적용 전 크기를 유지하여 복귀 누적 오차를 방지합니다.

            _zoomOwner = request.Owner;
            _zoomSource = request.Source;
            _hasZoomOwnership = true;
            StartZoomInternal(
                request.EndSize,
                request.Duration,
                request.Easing,
                request.UseUnscaledTime,
                request.ChangeOriginalSize);
            return true;
        }

        /// <summary>
        /// 현재 줌 소유자와 출처가 일치할 때 요청 전 카메라 크기로 복귀합니다.
        /// </summary>
        /// <param name="owner">복귀할 줌의 소유 시스템입니다.</param>
        /// <param name="source">복귀할 줌 요청의 출처 객체입니다.</param>
        /// <param name="duration">복귀 보간 시간입니다.</param>
        /// <param name="easeType">복귀 보간 방식입니다.</param>
        /// <param name="useUnscaledTime">Time.timeScale 영향을 무시할지 여부입니다.</param>
        /// <returns>소유권이 일치하여 복귀를 시작했으면 <see langword="true"/>입니다.</returns>
        public bool RestoreZoomIfOwnedBy(
            CameraZoomOwner owner,
            object source,
            float duration,
            Easing.EaseType easeType,
            bool useUnscaledTime)
        {
            if (!_hasZoomOwnership || _zoomOwner != owner || !ReferenceEquals(_zoomSource, source))
            {
                return false;
            }

            float restoreSize = _zoomRestoreSize;
            ClearZoomOwnership();
            StartZoomInternal(restoreSize, duration, easeType, useUnscaledTime, changeOriginalSize: false);
            return true;
        }

        /// <summary>
        /// 원본 Orthographic Size로 복귀하는 줌을 시작합니다.
        /// </summary>
        public void ResetZoom()
        {
            if (_camera == null || !_camera.orthographic)
            {
                return;
            }

            ClearZoomOwnership();
            _zoomTimer = 0f;
            _zoomStartSize = _camera.orthographicSize;
            _zoomEndSize = _originalOrthographicSize;
            _zoomDuration = 1f;
            _zoomEasing = Easing.EaseType.EaseOutQuad;
            _zoomUseUnscaledTime = false;
            _isZooming = true;
        }

        /// <summary>
        /// 진행 중인 줌을 중단합니다.
        /// </summary>
        /// <param name="snapToTarget">true이면 목표 Size를 즉시 적용합니다.</param>
        public void StopZoom(bool snapToTarget = false)
        {
            ClearZoomOwnership();
            if (!_isZooming)
            {
                return;
            }

            if (snapToTarget && _camera != null)
            {
                _camera.orthographicSize = _zoomEndSize;
            }

            _isZooming = false;
        }

        /// <summary>
        /// Orthographic Size를 즉시 적용합니다.
        /// </summary>
        public void SetOrthographicSizeImmediate(float size, bool changeOriginalSize)
        {
            ClearZoomOwnership();
            SetOrthographicSizeImmediateInternal(size, changeOriginalSize);
        }

        /// <summary>
        /// 교체 정책과 소유자 우선순위를 기준으로 새 줌 요청을 수락할지 판단합니다.
        /// </summary>
        /// <param name="request">수락 여부를 확인할 줌 요청입니다.</param>
        /// <returns>새 요청을 적용할 수 있으면 <see langword="true"/>입니다.</returns>
        private bool CanAcceptZoom(CameraZoomRequest request)
        {
            if (!_hasZoomOwnership)
            {
                return true;
            }

            if (_zoomOwner == request.Owner && ReferenceEquals(_zoomSource, request.Source))
            {
                return true;
            }

            int currentPriority = (int)_zoomOwner;
            int nextPriority = (int)request.Owner;
            switch (request.ReplaceMode)
            {
                case CameraZoomReplaceMode.IgnoreIfPlaying:
                    return !_isZooming && nextPriority >= currentPriority;

                case CameraZoomReplaceMode.IgnoreIfOwnerPriorityIsGreaterOrEqual:
                    return nextPriority > currentPriority;

                case CameraZoomReplaceMode.ReplaceCurrent:
                default:
                    return nextPriority >= currentPriority;
            }
        }

        /// <summary>
        /// 검증이 끝난 Orthographic Size 보간 값을 현재 카메라에 적용합니다.
        /// </summary>
        private void StartZoomInternal(
            float endSize,
            float duration,
            Easing.EaseType easeType,
            bool useUnscaledTime,
            bool changeOriginalSize)
        {
            if (_camera == null || !_camera.orthographic)
            {
                return;
            }

            float safeEndSize = Mathf.Max(endSize, 0.0001f);
            if (duration <= 0f)
            {
                SetOrthographicSizeImmediateInternal(safeEndSize, changeOriginalSize);
                _isZooming = false;
                return;
            }

            _zoomTimer = 0f;
            _zoomStartSize = _camera.orthographicSize;
            _zoomEndSize = safeEndSize;
            _zoomDuration = duration;
            _zoomEasing = easeType;
            _zoomUseUnscaledTime = useUnscaledTime;
            _isZooming = true;

            if (changeOriginalSize)
            {
                ChangeOriginalOrthographicSize(_zoomEndSize);
            }
        }

        /// <summary>
        /// 소유권 변경 없이 Orthographic Size를 즉시 적용합니다.
        /// </summary>
        private void SetOrthographicSizeImmediateInternal(float size, bool changeOriginalSize)
        {
            if (_camera == null || !_camera.orthographic)
            {
                return;
            }

            float safeSize = Mathf.Max(size, 0.0001f);
            _camera.orthographicSize = safeSize;
            if (changeOriginalSize)
            {
                _originalOrthographicSize = safeSize;
            }
        }

        /// <summary>
        /// 현재 카메라 줌 요청의 소유권과 복귀 기준을 초기화합니다.
        /// </summary>
        private void ClearZoomOwnership()
        {
            _zoomOwner = CameraZoomOwner.Default;
            _zoomSource = null;
            _zoomRestoreSize = 0f;
            _hasZoomOwnership = false;
        }

        /// <summary>
        /// 기본 Orthographic Size를 변경합니다.
        /// </summary>
        public void ChangeOriginalOrthographicSize(float size)
        {
            _originalOrthographicSize = Mathf.Max(size, 0.0001f);
        }

        /// <summary>
        /// 컷신 독점 카메라 제어를 시작합니다.
        /// </summary>
        public bool BeginOverride(object owner, Vector2 position)
        {
            if (owner == null || _cameraTransform == null)
            {
                return false;
            }

            _overrideOwner = owner;
            _isOverrideActive = true;
            StopAllShakes();
            StopZoom();
            return SetOverridePosition(owner, position);
        }

        /// <summary>
        /// 컷신 독점 카메라 위치를 갱신합니다.
        /// </summary>
        public bool SetOverridePosition(object owner, Vector2 position)
        {
            if (!_isOverrideActive || !ReferenceEquals(_overrideOwner, owner) || _cameraTransform == null)
            {
                return false;
            }

            _overridePosition = new Vector3(position.x, position.y, _cameraTransform.position.z);
            return true;
        }

        /// <summary>
        /// 컷신 독점 카메라 제어를 종료합니다.
        /// </summary>
        public bool EndOverride(object owner)
        {
            if (!_isOverrideActive || !ReferenceEquals(_overrideOwner, owner))
            {
                return false;
            }

            ClearOverride();
            return true;
        }

        /// <summary>
        /// 소유자와 관계없이 컷신 독점 제어를 강제로 해제합니다.
        /// </summary>
        public void ClearOverride()
        {
            _isOverrideActive = false;
            _overrideOwner = null;
            _overridePosition = _cameraTransform != null ? _cameraTransform.position : Vector3.zero;
            _shakeOffset = Vector3.zero;
        }

        private static Vector3 EvaluateShakeOffset(ActiveCameraShake shake)
        {
            switch (shake.Request.ShakeType)
            {
                case CameraShakeType.DirectionalImpulse:
                    return EvaluateDirectionalImpulseOffset(shake);
                case CameraShakeType.DirectionalOscillation:
                    return EvaluateDirectionalOscillationOffset(shake);
                case CameraShakeType.Common:
                default:
                    return EvaluateCommonShakeOffset(shake);
            }
        }

        private static Vector3 EvaluateCommonShakeOffset(ActiveCameraShake shake)
        {
            float normalized = Mathf.Clamp01(shake.Elapsed / shake.Request.Duration);
            float attenuation = EvaluateShakeAttenuation(shake.Request, normalized);
            float phase = shake.PhaseOffset + normalized * Mathf.Max(1, shake.Request.RepeatCount) * Mathf.PI * 2f;
            float signedX = Mathf.Sin(phase);
            float signedY = Mathf.Cos(phase + Mathf.PI * 0.5f);

            float rightStrength = ResolveDirectionalStrength(shake.Request.RightStrength, shake.Request.Strength, shake.Request.AxisStrength.x);
            float leftStrength = ResolveDirectionalStrength(shake.Request.LeftStrength, shake.Request.Strength, shake.Request.AxisStrength.x);
            float upStrength = ResolveDirectionalStrength(shake.Request.UpStrength, shake.Request.Strength, shake.Request.AxisStrength.y);
            float downStrength = ResolveDirectionalStrength(shake.Request.DownStrength, shake.Request.Strength, shake.Request.AxisStrength.y);

            float amplitudeX = signedX >= 0f ? rightStrength : leftStrength;
            float amplitudeY = signedY >= 0f ? upStrength : downStrength;
            return new Vector3(signedX * amplitudeX * attenuation, signedY * amplitudeY * attenuation, 0f);
        }

        private static Vector3 EvaluateDirectionalImpulseOffset(ActiveCameraShake shake)
        {
            float normalized = Mathf.Clamp01(shake.Elapsed / shake.Request.Duration);
            Vector2 direction = ResolveShakeDirection(shake.Request.Direction);
            float weight = EvaluateImpulseWeight(shake.Request, normalized);
            Vector2 offset = direction * Mathf.Max(0f, shake.Request.Strength) * weight;
            return new Vector3(offset.x, offset.y, 0f);
        }

        private static Vector3 EvaluateDirectionalOscillationOffset(ActiveCameraShake shake)
        {
            float normalized = Mathf.Clamp01(shake.Elapsed / shake.Request.Duration);
            float attenuation = EvaluateShakeAttenuation(shake.Request, normalized);
            float phase = shake.PhaseOffset + normalized * Mathf.Max(1, shake.Request.RepeatCount) * Mathf.PI * 2f;
            float signed = Mathf.Sin(phase);
            Vector2 direction = ResolveShakeDirection(shake.Request.Direction);
            Vector2 offset = direction * signed * Mathf.Max(0f, shake.Request.Strength) * attenuation;
            return new Vector3(offset.x, offset.y, 0f);
        }

        private static float ResolveDirectionalStrength(float directionalStrength, float baseStrength, float axisWeight)
        {
            return directionalStrength > 0f ? directionalStrength : Mathf.Max(0f, baseStrength) * Mathf.Max(0f, axisWeight);
        }

        private static float EvaluateShakeAttenuation(CameraShakeRequest request, float normalized)
        {
            if (request.DecayMode == CameraShakeDecayMode.Smooth)
            {
                return Mathf.SmoothStep(1f, 0f, normalized);
            }

            return 1f - normalized;
        }

        private static float EvaluateImpulseWeight(CameraShakeRequest request, float normalized)
        {
            if (request.ImpulseCurve != null && request.ImpulseCurve.length > 0)
            {
                return Mathf.Max(0f, request.ImpulseCurve.Evaluate(normalized));
            }

            return Mathf.SmoothStep(1f, 0f, normalized);
        }

        private static Vector2 ResolveShakeDirection(Vector2 direction)
        {
            return direction.sqrMagnitude <= 0.0001f ? Vector2.right : direction.normalized;
        }
    }
}
