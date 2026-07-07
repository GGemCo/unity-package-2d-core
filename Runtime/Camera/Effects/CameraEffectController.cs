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
            if (_isOverrideActive || _camera == null || !_camera.orthographic)
            {
                return;
            }

            float safeEndSize = Mathf.Max(endSize, 0.0001f);
            if (duration <= 0f)
            {
                SetOrthographicSizeImmediate(safeEndSize, changeOriginalSize);
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
        /// 원본 Orthographic Size로 복귀하는 줌을 시작합니다.
        /// </summary>
        public void ResetZoom()
        {
            if (_camera == null || !_camera.orthographic)
            {
                return;
            }

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
