using System.Collections.Generic;
using UnityEngine;

namespace GGemCo2DCore
{
    /// <summary>
    /// 모든 발사체의 공통 로직을 담당하는 베이스 클래스.
    /// - 초기화(테이블/런타임 파라미터/콜라이더/Visual)
    /// - 발사(좌표/오브젝트)
    /// - 진행률/회전/화면이탈 처리
    /// - 충돌 및 데미지 처리(시각 표현과 분리)
    /// </summary>
    public abstract class ProjectileBase : MonoBehaviour
    {
        // ---- Static (Table) ----
        protected StruckTableProjectile Info;

        // ---- Runtime (Dynamic) ----
        protected MetadataProjectile Runtime;

        // ---- Movement ----
        protected float Speed;
        protected float JourneyLength;
        protected float StartTime;
        protected Vector2 StartPoint;
        protected Vector2 TargetPoint;
        protected CharacterBase TargetObject; // Fixed 타겟용(발사 시점에 좌표 스냅)
        protected Vector2 PrevPos;
        protected Vector2 Direction;
        protected bool Initialized;

        // ---- Combat ----
        protected long Damage;
        protected ConfigCommon.DamageType DamageType;
        protected CharacterBase FromCharacter;
        protected int SkillUid;
        protected int AttackId;

        // ---- Visual ----
        private IProjectileVisual _visual;
        private SoundPlaybackHandle _flightSoundHandle;

        // ---- Collision Sweep (anti-tunneling) ----
        private CapsuleCollider2D _hitCollider;
        private ContactFilter2D _castFilter;
        private RaycastHit2D[] _castResults;
        private Collider2D[] _overlapResults;
        private readonly HashSet<CharacterBase> _latchedHitTargets = new();
        private readonly HashSet<CharacterBase> _currentOverlapTargets = new();
        private readonly List<CharacterBase> _releasedHitTargets = new();
        private readonly HashSet<Collider2D> _latchedEnvironmentHitColliders = new();
        private readonly HashSet<Collider2D> _currentOverlapEnvironmentHitColliders = new();
        private readonly List<Collider2D> _releasedEnvironmentHitColliders = new();
        private bool _isTerminatedByHit;
        private bool _isWaitingForEndVisual;

        /// <summary>
        /// 즉시 충돌 데미지 정책을 사용할지 여부입니다.
        /// - 기본 구현은 DamageApplyMode가 OnHit일 때 활성화됩니다.
        /// - 주기 데미지형 프로젝타일은 false를 반환하도록 재정의할 수 있습니다.
        /// </summary>
        protected virtual bool ShouldHandleImmediateCollisionDamage
            => EffectiveDamageApplyMode == ProjectileConstants.DamageApplyMode.OnHit;

        /// <summary>
        /// 화면 밖으로 나갔을 때 즉시 제거할지 여부입니다.
        /// - 경로 끝까지 유지해야 하는 발사체는 false를 반환해 카메라 밖 제거를 막을 수 있습니다.
        /// </summary>
        protected virtual bool ShouldDestroyWhenOutOfView => true;

        /// <summary>
        /// 현재 발사체의 최종 데미지 적용 방식을 반환합니다.
        /// - 런타임 오버라이드가 있으면 이를 우선 사용하고, 없으면 테이블 기본값을 사용합니다.
        /// </summary>
        protected ProjectileConstants.DamageApplyMode EffectiveDamageApplyMode
        {
            get
            {
                if (Runtime != null && Runtime.UseDamageApplyModeOverride)
                    return Runtime.DamageApplyModeOverride;

                return Info != null
                    ? Info.DamageApplyMode
                    : ProjectileConstants.DamageApplyMode.OnHit;
            }
        }

        /// <summary>
        /// 현재 발사체의 최종 타겟 충돌 생존 정책을 반환합니다.
        /// - 런타임 오버라이드가 있으면 이를 우선 사용하고, 없으면 즉시 제거를 기본값으로 사용합니다.
        /// </summary>
        protected ProjectileConstants.HitLifetimeMode EffectiveHitLifetimeMode
        {
            get
            {
                if (Runtime != null && Runtime.UseHitLifetimeModeOverride)
                    return Runtime.HitLifetimeModeOverride;

                return ProjectileConstants.HitLifetimeMode.DestroyOnTargetHit;
            }
        }

        /// <summary>
        /// 현재 발사체의 최종 도착 정책을 반환합니다.
        /// - 런타임 오버라이드가 있으면 이를 우선 사용합니다.
        /// - 오버라이드가 없으면 도착 시 제거를 기본값으로 사용합니다.
        /// </summary>
        protected ProjectileConstants.ArrivalPolicy EffectiveArrivalPolicy
        {
            get
            {
                if (Runtime != null && Runtime.UseArrivalPolicyOverride)
                    return Runtime.ArrivalPolicyOverride;

                return ProjectileConstants.ArrivalPolicy.DestroyOnArrived;
            }
        }

        /// <summary>
        /// 현재 발사체의 최종 환경 충돌 처리 정책을 반환합니다.
        /// - 런타임 오버라이드가 켜져 있으면 Skill 이벤트의 정책을 사용합니다.
        /// - 오버라이드가 없으면 기존 동작 보존을 위해 환경 충돌을 무시합니다.
        /// </summary>
        protected ProjectileConstants.EnvironmentHitPolicy EffectiveEnvironmentHitPolicy
        {
            get
            {
                if (Runtime != null && Runtime.UseEnvironmentHitPolicyOverride)
                    return Runtime.EnvironmentHitPolicyOverride;

                return ProjectileConstants.EnvironmentHitPolicy.Ignore;
            }
        }

        /// <summary>
        /// 환경 충돌 Hit VFX 정책을 실제로 처리해야 하는지 확인합니다.
        /// </summary>
        protected bool ShouldHandleEnvironmentHit
            => EffectiveEnvironmentHitPolicy != ProjectileConstants.EnvironmentHitPolicy.Ignore &&
               ResolveEnvironmentHitLayerMask() != 0;

        /// <summary>
        /// 현재 발사체의 최종 주기 데미지 간격(초)을 반환합니다.
        /// - 런타임 오버라이드가 있으면 이를 우선 사용하고, 없으면 테이블 기본값을 사용합니다.
        /// </summary>
        protected float EffectiveTickDamageInterval
        {
            get
            {
                if (Runtime != null && Runtime.UseTickDamageIntervalOverride)
                    return Mathf.Max(0f, Runtime.TickDamageIntervalOverride);

                return Info != null
                    ? Mathf.Max(0f, Info.TickDamageInterval)
                    : 0f;
            }
        }

        /// <summary>
        /// End Visual 재생 대기 중인지 확인합니다.
        /// </summary>
        protected bool IsWaitingForEndVisual => _isWaitingForEndVisual;

        #region Lifecycle
        /// <summary>
        /// 테이블(정적) + 메타데이터(동적)로 발사체를 초기화합니다.
        /// </summary>
        public virtual void Initialize(StruckTableProjectile info, MetadataProjectile metadata)
        {
            if (info == null)
            {
                Destroy(gameObject);
                return;
            }

            Info = info;
            Runtime = metadata;

            // 런타임 파라미터 반영
            FromCharacter = metadata != null ? metadata.Owner : null;
            Damage = metadata != null ? metadata.Damage : 0;
            DamageType = metadata != null ? metadata.DamageType : ConfigCommon.DamageType.Physic;
            SkillUid = metadata != null ? metadata.SkillUid : 0;
            AttackId = metadata != null ? metadata.AttackId : 0;

            float speedMul = metadata != null ? metadata.SpeedMultiplier : 1f;
            Speed = info.MoveSpeed * Mathf.Max(0.01f, speedMul);

            // Rigidbody2D: 스크립트 제어 이동을 위해 Kinematic.
            Rigidbody2D rb = ComponentController.AddRigidbody2D(gameObject);
            rb.bodyType = RigidbodyType2D.Kinematic;

            // Collider2D: Trigger 로 충돌 판정
            Vector2 size = (info.ColliderSize != Vector2.zero) ? info.ColliderSize : Vector2.zero;
            // ColliderOffset: Projectile 로컬 좌표 기준(회전/스케일에도 자연스럽게 따라감)
            Vector2 offset = info.ColliderOffset;
            _hitCollider = ComponentController.AddCapsuleCollider2D(gameObject, true, offset, size);
            // Cast 결과 버퍼(할당 최소화)
            if (_castResults == null) _castResults = new RaycastHit2D[16];
            if (_overlapResults == null) _overlapResults = new Collider2D[16];
            SetupCastFilter();

            _latchedHitTargets.Clear();
            _currentOverlapTargets.Clear();
            _releasedHitTargets.Clear();
            _latchedEnvironmentHitColliders.Clear();
            _currentOverlapEnvironmentHitColliders.Clear();
            _releasedEnvironmentHitColliders.Clear();
            _isTerminatedByHit = false;
            _isWaitingForEndVisual = false;

            // Visual 연결(Effect/Sprite/Animator/None)
            _visual = ProjectileVisualFactory.Attach(transform, info, metadata);
            _visual?.OnSpawn(new ProjectileVisualSpawnContext(transform, info, metadata));
        }

