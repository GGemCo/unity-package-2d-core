using System.Collections;
using UnityEngine;
using Event = UnityEngine.Event;

namespace GGemCo2DCore
{
    /// <summary>
    /// 몬스터 선공, 후공 처리 
    /// </summary>
    public class ControllerMonster : CharacterBaseController, IMonsterCombatDriver, IMonsterBrainSuspendProvider
    {
        private const float MoveDirectionEpsilonSqr = 0.000001f;

        private Coroutine _coroutineAttack;
        private float _delayTimeAttack;
        private Monster _monster;
        private Collider2D[] _collider2Ds;

        #region IMonsterCombatDriver

        /// <inheritdoc />
        public bool IsAggro => targetCharacter != null && targetCharacter.IsAggro();

        /// <inheritdoc />
        public bool IsDead => targetCharacter != null && targetCharacter.IsStatusDead();

        /// <inheritdoc />
        public float HpPercent
        {
            get
            {
                if (targetCharacter == null) return 0f;
                float max = Mathf.Max(1f, targetCharacter.BaseHp);
                return Mathf.Clamp01(targetCharacter.CurrentHp.Value / max);
            }
        }

        /// <inheritdoc />
        public bool TryGetTarget(out Transform target)
        {
            target = targetCharacter != null ? targetCharacter.attackerTransform : null;
            return target != null;
        }

        /// <inheritdoc />
        public bool IsTargetInAttackRange() => SearchAttackerTarget();

        /// <inheritdoc />
        public void RequestWait() => Wait();

        /// <summary>
        /// 이동 요청을 fire-and-forget 방식으로 전달한다.
        /// </summary>
        /// <remarks>
        /// 거부 사유가 필요한 호출부는 <see cref="TryRequestMove"/>를 사용한다.
        /// </remarks>
        public void RequestMove(Vector2 direction)
        {
            _ = TryRequestMove(direction, out _);
        }

        /// <summary>
        /// 이동 요청을 수행하고, 거부 시 원인 코드를 반환한다.
        /// </summary>
        /// <param name="direction">월드 기준 이동 방향 벡터.</param>
        /// <param name="failureReason">거부 사유 코드.</param>
        /// <returns>이동이 실제로 수행되면 true, 아니면 false.</returns>
        public bool TryRequestMove(Vector2 direction, out MonsterMoveRequestFailureReason failureReason)
        {
            failureReason = MonsterMoveRequestFailureReason.None;

            if (targetCharacter == null)
            {
                failureReason = MonsterMoveRequestFailureReason.CharacterMissing;
                return false;
            }

            if (direction.sqrMagnitude <= MoveDirectionEpsilonSqr)
            {
                failureReason = MonsterMoveRequestFailureReason.ZeroDirection;
                return false;
            }

            Vector2 filteredDirection = GetFilteredDirection(direction);
            if (filteredDirection.sqrMagnitude <= MoveDirectionEpsilonSqr)
            {
                failureReason = MonsterMoveRequestFailureReason.AxisLocked;
                return false;
            }

            if (targetCharacter.IsStatusDontMove())
            {
                failureReason = MonsterMoveRequestFailureReason.StatusDontMove;
                return false;
            }

            if (targetCharacter.IsStatusAttack())
            {
                failureReason = MonsterMoveRequestFailureReason.StatusAttack;
                return false;
            }

            if (targetCharacter.IsStatusDead())
            {
                failureReason = MonsterMoveRequestFailureReason.StatusDead;
                return false;
            }

            float speed = targetCharacter.currentMoveStep * targetCharacter.GetCurrentMoveSpeed();
            if (speed <= 0f)
            {
                failureReason = MonsterMoveRequestFailureReason.SpeedNonPositive;
                return false;
            }

            targetCharacter.directionNormalize = filteredDirection;

            if (!Run())
            {
                // 상태 전환 타이밍으로 Run이 직전에 거부될 수 있어, 거부 사유를 재평가한다.
                if (!TryResolveMoveFailureReason(direction, filteredDirection, out failureReason))
                    failureReason = MonsterMoveRequestFailureReason.Unknown;
                return false;
            }

            return true;
        }

