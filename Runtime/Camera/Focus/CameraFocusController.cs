using UnityEngine;

namespace GGemCo2DCore
{
    /// <summary>
    /// 기본 게임플레이 Follow를 보존하면서 임시 카메라 포커스의 이동과 복귀를 계산합니다.
    /// </summary>
    internal sealed class CameraFocusController
    {
        private CameraFocusRequest _request;
        private Vector3 _transitionStartPosition;
        private float _transitionElapsed;
        private bool _isActive;
        private bool _isTransitionComplete;
        private bool _isRestoring;
        private Vector3 _restoreStartPosition;
        private float _restoreDuration;
        private float _restoreElapsed;
        private Easing.EaseType _restoreEasing;
        private bool _restoreUseUnscaledTime;

        /// <summary>임시 카메라 포커스 또는 복귀 보간이 활성화되어 있는지 반환합니다.</summary>
        public bool IsActive => _isActive;

        /// <summary>현재 포커스가 기본 게임플레이 Follow로 복귀 중인지 반환합니다.</summary>
        public bool IsRestoring => _isActive && _isRestoring;

        /// <summary>현재 포커스 위치에 맵 경계를 적용할지 반환합니다.</summary>
        public bool RespectMapBounds => !_isActive || _request.RespectMapBounds;

        /// <summary>
        /// 소유권과 교체 정책을 확인한 뒤 새 카메라 포커스를 시작합니다.
        /// </summary>
        /// <param name="request">적용할 카메라 포커스 요청입니다.</param>
        /// <param name="currentPosition">전환을 시작할 현재 카메라 기본 위치입니다.</param>
        /// <returns>요청이 수락되었으면 <see langword="true"/>입니다.</returns>
        public bool TryStart(CameraFocusRequest request, Vector3 currentPosition)
        {
            if (!request.IsValid || (request.Owner != CameraFocusOwner.Default && request.Source == null))
                return false;
            if (!CanAccept(request))
                return false;

            _request = request;
            _request.Duration = Mathf.Max(0f, request.Duration);
            _transitionStartPosition = currentPosition;
            _transitionElapsed = 0f;
            _isActive = true;
            _isTransitionComplete = false;
            _isRestoring = false;
            return true;
        }

        /// <summary>
        /// 소유자와 출처가 일치할 때 기본 게임플레이 Follow 위치로 복귀를 시작합니다.
        /// </summary>
        /// <param name="owner">복귀할 포커스의 소유 시스템입니다.</param>
        /// <param name="source">복귀할 포커스 요청 출처입니다.</param>
        /// <param name="currentPosition">복귀를 시작할 현재 카메라 기본 위치입니다.</param>
        /// <param name="duration">복귀 보간 시간입니다.</param>
        /// <param name="easing">복귀 보간 방식입니다.</param>
        /// <param name="useUnscaledTime">Time.timeScale 영향을 무시할지 여부입니다.</param>
        /// <returns>소유권이 일치해 복귀를 처리했으면 <see langword="true"/>입니다.</returns>
        public bool RestoreIfOwnedBy(
            CameraFocusOwner owner,
            object source,
            Vector3 currentPosition,
            float duration,
            Easing.EaseType easing,
            bool useUnscaledTime)
        {
            if (!_isActive || _request.Owner != owner || !ReferenceEquals(_request.Source, source))
                return false;

            if (duration <= 0f)
            {
                Reset();
                return true;
            }

            _isRestoring = true;
            _restoreStartPosition = currentPosition;
            _restoreDuration = duration;
            _restoreElapsed = 0f;
            _restoreEasing = easing;
            _restoreUseUnscaledTime = useUnscaledTime;
            return true;
        }

