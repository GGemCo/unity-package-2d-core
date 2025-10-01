using UnityEngine;

namespace GGemCo2DCore
{
    /// <summary>
    /// 무한(지속) 공격 전용 함정
    /// - 트리거 내부에 있는 플레이어에게 일정 간격(timeTick)으로 지속 피해
    /// - 애니메이션은 attack 루프로 가볍게 유지
    /// </summary>
    public sealed class ObjectTrapInfinity : DefaultObjectTrap, ITrapAttackRangeHandlerEnter, ITrapAttackRangeHandlerStay, ITrapAttackRangeHandlerExit
    {
        [Header("지속 타격 간격(초)")]
        [Min(0.01f)] [SerializeField] private float timeTick = 0.5f;
        private float _nextTickTime;

        protected override void Awake()
        { base.Awake(); SetAttackRangeEnabled(true); _nextTickTime = 0f; }
        private void OnEnable() { SetPlayerInRange(null); _nextTickTime = 0f; }
        private void Start() { PlayAnimSafe(AnimAttack, true); }
#if UNITY_EDITOR
        protected override void OnValidate() { base.OnValidate(); if (timeTick < 0.01f) timeTick = 0.01f; }
#endif
        public void OnEnter(CharacterBase player)
        {
            if (!player) return;
            SetPlayerInRange(player);
            playerInRange.SetRigidBody2DSleepMode(RigidbodySleepMode2D.NeverSleep);
            _nextTickTime = Time.time + timeTick; // 진입 즉시 1틱 방지용 지연
            ApplyDamage(player);
        }
        public void OnStay(CharacterBase player)
        {
            if (!player) return;
            if (Time.time < _nextTickTime) return;
            ApplyDamage(player); _nextTickTime = Time.time + timeTick;
        }
        public void OnExit(CharacterBase player)
        {
            if (!player) return; if (playerInRange != player) return;
            playerInRange.SetRigidBody2DSleepMode(RigidbodySleepMode2D.StartAwake); playerInRange = null;
        }
    }
}