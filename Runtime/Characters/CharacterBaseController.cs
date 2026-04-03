using UnityEngine;

namespace GGemCo2DCore
{
    public class CharacterBaseController : MonoBehaviour
    {
        public CharacterBase targetCharacter;
        protected ICharacterAnimationController iCharacterAnimationController;

        // Collider & Bounds
        protected Vector2 capsuleColliderOffset;
        protected Vector2 capsuleColliderSize;
        protected CapsuleDirection2D capsuleDirection2D;

        // 맵 경계(좌하단/우상단)
        protected Vector2 minBounds;
        protected Vector2 maxBounds;
        private Vector2 _mapSize;
        private MapManager _mapManager;

        // ---- 경계 체크 on/off (방향별) ----
        [Header("Boundary Limit Switches")]
        [Tooltip("왼쪽 경계 제한 활성화")] protected bool LimitLeft = true;
        [Tooltip("오른쪽 경계 제한 활성화")] protected bool LimitRight = true;
        [Tooltip("아래(바닥) 경계 제한 활성화")] protected bool LimitBottom = true;
        [Tooltip("위(천장) 경계 제한 활성화")] protected bool LimitTop = true;

        protected virtual void Awake()
        {
            targetCharacter = GetComponent<CharacterBase>();
            if (!AddressableLoaderSettings.Instance?.playerSettings) return;
            LimitLeft = AddressableLoaderSettings.Instance.playerSettings.limitBoundaryLeft;
            LimitRight = AddressableLoaderSettings.Instance.playerSettings.limitBoundaryRight;
            LimitBottom = AddressableLoaderSettings.Instance.playerSettings.limitBoundaryBottom;
            LimitTop = AddressableLoaderSettings.Instance.playerSettings.limitBoundaryTop;
        }

        protected virtual void Start()
        {
            // 타일맵 좌하단(0,0) 기준
            minBounds = new Vector2(0f, 0f);

            _mapManager = SceneGame.Instance != null ? SceneGame.Instance.mapManager : null;
            RefreshMapBounds(force: true);

            iCharacterAnimationController = targetCharacter.CharacterAnimationController;

            // Awake에서 콜라이더가 붙으므로 Start에서 참조
            capsuleColliderSize = Vector2.zero;
            if (targetCharacter != null && targetCharacter.colliderAttackRange != null)
            {
                capsuleColliderOffset   = targetCharacter.colliderAttackRange.offset;
                capsuleColliderSize     = targetCharacter.colliderAttackRange.size;
                capsuleDirection2D      = targetCharacter.colliderAttackRange.direction;
            }

            // 최초 1회 계산
            UpdateCheckMaxBounds();
        }


        protected bool RefreshMapBounds(bool force = false)
        {
            if (_mapManager == null)
            {
                _mapManager = SceneGame.Instance != null ? SceneGame.Instance.mapManager : null;
            }

            if (_mapManager == null)
            {
                if (force)
                {
                    _mapSize = Vector2.zero;
                }

                return false;
            }

            Vector2 latestMapSize = _mapManager.GetCurrentMapSize();
            if (!force && latestMapSize == Vector2.zero && _mapSize != Vector2.zero)
            {
                return true;
            }

            _mapSize = latestMapSize;
            return _mapSize != Vector2.zero;
        }

        /// <summary>
        /// 현재 맵 크기와 캐릭터 크기를 고려하여 유효한 움직임 경계 재계산
        /// </summary>
        protected void UpdateCheckMaxBounds()
        {
            if (targetCharacter.IsStatusDead()) return;

            RefreshMapBounds();
            if (_mapSize == Vector2.zero)
            {
                maxBounds = minBounds;
                return;
            }

            // 캐릭터 폭/높이(Flip 포함). Transform.localScale은 음수(Flip) 가능 → 절댓값 사용
            float characterWidth  = Mathf.Abs(targetCharacter.GetWidth()  * targetCharacter.transform.localScale.x);
            float characterHeight = Mathf.Abs(targetCharacter.GetHeight() * targetCharacter.transform.localScale.y);

            // 좌우: 반폭, 상단: 전체 높이만큼 여유(기존 로직 유지)
            float halfWidth = characterWidth * 0.5f;

            // minBounds.x는 반폭만큼 오른쪽으로, maxBounds.x는 맵 가로 - 반폭
            minBounds.x = halfWidth;
            maxBounds.x = _mapSize.x - halfWidth;

            // minBounds.y는 (0), maxBounds.y는 맵 세로 - 캐릭터 전체 높이
            // (기존 코드 유지: 발 위치가 맵 밖으로 나가지 않게 상단을 높이만큼 빼서 계산)
            maxBounds.y = _mapSize.y - characterHeight;

            // 방어적: 경계 역전 방지
            if (maxBounds.x < minBounds.x) maxBounds.x = minBounds.x;
            if (maxBounds.y < minBounds.y) maxBounds.y = minBounds.y;
        }

