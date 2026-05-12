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

        // ---- Collision Sweep (anti-tunneling) ----
        private CapsuleCollider2D _hitCollider;
        private ContactFilter2D _castFilter;
        private RaycastHit2D[] _castResults;
        private Collider2D[] _overlapResults;
        private bool _hasHit;
        private bool _isWaitingForEndVisual;

        /// <summary>
        /// 즉시 충돌 데미지 정책을 사용할지 여부입니다.
        /// - 기본 프로젝타일은 true이며, 주기 데미지형 프로젝타일은 false로 재정의합니다.
        /// </summary>
        protected virtual bool ShouldHandleImmediateCollisionDamage => true;

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
        /// 좌표 타겟으로 발사
        /// </summary>
        public virtual void Launch(Vector2 targetPos)
        {
            TargetObject = null;

            SetStartPoint();

            TargetPoint = targetPos;
            JourneyLength = Vector2.Distance(StartPoint, TargetPoint);
            StartTime = Time.time;

            Direction = (TargetPoint - StartPoint).normalized;
            if (Direction.sqrMagnitude < 1e-6f)
                Direction = Vector2.right;

            transform.position = StartPoint;
            PrevPos = StartPoint;

            Initialized = true;

            if (ShouldHandleImmediateCollisionDamage)
            {
                // 발사 직후 이미 타겟/지면과 겹쳐 있는 경우를 보정한다.
                // Trigger Enter / Cast는 "생성 시점의 초기 겹침"을 놓칠 수 있으므로,
                // Launch 직후 한 번 즉시 overlap 검사를 수행한다.
                TryHandleInitialOverlap();
            }
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

            // 등속 이동: 현재까지 이동 거리 / 전체 거리 => 0..1
            float distCovered = (Time.fixedTime - StartTime) * Speed;
            float t = (JourneyLength > 0f) ? (distCovered / JourneyLength) : 1f;

            Vector2 newPos = ComputePosition(t);
            Vector2 delta = newPos - PrevPos;

            // Anti-tunneling: 이동 구간(PrevPos → newPos)을 스윕(Cast)으로 선검출합니다.
            if (ShouldHandleImmediateCollisionDamage && TrySweepHit(delta, out var sweepHit))
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

            if (t >= 1f)
            {
                OnArrived();
                return;
            }

            if (!IsInCameraView())
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


        private void SetupCastFilter()
        {
            // Physics2D 레이어 충돌 매트릭스를 그대로 따릅니다.
            _castFilter = new ContactFilter2D
            {
                useTriggers = true,
                useLayerMask = true
            };
            _castFilter.SetLayerMask(Physics2D.GetLayerCollisionMask(gameObject.layer));
        }

        private bool TrySweepHit(Vector2 delta, out RaycastHit2D bestHit)
        {
            bestHit = default;

            if (_hasHit) return false;
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

                if (!IsValidHitCandidate(col)) continue;

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
            if (_hasHit) return;
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

                if (!IsValidHitCandidate(col))
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
        /// 충돌 후보가 현재 발사체의 유효한 타겟인지 확인합니다.
        /// - 지면은 즉시 충돌 정책에서 항상 유효한 대상으로 처리합니다.
        /// - 캐릭터는 시전자와 대상의 태그 조합을 기준으로 판정합니다.
        /// </summary>
        /// <param name="other">판정할 Collider입니다.</param>
        /// <returns>유효한 충돌 후보이면 true를 반환합니다.</returns>
        protected bool IsValidHitCandidate(Collider2D other)
        {
            if (!other) return false;

            // 지면은 항상 처리
            if (other.CompareTag(ConfigTags.GetValue(ConfigTags.Keys.MapGround)))
                return true;

            // 타겟은 "루트 캐릭터" 기준으로 판정
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

        private bool TryHandleHit(Collider2D other, Vector2? overrideWorldPos = null)
        {
            if (_hasHit) return true;
            if (!other) return false;

            if (overrideWorldPos.HasValue)
                transform.position = overrideWorldPos.Value;

            // 지면
            if (other.CompareTag(ConfigTags.GetValue(ConfigTags.Keys.MapGround)))
            {
                _hasHit = true;
                NotifyHitVisual(other);
                Destroy(gameObject);
                return true;
            }

            // 타겟(루트 기준)
            if (!FromCharacter) return false;

            var target = ResolveTargetCharacter(other);
            if (!target) return false;

            bool fromMonster = FromCharacter.CompareTag(ConfigTags.GetValue(ConfigTags.Keys.Monster));
            bool fromPlayer = FromCharacter.CompareTag(ConfigTags.GetValue(ConfigTags.Keys.Player));
            bool toMonster = target.CompareTag(ConfigTags.GetValue(ConfigTags.Keys.Monster));
            bool toPlayer = target.CompareTag(ConfigTags.GetValue(ConfigTags.Keys.Player));

            if (!((fromMonster && toPlayer) || (fromPlayer && toMonster)))
                return false;

            var area = ResolveHitArea(other, target);
            _hasHit = true;

            if (area)
            {
                OnHitTarget(area, other);
            }
            else
            {
                // HitArea가 없는 경우도 안전하게 처리합니다.
                NotifyHitVisual(other);
                ApplyDamageToTarget(target);
                Destroy(gameObject);
            }

            return true;
        }

        /// <summary>
        /// Collider 기준으로 데미지 대상 캐릭터를 해석합니다.
        /// - 지면은 데미지 대상이 아니므로 false를 반환합니다.
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
                NotifyHitVisual(other);

            ApplyDamageToTarget(target);
            return true;
        }

        protected virtual void OnArrived()
        {
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
        /// 현재 위치에서 히트 Visual 콜백을 실행합니다.
        /// </summary>
        /// <param name="hitCollider">히트 대상 Collider입니다.</param>
        protected void NotifyHitVisual(Collider2D hitCollider)
        {
            _visual?.OnHit(new ProjectileVisualHitContext(transform.position, FromCharacter, hitCollider));
        }

        /// <summary>
        /// 현재 발사체의 런타임 정보를 바탕으로 데미지 메타데이터를 생성합니다.
        /// </summary>
        /// <returns>대상에게 전달할 데미지 메타데이터입니다.</returns>
        protected MetadataDamage CreateDamageMetadata()
        {
            return new MetadataDamage
            {
                damage = Damage,
                attacker = FromCharacter ? FromCharacter.gameObject : null,
                damageType = DamageType,
                SkillUid = SkillUid,
                AttackId = AttackId,
                ElementGaugeApplications = Runtime != null ? Runtime.ElementGaugeApplications : null,
            };
        }

        /// <summary>
        /// 지정한 타겟 캐릭터에게 현재 발사체의 데미지를 적용합니다.
        /// </summary>
        /// <param name="target">데미지를 받을 캐릭터입니다.</param>
        protected void ApplyDamageToTarget(CharacterBase target)
        {
            if (!target)
                return;

            target.TakeDamage(CreateDamageMetadata());
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
            if (_hasHit) return;
            if (!ShouldHandleImmediateCollisionDamage) return;

            // 기존 정책을 유지하되, "충돌한 콜라이더의 태그"가 아니라 "루트 캐릭터" 기준으로 판정합니다.
            TryHandleHit(other);
        }

        protected virtual void OnHitTarget(CharacterHitArea area, Collider2D hitCollider)
        {
            NotifyHitVisual(hitCollider);

            if (!area) return;

            ApplyDamageToTarget(area.target);

            Destroy(gameObject);
        }

        #endregion

        protected virtual void OnDestroy()
        {
            _visual?.OnDespawn();
        }
    }
}