        protected virtual void Start()
        {
        }

        #endregion

        #region Launch

        protected void SetStartPoint()
        {
            StartPoint = transform.position;
            if (FromCharacter)
                StartPoint = FromCharacter.transform.position;

            if (Info.StartPosition != Vector2.zero)
                StartPoint += Info.StartPosition;
        }

        /// <summary>
        /// 좌표 타겟으로 발사합니다.
        /// - 공통 시작/목표 좌표를 먼저 계산합니다.
        /// - 파생 클래스가 시작 위치나 경로를 다시 보정하지 않는 기본 타입은 즉시 발사 완료 처리를 수행합니다.
        /// </summary>
        /// <param name="targetPos">발사체가 향할 목표 월드 좌표입니다.</param>
        public virtual void Launch(Vector2 targetPos)
        {
            TargetObject = null;
            PrepareLaunch(targetPos);
            CompleteLaunchAfterPositionResolved();
        }

        /// <summary>
        /// 발사 공통 좌표와 이동 값을 계산합니다.
        /// - 이 단계에서는 초기 Overlap 검사를 실행하지 않습니다.
        /// - Path/Segment 타입처럼 최종 시작 위치를 다시 계산하는 파생 클래스가 안전하게 재사용할 수 있습니다.
        /// </summary>
        /// <param name="targetPos">발사체가 향할 목표 월드 좌표입니다.</param>
        protected void PrepareLaunch(Vector2 targetPos)
        {
            SetStartPoint();

            TargetPoint = targetPos;
            JourneyLength = Vector2.Distance(StartPoint, TargetPoint);
            StartTime = Time.time;

            Direction = (TargetPoint - StartPoint).normalized;
            if (Direction.sqrMagnitude < 1e-6f)
                Direction = Vector2.right;

            transform.position = StartPoint;
            PrevPos = StartPoint;
            Initialized = false;
        }

        /// <summary>
        /// 파생 클래스의 최종 시작 위치/경로 보정이 끝난 뒤 발사를 완료합니다.
        /// - Transform 이동 직후 Physics2D 쿼리를 수행하기 전에 Collider 위치를 물리 엔진에 동기화합니다.
        /// - 생성 직후 월드 원점 또는 보정 전 시작점에서 InitialOverlap이 처리되는 문제를 방지합니다.
        /// </summary>
        /// <returns>초기 Overlap 처리 후에도 발사체가 계속 유효하면 true를 반환합니다.</returns>
        protected bool CompleteLaunchAfterPositionResolved()
        {
            transform.position = StartPoint;
            PrevPos = StartPoint;
            Initialized = true;

            if (_hitCollider != null)
                Physics2D.SyncTransforms();

            if (ShouldHandleImmediateCollisionDamage || ShouldHandleEnvironmentHit)
            {
                // 발사 직후 이미 타겟/환경 Collider와 겹쳐 있는 경우를 보정한다.
                // Trigger Enter / Cast는 "생성 시점의 초기 겹침"을 놓칠 수 있으므로,
                // 최종 시작 위치가 확정된 뒤 한 번 즉시 overlap 검사를 수행한다.
                TryHandleInitialOverlap();
            }

            if (!_isTerminatedByHit)
                StartFlightSound();

            return !_isTerminatedByHit;
        }

        /// <summary>
        /// 오브젝트(캐릭터)의 히트 영역을 기준으로 발사 (Fixed Target)
        /// </summary>
        public virtual void Launch(CharacterBase targetObj)
        {
            if (!targetObj)
            {
                GcLogger.LogWarning("[Projectile] Launch called with null target.");
                Destroy(gameObject);
                return;
            }

            TargetObject = targetObj;
            float y = targetObj.GetRandomPositionYInHitArea();
            Launch(new Vector2(targetObj.transform.position.x, y));
        }

        #endregion

        #region Update Loop

        protected virtual void FixedUpdate()
        {
            if (!Initialized || _isWaitingForEndVisual) return;

            RefreshImmediateHitLatchState();

            // 등속 이동: 현재까지 이동 거리 / 전체 거리 => 0..1
            float distCovered = (Time.fixedTime - StartTime) * Speed;
            float t = (JourneyLength > 0f) ? (distCovered / JourneyLength) : 1f;
            bool reachedRouteEnd = t >= 1f;
            bool continueAfterArrived = reachedRouteEnd && ShouldContinueAfterArrivedByPolicy();
            Vector2 newPos = continueAfterArrived
                ? ComputePositionAfterArrived()
                : ComputePosition(t);
            Vector2 delta = newPos - PrevPos;

            // Anti-tunneling: 이동 구간(PrevPos → newPos)을 스윕(Cast)으로 선검출합니다.
            if ((ShouldHandleImmediateCollisionDamage || ShouldHandleEnvironmentHit) && TrySweepHit(delta, out var sweepHit))
            {
                // centroid가 0일 수 있어 point가 유효하면 point를 사용합니다.
                Vector2 hitPos = sweepHit.point != Vector2.zero ? sweepHit.point : sweepHit.centroid;
                if (TryHandleHit(sweepHit.collider, hitPos))
                    return;
            }

            ApplyRotationByDelta(delta);

            PrevPos = newPos;
            transform.position = newPos;

            // Visual update (flip 등)
            UpdateVisual(newPos, delta);
            OnProjectileMoved(newPos, delta, t);

            if (reachedRouteEnd && !continueAfterArrived)
            {
                OnArrived();
                return;
            }

            if (ShouldDestroyWhenOutOfView && !IsInCameraView())
            {
                GcLogger.Log("[Projectile] Out of camera view. Destroy.");
                Destroy(gameObject);
            }
        }

        /// <summary>
        /// 파생 클래스가 구현: 진행률(t) → 위치
        /// </summary>
        protected abstract Vector2 ComputePosition(float t);

        /// <summary>
        /// 도착 처리 정책에 따라 종착점 도달 후에도 계속 이동해야 하는지 확인합니다.
        /// </summary>
        /// <returns>
        /// <see cref="ProjectileConstants.ArrivalPolicy.ContinueAfterArrived"/>가 설정되면
        /// <see langword="true"/>를 반환합니다.
        /// </returns>
        protected bool ShouldContinueAfterArrivedByPolicy()
        {
            return EffectiveArrivalPolicy == ProjectileConstants.ArrivalPolicy.ContinueAfterArrived;
        }

        /// <summary>
        /// 종착 지점 도달 이후의 다음 이동 위치를 계산합니다.
        /// - 마지막 진행 방향으로 고정 속도 이동을 이어갑니다.
        /// - 유효한 방향을 찾지 못하면 시작점→타겟점 방향을 사용하고,
        ///   그것도 불가능하면 기본값으로 우측 방향을 사용합니다.
        /// </summary>
        /// <returns>도착 이후 프레임에서 적용할 월드 좌표입니다.</returns>
        protected Vector2 ComputePositionAfterArrived()
        {
            Vector2 continueDirection = Direction;
            if (continueDirection.sqrMagnitude <= 1e-6f)
                continueDirection = (TargetPoint - StartPoint).normalized;

            if (continueDirection.sqrMagnitude <= 1e-6f)
                continueDirection = Vector2.right;

            return PrevPos + (continueDirection * (Speed * Time.fixedDeltaTime));
        }