        /// <summary>
        /// 현재 프레임의 임시 포커스 또는 게임플레이 Follow 복귀 위치를 계산합니다.
        /// </summary>
        /// <param name="currentPosition">현재 카메라 기본 위치입니다.</param>
        /// <param name="gameplayFollowPosition">복귀할 게임플레이 Follow 목표 위치입니다.</param>
        /// <param name="gameplayFollowOffset">맵 기본 Follow Offset입니다.</param>
        /// <param name="followSpeed">초기 이동 완료 후 살아 있는 대상을 추적할 속도입니다.</param>
        /// <returns>이번 프레임에 적용할 카메라 기본 위치입니다.</returns>
        public Vector3 Evaluate(
            Vector3 currentPosition,
            Vector3 gameplayFollowPosition,
            Vector2 gameplayFollowOffset,
            float followSpeed)
        {
            if (!_isActive)
                return currentPosition;

            if (_isRestoring)
                return EvaluateRestore(gameplayFollowPosition);

            if (!TryResolveFocusPosition(currentPosition.z, gameplayFollowOffset, out Vector3 targetPosition))
            {
                GcLogger.LogWarning($"[{nameof(CameraFocusController)}] 추적 대상이 사라져 임시 카메라 포커스를 종료합니다.");
                Reset();
                return currentPosition;
            }

            float deltaTime = _request.UseUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
            if (!_isTransitionComplete && _request.Duration > 0f)
            {
                _transitionElapsed += deltaTime;
                float t = Mathf.Clamp01(_transitionElapsed / _request.Duration);
                float eased = Easing.Apply(t, _request.Easing);
                Vector3 result = Vector3.Lerp(_transitionStartPosition, targetPosition, eased);
                _isTransitionComplete = t >= 1f;
                return result;
            }

            _isTransitionComplete = true;
            if (_request.TrackingMode == CameraFocusTrackingMode.SnapshotPosition)
                return targetPosition;

            float followWeight = Mathf.Clamp01(deltaTime * Mathf.Max(0f, followSpeed));
            return Vector3.Lerp(currentPosition, targetPosition, followWeight);
        }

        /// <summary>
        /// 현재 임시 카메라 포커스와 복귀 상태를 즉시 초기화합니다.
        /// </summary>
        public void Reset()
        {
            _request = default;
            _transitionStartPosition = Vector3.zero;
            _transitionElapsed = 0f;
            _isActive = false;
            _isTransitionComplete = false;
            _isRestoring = false;
            _restoreStartPosition = Vector3.zero;
            _restoreDuration = 0f;
            _restoreElapsed = 0f;
            _restoreEasing = Easing.EaseType.Linear;
            _restoreUseUnscaledTime = false;
        }

        /// <summary>
        /// 현재 요청과 새 요청의 소유권 및 교체 정책을 비교합니다.
        /// </summary>
        private bool CanAccept(CameraFocusRequest request)
        {
            if (!_isActive)
                return true;
            if (_request.Owner == request.Owner && ReferenceEquals(_request.Source, request.Source))
                return true;

            int currentPriority = (int)_request.Owner;
            int nextPriority = (int)request.Owner;
            switch (request.ReplaceMode)
            {
                case CameraFocusReplaceMode.IgnoreIfActive:
                    return false;
                case CameraFocusReplaceMode.IgnoreIfOwnerPriorityIsGreaterOrEqual:
                    return nextPriority > currentPriority;
                case CameraFocusReplaceMode.ReplaceCurrent:
                default:
                    return nextPriority >= currentPriority;
            }
        }

        /// <summary>
        /// 추적 방식에 따라 대상 위치와 추가 Offset을 합산합니다.
        /// </summary>
        private bool TryResolveFocusPosition(float cameraZ, Vector2 gameplayFollowOffset, out Vector3 position)
        {
            Vector2 sourcePosition;
            if (_request.TrackingMode == CameraFocusTrackingMode.FollowTarget)
            {
                if (_request.Target == null)
                {
                    position = default;
                    return false;
                }

                sourcePosition = _request.Target.position;
            }
            else
            {
                sourcePosition = _request.SnapshotPosition;
            }

            Vector2 resolved = sourcePosition + gameplayFollowOffset + _request.Offset;
            position = new Vector3(resolved.x, resolved.y, cameraZ);
            return true;
        }

        /// <summary>
        /// 저장된 복귀 시작 위치에서 현재 게임플레이 Follow 위치까지 보간합니다.
        /// </summary>
        private Vector3 EvaluateRestore(Vector3 gameplayFollowPosition)
        {
            float deltaTime = _restoreUseUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
            _restoreElapsed += deltaTime;
            float t = Mathf.Clamp01(_restoreElapsed / Mathf.Max(0.0001f, _restoreDuration));
            float eased = Easing.Apply(t, _restoreEasing);
            Vector3 result = Vector3.Lerp(_restoreStartPosition, gameplayFollowPosition, eased);
            if (t >= 1f)
                Reset();

            return result;
        }
    }
}
