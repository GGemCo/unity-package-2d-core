using UnityEngine;

namespace GGemCo2DCore
{
    /// <summary>
    /// 동행 캐릭터가 지정 대상과 일정 거리를 유지하며 따라가도록 처리하는 범용 컨트롤러입니다.
    /// 보스 전용 연출, 펫, 소환수, 안내 NPC 등에 재사용할 수 있도록 특정 게임 규칙은 포함하지 않습니다.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class CompanionFollowController : MonoBehaviour
    {
        [SerializeField] private CompanionFollowSettings settings = new();

        private Transform _target;
        private CharacterBase _owner;
        private Rigidbody2D _rigidbody2D;
        private bool _isFollowing;
        private bool _isMoving;

        /// <summary>
        /// 현재 따라가는 대상 Transform입니다.
        /// </summary>
        public Transform Target => _target;

        /// <summary>
        /// 추적 동작 활성 여부입니다.
        /// </summary>
        public bool IsFollowing => _isFollowing;

        /// <summary>
        /// 동행 컨트롤러가 부착된 캐릭터와 물리 컴포넌트를 캐시하고 설정값을 보정합니다.
        /// </summary>
        private void Awake()
        {
            _owner = GetComponent<CharacterBase>();
            _rigidbody2D = GetComponent<Rigidbody2D>();
            settings ??= new CompanionFollowSettings();
            settings.Normalize();
        }

        /// <summary>
        /// 동행 대상과 추적 설정을 바인딩하고 추적을 시작합니다.
        /// </summary>
        /// <param name="target">따라갈 대상입니다.</param>
        /// <param name="overrideSettings">런타임에서 덮어쓸 추적 설정입니다. null이면 인스펙터 설정을 사용합니다.</param>
        public void StartFollow(Transform target, CompanionFollowSettings overrideSettings = null)
        {
            _target = target;
            if (overrideSettings != null)
            {
                settings = overrideSettings;
            }

            settings ??= new CompanionFollowSettings();
            settings.Normalize();
            _isFollowing = _target != null;
            _isMoving = false;
        }

        /// <summary>
        /// 추적을 중단하고 캐릭터를 대기 상태로 전환합니다.
        /// </summary>
        public void StopFollow()
        {
            _isFollowing = false;
            _target = null;
            SetMoving(false);
        }

        /// <summary>
        /// 물리 갱신 주기에 맞춰 대상과의 거리 유지 이동을 처리합니다.
        /// </summary>
        private void FixedUpdate()
        {
            if (!_isFollowing || _target == null)
            {
                SetMoving(false);
                return;
            }

            Vector2 currentPosition = _rigidbody2D != null ? _rigidbody2D.position : (Vector2)transform.position;
            Vector2 desiredPosition = (Vector2)_target.position + settings.followOffset;
            Vector2 toDesired = desiredPosition - currentPosition;
            float distance = toDesired.magnitude;

            if (settings.teleportDistance > 0f && distance >= settings.teleportDistance)
            {
                MoveTo(desiredPosition);
                SetFacingByDirection(toDesired);
                SetMoving(false);
                return;
            }

            if (distance <= settings.minDistance)
            {
                SetMoving(false);
                return;
            }

            if (distance < settings.maxDistance)
            {
                SetMoving(false);
                return;
            }

            Vector2 nextPosition = Vector2.MoveTowards(
                currentPosition,
                desiredPosition,
                settings.moveSpeed * Time.fixedDeltaTime);

            MoveTo(nextPosition);
            SetFacingByDirection(toDesired);
            SetMoving(true);
        }

        /// <summary>
        /// Rigidbody2D가 있으면 물리 업데이트 경로로, 없으면 Transform 경로로 위치를 적용합니다.
        /// </summary>
        /// <param name="position">이동할 월드 좌표입니다.</param>
        private void MoveTo(Vector2 position)
        {
            if (_rigidbody2D != null)
            {
                _rigidbody2D.MovePosition(position);
                return;
            }

            transform.position = new Vector3(position.x, position.y, transform.position.z);
        }

        /// <summary>
        /// 이동 방향을 기준으로 캐릭터 바라보기 방향을 갱신합니다.
        /// </summary>
        /// <param name="direction">이동 방향 벡터입니다.</param>
        private void SetFacingByDirection(Vector2 direction)
        {
            if (!settings.flipByMoveDirection || _owner == null || direction.sqrMagnitude <= 0.0001f)
            {
                return;
            }

            _owner.SetFacing(direction);
        }

        /// <summary>
        /// 이동 상태 변화에 따라 대기/이동 애니메이션을 전환합니다.
        /// </summary>
        /// <param name="moving">현재 이동 중이면 true입니다.</param>
        private void SetMoving(bool moving)
        {
            if (_isMoving == moving)
            {
                return;
            }

            _isMoving = moving;
            if (!settings.updateAnimation || _owner?.CharacterAnimationController == null)
            {
                return;
            }

            if (_isMoving)
            {
                _owner.CharacterAnimationController.PlayRunAnimation();
            }
            else
            {
                _owner.CharacterAnimationController.PlayWaitAnimation();
            }
        }
    }
}