        /// <summary>
        /// 프로젝타일이 한 스텝 이동한 직후 호출되는 확장 지점입니다.
        /// - Path 타입의 주기 데미지처럼 이동 후 처리가 필요한 파생 클래스가 사용합니다.
        /// </summary>
        /// <param name="newPos">이번 스텝에서 적용된 새 위치입니다.</param>
        /// <param name="delta">이전 위치에서 새 위치까지의 이동량입니다.</param>
        /// <param name="normalizedTime">전체 이동 기준 진행률입니다.</param>
        protected virtual void OnProjectileMoved(Vector2 newPos, Vector2 delta, float normalizedTime)
        {
            // 기본 구현: NOP
        }


        /// <summary>
        /// 발사체 충돌 쿼리에 사용할 ContactFilter2D를 구성합니다.
        /// - Physics2D Layer Collision Matrix 전체를 그대로 쓰지 않고, 현재 발사체가 실제로 조회해야 하는 목적별 레이어만 직접 조합합니다.
        /// - 데미지 대상은 시전자 진영 기준 HitArea 레이어로 제한하고, 환경 Hit는 별도 환경 레이어 마스크로만 추가합니다.
        /// </summary>
        private void SetupCastFilter()
        {
            int layerMask = ResolveProjectileQueryLayerMask();

            _castFilter = new ContactFilter2D
            {
                useTriggers = true,
                useLayerMask = true
            };
            _castFilter.SetLayerMask(layerMask);
        }

        /// <summary>
        /// 발사체가 실제 쿼리할 레이어 마스크를 반환합니다.
        /// - 데미지 적용 모드가 켜져 있으면 상대 진영 HitArea 레이어를 포함합니다.
        /// - 환경 Hit 정책이 켜져 있으면 Ground/Wall 또는 커스텀 환경 레이어를 포함합니다.
        /// </summary>
        /// <returns>Cast/Overlap에 사용할 최종 레이어 마스크입니다.</returns>
        private int ResolveProjectileQueryLayerMask()
        {
            int layerMask = 0;

            if (ShouldQueryDamageTargetLayers())
                layerMask |= ResolveDamageTargetLayerMask();

            if (ShouldHandleEnvironmentHit)
                layerMask |= ResolveEnvironmentHitLayerMask();

            return layerMask;
        }

        /// <summary>
        /// 데미지 대상 레이어를 쿼리해야 하는지 확인합니다.
        /// - OnHit은 즉시 충돌 판정에 필요합니다.
        /// - PeriodicOverlap은 Tick 시점의 Overlap 조회에 필요합니다.
        /// </summary>
        /// <returns>데미지 대상 HitArea 레이어를 필터에 포함해야 하면 true를 반환합니다.</returns>
        private bool ShouldQueryDamageTargetLayers()
        {
            return EffectiveDamageApplyMode != ProjectileConstants.DamageApplyMode.None;
        }

        /// <summary>
        /// 시전자 진영을 기준으로 데미지 대상 HitArea 레이어 마스크를 계산합니다.
        /// - 플레이어가 발사하면 몬스터 HitArea만 조회합니다.
        /// - 몬스터가 발사하면 플레이어 HitArea만 조회합니다.
        /// - 시전자를 알 수 없는 에디터 테스트 상황에서는 양쪽 HitArea를 모두 포함합니다.
        /// </summary>
        /// <returns>데미지 대상 후보 HitArea 레이어 마스크입니다.</returns>
        private int ResolveDamageTargetLayerMask()
        {
            bool fromMonster = FromCharacter && FromCharacter.CompareTag(ConfigTags.GetValue(ConfigTags.Keys.Monster));
            bool fromPlayer = FromCharacter && FromCharacter.CompareTag(ConfigTags.GetValue(ConfigTags.Keys.Player));

            if (fromMonster)
                return GetLayerMask(ConfigLayer.Keys.HitAreaPlayer);

            if (fromPlayer)
                return GetLayerMask(ConfigLayer.Keys.HitAreaMonster);

            return GetLayerMask(ConfigLayer.Keys.HitAreaPlayer) |
                   GetLayerMask(ConfigLayer.Keys.HitAreaMonster);
        }

        /// <summary>
        /// ConfigLayer 키에 대응하는 Unity 레이어 마스크를 반환합니다.
        /// - 레이어 이름이 비어 있거나 프로젝트에 없으면 0을 반환하여 안전하게 제외합니다.
        /// </summary>
        /// <param name="key">조회할 ConfigLayer 키입니다.</param>
        /// <returns>해당 레이어의 비트마스크입니다.</returns>
        private static int GetLayerMask(ConfigLayer.Keys key)
        {
            string layerName = ConfigLayer.GetValue(key);
            return string.IsNullOrEmpty(layerName) ? 0 : LayerMask.GetMask(layerName);
        }

        private bool TrySweepHit(Vector2 delta, out RaycastHit2D bestHit)
        {
            bestHit = default;

            if (_isTerminatedByHit) return false;
            if (_hitCollider == null) return false;

            float dist = delta.magnitude;
            if (dist <= 1e-6f) return false;

            Vector2 dir = delta / dist;

            int count = _hitCollider.Cast(dir, _castFilter, _castResults, dist);
            if (count <= 0) return false;

            float bestScore = float.PositiveInfinity;
            RaycastHit2D candidate = default;
            bool found = false;

            for (int i = 0; i < count; i++)
            {
                var hit = _castResults[i];
                var col = hit.collider;
                if (!col) continue;

                // 자기 자신(혹은 발사자)과의 히트는 무시
                if (_hitCollider != null && col == _hitCollider) continue;

                if (FromCharacter)
                {
                    var owner = col.GetComponentInParent<CharacterBase>();
                    if (owner == FromCharacter) continue;
                }

                if (!IsValidImmediateHitCandidate(col)) continue;

                // fraction이 유효하면 그것을, 아니면 centroid 기반 거리로 점수 계산
                float score;
                if (hit.fraction > 0f)
                {
                    score = hit.fraction;
                }
                else
                {
                    score = Vector2.Distance(PrevPos, hit.centroid);
                }

                if (score < bestScore)
                {
                    bestScore = score;
                    candidate = hit;
                    found = true;
                }
            }

            if (!found) return false;

            bestHit = candidate;
            return true;
        }

        private void TryHandleInitialOverlap()
        {
            if (_isTerminatedByHit) return;
            if (_hitCollider == null) return;
            if (_overlapResults == null || _overlapResults.Length == 0)
                _overlapResults = new Collider2D[16];

            int count = CompatPhysics2D.OverlapColliderNonAlloc(_hitCollider, _castFilter, _overlapResults);
            if (count <= 0) return;

            Collider2D bestCandidate = null;
            float bestDistance = float.PositiveInfinity;

            for (int i = 0; i < count; i++)
            {
                var col = _overlapResults[i];
                if (!col) continue;

                if (_hitCollider != null && col == _hitCollider)
                    continue;

                if (FromCharacter)
                {
                    var owner = col.GetComponentInParent<CharacterBase>();
                    if (owner == FromCharacter)
                        continue;
                }

                if (!IsValidImmediateHitCandidate(col))
                    continue;

                float distance = Vector2.Distance(StartPoint, col.ClosestPoint(StartPoint));
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    bestCandidate = col;
                }
            }

            if (!bestCandidate) return;

            Vector2 hitPos = bestCandidate.ClosestPoint(StartPoint);
            if ((hitPos - Vector2.zero).sqrMagnitude <= 1e-8f)
                hitPos = StartPoint;

            TryHandleHit(bestCandidate, hitPos);
        }

