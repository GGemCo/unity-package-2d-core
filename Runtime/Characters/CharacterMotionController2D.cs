using UnityEngine;

namespace GGemCo2DCore
{
    /// <summary>
    /// 2D 캐릭터 공용 모션 이동 컨트롤러(전진/대시/러시 등).
    /// - 플레이어/몬스터 공용으로 사용할 수 있게 "입력"과 무관하게 동작합니다.
    /// - Rigidbody2D가 Kinematic이면 MovePosition 기반 이동을 권장합니다.
    /// - Distance 기반 + Easing 적용(시간축과 정밀 동기화).
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class CharacterMotionController2D : MonoBehaviour, ICharacterMotionController
    {
        [Header("References")]
        [SerializeField] private Rigidbody2D rb;

        private bool _isLunging;
        private Vector2 _dir;
        private float _duration;
        private float _elapsed;
        private float _distance;
        private float _movedDistance;
        private Easing.EaseType _easeType;
        private bool _stopAtEnd;
        private bool _useMovePosition;

        public bool IsLunging => _isLunging;

        private void Reset()
        {
            rb = GetComponentInParent<Rigidbody2D>();
        }

        private void Awake()
        {
            if (rb == null)
                rb = GetComponentInParent<Rigidbody2D>();
        }

        public bool TryStartLunge(in LungeRequest request)
        {
            if (rb == null) return false;
            if (request.DurationSeconds <= 0f || request.Distance <= 0f) return false;

            // 중복 러시 방지(필요 시 정책 확장 가능)
            if (_isLunging) return false;

            _isLunging = true;
            _dir = request.Direction;
            _duration = request.DurationSeconds;
            _elapsed = 0f;
            _distance = request.Distance;
            _movedDistance = 0f;
            _easeType = request.EaseType;
            _stopAtEnd = request.StopAtEnd;
            _useMovePosition = request.UseMovePosition;

            return true;
        }

        public void CancelLunge(int reason = 0)
        {
            if (!_isLunging) return;

            _isLunging = false;
            _elapsed = 0f;
            _duration = 0f;

            // velocity 기반 구현을 사용하는 경우를 대비해 정지 정책 제공
            if (_stopAtEnd && rb != null && rb.bodyType == RigidbodyType2D.Dynamic)
            {
                rb.SetLinearVelocity(new Vector2(0f, rb.linearVelocity.y));
            }
        }

        private void FixedUpdate()
        {
            if (!_isLunging) return;
            if (rb == null)
            {
                _isLunging = false;
                return;
            }

            float dt = Time.fixedDeltaTime;
            _elapsed += dt;

            // 정규화 시간(0~1)
            float t = _duration <= 1e-6f ? 1f : Mathf.Clamp01(_elapsed / _duration);

            // Easing 적용(0~1)
            float eased = Easing.Apply(t, _easeType);

            // 목표 누적 이동거리
            float targetDistance = _distance * eased;

            // 이번 프레임 이동해야 할 거리(증분)
            float deltaDistance = targetDistance - _movedDistance;
            _movedDistance = targetDistance;

            // 프레임 독립 이동량
            Vector2 delta = _dir * deltaDistance;

            // Kinematic: MovePosition 권장
            if (_useMovePosition && rb.bodyType == RigidbodyType2D.Kinematic)
            {
                rb.MovePosition(rb.position + delta);
            }
            else
            {
                // Dynamic 또는 정책상 velocity 사용
                // - Distance 기반이므로, 증분거리/시간으로 순간 속도를 산출한다.
                float vx = dt > 1e-6f ? (delta.x / dt) : 0f;
                rb.SetLinearVelocity(new Vector2(vx, rb.linearVelocity.y));
            }

            if (t >= 1f)
            {
                _isLunging = false;

                if (_stopAtEnd && rb.bodyType == RigidbodyType2D.Dynamic)
                {
                    rb.SetLinearVelocity(new Vector2(0f, rb.linearVelocity.y));
                }
            }
        }
    }
}
