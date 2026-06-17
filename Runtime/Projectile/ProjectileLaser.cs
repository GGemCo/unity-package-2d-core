using UnityEngine;

namespace GGemCo2DCore
{
    /// <summary>
    /// 레이저 전용 프로젝타일(히트스캔).
    /// - ProjectileBase 수명주기(Initialize/Launch)는 따르되, 이동 보간(Update)은 사용하지 않는다.
    /// - Raycast로 히트 지점을 계산하고, tickInterval로 지속 데미지를 적용한다.
    /// - 시각 표현은 IProjectileLaserVisual 구현체가 있을 때만 Endpoints를 전달한다.
    /// </summary>
    public class ProjectileLaser : ProjectileBase
    {
        [Header("Laser / Ray Settings")]
        [Tooltip("레이저 최대 사거리(월드 단위)")]
        [Min(0.01f)] public float MaxDistance = 1000f;

        [Tooltip("레이어 마스크(지면/캐릭터 등). 기본 전체")]
        public LayerMask HitMask = ~0;

        [Tooltip("틱 데미지 주기(초). 0이면 1회만 적용")]
        [Min(0f)] public float TickInterval = 1f;

        [Tooltip("지속 시간(초). 음수면 무한 지속(외부에서 Stop/Destroy)")]
        public float Duration = 3f;

        private float _elapsed;
        private float _tickAcc;
        private bool _oneShotApplied; // TickInterval==0 일 때 1회성 판정용

        private IProjectileLaserVisual _laserVisual;

        protected override void Start()
        {
            base.Start();
            _laserVisual = FindLaserVisual();
        }

        /// <summary>
        /// ProjectileBase.Update()는 보간 이동/화면 이탈 파괴를 수행합니다.
        /// 레이저는 그 루틴이 맞지 않으므로 여기서 완전히 재정의합니다.
        /// </summary>
        protected override void FixedUpdate()
        {
            if (!Initialized) return;

            // 수명 관리
            _elapsed += Time.deltaTime;
            if (Duration >= 0f && _elapsed >= Duration)
            {
                Destroy(gameObject);
                return;
            }

            // --- 레이캐스트 원점/방향 ---
            Vector3 start = StartPoint;

            Vector3 dir3;
            if (TargetObject != null)
            {
                dir3 = (TargetObject.transform.position - start).normalized;
                if (dir3.sqrMagnitude < 1e-6f)
                    dir3 = transform.right;
            }
            else if (TargetPoint != Vector2.zero)
            {
                dir3 = ((Vector3)TargetPoint - start).normalized;
                if (dir3.sqrMagnitude < 1e-6f)
                    dir3 = transform.right;
            }
            else
            {
                dir3 = transform.right;
                if (FromCharacter && FromCharacter.IsFlipped())
                    dir3 = -dir3;
            }

            Vector2 origin = start;
            Vector2 dir = ((Vector2)dir3).normalized;

            Vector3 end = start + (Vector3)(dir * MaxDistance);

            RaycastHit2D hit = Physics2D.Raycast(origin, dir, MaxDistance, HitMask);
            if (hit.collider != null)
            {
                end = hit.point;
                TryApplyTickDamage(hit.collider);
            }

            // 시각 표현 갱신(선택)
            _laserVisual?.SetEndpoints(start, end);
        }

        protected override void OnArrived()
        {
            // NOP
        }

        private void TryApplyTickDamage(Collider2D col)
        {
            if (!FromCharacter) return;

            // 플레이어↔몬스터 조합만 판정(레거시 정책 유지)
            bool fromMonster = FromCharacter.CompareTag(ConfigTags.GetValue(ConfigTags.Keys.Monster));
            bool toPlayer = col.CompareTag(ConfigTags.GetValue(ConfigTags.Keys.Player));

            bool fromPlayer = FromCharacter.CompareTag(ConfigTags.GetValue(ConfigTags.Keys.Player));
            bool toMonster = col.CompareTag(ConfigTags.GetValue(ConfigTags.Keys.Monster));

            if (!((fromMonster && toPlayer) || (fromPlayer && toMonster))) return;

            var area = col.GetComponent<CharacterHitArea>();
            if (!area) return;

            if (TickInterval <= 0f)
            {
                if (_oneShotApplied) return;
                _oneShotApplied = true;
                DealDamage(area);
                return;
            }

            _tickAcc += Time.deltaTime;
            if (_tickAcc >= TickInterval)
            {
                _tickAcc = 0f;
                DealDamage(area);
            }
        }

        private void DealDamage(CharacterHitArea area)
        {
            DamageCalculationBreakdown damageBreakdown = ResolveDamageBreakdown();
            var md = new MetadataDamage
            {
                damage = damageBreakdown != null ? damageBreakdown.TotalFinalDamage : Damage,
                attacker = FromCharacter ? FromCharacter.gameObject : gameObject,
                damageType = DamageType,
                DamageBreakdown = damageBreakdown,
                SkillUid = SkillUid,
                AttackId = AttackId
            };
            area.target?.TakeDamage(md);
        }

        /// <summary>
        /// 레거시 프로젝타일 레이저의 단일 데미지를 속성별 분해 결과로 변환합니다.
        /// </summary>
        /// <returns>대상 저항 적용 전의 데미지 분해 결과입니다.</returns>
        private DamageCalculationBreakdown ResolveDamageBreakdown()
        {
            CalculateManager calculateManager = CalculateManager.GetActive();
            return calculateManager != null
                ? calculateManager.CreateOutgoingDamageBreakdown(
                    Damage,
                    DamageType,
                    FromCharacter,
                    includeAttackerElementDamageParts: false)
                : null;
        }

        /// <summary>
        /// 레이저는 보간 이동을 하지 않으므로 이 메서드는 사용되지 않습니다.
        /// (ProjectileBase 요구사항 충족을 위해 형태만 남깁니다.)
        /// </summary>
        protected override Vector2 ComputePosition(float t) => transform.position;

        private IProjectileLaserVisual FindLaserVisual()
        {
            // Unity의 GetComponent는 인터페이스 직접 조회가 제한적이므로 MonoBehaviour에서 탐색한다.
            var behaviours = GetComponents<MonoBehaviour>();
            foreach (var b in behaviours)
            {
                if (b is IProjectileLaserVisual lv)
                    return lv;
            }
            return null;
        }
    }
}