        /// <summary>
        /// 충돌 후보가 현재 발사체의 유효한 데미지 타겟인지 확인합니다.
        /// - 환경 Collider는 데미지 대상이 아니므로 별도 환경 히트 정책에서 처리합니다.
        /// - 캐릭터는 시전자와 대상의 태그 조합을 기준으로 판정합니다.
        /// </summary>
        /// <param name="other">판정할 Collider입니다.</param>
        /// <returns>유효한 데미지 타겟이면 true를 반환합니다.</returns>
        protected bool IsValidHitCandidate(Collider2D other)
        {
            if (!other) return false;

            if (!FromCharacter) return false;

            var target = ResolveTargetCharacter(other);
            if (!target) return false;

            bool fromMonster = FromCharacter.CompareTag(ConfigTags.GetValue(ConfigTags.Keys.Monster));
            bool fromPlayer = FromCharacter.CompareTag(ConfigTags.GetValue(ConfigTags.Keys.Player));
            bool toMonster = target.CompareTag(ConfigTags.GetValue(ConfigTags.Keys.Monster));
            bool toPlayer = target.CompareTag(ConfigTags.GetValue(ConfigTags.Keys.Player));

            return (fromMonster && toPlayer) || (fromPlayer && toMonster);
        }

        /// <summary>
        /// 충돌 Collider에서 실제 데미지를 받을 캐릭터를 찾습니다.
        /// - CharacterHitArea가 있으면 HitArea의 target을 우선 사용합니다.
        /// - 없으면 Collider가 붙은 오브젝트의 CharacterBase를 사용합니다.
        /// </summary>
        /// <param name="other">타겟을 찾을 Collider입니다.</param>
        /// <returns>해결된 타겟 캐릭터입니다. 없으면 null입니다.</returns>
        protected CharacterBase ResolveTargetCharacter(Collider2D other)
        {
            if (!other) return null;

            // HitArea가 있으면 해당 타겟을 우선 사용
            var hitArea = other.GetComponent<CharacterHitArea>();
            if (hitArea && hitArea.target) return hitArea.target;

            // 없으면 루트 CharacterBase로 판정
            return other.GetComponent<CharacterBase>();
        }

        /// <summary>
        /// 충돌 Collider에서 HitArea 정보를 찾습니다.
        /// - Collider에 직접 붙은 HitArea를 우선 사용하고, 없으면 타겟 루트에서 탐색합니다.
        /// </summary>
        /// <param name="other">충돌한 Collider입니다.</param>
        /// <param name="target">해결된 타겟 캐릭터입니다.</param>
        /// <returns>HitArea를 찾으면 반환하고, 없으면 null을 반환합니다.</returns>
        protected CharacterHitArea ResolveHitArea(Collider2D other, CharacterBase target)
        {
            if (!other) return null;

            // 충돌한 콜라이더가 HitArea 하위일 수 있으므로 Parent 우선
            var area = other.GetComponent<CharacterHitArea>();
            if (area) return area;

            // 루트에서 탐색(레거시 호환)
            if (target)
                return target.GetComponent<CharacterHitArea>();

            return null;
        }

        /// <summary>
        /// 즉시 충돌 데미지 정책에 따라 충돌 대상을 처리합니다.
        /// - OnHit 모드에서는 타겟에게 데미지를 적용하고, 완전히 이탈하기 전까지는 같은 타겟 재적중을 잠급니다.
        /// - HitLifetimeMode가 DestroyOnTargetHit이면 데미지 적용 후 즉시 제거합니다.
        /// - KeepUntilRouteEnd이면 데미지를 준 뒤 계속 이동합니다.
        /// </summary>
        /// <param name="other">충돌한 Collider입니다.</param>
        /// <param name="overrideWorldPos">스윕 적중 시 사용할 월드 적중 위치입니다.</param>
        /// <returns>발사체가 종료되어 현재 스텝을 중단해야 하면 true를 반환합니다.</returns>
        private bool TryHandleHit(Collider2D other, Vector2? overrideWorldPos = null)
        {
            if (_isTerminatedByHit)
                return true;

            if (!other)
                return false;

            Vector2 hitWorldPos = overrideWorldPos ?? (Vector2)transform.position;

            if (TryHandleEnvironmentHit(other, hitWorldPos))
                return true;

            if (!ShouldHandleImmediateCollisionDamage)
                return false;

            if (!TryResolveDamageTarget(other, out CharacterBase target))
                return false;

            if (!CanApplyImmediateHitToTarget(target))
                return false;

            CharacterHitArea area = ResolveHitArea(other, target);
            MarkImmediateHitTarget(target);
            OnHitTarget(area, other, target, hitWorldPos);

            if (!ShouldTerminateOnImmediateHit(other))
                return false;

            _isTerminatedByHit = true;
            Destroy(gameObject);
            return true;
        }

        /// <summary>
        /// 동일 타겟에게 즉시 충돌 데미지를 다시 적용할 수 있는지 확인합니다.
        /// - 같은 타겟의 여러 HitArea와 겹치더라도, 완전히 이탈하기 전까지는 1회만 허용합니다.
        /// </summary>
        /// <param name="target">판정할 대상 캐릭터입니다.</param>
        /// <returns>이번 충돌에서 데미지를 적용할 수 있으면 true를 반환합니다.</returns>
        private bool CanApplyImmediateHitToTarget(CharacterBase target)
        {
            return target && !_latchedHitTargets.Contains(target);
        }

        /// <summary>
        /// 즉시 충돌 데미지를 준 타겟을 현재 겹침 잠금 상태로 등록합니다.
        /// - 타겟의 모든 HitArea에서 완전히 이탈하면 잠금을 해제하여 재적중을 허용합니다.
        /// </summary>
        /// <param name="target">잠글 대상 캐릭터입니다.</param>
        private void MarkImmediateHitTarget(CharacterBase target)
        {
            if (!target)
                return;

            _latchedHitTargets.Add(target);
        }

        /// <summary>
        /// 현재 충돌 후보가 즉시 처리 대상으로 유효한지 확인합니다.
        /// - 환경 Collider는 환경 Hit VFX 정책이 켜져 있고, 아직 겹침 잠금 상태가 아닐 때 유효합니다.
        /// - 타겟은 즉시 충돌 데미지 정책이 켜져 있고, 팀 판정과 재적중 잠금 상태를 통과해야 합니다.
        /// </summary>
        /// <param name="other">판정할 Collider입니다.</param>
        /// <returns>즉시 충돌 처리를 진행할 수 있으면 true를 반환합니다.</returns>
        private bool IsValidImmediateHitCandidate(Collider2D other)
        {
            if (!other)
                return false;

            if (IsEnvironmentHitCollider(other))
                return ShouldHandleEnvironmentHit && CanPlayEnvironmentHit(other);

            if (!ShouldHandleImmediateCollisionDamage)
                return false;

            if (!TryResolveDamageTarget(other, out CharacterBase target))
                return false;

            return CanApplyImmediateHitToTarget(target);
        }

        /// <summary>
        /// 환경 Collider 충돌에 따른 Hit VFX와 수명 정책을 처리합니다.
        /// - 환경 충돌은 데미지를 적용하지 않고 ProjectileVisual의 Hit 콜백만 실행합니다.
        /// - 같은 환경 Collider와 계속 겹쳐 있는 동안에는 중복 Hit VFX를 출력하지 않습니다.
        /// </summary>
        /// <param name="other">충돌한 환경 Collider입니다.</param>
        /// <param name="hitWorldPos">Hit VFX를 출력할 월드 좌표입니다.</param>
        /// <returns>발사체가 종료되어 현재 스텝을 중단해야 하면 true를 반환합니다.</returns>
        private bool TryHandleEnvironmentHit(Collider2D other, Vector2 hitWorldPos)
        {
            if (!ShouldHandleEnvironmentHit || !IsEnvironmentHitCollider(other))
                return false;

            if (!CanPlayEnvironmentHit(other))
                return false;

            MarkEnvironmentHitCollider(other);
            NotifyHitVisual(other, hitWorldPos);

            ProjectileConstants.EnvironmentHitPolicy policy = EffectiveEnvironmentHitPolicy;
            bool shouldTerminate = policy == ProjectileConstants.EnvironmentHitPolicy.PlayHitVisualAndDestroy ||
                                   (policy == ProjectileConstants.EnvironmentHitPolicy.PlayHitVisualAndFollowHitLifetime &&
                                    ShouldTerminateOnImmediateHit(other));

            if (!shouldTerminate)
                return false;

            _isTerminatedByHit = true;
            Destroy(gameObject);
            return true;
        }

