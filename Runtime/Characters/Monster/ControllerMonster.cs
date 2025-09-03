using System.Collections;
using UnityEngine;
using Event = UnityEngine.Event;

namespace GGemCo2DCore
{
    /// <summary>
    /// 몬스터 선공, 후공 처리 
    /// </summary>
    public class ControllerMonster : CharacterBaseController
    {
        private Coroutine coroutineAttack;
        private float delayTimeAttack;
        private Monster _monster;
        private Collider2D[] _collider2Ds;

        protected override void Awake()
        {
            base.Awake();
            _monster = targetCharacter as Monster;
        }

        protected override void Start()
        {
            base.Start();
            if (delayTimeAttack <= 0)
            {
                delayTimeAttack =
                    iCharacterAnimationController.GetCharacterAnimationDuration(
                        ICharacterAnimationController.AttackAnim, false);
            }
        }

        public void Initialize(Collider2D[] collider2Ds)
        {
            _collider2Ds = collider2Ds;
        }
        /// <summary>
        /// 입력 처리 - 공격자 방향 계산
        /// </summary>
        private void HandleInput()
        {
            if (!targetCharacter.IsAggro() || targetCharacter.attackerTransform == null ||
                targetCharacter.IsStatusDead()) return;
            var raw = (targetCharacter.attackerTransform.position - targetCharacter.transform.position);
            targetCharacter.directionNormalize = GetFilteredDirection(raw);
        }
#if GGEMCO_2D_CONTROL
        private void FixedUpdate()
#else
        private void Update()
#endif
        {
            if (!CheckPossibleControl()) return;
            
            if (targetCharacter.IsAggro())
            {
                if (SearchAttackerTarget())
                {
                    StartAttackCoroutine();
                }
                else
                {
                    HandleInput();
                    Run();
                }
            }
            else
            {
                Wait();
            }
        }
        /// <summary>
        /// Wait  
        /// </summary>
        /// <returns></returns>
        protected override bool Wait()
        {
            if (!base.Wait()) return false;
            StopAttackCoroutine();
            return true;
        }
        // 축 플래그에 따라 방향을 정제
        private Vector2 GetFilteredDirection(Vector2 dir)
        {
            if (!_monster.canMoveX) dir.x = 0f;
            if (!_monster.canMoveY) dir.y = 0f;
            return dir.sqrMagnitude > 0f ? dir.normalized : Vector2.zero;
        }
        /// <summary>
        /// run 
        /// </summary>
        public override bool Run()
        {
            if (targetCharacter.IsStatusDontMove()) return false;
            if (targetCharacter.IsStatusAttack()) return false;
            if (targetCharacter.IsStatusDead()) return false;
            
            // 1) 방향 (이미 HandleInput에서 정제되지만, 안전하게 한 번 더 보정)
            var dir = GetFilteredDirection(targetCharacter.directionNormalize);
            
            // 2) 정지 처리: 이동 축이 모두 막혔거나 입력이 0이면 대기
            if (dir == Vector2.zero)
            {
                return Wait();
            }
            // 3) 바라보는 방향(플랫포머: X 기준)
            if (Mathf.Abs(dir.x) > 0.0001f)
            {
                var facing = dir.x >= 0f ? CharacterConstants.FacingDirection8.Right
                    : CharacterConstants.FacingDirection8.Left;
                targetCharacter.SetFacing(facing);
            }
            
            iCharacterAnimationController?.PlayRunAnimation();
            
            // 4) 경계 업데이트
            UpdateCheckMaxBounds();
            
            // 5) 이동 벡터 계산
            float speed = targetCharacter.currentMoveStep * targetCharacter.GetCurrentMoveSpeed();
            Vector3 delta = (Vector3)(dir * (speed * Time.deltaTime));
            
            // 6) 다음 위치
            Vector3 cur  = targetCharacter.transform.position;
            Vector3 next = cur + delta;

            // 7) 경계 클램프
            next.x = Mathf.Clamp(next.x, minBounds.x, maxBounds.x);
            next.y = Mathf.Clamp(next.y, minBounds.y, maxBounds.y);
            
            // 8) Y 이동 금지 옵션일 때, 위치의 Y는 고정(중력 없이 이동하는 현재 구조에 적합)
            if (!_monster.canMoveX) next.x = cur.x;
            if (!_monster.canMoveY) next.y = cur.y;

            // 9) 실제 반영
            targetCharacter.transform.position = next;
            
            StopAttackCoroutine();
            return true;
        }
        /// <summary>
        /// 주위에서 공격자를 검색
        /// </summary>
        private bool SearchAttackerTarget()
        {
            if (targetCharacter.attackerTransform == null) return false;
            if (targetCharacter.IsStatusAttack() || targetCharacter.IsStatusDead()) return false;
            Vector2 size = new Vector2(capsuleColliderSize.x * Mathf.Abs(transform.localScale.x), capsuleColliderSize.y * transform.localScale.y);
            // 캡슐 콜라이더 2D와 충돌 중인 모든 콜라이더를 검색
            Vector2 point = (Vector2)transform.position + capsuleColliderOffset * transform.localScale;
#if UNITY_6000_0_OR_NEWER
            int hitCount = Physics2D.OverlapCapsule(point, size, capsuleDirection2D, 0f,
                new ContactFilter2D().NoFilter(), _collider2Ds);
            for (int i = 0; i < hitCount; i++)
            {
                Collider2D hit = _collider2Ds[i];
#else
            Physics2D.OverlapCapsuleNonAlloc(point, size, colliderCheckCharacter.direction, 0f, _collider2Ds);
            foreach (var hit in _collider2Ds)
            {
#endif
                if (hit.CompareTag(targetCharacter.attackerTransform.tag) && hit.GetComponent<CharacterHitArea>() != null)
                {
                    return true;
                }
            }
            return false;
        }
        /// <summary>
        /// DelayTimeAttack 시간 후에 공격하기
        /// </summary>
        /// <returns></returns>
        private IEnumerator DownAttackByTime()
        {
            while (true)
            {
                Attack();
                yield return new WaitForSeconds(delayTimeAttack);
            }
        }
        /// <summary>
        /// 공격 실행
        /// </summary>
        protected override void Attack()
        {
            // 공격자가 죽었을 때
            if (targetCharacter.IsAttackerStatusDead())
            {
                targetCharacter.SetAttackerTarget(null);
                Stop();
                return;
            }
            if (targetCharacter.IsStatusAttack() || targetCharacter.IsStatusDead() || targetCharacter.IsStatusKnockback()) return;

            // 공격자 방향 찾기
            HandleInput();
            CharacterConstants.FacingDirection8 facing = ToFacingDirection8(targetCharacter.directionNormalize);
            targetCharacter.SetFacing(facing);
            
            targetCharacter.SetStatusAttack();
            iCharacterAnimationController?.PlayAttackAnimation();
        }
        /// <summary>
        /// 공격하기 코루틴 시작
        /// </summary>
        private void StartAttackCoroutine()
        {
            if (coroutineAttack != null || targetCharacter.IsStatusAttack() || targetCharacter.IsStatusDead()
                || targetCharacter.IsStatusKnockback()
                ) return;

            coroutineAttack = StartCoroutine(DownAttackByTime());
        }
        /// <summary>
        /// 공격하기 코루틴 정지
        /// </summary>
        public void StopAttackCoroutine()
        {
            if (coroutineAttack == null) return;

            StopCoroutine(coroutineAttack);
            coroutineAttack = null;
        }
        /// <summary>
        /// 어그로 on 이고 공격자 transform 이 있을때 플레이어가 몬스터 가까이 가면 attack 상태 처리
        /// </summary>
        /// <param name="collision"></param>
        private void OnTriggerEnter2D(Collider2D collision)
        {
            if (collision.CompareTag(ConfigTags.GetValue(ConfigTags.Keys.Player)))
            {
                if (targetCharacter.IsStatusDead()) return;
                
                if (targetCharacter.IsAggro() && targetCharacter.attackerTransform != null)
                {
                    Attack();
                }
                // 선공
                else if (targetCharacter.GetAttackType() == CharacterConstants.AttackType.AggroFirst && targetCharacter.IsAggro() == false)
                {
                    targetCharacter.SetAggro(true);
                    targetCharacter.SetAttackerTarget(collision.gameObject.transform);
                }
            }
        }
        /// <summary>
        /// 몬스터 공격 범위 밖으로 플레이어가 나가면 공격 상태 취소하기
        /// </summary>
        /// <param name="collision"></param>
        private void OnTriggerExit2D(Collider2D collision)
        {
            if (collision.CompareTag(ConfigTags.GetValue(ConfigTags.Keys.Player)))
            {
                StopAttackCoroutine();
            }
        }

        protected void OnSpineEventShake(Event @event)
        {
        }
    }
}