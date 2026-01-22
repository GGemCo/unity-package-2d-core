using UnityEngine;

namespace GGemCo2DCore
{
    /// <summary>
    /// 레이저 전용 프로젝타일
    /// - ProjectileBase 수명주기(Initialize/Start/Launch)는 그대로 따름
    /// - 이동 보간 대신 2D Raycast(히트스캔)로 충돌/길이 산출
    /// - EffectLaser.SetEndpoints(start, end)로 길이/방향을 프레임마다 갱신
    /// - tickInterval로 지속 데미지(0이면 1회성)
    /// </summary>
    public class ProjectileLaser : ProjectileBase
    {
        [Header("Laser / Ray Settings")]
        [Tooltip("레이저 최대 사거리(월드 단위)")]
        [Min(0.01f)] public float MaxDistance = 1000f;

        [Tooltip("레이어 마스크(지면/캐릭터 등). 기본 전체")]
        // public LayerMask HitMask = ~0;
        public LayerMask HitMask = ~0;

        [Tooltip("틱 데미지 주기(초). 0이면 1회만 적용")]
        [Min(0f)] public float TickInterval = 1f;

        [Tooltip("지속 시간(초). 음수면 무한 지속(외부에서 Stop/Destroy)")]
        public float Duration = 3f;

        [Header("Laser Visual")]
        [Tooltip("기본 굵기(월드 단위). EffectLaser에 전달")]
        [Min(0f)] public float DefaultThickness = 3f;

        private EffectLaser _laser;      // ProjectileEffect가 반드시 EffectLaser 여야 함
        private float _elapsed;
        private float _tickAcc;
        private bool _oneShotApplied;    // TickInterval==0 일 때 1회성 판정용

        /// <summary>
        /// ProjectileBase.Start()에서 EffectManager.CreateEffect(Info.EffectUid)를 호출합니다.
        /// 레이저는 해당 이펙트가 반드시 EffectLaser 타입이어야 합니다.
        /// </summary>
        protected override void Start()
        {
            base.Start();

            _laser = ProjectileEffect as EffectLaser;
            if (_laser == null)
            {
                // 이펙트 프리팹/EffectManager 분기가 미설정인 경우
                GcLogger.LogError("[ProjectileLaser] Effect is not EffectLaser. Check Effect prefab or EffectManager IsLaser flag.");
                enabled = false;
                return;
            }

            if (DefaultThickness > 0f)
                _laser.SetThickness(DefaultThickness);
            
            int monster = LayerMask.GetMask(ConfigLayer.GetValue(ConfigLayer.Keys.MonsterHitArea));
            HitMask = monster;
        }

        /// <summary>
        /// ProjectileBase.Update()는 보간 이동/회전/화면 이탈 파괴를 수행합니다.
        /// 레이저는 그 루틴이 맞지 않으므로 여기서 완전히 재정의합니다.
        /// </summary>
        protected override void Update()
        {
            if (!Initialized) return;

            // 수명 관리
            _elapsed += Time.deltaTime;
            if (Duration >= 0f && _elapsed >= Duration)
            {
                StopLaser();
                return;
            }

            // --- 레이캐스트 원점/방향 ---
            // 1) 기본 시작점: 시전자 위치(FromCharacter) + Info.StartPosition 오프셋
            Vector3 start = StartPoint;

            // 2) 방향:
            //    - 목표 좌표가 있으면 Start→Target 로컬 방향
            //    - 없으면 시전자의 바라보는 방향(오브젝트 +X, Flip 고려)
            Vector3 dir3;
            if (TargetObject != null)
            {
                // 고정 타겟이면 Y는 히트영역에서 샘플된 값이 이미 Launch에서 반영됨
                dir3 = (TargetObject.transform.position - start).normalized;
                if (dir3.sqrMagnitude < 1e-6f)
                    dir3 = transform.right; // 예외 보정
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
            Vector2 dir    = ((Vector2)dir3).normalized;

            // --- 레이캐스트 ---
            Vector3 end = start + (Vector3)(dir * MaxDistance);

            RaycastHit2D hit = Physics2D.Raycast(origin, dir, MaxDistance, HitMask);
            if (hit.collider != null)
            {
                end = hit.point;

                // 데미지 적용(Tick)
                TryApplyTickDamage(hit.collider);
            }

            // --- 이펙트 길이/방향 갱신 ---
            _laser.SetEndpoints(start, end);

            // 화면 이탈은 레이저의 start/end 어느 한쪽이 보이면 유지.
            // 필요 시 start/end 모두 뷰포트 밖이면 종료하는 정책으로 바꿀 수 있음.
        }

        /// <summary>
        /// ProjectileBase의 보간 완료 콜백은 레이저에선 사용하지 않음.
        /// </summary>
        protected override void OnArrived()
        {
            // NOP
        }

        private void TryApplyTickDamage(Collider2D col)
        {
            if (!FromCharacter) return;

            // 플레이어↔몬스터 조합만 판정
            bool fromMonster = FromCharacter.CompareTag(ConfigTags.GetValue(ConfigTags.Keys.Monster));
            bool toPlayer    = col.CompareTag(ConfigTags.GetValue(ConfigTags.Keys.Player));

            bool fromPlayer  = FromCharacter.CompareTag(ConfigTags.GetValue(ConfigTags.Keys.Player));
            bool toMonster   = col.CompareTag(ConfigTags.GetValue(ConfigTags.Keys.Monster));

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
            var md = new MetadataDamage
            {
                damage     = Damage,
                attacker   = FromCharacter ? FromCharacter.gameObject : gameObject,
                damageType = ConfigCommon.DamageType.Physic // TODO: 테이블 기반으로 확장
            };
            area.target?.TakeDamage(md);
        }

        private void StopLaser()
        {
            // End 애니 후 DefaultEffect 파이프라인으로 소멸
            ProjectileEffect?.PlayEndAnimation();
            // ProjectileEffect가 End 애니로 파괴될 때, 이 컴포넌트가 붙은 GO도 함께 정리되도록
            // 이 오브젝트를 Effect의 자식으로 운용하거나(현재 구조상 Projectile가 부모),
            // End 애니 종료 이벤트에서 부모를 Destroy 처리하도록 DefaultEffect 설정을 조정하세요.
            Destroy(gameObject);
        }

        /// <summary>
        /// 레이저는 보간 이동을 하지 않으므로 이 메서드는 사용되지 않습니다.
        /// (ProjectileBase 요구사항 충족을 위해 형태만 남깁니다.)
        /// </summary>
        protected override Vector2 ComputePosition(float t) => transform.position;
    }
}
