using UnityEngine;

namespace GGemCo2DCore
{
    /// <summary>
    /// 공격 범위 전용 Trigger Collider.
    /// - Player HitArea 감지 후, 대상 Trap(DefaultObjectTrap 파생)으로 Enter/Stay/Exit 이벤트 전달
    /// - 공격 판정 On/Off는 SetTriggerEnabled로 제어
    /// </summary>
    public class ObjectTrapAttackRange : MonoBehaviour
    {
        [Header("타깃 Trap")]
        [Tooltip("트리거 이벤트를 전달받아 동작할 Trap (DefaultObjectTrap 파생)")]
        private DefaultObjectTrap _targetTrap;

        [Header("트리거 콜라이더")]
        [Tooltip("감지 영역으로 사용할 Trigger Collider2D. 비워두면 자체/자식에서 자동 검색(최초 1회)")]
        private Collider2D _triggerRange;

        public void SetTargetTrap(DefaultObjectTrap trap) => _targetTrap = trap;
        public void SetTriggerEnabled(bool set)
        {
            if (!_triggerRange) return;
            _triggerRange.enabled = set; _triggerRange.isTrigger = set;
        }

        private void Awake()
        {
            _triggerRange = GetComponent<Collider2D>();
            if (!_triggerRange)
            {
                GcLogger.LogError("[ObjectTrapAttackRange] Trigger Collider2D가 없습니다.");
                enabled = false; return;
            }

            SetTriggerEnabled(false); // 기본 비활성
        }
        private void Start()
        {
            if (_targetTrap) return;
            GcLogger.LogWarning("[ObjectTrapAttackRange] targetTrap 미할당. 이벤트가 전달되지 않습니다.");
            enabled = false;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!IsPlayerHitArea(other, out var player)) return;
            (_targetTrap as ITrapAttackRangeHandlerEnter)?.OnEnter(player);
        }
        private void OnTriggerStay2D(Collider2D other)
        {
            if (!IsPlayerHitArea(other, out var player)) return;
            (_targetTrap as ITrapAttackRangeHandlerStay)?.OnStay(player);
        }
        private void OnTriggerExit2D(Collider2D other)
        {
            if (!IsPlayerHitArea(other, out var player)) return;
            (_targetTrap as ITrapAttackRangeHandlerExit)?.OnExit(player);
        }

        private bool IsPlayerHitArea(Collider2D other, out CharacterBase player)
        {
            player = null; if (!other) return false;
            if (!other.CompareTag(ConfigTags.GetValue(ConfigTags.Keys.Player))) return false;
            var hitArea = other.GetComponent<CharacterHitArea>();
            if (!hitArea) return false; player = hitArea.target; return player != null;
        }
    }
}