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
        protected virtual void Update()
        {
            if (!Initialized) return;

            // 등속 이동: 현재까지 이동 거리 / 전체 거리 => 0..1
            float distCovered = (Time.time - StartTime) * Speed;
            float t = (JourneyLength > 0f) ? (distCovered / JourneyLength) : 1f;

            if (t >= 1f)
            {
                OnArrived();
                return;
            }

            Vector2 newPos = ComputePosition(t);
            Vector2 delta = newPos - PrevPos;

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

        protected bool IsInCameraView()
        {
            var cam = SceneGame.Instance != null ? SceneGame.Instance.mainCamera : null;
            if (!cam) return true; // 카메라 없으면 판정 생략

            Vector3 v = cam.WorldToViewportPoint(transform.position);
            return v.x is >= 0f and <= 1f && v.y is >= 0f and <= 1f;
        }
        #endregion

        #region Collision
        private void OnTriggerEnter2D(Collider2D other)
        {
            // 플레이어 ↔ 몬스터 상호 충돌만 체크(레거시 정책 유지)
            if (!FromCharacter) return;

            bool fromMonster = FromCharacter.CompareTag(ConfigTags.GetValue(ConfigTags.Keys.Monster));
            bool toPlayer = other.CompareTag(ConfigTags.GetValue(ConfigTags.Keys.Player));
            bool fromPlayer = FromCharacter.CompareTag(ConfigTags.GetValue(ConfigTags.Keys.Player));
            bool toMonster = other.CompareTag(ConfigTags.GetValue(ConfigTags.Keys.Monster));

            if ((fromMonster && toPlayer) || (fromPlayer && toMonster))
            {
                var area = other.GetComponent<CharacterHitArea>();
                if (area) OnHitTarget(area, other);
                return;
            }

            // 지면과 충돌 시 파괴(맵에 박히는 타입이 아니라면)
            if (other.CompareTag(ConfigTags.GetValue(ConfigTags.Keys.MapGround)))
            {
                _visual?.OnHit(new ProjectileVisualHitContext(transform.position, FromCharacter, other));
                Destroy(gameObject);
            }
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
