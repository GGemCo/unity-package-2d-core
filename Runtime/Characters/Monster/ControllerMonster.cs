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
        private const float BtMoveDirectionBlendPerSecond = 0.000001f;
        private const float BtMoveIntentKeepAliveSeconds = 5f;

        private Coroutine _coroutineAttack;
        private float _delayTimeAttack;
        private Monster _monster;
        private Collider2D[] _collider2Ds;
        private bool _hasBtMoveIntent;
        private Vector2 _btMoveIntentDirection;
        private Vector2 _btSmoothedDirection;
        private float _lastBtMoveIntentTime;

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
        /// 이동 의도 등록/즉시 1회 이동 시도는 <see cref="TryRequestMove"/>에서 공통 처리한다.
        /// 거부 사유가 필요한 호출부는 <see cref="TryRequestMove"/>를 직접 사용한다.
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
        /// <remarks>
        /// BT가 저주기(예: 1Hz)로 평가되더라도 이동은 프레임 단위로 이어져야 하므로,
        /// 본 함수 진입 시 이동 의도를 먼저 등록해 <see cref="TickBtMoveIntent"/>가 연속 이동을 유지하도록 한다.
        /// </remarks>
        public bool TryRequestMove(Vector2 direction, out MonsterMoveRequestFailureReason failureReason)
        {
            failureReason = MonsterMoveRequestFailureReason.None;
            RegisterBtMoveIntent(direction);

            if (targetCharacter == null)
            {
                failureReason = MonsterMoveRequestFailureReason.CharacterMissing;
                ClearBtMoveIntent();
                return false;
            }

            if (direction.sqrMagnitude <= MoveDirectionEpsilonSqr)
            {
                failureReason = MonsterMoveRequestFailureReason.ZeroDirection;
                ClearBtMoveIntent();
                return false;
            }

            Vector2 filteredDirection = GetFilteredDirection(direction);
            if (filteredDirection.sqrMagnitude <= MoveDirectionEpsilonSqr)
            {
                failureReason = MonsterMoveRequestFailureReason.AxisLocked;
                ClearBtMoveIntent();
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
                ClearBtMoveIntent();
                return false;
            }

            if (targetCharacter.IsStatusDead())
            {
                failureReason = MonsterMoveRequestFailureReason.StatusDead;
                ClearBtMoveIntent();
                return false;
            }

            float speed = targetCharacter.currentMoveStep * targetCharacter.GetCurrentMoveSpeed();
            if (speed <= 0f)
            {
                failureReason = MonsterMoveRequestFailureReason.SpeedNonPositive;
                ClearBtMoveIntent();
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
            ClearBtMoveIntent();
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
        public void RequestAttackOnce()
        {
            ClearBtMoveIntent();
            Attack();
        }

        public void RequestClearAggro()
        {
            targetCharacter.SetAggro(false);
            ClearBtMoveIntent();
        }

        #endregion

        #region IMonsterBrainSuspendProvider

        /// <summary>
        /// 몬스터 Brain(BT/레거시) 틱을 일시 정지해야 하는지 반환한다.
        /// </summary>
        /// <remarks>
        /// 컬링 복귀 직후 페이드 연출이 끝나기 전에는 AI 판단을 멈춰
        /// 시각적 등장 타이밍과 실제 전투 입력 타이밍을 일치시킨다.
        /// </remarks>
        public bool ShouldSuspendBrain =>
            targetCharacter != null &&
            (targetCharacter.IsStatusDead() ||
             targetCharacter.IsDeathPending ||
             targetCharacter.IsBrainLocked() ||
             targetCharacter.IsDontControl() ||
             targetCharacter.IsStatusDamage() ||
             targetCharacter.IsFading);

        #endregion

        protected override void Awake()
        {
            base.Awake();
            _monster = targetCharacter as Monster;
            ClearBtMoveIntent();

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

#if GGEMCO_2D_CONTROL
        private void FixedUpdate()
        {
            TickBtMoveIntent(Time.fixedDeltaTime);
        }
#else
        private void Update()
        {
            TickBtMoveIntent(Time.deltaTime);
        }
#endif

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

        }

        /// <summary>
        /// 공격 범위 판정에 사용할 충돌체 캐시를 초기화한다.
        /// </summary>
        /// <param name="collider2Ds">공격 범위 계산에 사용할 충돌체 배열.</param>
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
            ClearBtMoveIntent();
            return true;
        }

        /// <summary>
        /// BT에서 전달한 이동 의도를 등록한다.
        /// </summary>
        /// <param name="direction">월드 기준 이동 방향 벡터.</param>
        /// <remarks>
        /// BT 평가 주기가 낮아도 연속 이동이 유지되도록 마지막 의도와 입력 시각을 캐시한다.
        /// </remarks>
        private void RegisterBtMoveIntent(Vector2 direction)
        {
            _hasBtMoveIntent = true;
            _btMoveIntentDirection = direction;
            _lastBtMoveIntentTime = Time.time;
        }

        /// <summary>
        /// BT 이동 의도 캐시를 비운다.
        /// </summary>
        private void ClearBtMoveIntent()
        {
            _hasBtMoveIntent = false;
            _btMoveIntentDirection = Vector2.zero;
            _btSmoothedDirection = Vector2.zero;
            _lastBtMoveIntentTime = 0f;
        }

        /// <summary>
        /// BT 이동 의도를 프레임 단위로 소비하여 자연스럽게 연속 이동한다.
        /// </summary>
        /// <param name="deltaTime">현재 프레임 델타 타임.</param>
        /// <remarks>
        /// - BT는 "추적 의도"만 결정하고, 실제 이동 적용은 본 함수가 매 프레임 담당한다.
        /// - Brain 정지 조건(사망/락/피격/페이드 등)에 진입하면 이동 의도를 즉시 폐기한다.
        /// - 공격 범위 진입 시 즉시 이동을 멈춰 다음 BT 틱에서 공격 분기로 자연스럽게 전환되게 한다.
        /// </remarks>
        private void TickBtMoveIntent(float deltaTime)
        {
            if (!_hasBtMoveIntent)
                return;

            if (targetCharacter == null)
            {
                ClearBtMoveIntent();
                return;
            }

            if (ShouldSuspendBrain)
            {
                Wait();
                ClearBtMoveIntent();
                return;
            }

            if (Time.time - _lastBtMoveIntentTime > BtMoveIntentKeepAliveSeconds)
            {
                ClearBtMoveIntent();
                return;
            }

            if (SearchAttackerTarget())
            {
                Wait();
                return;
            }

            Vector2 filteredDirection = GetFilteredDirection(_btMoveIntentDirection);
            if (filteredDirection.sqrMagnitude <= MoveDirectionEpsilonSqr)
            {
                ClearBtMoveIntent();
                return;
            }

            float maxDelta = Mathf.Max(0f, BtMoveDirectionBlendPerSecond * deltaTime);
            _btSmoothedDirection = Vector2.MoveTowards(_btSmoothedDirection, filteredDirection, maxDelta);

            Vector2 moveDirection = _btSmoothedDirection.sqrMagnitude > MoveDirectionEpsilonSqr
                ? _btSmoothedDirection.normalized
                : filteredDirection;

            targetCharacter.directionNormalize = moveDirection;

            if (!Run() &&
                (targetCharacter.IsStatusAttack() || targetCharacter.IsStatusDead() || targetCharacter.IsStatusDontMove()))
            {
                ClearBtMoveIntent();
            }
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

        /// <summary>
        /// 몬스터 축 이동 제한 설정을 반영해 입력 방향을 정제한다.
        /// </summary>
        /// <param name="dir">정제 전 방향 벡터.</param>
        /// <returns>축 제한이 반영된 정규화 방향. 이동 불가 시 <see cref="Vector2.zero"/>.</returns>
        private Vector2 GetFilteredDirection(Vector2 dir)
        {
            if (!_monster.canMoveX) dir.x = 0f;
            if (!_monster.canMoveY) dir.y = 0f;
            return dir.sqrMagnitude > 0f ? dir.normalized : Vector2.zero;
        }
        /// <summary>
        /// 현재 방향/속도/상태를 기준으로 실제 이동 프레임을 처리한다.
        /// </summary>
        /// <returns>이동이 수락되어 처리되면 true, 상태/입력 조건으로 거부되면 false.</returns>
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

            // 9) 캐릭터 Body Collider 겹침 방지 정책을 적용한 뒤 실제 위치를 반영
            Vector3 requestedDelta = next - cur;
            targetCharacter.TryResolveCharacterBodyMove(requestedDelta, out Vector3 resolvedDelta);
            targetCharacter.transform.position = cur + resolvedDelta;
            
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
