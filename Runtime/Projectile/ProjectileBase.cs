using UnityEngine;

namespace GGemCo2DCore
{
    /// <summary>
    /// 모든 발사체의 공통 로직을 담당하는 베이스 클래스.
    /// - 초기화(테이블/속도/콜라이더/이펙트)
    /// - 발사(좌표/오브젝트)
    /// - 진행률/회전/Flip/화면이탈/수명관리
    /// - 충돌 및 데미지 처리
    /// </summary>
    public abstract class ProjectileBase : MonoBehaviour
    {
        // ---- Table / Config ----
        protected StruckTableProjectile Info;
        protected StruckTableEffect StruckEffect;

        // ---- Movement ----
        protected float Speed;
        protected float JourneyLength;
        protected float StartTime;
        protected Vector2 StartPoint;
        protected Vector2 TargetPoint;
        protected CharacterBase TargetObject; // Fixed 타겟용(발사 시점에 좌표 스냅)
        protected Vector2 PrevPos;
        protected Vector3 Direction; // Start→현재 이동 방향(회전/Flip 참고)
        protected bool Initialized;

        // ---- Visual / Effect ----
        protected DefaultEffect ProjectileEffect;
        protected EffectManager EffectManager;
        protected bool ShouldFlip;

        // ---- Combat ----
        protected long Damage;
        protected CharacterBase FromCharacter;

        // ---- Misc ----
        private const float PositionThreshold = 0.1f;

        #region Lifecycle
        public virtual void Initialize(StruckTableProjectile info)
        {
            if (info == null)
            {
                Destroy(gameObject);
                return;
            }

            Info  = info;
            Speed = info.MoveSpeed;

            // Rigidbody2D: 스크립트 제어 이동을 위해 Kinematic.
            Rigidbody2D rb = ComponentController.AddRigidbody2D(gameObject);
            rb.bodyType = RigidbodyType2D.Kinematic;

            // Collider2D: Trigger 로 충돌 판정
            Vector2 size = (info.ColliderSize != Vector2.zero) ? info.ColliderSize : Vector2.zero;
            ComponentController.AddCapsuleCollider2D(gameObject, true, Vector2.zero, size);

            // 시작 좌표는 발사 직전 SetStartPoint에서 계산.
            
            EffectManager = SceneGame.Instance.EffectManager;

            // 발사체 기본 이펙트 생성
            ProjectileEffect = EffectManager.CreateEffect(Info.EffectUid);
            if (!ProjectileEffect)
            {
                // 이펙트가 반드시 필요하지 않다면, 경고만 남기고 진행 가능
                GcLogger.LogWarning("[Projectile] Effect not found. Continue without visual.");
                return;
            }

            ProjectileEffect.transform.SetParent(transform);
            ProjectileEffect.transform.localPosition = Vector3.zero;

            // 충돌 후 종료 애니를 돌릴 수 있으므로 무한(-1) 지속
            ProjectileEffect.SetDuration(-1);

            if (Info.EffectScale > 0)
                ProjectileEffect.SetScale(Info.EffectScale);

            StruckEffect = TableLoaderManager.Instance.GetEffectData(Info.EffectUid);
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

            TargetPoint    = targetPos;
            JourneyLength  = Vector2.Distance(StartPoint, TargetPoint);
            StartTime      = Time.time;
            Direction      = (TargetPoint - StartPoint).normalized;

            transform.position = StartPoint;
            UpdateFlipByDefaultDirection();

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
            ApplyRotationByDelta(newPos - PrevPos);
            PrevPos = newPos;
            transform.position = newPos;

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
            // 좌표 타겟까지 도달했을 때 기본 처리는 소멸.
            Destroy(gameObject);
        }
        #endregion