        /// <inheritdoc />
        public void RequestFaceToTarget()
        {
            if (targetCharacter == null || targetCharacter.attackerTransform == null) return;
            var raw = (targetCharacter.attackerTransform.position - targetCharacter.transform.position);
            var dir = GetFilteredDirection(raw);
            if (dir == Vector2.zero) return;

            // 플랫포머: X 기준
            if (Mathf.Abs(dir.x) > 0.0001f)
            {
                var facing = dir.x >= 0f ? CharacterConstants.FacingDirection8.Right : CharacterConstants.FacingDirection8.Left;
                targetCharacter.SetFacing(facing);
            }
            else
            {
                CharacterConstants.FacingDirection8 facing = CharacterConstants.ToFacingDirection8(dir);
                targetCharacter.SetFacing(facing);
            }
        }

        /// <inheritdoc />
        public void RequestAttackOnce() => Attack();

        public void RequestClearAggro()
        {
            targetCharacter.SetAggro(false);
        }

        #endregion

        #region IMonsterBrainSuspendProvider

        /// <inheritdoc />
        public bool ShouldSuspendBrain =>
            targetCharacter != null &&
            (targetCharacter.IsStatusDead() ||
             targetCharacter.IsDeathPending ||
             targetCharacter.IsBrainLocked() ||
             targetCharacter.IsDontControl() ||
             targetCharacter.IsStatusDamage());

        #endregion

        protected override void Awake()
        {
            base.Awake();
            _monster = targetCharacter as Monster;

            EnsureLegacyBrain();
        }

        protected override void Start()
        {
            base.Start();
            if (_delayTimeAttack <= 0)
            {
                if (iCharacterAnimationController == null) return;
                _delayTimeAttack =
                    iCharacterAnimationController.GetCharacterAnimationDuration(
                        ICharacterAnimationController.AttackAnim, false);
            }
        }

        /// <summary>
        /// 레거시 Brain이 존재하지 않고, 외부 Brain도 아직 붙지 않은 경우 기본 레거시 Brain을 보장한다.
        /// </summary>
        private void EnsureLegacyBrain()
        {
            // BT 러너는 런타임에 AddComponent 될 수 있으므로,
            // 여기서는 "없으면 추가"만 수행하고, 실제 실행은 MonsterBrainSelector 우선순위로 결정한다.
            if (GetComponent<IMonsterBrain>() == null)
            {
                gameObject.AddComponent<MonsterLegacyBrain>();
            }

            // Brain 틱은 중앙 틱커가 담당한다.
            if (GetComponent<MonsterBrainTicker>() == null)
            {
                gameObject.AddComponent<MonsterBrainTicker>();
            }

        }public void Initialize(Collider2D[] collider2Ds)
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