        /// <summary>
        /// 환경 Hit VFX를 재생할 수 있는지 확인합니다.
        /// - 같은 환경 Collider와 겹쳐 있는 동안에는 1회만 허용합니다.
        /// </summary>
        /// <param name="other">판정할 환경 Collider입니다.</param>
        /// <returns>이번 충돌에서 Hit VFX를 출력할 수 있으면 true를 반환합니다.</returns>
        private bool CanPlayEnvironmentHit(Collider2D other)
        {
            return other && !_latchedEnvironmentHitColliders.Contains(other);
        }

        /// <summary>
        /// 환경 Hit VFX를 출력한 Collider를 현재 겹침 잠금 상태로 등록합니다.
        /// </summary>
        /// <param name="other">잠글 환경 Collider입니다.</param>
        private void MarkEnvironmentHitCollider(Collider2D other)
        {
            if (!other)
                return;

            _latchedEnvironmentHitColliders.Add(other);
        }

        /// <summary>
        /// Collider가 환경 Hit VFX 대상 레이어에 속하는지 확인합니다.
        /// </summary>
        /// <param name="other">판정할 Collider입니다.</param>
        /// <returns>환경 충돌 대상으로 등록된 레이어이면 true를 반환합니다.</returns>
        private bool IsEnvironmentHitCollider(Collider2D other)
        {
            if (!other)
                return false;

            int mask = ResolveEnvironmentHitLayerMask();
            if (mask == 0)
                return false;

            int otherLayerMask = 1 << other.gameObject.layer;
            return (mask & otherLayerMask) != 0;
        }

        /// <summary>
        /// 현재 발사체에서 사용할 환경 Hit VFX 레이어 마스크를 반환합니다.
        /// - Skill 이벤트가 커스텀 LayerMask를 지정하면 해당 값을 사용합니다.
        /// - 지정하지 않으면 Core 기본 Ground/Wall 레이어 마스크를 사용합니다.
        /// </summary>
        /// <returns>환경 Hit VFX 후보 레이어 마스크입니다.</returns>
        private int ResolveEnvironmentHitLayerMask()
        {
            if (Runtime != null && Runtime.UseEnvironmentHitLayerMaskOverride)
                return Runtime.EnvironmentHitLayerMaskOverride;

            return ProjectileConstants.GetDefaultEnvironmentHitLayerMask();
        }

        /// <summary>
        /// 즉시 충돌 후 발사체를 종료해야 하는지 판정합니다.
        /// - OnHit 모드에서 DestroyOnTargetHit이면 타겟/지형 충돌 후 즉시 제거합니다.
        /// - KeepUntilRouteEnd이면 충돌 후에도 라우트를 계속 진행합니다.
        /// </summary>
        /// <param name="other">충돌한 Collider입니다.</param>
        /// <returns>즉시 종료가 필요하면 true를 반환합니다.</returns>
        protected virtual bool ShouldTerminateOnImmediateHit(Collider2D other)
        {
            return EffectiveHitLifetimeMode == ProjectileConstants.HitLifetimeMode.DestroyOnTargetHit;
        }

        /// <summary>
        /// 현재 발사체 Collider와 실제로 겹치고 있는 대상 집합을 기준으로 재적중 잠금을 갱신합니다.
        /// - 같은 타겟의 여러 HitArea 중 일부만 빠졌을 때는 타겟 잠금을 유지합니다.
        /// - 환경 Collider와 완전히 이탈하면 환경 Hit VFX 잠금을 해제해 재충돌 연출을 허용합니다.
        /// </summary>
        private void RefreshImmediateHitLatchState()
        {
            if (!ShouldHandleImmediateCollisionDamage && !ShouldHandleEnvironmentHit)
                return;

            Collider2D[] results = GetOverlapResultsBuffer();
            int count = OverlapHitCollider(results);

            _currentOverlapTargets.Clear();
            _currentOverlapEnvironmentHitColliders.Clear();

            for (int i = 0; i < count; i++)
            {
                Collider2D col = results[i];
                if (!col || col == HitCollider)
                    continue;

                if (ShouldHandleEnvironmentHit && IsEnvironmentHitCollider(col))
                    _currentOverlapEnvironmentHitColliders.Add(col);

                if (!ShouldHandleImmediateCollisionDamage)
                    continue;

                if (!TryResolveDamageTarget(col, out CharacterBase target))
                    continue;

                _currentOverlapTargets.Add(target);
            }

            ReleaseDetachedTargetHitLatches();
            ReleaseDetachedEnvironmentHitLatches();
        }

        /// <summary>
        /// 현재 겹침 목록에서 사라진 타겟 재적중 잠금을 해제합니다.
        /// </summary>
        private void ReleaseDetachedTargetHitLatches()
        {
            if (_latchedHitTargets.Count == 0)
                return;

            _releasedHitTargets.Clear();
            foreach (CharacterBase target in _latchedHitTargets)
            {
                if (!target || !_currentOverlapTargets.Contains(target))
                    _releasedHitTargets.Add(target);
            }

            for (int i = 0; i < _releasedHitTargets.Count; i++)
                _latchedHitTargets.Remove(_releasedHitTargets[i]);
        }

        /// <summary>
        /// 현재 겹침 목록에서 사라진 환경 Collider Hit VFX 잠금을 해제합니다.
        /// </summary>
        private void ReleaseDetachedEnvironmentHitLatches()
        {
            if (_latchedEnvironmentHitColliders.Count == 0)
                return;

            _releasedEnvironmentHitColliders.Clear();
            foreach (Collider2D collider in _latchedEnvironmentHitColliders)
            {
                if (!collider || !_currentOverlapEnvironmentHitColliders.Contains(collider))
                    _releasedEnvironmentHitColliders.Add(collider);
            }

            for (int i = 0; i < _releasedEnvironmentHitColliders.Count; i++)
                _latchedEnvironmentHitColliders.Remove(_releasedEnvironmentHitColliders[i]);
        }

        /// <summary>
        /// Collider 기준으로 데미지 대상 캐릭터를 해석합니다.
        /// - 환경 Collider는 데미지 대상이 아니므로 false를 반환합니다.
        /// - 시전자와 대상의 태그 조합이 유효하지 않으면 false를 반환합니다.
        /// </summary>
        /// <param name="other">대상 후보 Collider입니다.</param>
        /// <param name="target">해석된 데미지 대상 캐릭터입니다.</param>
        /// <returns>데미지를 적용할 수 있으면 true를 반환합니다.</returns>
        protected bool TryResolveDamageTarget(Collider2D other, out CharacterBase target)
        {
            target = null;
            if (!other || !FromCharacter)
                return false;

            if (other.CompareTag(ConfigTags.GetValue(ConfigTags.Keys.MapGround)))
                return false;

            target = ResolveTargetCharacter(other);
            if (!target)
                return false;

            return IsValidHitCandidate(other);
        }

        /// <summary>
        /// Collider에서 데미지 대상을 찾아 즉시 데미지를 적용합니다.
        /// - 발사체를 제거하지 않으므로 주기 데미지형 프로젝타일에서 재사용할 수 있습니다.
        /// </summary>
        /// <param name="other">대상 후보 Collider입니다.</param>
        /// <param name="playHitVisual">히트 Visual 콜백을 실행할지 여부입니다.</param>
        /// <returns>데미지를 적용했으면 true를 반환합니다.</returns>
        protected bool TryApplyDamageToCollider(Collider2D other, bool playHitVisual)
        {
            if (!TryResolveDamageTarget(other, out CharacterBase target))
                return false;

            if (playHitVisual)
            {
                CharacterHitArea area = ResolveHitArea(other, target);
                Vector2 hitWorldPosition = ResolveHitVfxWorldPosition(area, other, target, (Vector2)transform.position);
                NotifyHitVisual(other, hitWorldPosition);
            }

            ApplyDamageToTarget(target);
            return true;
        }

        protected virtual void OnArrived()
        {
            if (ShouldStopFlightSoundOnArrived())
                StopFlightSound();

            if (TryPlayEndAndDestroy())
                return;

            Destroy(gameObject);
        }

        #endregion

        private bool TryPlayEndAndDestroy()
        {
            if (_isWaitingForEndVisual)
                return true;

            if (_visual == null)
                return false;

            _isWaitingForEndVisual = _visual.TryPlayEnd(HandleEndVisualComplete);
            return _isWaitingForEndVisual;
        }

