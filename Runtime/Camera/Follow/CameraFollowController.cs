using UnityEngine;

namespace GGemCo2DCore
{
    /// <summary>
    /// 플레이어 또는 지정된 대상을 따라가는 카메라 기본 위치를 계산합니다.
    /// </summary>
    public sealed class CameraFollowController
    {
        private Transform _followTarget;
        private ICameraVerticalFollowStateSource _verticalFollowStateSource;
        private Vector2 _offset;
        private Vector2 _deadZone;
        private float _moveSpeed;
        private float _verticalFollowInfluence;
        private bool _useDeadZone;
        private bool _hasVerticalFollowAnchor;
        private float _verticalFollowAnchorTargetY;

        /// <summary>
        /// 현재 추적 대상 Transform입니다.
        /// </summary>
        public Transform FollowTarget => _followTarget;

        /// <summary>
        /// 추적 대상이 지정되어 있는지 반환합니다.
        /// </summary>
        public bool HasFollowTarget => _followTarget != null;

        /// <summary>
        /// 카메라가 추적 대상 기준으로 유지할 월드 오프셋입니다.
        /// </summary>
        public Vector2 Offset
        {
            get => _offset;
            set => _offset = value;
        }

        /// <summary>
        /// 대상 추적 속도입니다.
        /// </summary>
        public float MoveSpeed
        {
            get => _moveSpeed;
            set => _moveSpeed = Mathf.Max(0f, value);
        }

        /// <summary>
        /// 현재 Dead Zone 반경입니다.
        /// </summary>
        public Vector2 DeadZone
        {
            get => _deadZone;
            set
            {
                _deadZone = new Vector2(Mathf.Max(0f, value.x), Mathf.Max(0f, value.y));
                _useDeadZone = _deadZone.x > 0f || _deadZone.y > 0f;
            }
        }

        /// <summary>
        /// 추적 계산에 사용할 설정값을 갱신합니다.
        /// </summary>
        /// <param name="moveSpeed">대상 추적 속도입니다.</param>
        /// <param name="offset">추적 대상 기준 카메라 오프셋입니다.</param>
        /// <param name="useDeadZone">Dead Zone 사용 여부입니다.</param>
        /// <param name="deadZone">Dead Zone 반경입니다.</param>
        /// <param name="verticalFollowInfluence">점프/낙하 상태에서 Y축 추적에 반영할 비율입니다.</param>
        public void Configure(
            float moveSpeed,
            Vector2 offset,
            bool useDeadZone,
            Vector2 deadZone,
            float verticalFollowInfluence)
        {
            _moveSpeed = Mathf.Max(0f, moveSpeed);
            _offset = offset;
            _useDeadZone = useDeadZone;
            _deadZone = new Vector2(Mathf.Max(0f, deadZone.x), Mathf.Max(0f, deadZone.y));
            _verticalFollowInfluence = Mathf.Clamp01(verticalFollowInfluence);
            _hasVerticalFollowAnchor = false;
        }

        /// <summary>
        /// 추적 대상을 제거합니다.
        /// </summary>
        public void RemoveTarget()
        {
            _followTarget = null;
            RefreshVerticalFollowStateSource();
        }

        /// <summary>
        /// 추적 대상을 설정합니다.
        /// </summary>
        /// <param name="target">새 추적 대상입니다.</param>
        public void SetTarget(Transform target)
        {
            _followTarget = target;
            RefreshVerticalFollowStateSource();
        }

        /// <summary>
        /// 현재 추적 대상이 전달된 대상과 같은지 확인합니다.
        /// </summary>
        /// <param name="target">비교할 Transform입니다.</param>
        /// <returns>현재 추적 대상이면 true를 반환합니다.</returns>
        public bool IsFollowing(Transform target)
        {
            return _followTarget == target;
        }

        /// <summary>
        /// 현재 추적 상태를 기준으로 카메라 기본 위치를 계산합니다.
        /// </summary>
        /// <param name="currentBasePosition">효과가 적용되기 전 현재 카메라 기본 위치입니다.</param>
        /// <param name="deltaTime">이번 프레임의 시간 간격입니다.</param>
        /// <returns>추적 계산이 반영된 카메라 기본 위치입니다.</returns>
        public Vector3 EvaluateBasePosition(Vector3 currentBasePosition, float deltaTime)
        {
            if (_followTarget == null)
            {
                return currentBasePosition;
            }

            Vector3 targetPosition = _followTarget.position + new Vector3(_offset.x, _offset.y, 0f);
            targetPosition.y = EvaluateVerticalFollowTargetY(targetPosition.y);
            targetPosition = ApplyDeadZone(currentBasePosition, targetPosition);

            float lerpWeight = Mathf.Clamp01(deltaTime * _moveSpeed);
            Vector3 resolvedPosition = Vector3.Lerp(currentBasePosition, targetPosition, lerpWeight);
            resolvedPosition.z = currentBasePosition.z;
            return resolvedPosition;
        }

        /// <summary>
        /// Follow Offset Y 값을 즉시 갱신하고 수직 추적 기준점을 초기화합니다.
        /// </summary>
        /// <param name="offsetY">새 Follow Offset Y 값입니다.</param>
        public void SetOffsetY(float offsetY)
        {
            _offset.y = offsetY;
            _hasVerticalFollowAnchor = false;
        }

        private float EvaluateVerticalFollowTargetY(float targetY)
        {
            if (_verticalFollowStateSource == null || !_verticalFollowStateSource.IsVerticalFollowInfluenceActive)
            {
                _hasVerticalFollowAnchor = false;
                return targetY;
            }

            if (Mathf.Approximately(_verticalFollowInfluence, 1f))
            {
                _hasVerticalFollowAnchor = true;
                _verticalFollowAnchorTargetY = targetY;
                return targetY;
            }

            if (!_hasVerticalFollowAnchor)
            {
                _verticalFollowAnchorTargetY = targetY;
                _hasVerticalFollowAnchor = true;
            }

            float deltaY = targetY - _verticalFollowAnchorTargetY;
            return _verticalFollowAnchorTargetY + deltaY * _verticalFollowInfluence;
        }

        private Vector3 ApplyDeadZone(Vector3 currentBasePosition, Vector3 targetPosition)
        {
            if (!_useDeadZone)
            {
                return targetPosition;
            }

            Vector3 resolvedPosition = currentBasePosition;

            if (_deadZone.x <= 0f)
            {
                resolvedPosition.x = targetPosition.x;
            }
            else
            {
                float deltaX = targetPosition.x - currentBasePosition.x;
                if (deltaX > _deadZone.x)
                {
                    resolvedPosition.x = targetPosition.x - _deadZone.x;
                }
                else if (deltaX < -_deadZone.x)
                {
                    resolvedPosition.x = targetPosition.x + _deadZone.x;
                }
            }

            if (_deadZone.y <= 0f)
            {
                resolvedPosition.y = targetPosition.y;
            }
            else
            {
                float deltaY = targetPosition.y - currentBasePosition.y;
                if (deltaY > _deadZone.y)
                {
                    resolvedPosition.y = targetPosition.y - _deadZone.y;
                }
                else if (deltaY < -_deadZone.y)
                {
                    resolvedPosition.y = targetPosition.y + _deadZone.y;
                }
            }

            resolvedPosition.z = targetPosition.z;
            return resolvedPosition;
        }

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
    }
}