        protected virtual bool Wait()
        {
            if (targetCharacter.IsStatusAttack()) return false;
            if (targetCharacter.IsStatusDead()) return false;
            
            iCharacterAnimationController?.PlayWaitAnimation();
            return true;
        }

        /// <summary>
        /// run 애니메이션 및 이동(경계 체크 포함)
        /// </summary>
        public virtual bool Run()
        {
            if (targetCharacter.IsStatusDontMove() || targetCharacter.IsDontControl()) return false;
            if (targetCharacter.IsStatusAttack())   return false;
            if (targetCharacter.IsStatusDead())     return false;

            CharacterConstants.FacingDirection8 facing = CharacterConstants.ToFacingDirection8(targetCharacter.directionNormalize);
            targetCharacter.SetFacing(facing);

            if (!targetCharacter.IsStatusJump())
            {
                iCharacterAnimationController?.PlayRunAnimation();
            }

            UpdateCheckMaxBounds();

            // 이동량 계산
            Vector3 current = targetCharacter.transform.position;
            Vector3 delta   = targetCharacter.directionNormalize * (targetCharacter.currentMoveStep * targetCharacter.GetCurrentMoveSpeed() * Time.deltaTime);
            Vector3 next    = current + delta;

            // ---- 방향 개별 on/off를 반영한 경계 Clamp ----
            next.x = ClampAxisWithSides(
                value: next.x,
                minEnabled: LimitLeft,
                minValue:   minBounds.x,
                maxEnabled: LimitRight,
                maxValue:   maxBounds.x
            );

            next.y = ClampAxisWithSides(
                value: next.y,
                minEnabled: LimitBottom,
                minValue:   minBounds.y,
                maxEnabled: LimitTop,
                maxValue:   maxBounds.y
            );

            targetCharacter.transform.position = next;
            return true;
        }

        /// <summary>
        /// 경계(최소/최대)를 방향별 스위치에 따라 선택적으로 적용
        /// </summary>
        protected static float ClampAxisWithSides(float value, bool minEnabled, float minValue, bool maxEnabled, float maxValue)
        {
            // min/max 모두 off → 제한 없음
            if (!minEnabled && !maxEnabled) return value;

            // min만 on → 하한만 적용
            if (minEnabled && !maxEnabled)
                return Mathf.Max(value, minValue);

            // max만 on → 상한만 적용
            if (!minEnabled && maxEnabled)
                return Mathf.Min(value, maxValue);

            // 둘 다 on → 일반 Clamp
            return Mathf.Clamp(value, minValue, maxValue);
        }

        /// <summary>
        /// 공격 실행
        /// </summary>
        public virtual void Attack()
        {
            if (targetCharacter.IsStatusAttack() || targetCharacter.IsStatusDead()) return;

            CharacterConstants.FacingDirection8 facing = CharacterConstants.ToFacingDirection8(targetCharacter.directionNormalize);
            targetCharacter.SetFacing(facing);

            targetCharacter.SetStatusAttack();
            iCharacterAnimationController?.PlayAttackAnimation();
        }

        /// <summary>
        /// 모든 행동을 멈추고 wait 애니메이션 실행
        /// </summary>
        protected virtual void Stop()
        {
            targetCharacter.SetAttackerTarget(null);
            targetCharacter.SetAggro(false);
            targetCharacter.SetStatusIdle();
            iCharacterAnimationController?.PlayWaitAnimation();
        }

        /// <summary>
        /// 맵 크기가 바뀔 때 호출
        /// </summary>
        public void ChangeMapSize(Vector2 newMapSize)
        {
            _mapSize = newMapSize;
            UpdateCheckMaxBounds();
        }

        /// <summary>
        /// 플레이어, 몬스터 조작 가능한지 체크
        /// </summary>
        protected bool CheckPossibleControl()
        {
            if (!_mapManager.IsStateComplete())   return false;
            if (targetCharacter.IsStatusMoveForce()) return false;
            if (targetCharacter.IsStatusDead())      return false;
            if (targetCharacter.IsStatusDamage())    return false;
            if (targetCharacter.IsDontControl()) return false;
            return true;
        }
    }
}
