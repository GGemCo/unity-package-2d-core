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

        // ---- Visual ----
        private IProjectileVisual _visual;

        // ---- Collision Sweep (anti-tunneling) ----
        private CapsuleCollider2D _hitCollider;
        private ContactFilter2D _castFilter;
        private RaycastHit2D[] _castResults;
        private bool _hasHit;

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

            float speedMul = metadata != null ? metadata.SpeedMultiplier : 1f;
            Speed = info.MoveSpeed * Mathf.Max(0.01f, speedMul);

            // Rigidbody2D: 스크립트 제어 이동을 위해 Kinematic.
            Rigidbody2D rb = ComponentController.AddRigidbody2D(gameObject);
            rb.bodyType = RigidbodyType2D.Kinematic;

            // Collider2D: Trigger 로 충돌 판정
            Vector2 size = (info.ColliderSize != Vector2.zero) ? info.ColliderSize : Vector2.zero;
            ComponentController.AddCapsuleCollider2D(gameObject, true, Vector2.zero, size);

            _hitCollider = GetComponent<CapsuleCollider2D>();
            // Cast 결과 버퍼(할당 최소화)
            if (_castResults == null) _castResults = new RaycastHit2D[16];
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
            if (!Initialized) return;

            // 등속 이동: 현재까지 이동 거리 / 전체 거리 => 0..1
            float distCovered = (Time.fixedTime - StartTime) * Speed;
            float t = (JourneyLength > 0f) ? (distCovered / JourneyLength) : 1f;

            Vector2 newPos = ComputePosition(t);
            Vector2 delta = newPos - PrevPos;

            // Anti-tunneling: 이동 구간(PrevPos → newPos)을 스윕(Cast)으로 선검출합니다.
            if (TrySweepHit(delta, out var sweepHit))
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
            _visual?.OnUpdate(new ProjectileVisualUpdateContext(
                StartPoint,
                TargetPoint,
                newPos,
                delta,
                Direction));

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

        private bool IsValidHitCandidate(Collider2D other)
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

        private CharacterBase ResolveTargetCharacter(Collider2D other)
        {
            if (!other) return null;

            // HitArea가 있으면 해당 타겟을 우선 사용
            var hitArea = other.GetComponentInParent<CharacterHitArea>();
            if (hitArea && hitArea.target) return hitArea.target;

            // 없으면 루트 CharacterBase로 판정
            return other.GetComponentInParent<CharacterBase>();
        }

        private CharacterHitArea ResolveHitArea(Collider2D other, CharacterBase target)
        {
            if (!other) return null;

            // 충돌한 콜라이더가 HitArea 하위일 수 있으므로 Parent 우선
            var area = other.GetComponentInParent<CharacterHitArea>();
            if (area) return area;

            // 루트에서 탐색(레거시 호환)
            if (target)
                return target.GetComponentInChildren<CharacterHitArea>();

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
                _visual?.OnHit(new ProjectileVisualHitContext(transform.position, FromCharacter, other));
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
                // HitArea가 없는 경우도 안전하게 처리
                _visual?.OnHit(new ProjectileVisualHitContext(transform.position, FromCharacter, other));

                var md = new MetadataDamage
                {
                    damage = Damage,
                    attacker = FromCharacter ? FromCharacter.gameObject : null,
                    damageType = DamageType
                };
                target.TakeDamage(md);
                Destroy(gameObject);
            }

            return true;
        }

        protected virtual void OnArrived()
        {
            Destroy(gameObject);
        }

        #endregion

        #region Helpers

        protected void ApplyRotationByDelta(Vector2 delta)
        {
            // 이동 벡터 기준 Z-회전.
            if (delta.sqrMagnitude <= 0.0001f) return;
            float angle = Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0f, 0f, angle);
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
        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!Initialized) return;
            if (_hasHit) return;

            // 기존 정책을 유지하되, "충돌한 콜라이더의 태그"가 아니라 "루트 캐릭터" 기준으로 판정합니다.
            TryHandleHit(other);
        }

        protected virtual void OnHitTarget(CharacterHitArea area, Collider2D hitCollider)
        {
            _visual?.OnHit(new ProjectileVisualHitContext(transform.position, FromCharacter, hitCollider));

            if (!area) return;

            var md = new MetadataDamage
            {
                damage = Damage,
                attacker = FromCharacter ? FromCharacter.gameObject : null,
                damageType = DamageType
            };
            area.target?.TakeDamage(md);

            Destroy(gameObject);
        }

        #endregion

        protected virtual void OnDestroy()
        {
            _visual?.OnDespawn();
        }
    }
}