        #region Visual Helpers
        protected void UpdateFlipByDefaultDirection()
        {
            if (StruckEffect == null || !ProjectileEffect) return;

            ShouldFlip = false;
            // 기본 방향 기준으로 현재 Start→Target 의 좌우를 판단하여 Flip
            if (StruckEffect.DefaultDirection == ConfigCommon.DirectionType.Right && TargetPoint.x < StartPoint.x)
                ShouldFlip = true;
            else if (StruckEffect.DefaultDirection == ConfigCommon.DirectionType.Left && TargetPoint.x > StartPoint.x)
                ShouldFlip = true;

            ProjectileEffect.SetFlip(ShouldFlip);
        }

        protected void ApplyRotationByDelta(Vector2 delta)
        {
            // 이동 벡터 기준 Z-회전. Flip은 스프라이트 반전만 담당.
            if (delta.sqrMagnitude <= 0.0001f) return;
            float angle = Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg;
            // 기본 방향이 "왼쪽(-X 방향)"일 경우, 180도 보정
            if (StruckEffect.DefaultDirection == ConfigCommon.DirectionType.Left)
            {
                if (Direction.x < 0)
                {
                    angle += 180;
                }
            }
            transform.rotation = Quaternion.Euler(0f, 0f, angle);
        }

        protected bool IsInCameraView()
        {
            var cam = SceneGame.Instance.mainCamera;
            if (!cam) return true; // 카메라 없으면 판정 생략
            Vector3 v = cam.WorldToViewportPoint(transform.position);
            return v.x is >= 0f and <= 1f && v.y is >= 0f and <= 1f;
        }
        #endregion

        #region Collision
        private void OnTriggerEnter2D(Collider2D other)
        {
            // 플레이어 ↔ 몬스터 상호 충돌만 체크
            if (!FromCharacter) return;

            bool fromMonster = FromCharacter.CompareTag(ConfigTags.GetValue(ConfigTags.Keys.Monster));
            bool toPlayer    = other.CompareTag(ConfigTags.GetValue(ConfigTags.Keys.Player));
            bool fromPlayer  = FromCharacter.CompareTag(ConfigTags.GetValue(ConfigTags.Keys.Player));
            bool toMonster   = other.CompareTag(ConfigTags.GetValue(ConfigTags.Keys.Monster));

            if ((fromMonster && toPlayer) || (fromPlayer && toMonster))
            {
                var area = other.GetComponent<CharacterHitArea>();
                if (area) OnHitTarget(area);
                return;
            }

            // 지면과 충돌 시 파괴(맵에 박히는 타입이 아니라면)
            if (other.CompareTag(ConfigTags.GetValue(ConfigTags.Keys.MapGround)))
            {
                GcLogger.Log("[Projectile] Destroyed by MapGround.");
                Destroy(gameObject);
            }
        }

        protected virtual void OnHitTarget(CharacterHitArea area)
        {
            ShowHitEffect();

            if (!area) return;

            var md = new MetadataDamage
            {
                damage     = Damage,
                attacker   = FromCharacter ? FromCharacter.gameObject : null,
                damageType = ConfigCommon.DamageType.Physic // TODO: 테이블 기반 타입 적용 시 확장
            };
            area.target?.TakeDamage(md);
        }

        protected void ShowHitEffect()
        {
            // HitEffect가 따로 있으면 발사체는 즉시 소멸하고 Hit 이펙트를 새로 생성.
            if (Info.HitEffectUid > 0)
            {
                Destroy(gameObject);

                var hit = EffectManager.CreateEffect(Info.HitEffectUid);
                if (!hit) return;

                hit.SetCreateCharacter(FromCharacter);
                hit.transform.position = transform.position;
                hit.SetFlip(ShouldFlip);
            }
            else
            {
                // 별도 HitEffect가 없으면 현재 이펙트의 End 애니를 재생
                ProjectileEffect?.PlayEndAnimation();
            }
        }
        #endregion

        #region External Setters
        public void SetFromCharacter(CharacterBase character) => FromCharacter = character;
        public void SetDamage(long value) => Damage = value;
        #endregion
    }
}
