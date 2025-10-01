using UnityEngine;

namespace GGemCo2DCore
{
    /// <summary>
    /// 외부 트리거(Trigger Collider)에서 발생한 접촉 이벤트를 Trap으로 전달하는 감지기.
    /// - 감지만 담당하며, 실제 공격/상태 전이는 Trap 쪽(DefaultObjectTrap 파생)이 처리.
    /// - 레이어/태그 필터로 불필요한 호출을 차단.
    /// - Enter/Stay/Exit 전달 여부를 선택 가능.
    /// 사용 계약:
    /// 1) triggerRange에는 Trigger용 Collider2D를 할당(없으면 자식에서 자동 탐색)
    /// 2) isTrigger = true 강제
    /// 3) targetTrap에 이벤트를 전달받을 Trap 컴포넌트를 연결
    /// </summary>
    public sealed class TrapTriggerDetector : MonoBehaviour
    {
        // ----------------------------
        // Serialized Settings (Designer)
        // ----------------------------

        [Header("타깃 Trap")]
        [Tooltip("트리거 이벤트를 전달받아 동작할 Trap (DefaultObjectTrap 파생)")]
        private DefaultObjectTrap _targetTrap;

        [Header("트리거 콜라이더")]
        [Tooltip("감지 영역으로 사용할 Trigger Collider2D. 비워두면 자식에서 자동 검색(최초 1회)")]
        private Collider2D _triggerRange;

        [Space(6)]
        [Header("전달 옵션")]
        [Tooltip("OnTriggerEnter2D 발생 시 Trap으로 전달")]
        private readonly bool _forwardOnEnter = true;

        [Tooltip("OnTriggerStay2D 발생 시 Trap으로 전달")]
        private bool _forwardOnStay;

        [Tooltip("OnTriggerExit2D 발생 시 Trap으로 전달")]
        private bool _forwardOnExit;

        [Space(6)]
        [Header("필터 옵션")]
        [Tooltip("이 레이어 마스크에 포함된 Collider만 전달 (없으면 모든 레이어 허용)")]
        private LayerMask _layerMask = ~0; // 기본: 전체

        [Tooltip("비워두면 태그 무시. 값이 있으면 해당 태그와 일치할 때만 전달")]
        private readonly string _requiredTag = ConfigTags.GetValue(ConfigTags.Keys.Player);

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
            // 트리거 콜라이더 결합(없으면 자식에서 1회 검색)
            if (!_triggerRange)
            {
                _triggerRange = GetComponent<Collider2D>();
            }

            if (!_triggerRange)
            {
                GcLogger.LogError("[TrapTriggerDetector] Trigger Collider2D가 없습니다.");
                enabled = false;
                return;
            }

            // 반드시 Trigger로 동작
            _triggerRange.isTrigger = true;
        }

        private void OnEnable()
        {
            // 기본적으로 트리거 활성
            SetTriggerEnabled(true);
        }

        private void Start()
        {
            // Trap 유효성 체크(없어도 동작은 되나 경고)
            if (!_targetTrap)
                GcLogger.LogWarning("[TrapTriggerDetector] targetTrap이 할당되지 않았습니다. 이벤트가 전달되지 않습니다.");
        }

        private void OnDisable()
        {
            // 비활성화 시 충돌 이벤트가 들어오지 않도록 끔
            SetTriggerEnabled(false);
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            // 에디터에서 실수 방지: isTrigger 강제
            if (_triggerRange) _triggerRange.isTrigger = true;
        }
#endif

        // ----------------------------
        // Physics Callbacks (Forwarding)
        // ----------------------------

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!_forwardOnEnter) return;
            if (!PassesFilter(other)) return;

            // Trap으로 그대로 전달
            _targetTrap?.OnTrigger(other);
        }

        private void OnTriggerStay2D(Collider2D other)
        {
            if (!_forwardOnStay) return;
            if (!PassesFilter(other)) return;

            _targetTrap?.OnTrigger(other);
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            if (!_forwardOnExit) return;
            if (!PassesFilter(other)) return;

            _targetTrap?.OnTrigger(other);
        }

        // ----------------------------
        // Helpers
        // ----------------------------

        /// <summary>
        /// 레이어/태그 조건을 통과하는지 검사
        /// - layerMask: 포함 레이어만 허용
        /// - requiredTag: 비어있지 않으면 해당 태그만 허용
        /// </summary>
        private bool PassesFilter(Collider2D other)
        {
            if (!other) return false;

            // 레이어 필터
            int otherLayerBit = 1 << other.gameObject.layer;
            if ((_layerMask.value & otherLayerBit) == 0) return false;

            // 태그 필터(선택)
            if (!string.IsNullOrEmpty(_requiredTag) && !other.CompareTag(_requiredTag))
                return false;

            return true;
        }
    }
}