        /// <summary>
        /// 레거시(비-BT) 몬스터 AI 의사결정 틱.
        /// </summary>
        /// <remarks>
        /// - Unity Update/FixedUpdate는 <see cref="MonsterLegacyBrain"/>이 담당한다.
        /// - 외부 Brain(BT 등)이 붙으면 우선순위에 의해 본 틱은 호출되지 않는다.
        /// </remarks>
        internal void TickLegacy()
        {
            if (!CheckPossibleControl())
            {
                StopAttackCoroutine();
                return;
            }

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
        /// 몬스터가 공격/공격 루프를 시작할 수 있는 상태인지 확인한다.
        /// </summary>
        private bool CanStartAttackAction()
        {
            if (targetCharacter == null) return false;
            if (targetCharacter.IsStatusDead()) return false;
            if (targetCharacter.IsStatusAttack()) return false;
            if (targetCharacter.IsDontControl()) return false;
            if (targetCharacter.IsStatusDamage()) return false;
            if (targetCharacter.IsStatusMoveForce()) return false;
            return true;
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

        /// <summary>
        /// 이동 거부 사유를 상태/입력 기준으로 재평가한다.
        /// </summary>
        /// <param name="rawDirection">요청된 원본 방향.</param>
        /// <param name="filteredDirection">축 제한이 반영된 방향.</param>
        /// <param name="failureReason">재평가된 거부 사유.</param>
        /// <returns>거부 사유를 특정했으면 true, 특정하지 못했으면 false.</returns>
        private bool TryResolveMoveFailureReason(
            Vector2 rawDirection,
            Vector2 filteredDirection,
            out MonsterMoveRequestFailureReason failureReason)
        {
            failureReason = MonsterMoveRequestFailureReason.None;

            if (targetCharacter == null)
            {
                failureReason = MonsterMoveRequestFailureReason.CharacterMissing;
                return true;
            }

            if (rawDirection.sqrMagnitude <= MoveDirectionEpsilonSqr)
            {
                failureReason = MonsterMoveRequestFailureReason.ZeroDirection;
                return true;
            }

            if (filteredDirection.sqrMagnitude <= MoveDirectionEpsilonSqr)
            {
                failureReason = MonsterMoveRequestFailureReason.AxisLocked;
                return true;
            }

            if (targetCharacter.IsStatusDontMove())
            {
                failureReason = MonsterMoveRequestFailureReason.StatusDontMove;
                return true;
            }

            if (targetCharacter.IsStatusAttack())
            {
                failureReason = MonsterMoveRequestFailureReason.StatusAttack;
                return true;
            }

            if (targetCharacter.IsStatusDead())
            {
                failureReason = MonsterMoveRequestFailureReason.StatusDead;
                return true;
            }

            float speed = targetCharacter.currentMoveStep * targetCharacter.GetCurrentMoveSpeed();
            if (speed <= 0f)
            {
                failureReason = MonsterMoveRequestFailureReason.SpeedNonPositive;
                return true;
            }

            return false;
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
            
            // 4) 이동 벡터 계산
            float speed = targetCharacter.currentMoveStep * targetCharacter.GetCurrentMoveSpeed();
            if (speed <= 0) return false;
            
            iCharacterAnimationController?.PlayRunAnimation();
            
            // 5) 경계 업데이트
            UpdateCheckMaxBounds();

            // GcLogger.Log($"dir: {dir}, step: {targetCharacter.currentMoveStep}, speed: {targetCharacter.GetCurrentMoveSpeed()}, total speed: {speed}");
            Vector3 delta = dir * (speed * Time.deltaTime);
            
            // 6) 다음 위치
            Vector3 cur  = targetCharacter.transform.position;
            Vector3 next = cur + delta;

            // 7) 경계 클램프
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

            // ContactFilter2D.noFilter 사용 (필요하면 레이어/트리거 정책을 별도 생성해서 전달)
            int hitCount = CompatPhysics2D.OverlapCapsuleNonAlloc(
                point, size, capsuleDirection2D, 0f,
                _collider2Ds);
            
            for (int i = 0; i < hitCount; i++)
            {
                Collider2D hit = _collider2Ds[i];
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
                yield return new WaitForSeconds(_delayTimeAttack);
            }
        }
        /// <summary>
        /// 공격 실행
        /// </summary>
        public override void Attack()
        {
            // 공격자가 죽었을 때
            if (targetCharacter.IsAttackerStatusDead())
            {
                targetCharacter.SetAttackerTarget(null);
                StopAttackCoroutine();
                Stop();
                return;
            }
            if (!CanStartAttackAction()) return;

            // 공격자 방향 찾기
            HandleInput();
            CharacterConstants.FacingDirection8 facing = CharacterConstants.ToFacingDirection8(targetCharacter.directionNormalize);
            targetCharacter.SetFacing(facing);
            
            targetCharacter.SetStatusAttack();
            iCharacterAnimationController?.PlayAttackAnimation();
        }
        /// <summary>
        /// 공격하기 코루틴 시작
        /// </summary>
        private void StartAttackCoroutine()
        {
            if (_coroutineAttack != null) return;
            if (!CanStartAttackAction()) return;

            _coroutineAttack = StartCoroutine(DownAttackByTime());
        }
        /// <summary>
        /// 공격하기 코루틴 정지
        /// </summary>
        public void StopAttackCoroutine()
        {
            if (_coroutineAttack == null) return;

            StopCoroutine(_coroutineAttack);
            _coroutineAttack = null;
        }
        /// <summary>
        /// 어그로 on 이고 공격자 transform 이 있을때 플레이어가 몬스터 가까이 가면 attack 상태 처리
        /// </summary>
        /// <param name="collision"></param>
        private void OnTriggerEnter2D(Collider2D collision)
        {
            if (!MonsterBrainSelector.TryGetHighestActiveBrain(gameObject, out var brain)) return;
            brain.OnCharacterTriggerEnter(collision);
        }
        /// <summary>
        /// 몬스터 공격 범위 밖으로 플레이어가 나가면 공격 상태 취소하기
        /// </summary>
        /// <param name="collision"></param>
        private void OnTriggerExit2D(Collider2D collision)
        {
            if (!MonsterBrainSelector.TryGetHighestActiveBrain(gameObject, out var brain)) return;
            brain.OnCharacterTriggerExit(collision);
        }

        protected void OnSpineEventShake(Event @event)
        {
        }
    }
}
