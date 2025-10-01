using UnityEngine;

namespace GGemCo2DCore
{
    /// <summary>
    /// 무한(지속) 공격 전용 함정
    /// - 트리거 내부에 있는 플레이어에게 일정 간격(timeTick)으로 지속 피해를 부여합니다.
    /// - Animator/Spine 애니메이션 연계 없이, 간결한 지속 타격 로직에만 집중합니다.
    /// </summary>
    public sealed class ObjectTrapInfinity : DefaultObjectTrap, ITrapAttackRangeHandlerEnter, ITrapAttackRangeHandlerStay, ITrapAttackRangeHandlerExit
    {
        // ------------- 직렬화 설정 -------------

        [Header("지속 타격 간격")]
        [Tooltip("트리거 내부에 머무는 동안, 몇 초마다 피해를 줄지 (쿨다운, 최소 0.01)")]
        [Min(0.01f)] [SerializeField] private float timeTick = 0.5f;
        
        // ------------- 내부 상태 -------------

        // 다음 피해 적용 가능 시각 (쿨다운)
        private float _nextTickTime;

        // ------------- 라이프사이클 -------------

        protected override void Awake()
        {
            base.Awake();
            
            SetAttackRangeEnabled(true);

            // 초기 쿨다운(진입 즉시 1틱이 들어가지 않도록 설정)
            _nextTickTime = 0f;
        }
        
        private void OnEnable()
        {
            SetPlayerInRange(null);
            _nextTickTime = 0f;
        }

        private void Start()
        {
            PlayAnimSafe(AnimAttack, true);
        }

#if UNITY_EDITOR
        protected override void OnValidate()
        {
            base.OnValidate();
            if (timeTick < 0.01f) timeTick = 0.01f;
        }
#endif

        // ------------- 트리거 로직 -------------

        public void OnEnter(CharacterBase player)
        {
            if (!player) return;

            SetPlayerInRange(player);

            // 수면 방지: 트리거 내부 정지 시에도 Stay가 안정 호출되도록
            playerInRange.SetRigidBody2DSleepMode(RigidbodySleepMode2D.NeverSleep);

            // 진입 직후 한 텀 쉬고 틱을 주고 싶다면 다음과 같이 지연:
            _nextTickTime = Time.time + timeTick;
            
            ApplyDamage(player);
        }

        public void OnExit(CharacterBase player)
        {
            if (!player) return;
            if (playerInRange != player) return;
            // 슬립 모드 원복
            playerInRange.SetRigidBody2DSleepMode(RigidbodySleepMode2D.StartAwake);
            playerInRange = null;
        }

        public void OnStay(CharacterBase player)
        {
            if (!player) return;
            if (playerInRange != null && playerInRange != player) playerInRange = player;

            // 쿨다운 체크
            if (Time.time < _nextTickTime) return;

            ApplyDamage(player);
            _nextTickTime = Time.time + timeTick;
        }
    }
}
