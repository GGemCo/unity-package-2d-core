using UnityEngine;

namespace GGemCo2DCore
{
    public class ObjectTrapAttackRange : MonoBehaviour
    {
        [Header("타깃 Trap")]
        [Tooltip("트리거 이벤트를 전달받아 동작할 Trap (DefaultObjectTrap 파생)")]
        private DefaultObjectTrap _targetTrap;
        
        [Header("트리거 콜라이더")]
        [Tooltip("감지 영역으로 사용할 Trigger Collider2D. 비워두면 자체/자식에서 자동 검색(최초 1회)")]
        private Collider2D _triggerRange;
        
        // ----------------------------
        // Public API
        // ----------------------------

        /// <summary>런타임에서 Trap 할당(의존성 주입)</summary>
        public void SetTargetTrap(DefaultObjectTrap trap) => _targetTrap = trap;
        /// <summary>트리거 콜라이더 Enable/Disable</summary>
        public void SetTriggerEnabled(bool set)
        {
            if (!_triggerRange) return;
            _triggerRange.enabled = set;
            _triggerRange.isTrigger = set; // 안전장치
        }
        // ----------------------------
        // Unity Lifecycle
        // ----------------------------

        private void Awake()
        {
            _triggerRange = GetComponent<Collider2D>();
            SetTriggerEnabled(false);
            if (_triggerRange) return;
            GcLogger.LogError("공격 범위 Trigger Collider2D가 없습니다.");
            enabled = false;
        }
        private void Start()
        {
            if (_targetTrap) return;
            GcLogger.LogWarning("[TrapTriggerDetector] targetTrap이 할당되지 않았습니다. 이벤트가 전달되지 않습니다.");
            enabled = false;
        }
        // ----------------------------
        // Physics Callbacks (Forwarding)
        // ----------------------------

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!IsPlayerHitArea(other, out var player)) return;

            var attackController = _targetTrap as ITrapAttackRangeHandlerEnter;
            attackController?.OnEnter(player);
        }
        private void OnTriggerStay2D(Collider2D other)
        {
            if (!IsPlayerHitArea(other, out var player)) return;

            var attackController = _targetTrap as ITrapAttackRangeHandlerStay;
            attackController?.OnStay(player);
        }
        private void OnTriggerExit2D(Collider2D other)
        {
            if (!IsPlayerHitArea(other, out var player)) return;

            var attackController = _targetTrap as ITrapAttackRangeHandlerExit;
            attackController?.OnExit(player);
        }
        /// <summary>Player의 HitArea 콜라이더인지 검사하고 CharacterBase를 반환합니다.</summary>
        private bool IsPlayerHitArea(Collider2D other, out CharacterBase player)
        {
            player = null;
            if (!other) return false;

            // 태그 기반 필터 (ConfigTags 사용)
            if (!other.CompareTag(ConfigTags.GetValue(ConfigTags.Keys.Player))) return false;

            var hitArea = other.GetComponent<CharacterHitArea>();
            if (!hitArea) return false;

            player = hitArea.target;
            return player != null;
        }
    }
}