        private void HandleEndVisualComplete()
        {
            _isWaitingForEndVisual = false;

            if (this == null || gameObject == null)
                return;

            Destroy(gameObject);
        }

        #region Helpers

        /// <summary>
        /// 이동 벡터를 기준으로 발사체의 Z축 회전을 갱신합니다.
        /// - projectile 테이블의 RotateByMoveDirection이 꺼져 있으면 회전을 변경하지 않습니다.
        /// </summary>
        /// <param name="delta">이전 위치에서 현재 위치까지 이동한 월드 좌표 변화량입니다.</param>
        protected void ApplyRotationByDelta(Vector2 delta)
        {
            if (Info != null && !Info.RotateByMoveDirection)
                return;

            if (delta.sqrMagnitude <= 0.0001f)
                return;

            float angle = Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0f, 0f, angle);
        }

        /// <summary>
        /// 발사체의 히트 Collider와 겹치는 Collider를 NonAlloc 방식으로 조회합니다.
        /// - 주기 데미지형 프로젝타일이 현재 위치의 대상 목록을 수집할 때 사용합니다.
        /// </summary>
        /// <param name="results">결과를 받을 Collider 배열입니다.</param>
        /// <returns>겹친 Collider 수를 반환합니다.</returns>
        protected int OverlapHitCollider(Collider2D[] results)
        {
            if (_hitCollider == null || results == null || results.Length == 0)
                return 0;

            return CompatPhysics2D.OverlapColliderNonAlloc(_hitCollider, _castFilter, results);
        }

        /// <summary>
        /// 발사체 자체의 히트 Collider를 반환합니다.
        /// - 자기 자신과의 Overlap 결과를 걸러낼 때 사용합니다.
        /// </summary>
        protected Collider2D HitCollider => _hitCollider;

        /// <summary>
        /// Overlap 조회용 공유 버퍼를 반환합니다.
        /// - 반복 할당을 줄이기 위해 ProjectileBase가 보유한 버퍼를 재사용합니다.
        /// </summary>
        /// <returns>Overlap 결과 버퍼입니다.</returns>
        protected Collider2D[] GetOverlapResultsBuffer()
        {
            if (_overlapResults == null || _overlapResults.Length == 0)
                _overlapResults = new Collider2D[16];

            return _overlapResults;
        }

        /// <summary>
        /// 현재 발사체의 Hit VFX 위치 정책을 기준으로 타겟 적중 연출 좌표를 계산합니다.
        /// - CollisionPoint는 기존 충돌 처리에서 전달된 좌표를 그대로 사용합니다.
        /// - TargetOffset은 타겟 중심 좌표를 기준으로 오프셋을 적용합니다.
        /// - ProjectilePosition은 발사체 현재 좌표를 기준으로 오프셋을 적용합니다.
        /// - TargetHitAreaNormalized는 타겟 HitArea 내부 정규화 좌표를 월드 좌표로 변환합니다.
        /// </summary>
        /// <param name="area">적중한 HitArea입니다. 없을 수 있습니다.</param>
        /// <param name="hitCollider">충돌한 Collider입니다.</param>
        /// <param name="target">데미지를 받을 최종 타겟 캐릭터입니다.</param>
        /// <param name="collisionWorldPosition">충돌 처리에서 계산된 월드 좌표입니다.</param>
        /// <returns>Hit VFX를 출력할 최종 월드 좌표입니다.</returns>
        private Vector2 ResolveHitVfxWorldPosition(
            CharacterHitArea area,
            Collider2D hitCollider,
            CharacterBase target,
            Vector2 collisionWorldPosition)
        {
            if (Runtime == null)
                return collisionWorldPosition;

            Vector2 offset = Runtime.HitVfxOffset;
            switch (Runtime.HitVfxPositionPolicy)
            {
                case ProjectileConstants.HitVfxPositionPolicy.TargetOffset:
                    if (target)
                        return (Vector2)target.transform.position + offset;

                    return collisionWorldPosition + offset;

                case ProjectileConstants.HitVfxPositionPolicy.ProjectilePosition:
                    return (Vector2)transform.position + offset;

                case ProjectileConstants.HitVfxPositionPolicy.TargetHitAreaNormalized:
                    if (TryResolveTargetHitAreaNormalizedPoint(
                            area,
                            hitCollider,
                            target,
                            Runtime.HitVfxHitAreaNormalized,
                            out Vector2 hitAreaPoint))
                    {
                        return hitAreaPoint + offset;
                    }

                    if (target)
                        return (Vector2)target.transform.position + offset;

                    return collisionWorldPosition + offset;

                case ProjectileConstants.HitVfxPositionPolicy.CollisionPoint:
                default:
                    return collisionWorldPosition + offset;
            }
        }

        /// <summary>
        /// 타겟 HitArea 정규화 좌표(0~1)를 월드 좌표로 변환합니다.
        /// - 타겟의 대표 HitArea Collider를 우선 사용하고, 없으면 충돌 Collider를 대체 기준으로 사용합니다.
        /// - Collider Bounds 기준으로 계산하여 Capsule/Box 등 Collider 종류에 관계없이 동작합니다.
        /// </summary>
        /// <param name="area">충돌 Collider에서 찾은 HitArea 컴포넌트입니다.</param>
        /// <param name="hitCollider">실제 충돌한 Collider입니다.</param>
        /// <param name="target">데미지를 받을 최종 타겟 캐릭터입니다.</param>
        /// <param name="normalizedPoint">HitArea 내부 정규화 좌표입니다. (0,0)=좌하단, (1,1)=우상단입니다.</param>
        /// <param name="worldPoint">변환된 월드 좌표입니다.</param>
        /// <returns>좌표 계산에 성공했으면 <see langword="true"/>입니다.</returns>
        private static bool TryResolveTargetHitAreaNormalizedPoint(
            CharacterHitArea area,
            Collider2D hitCollider,
            CharacterBase target,
            Vector2 normalizedPoint,
            out Vector2 worldPoint)
        {
            worldPoint = default;

            Collider2D referenceCollider = null;
            if (target != null && target.colliderHitArea != null)
                referenceCollider = target.colliderHitArea;

            if (referenceCollider == null && area != null)
                referenceCollider = area.GetComponent<Collider2D>();

            if (referenceCollider == null && hitCollider != null)
                referenceCollider = hitCollider;

            if (referenceCollider == null)
                return false;

            Bounds bounds = referenceCollider.bounds;
            if (bounds.size.sqrMagnitude <= 1e-8f)
                return false;

            Vector2 clamped = new Vector2(
                Mathf.Clamp01(normalizedPoint.x),
                Mathf.Clamp01(normalizedPoint.y));

            worldPoint = new Vector2(
                Mathf.Lerp(bounds.min.x, bounds.max.x, clamped.x),
                Mathf.Lerp(bounds.min.y, bounds.max.y, clamped.y));
            return true;
        }

        /// <summary>
        /// 현재 위치에서 히트 Visual 콜백을 실행합니다.
        /// - 이 호출은 충돌 즉시 반응(예: Hit VFX 생성)만 전달합니다.
        /// - 발사체 종료 여부와 attached VFX 수명 정리는 별도의 종료 경로가 담당합니다.
        /// </summary>
        /// <param name="hitCollider">히트 대상 Collider입니다.</param>
        protected void NotifyHitVisual(Collider2D hitCollider)
        {
            NotifyHitVisual(hitCollider, transform.position);
        }

        /// <summary>
        /// 지정한 월드 위치에서 히트 Visual 콜백을 실행합니다.
        /// - 스윕 적중처럼 Transform 위치와 실제 적중 지점을 분리해야 할 때 사용합니다.
        /// - 히트 시점에 attached VFX를 종료하지 않고, 부모 발사체의 실제 종료 시점에 수명을 맞춥니다.
        /// </summary>
        /// <param name="hitCollider">히트 대상 Collider입니다.</param>
        /// <param name="worldPosition">히트 연출을 재생할 월드 위치입니다.</param>
        protected void NotifyHitVisual(Collider2D hitCollider, Vector2 worldPosition)
        {
            _visual?.OnHit(new ProjectileVisualHitContext(worldPosition, FromCharacter, hitCollider));
        }

        /// <summary>
        /// 현재 발사체의 런타임 정보와 실제 피격 대상을 바탕으로 데미지 메타데이터를 생성합니다.
        /// </summary>
        /// <param name="target">실제 피격 대상 캐릭터입니다.</param>
        /// <returns>대상에게 전달할 데미지 메타데이터입니다.</returns>
        protected MetadataDamage CreateDamageMetadata(CharacterBase target)
        {
            long resolvedDamage = ResolveDamageOnHit(target);
            bool damageApplied = resolvedDamage > 0L;
            int crowdControlUid = ResolveOnHitCrowdControlUid(
                Runtime != null ? Runtime.OnHitCrowdControls : null,
                damageApplied,
                ProjectileOnHitCrowdControlTiming.BeforeDamage);
            List<int> resolvedOnHitCrowdControls = CollectOnHitCrowdControlUids(
                Runtime != null ? Runtime.OnHitCrowdControls : null,
                damageApplied,
                ProjectileOnHitCrowdControlTiming.AfterDamage);

            return new MetadataDamage
            {
                damage = resolvedDamage,
                attacker = FromCharacter ? FromCharacter.gameObject : null,
                damageType = ResolveDamageTypeOnHit(),
                crowdControlUid = crowdControlUid,
                SkillUid = SkillUid,
                AttackId = AttackId,
                SkillHitMpGain = Runtime != null ? Runtime.SkillHitMpGain : 0,
                AllowMultipleSkillHitMpGainPerAttack = Runtime != null && Runtime.AllowMultipleSkillHitMpGainPerAttack,
                ResolvedOnHitCrowdControls = resolvedOnHitCrowdControls,
                HasPendingAfterDamageCrowdControl = HasPendingOnHitCrowdControl(
                    Runtime != null ? Runtime.OnHitCrowdControls : null,
                    damageApplied,
                    ProjectileOnHitCrowdControlTiming.AfterDamage),
                GuardAttackType = Runtime != null ? Runtime.GuardAttackType : GuardAttackType.Normal,
                GuardInteractionMode = Runtime != null ? Runtime.GuardInteractionMode : GuardInteractionMode.Normal,
            };
        }

        /// <summary>
        /// 프로젝타일이 실제 대상에 적중한 시점에 적용할 데미지를 계산합니다.
        /// </summary>
        /// <remarks>
        /// 스킬 프로젝타일에서 공식 컨텍스트가 전달된 경우 실제 피격 대상의 레벨, 방어력, 스탯을 반영하기 위해
        /// 적중 시점에 <see cref="CalculateManager.CalculateSkillDamage"/>를 다시 호출합니다.
        /// </remarks>
        /// <param name="target">실제 피격 대상 캐릭터입니다.</param>
        /// <returns>저항과 가드 적용 전의 프로젝타일 데미지입니다.</returns>
        protected long ResolveDamageOnHit(CharacterBase target)
        {
            ProjectileDamageFormulaContext context = Runtime != null
                ? Runtime.DamageFormulaContext
                : null;
            if (context == null)
            {
                return Damage;
            }

            CalculateManager calculateManager = CalculateManager.GetActive();
            if (calculateManager == null)
            {
                return Damage;
            }

            var request = new DamageFormulaRequest(
                FromCharacter,
                target,
                context.FormulaKey,
                context.BaseDamage,
                context.SkillDamageRate,
                context.EventMultiplier,
                context.OptionMultiplier,
                context.BuffRate,
                context.DamageType,
                context.RollCritical);
            return calculateManager.CalculateSkillDamage(request);
        }

        /// <summary>
        /// 적중 시점에 사용할 데미지 타입을 반환합니다.
        /// </summary>
        /// <returns>공식 컨텍스트가 있으면 컨텍스트의 데미지 타입, 없으면 발사체 기본 데미지 타입입니다.</returns>
        protected ConfigCommon.DamageType ResolveDamageTypeOnHit()
        {
            return Runtime != null && Runtime.DamageFormulaContext != null
                ? Runtime.DamageFormulaContext.DamageType
                : DamageType;
        }

        /// <summary>
        /// 프로젝타일 적중 시점에 적용할 Crowd Control UID 하나를 선택합니다.
        /// </summary>
        /// <param name="entries">프로젝타일 런타임 Crowd Control 후보 목록입니다.</param>
        /// <param name="damageApplied">데미지가 적용될 수 있는 상황인지 여부입니다.</param>
        /// <param name="timing">조회할 적용 시점입니다.</param>
        /// <returns>선택된 Crowd Control UID입니다. 없으면 0입니다.</returns>
        private static int ResolveOnHitCrowdControlUid(
            ProjectileOnHitCrowdControlEntry[] entries,
            bool damageApplied,
            ProjectileOnHitCrowdControlTiming timing)
        {
            if (entries == null || entries.Length == 0)
            {
                return 0;
            }

            for (int i = 0; i < entries.Length; i++)
            {
                ProjectileOnHitCrowdControlEntry entry = entries[i];
                if (!CanUseOnHitCrowdControl(entry, damageApplied, timing))
                {
                    continue;
                }

                return entry.CrowdControlUid;
            }

            return 0;
        }

        /// <summary>
        /// 프로젝타일 적중 후 데미지 파이프라인으로 전달할 Crowd Control UID 목록을 수집합니다.
        /// </summary>
        /// <param name="entries">프로젝타일 런타임 Crowd Control 후보 목록입니다.</param>
        /// <param name="damageApplied">데미지가 적용될 수 있는 상황인지 여부입니다.</param>
        /// <param name="timing">조회할 적용 시점입니다.</param>
        /// <returns>선택된 Crowd Control UID 목록입니다. 없으면 null입니다.</returns>
        private static List<int> CollectOnHitCrowdControlUids(
            ProjectileOnHitCrowdControlEntry[] entries,
            bool damageApplied,
            ProjectileOnHitCrowdControlTiming timing)
        {
            if (entries == null || entries.Length == 0)
            {
                return null;
            }

            List<int> result = null;
            for (int i = 0; i < entries.Length; i++)
            {
                ProjectileOnHitCrowdControlEntry entry = entries[i];
                if (!CanUseOnHitCrowdControl(entry, damageApplied, timing))
                {
                    continue;
                }

                result ??= new List<int>(entries.Length);
                result.Add(entry.CrowdControlUid);
            }

            return result;
        }

        /// <summary>
        /// 조건상 나중에 Crowd Control이 적용될 가능성이 있는지 확인합니다.
        /// </summary>
        /// <remarks>
        /// 실제 확률 판정은 데미지 메타데이터 생성 시 수행하지만, 피격 반응 억제 여부 판단에는 후보 존재 여부가 필요합니다.
        /// </remarks>
        /// <param name="entries">프로젝타일 런타임 Crowd Control 후보 목록입니다.</param>
        /// <param name="damageApplied">데미지가 적용될 수 있는 상황인지 여부입니다.</param>
        /// <param name="timing">조회할 적용 시점입니다.</param>
        /// <returns>조건을 만족하는 후보가 있으면 true입니다.</returns>
        private static bool HasPendingOnHitCrowdControl(
            ProjectileOnHitCrowdControlEntry[] entries,
            bool damageApplied,
            ProjectileOnHitCrowdControlTiming timing)
        {
            if (entries == null || entries.Length == 0)
            {
                return false;
            }

            for (int i = 0; i < entries.Length; i++)
            {
                ProjectileOnHitCrowdControlEntry entry = entries[i];
                if (entry.CrowdControlUid <= 0 || entry.Chance <= 0f || entry.Timing != timing)
                {
                    continue;
                }

                if (entry.RequireDamageDealt && !damageApplied)
                {
                    continue;
                }

                return true;
            }

            return false;
        }

        /// <summary>
        /// Crowd Control 후보가 현재 적중 조건에서 사용할 수 있는지 확인합니다.
        /// </summary>
        /// <param name="entry">검사할 Crowd Control 후보입니다.</param>
        /// <param name="damageApplied">데미지가 적용될 수 있는 상황인지 여부입니다.</param>
        /// <param name="timing">조회할 적용 시점입니다.</param>
        /// <returns>조건과 확률 판정을 모두 통과하면 true입니다.</returns>
        private static bool CanUseOnHitCrowdControl(
            ProjectileOnHitCrowdControlEntry entry,
            bool damageApplied,
            ProjectileOnHitCrowdControlTiming timing)
        {
            if (entry.CrowdControlUid <= 0 || entry.Timing != timing)
            {
                return false;
            }

            if (entry.RequireDamageDealt && !damageApplied)
            {
                return false;
            }

            if (entry.Chance <= 0f)
            {
                return false;
            }

            return entry.Chance >= 1f || Random.value <= entry.Chance;
        }

        /// <summary>
        /// 지정한 타겟 캐릭터에게 현재 발사체의 데미지를 적용합니다.
        /// </summary>
        /// <param name="target">데미지를 받을 캐릭터입니다.</param>
        protected void ApplyDamageToTarget(CharacterBase target)
        {
            if (!target)
                return;

            target.TakeDamage(CreateDamageMetadata(target));
        }

        protected void UpdateVisual(Vector2 newPos, Vector2 delta)
        {
            _visual?.OnUpdate(new ProjectileVisualUpdateContext(
                StartPoint,
                TargetPoint,
                newPos,
                delta,
                Direction));
        }

        protected bool IsInCameraView()
        {
            var cam = SceneGame.Instance != null ? SceneGame.Instance.mainCamera : null;
            if (!cam) return true; // 카메라 없으면 판정 생략

            Vector3 v = cam.WorldToViewportPoint(transform.position);
            return v.x is >= 0f and <= 1f && v.y is >= 0f and <= 1f;
        }


        /// <summary>
        /// 현재 메인 카메라의 월드 경계(Rect)를 구합니다.
        /// - Viewport(0..1)를 World로 변환하여 계산합니다.
        /// - Unity 공식 문서: Camera.ViewportToWorldPoint / Camera.WorldToViewportPoint
        /// </summary>
        protected bool TryGetCameraWorldRect(out Rect rect, float padding = 0f)
        {
            rect = default;

            var cam = SceneGame.Instance != null ? SceneGame.Instance.mainCamera : null;
            if (!cam) return false;

            // 2D(Orthographic) 기준: z는 카메라-오브젝트 거리로 맞춘다.
            float z = Mathf.Abs(cam.transform.position.z - transform.position.z);

            Vector3 w0 = cam.ViewportToWorldPoint(new Vector3(0f, 0f, z));
            Vector3 w1 = cam.ViewportToWorldPoint(new Vector3(1f, 1f, z));

            float minX = Mathf.Min(w0.x, w1.x) + padding;
            float maxX = Mathf.Max(w0.x, w1.x) - padding;
            float minY = Mathf.Min(w0.y, w1.y) + padding;
            float maxY = Mathf.Max(w0.y, w1.y) - padding;

            rect = Rect.MinMaxRect(minX, minY, maxX, maxY);
            return true;
        }

        #endregion

        #region Collision
        /// <summary>
        /// Trigger 진입 시 즉시 충돌 데미지 정책을 처리합니다.
        /// - 주기 데미지형 프로젝타일은 자체 Tick 로직을 사용하므로 여기서 처리하지 않습니다.
        /// </summary>
        /// <param name="other">진입한 대상 Collider입니다.</param>
        protected virtual void OnTriggerEnter2D(Collider2D other)
        {
            if (!Initialized || _isWaitingForEndVisual) return;
            if (_isTerminatedByHit) return;
            if (!ShouldHandleImmediateCollisionDamage && !ShouldHandleEnvironmentHit) return;

            // 기존 정책을 유지하되, 타겟 데미지는 "루트 캐릭터" 기준으로 판정하고,
            // 환경 Hit VFX는 LayerMask 기준으로 별도 처리합니다.
            TryHandleHit(other);
        }

        /// <summary>
        /// Trigger 이탈 시 현재 실제 겹침 상태를 다시 계산하여 재적중 잠금을 갱신합니다.
        /// - 같은 타겟의 일부 HitArea만 빠진 경우에는 잠금을 유지합니다.
        /// - 타겟의 모든 HitArea에서 완전히 이탈한 뒤 재진입하면 다시 데미지를 줄 수 있습니다.
        /// </summary>
        /// <param name="other">이탈한 대상 Collider입니다.</param>
        protected virtual void OnTriggerExit2D(Collider2D other)
        {
            if (!Initialized || _isWaitingForEndVisual) return;
            if (!ShouldHandleImmediateCollisionDamage && !ShouldHandleEnvironmentHit) return;

            if (ShouldHandleEnvironmentHit && IsEnvironmentHitCollider(other))
            {
                RefreshImmediateHitLatchState();
                return;
            }

            if (!ShouldHandleImmediateCollisionDamage)
                return;

            if (!TryResolveDamageTarget(other, out CharacterBase target))
                return;

            if (!_latchedHitTargets.Contains(target))
                return;

            RefreshImmediateHitLatchState();
        }

        /// <summary>
        /// 타겟 적중 시 데미지와 히트 연출을 적용합니다.
        /// - 생명 주기 종료 여부는 호출부에서 HitLifetimeMode로 별도 결정합니다.
        /// </summary>
        /// <param name="area">적중한 HitArea입니다. 없을 수 있습니다.</param>
        /// <param name="hitCollider">충돌한 Collider입니다.</param>
        /// <param name="target">데미지를 받을 최종 타겟 캐릭터입니다.</param>
        /// <param name="hitWorldPosition">히트 연출을 재생할 월드 위치입니다.</param>
        protected virtual void OnHitTarget(CharacterHitArea area, Collider2D hitCollider, CharacterBase target, Vector2 hitWorldPosition)
        {
            Vector2 hitVfxWorldPosition = ResolveHitVfxWorldPosition(area, hitCollider, target, hitWorldPosition);
            NotifyHitVisual(hitCollider, hitVfxWorldPosition);
            ApplyDamageToTarget(target);
        }

        #endregion

        protected virtual void OnDestroy()
        {
            StopFlightSound();
            _visual?.OnDespawn();
        }

        /// <summary>
        /// 프로젝타일 비행 사운드를 시작합니다.
        /// </summary>
        private void StartFlightSound()
        {
            SoundPlayRequest request = ResolveFlightSoundRequest();
            if (request == null || !request.IsValid)
                return;

            SoundManager soundManager = SceneGame.Instance != null ? SceneGame.Instance.soundManager : null;
            if (soundManager == null)
                return;

            // 비행 사운드는 프로젝타일 수명 종료 시 StopFlightSound에서 정리합니다.
            _flightSoundHandle = soundManager.Play(request);
        }

        /// <summary>
        /// 프로젝타일 비행 사운드 수명 정책을 반영한 최종 사운드 요청을 계산합니다.
        /// </summary>
        /// <returns>사운드 매니저에 전달할 요청입니다. 재생할 사운드가 없으면 null입니다.</returns>
        private SoundPlayRequest ResolveFlightSoundRequest()
        {
            SoundPlayRequest request = Runtime != null ? Runtime.FlightSound : null;
            if (request == null || !request.IsValid)
                return null;

            if (Runtime.FlightSoundLifetimePolicy == ProjectileFlightSoundLifetimePolicy.LoopUntilProjectileDestroyed)
                return request.CloneLoopUntilHandleStopped();

            return request.Clone();
        }

        /// <summary>
        /// 도착 시점에 비행 사운드를 정지해야 하는지 확인합니다.
        /// </summary>
        /// <returns>프로젝타일 파괴 전에도 도착 시 정지해야 하면 true입니다.</returns>
        private bool ShouldStopFlightSoundOnArrived()
        {
            return Runtime == null ||
                   Runtime.FlightSoundLifetimePolicy != ProjectileFlightSoundLifetimePolicy.LoopUntilProjectileDestroyed;
        }

        /// <summary>
        /// 프로젝타일 비행 사운드를 정지하고 핸들을 해제합니다.
        /// </summary>
        private void StopFlightSound()
        {
            if (_flightSoundHandle == null)
                return;

            _flightSoundHandle.Stop();
            _flightSoundHandle = null;
        }
    }
}
