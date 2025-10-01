using UnityEngine;

namespace GGemCo2DCore
{
    /// <summary>
    /// 외부 트리거(Trigger Collider)에서 발생한 접촉 이벤트를 Trap으로 전달하고,
    /// 지정한 동작 타입(start/end/toggle)에 따라 트랩을 제어합니다.
    /// - 감지만 담당하며, 실제 상태 전이는 Trap 쪽(DefaultObjectTrap 파생)이 처리합니다.
    /// - 필터(Layer/Tag)와 전달 이벤트(Enter/Stay/Exit)를 선택적으로 사용합니다.
    /// - end/toggle은 선택 인터페이스(ITrapExternalControl)를 구현했을 때 정밀 제어가 가능합니다.
    ///   (미구현 시 안전 폴백 로직으로 동작)
    /// 사용 계약:
    /// 1) triggerRange에는 Trigger용 Collider2D를 할당(없으면 자체/자식에서 자동 탐색)
    /// 2) triggerRange.isTrigger = true 강제
    /// 3) targetTrap에 제어 대상 Trap 컴포넌트를 연결
    /// </summary>
    public sealed class TrapTriggerDetector : MonoBehaviour
    {
        // ---- 타입 정의 ----
        private enum TriggerActionType
        {
            Start,  // 트리거 시 "시작"만 요청
            End,    // 트리거 시 "종료"만 요청
            Toggle  // 트리거 시 동작 중이면 종료, 정지 중이면 시작
        }

        // ----------------------------
        // Serialized Settings (Designer)
        // ----------------------------

        [Header("타깃 Trap")]
        [Tooltip("트리거 이벤트를 전달받아 동작할 Trap (DefaultObjectTrap 파생)")]
        private DefaultObjectTrap _targetTrap;

        [Header("트리거 콜라이더")]
        [Tooltip("감지 영역으로 사용할 Trigger Collider2D. 비워두면 자체/자식에서 자동 검색(최초 1회)")]
        private Collider2D _triggerRange;

        [Space(6)]
        [Header("동작 타입")]
        [Tooltip("Start: 시작만 / End: 종료만 / Toggle: 상태 반전(동작 중→종료, 정지→시작)" +
                 "\nToggle 이고, forwardOnExit가 true 일 경우, Enter일때 Start 요청, Exit일때 End 요청")]
        [SerializeField] private TriggerActionType actionType = TriggerActionType.Start;

        [Space(6)]
        [Header("전달 옵션")]
        [Tooltip("OnTriggerEnter2D 발생 시 Trap으로 전달/제어 수행")]
        [SerializeField] private bool forwardOnEnter = true;

        [Tooltip("OnTriggerStay2D 발생 시 Trap으로 전달/제어 수행")]
        [SerializeField] private bool forwardOnStay;

        [Tooltip("OnTriggerExit2D 발생 시 Trap으로 전달/제어 수행")]
        [SerializeField] private bool forwardOnExit;

        [Space(6)]
        [Header("필터 옵션")]
        [Tooltip("이 레이어 마스크에 포함된 Collider만 전달 (없으면 모든 레이어 허용)")]
        private LayerMask _layerMask = ~0; // 기본: 전체

        [Tooltip("비워두면 태그 무시. 값이 있으면 해당 태그와 일치할 때만 전달")] 
        private readonly ConfigTags.Keys _requiredTag = ConfigTags.Keys.Player;

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
            // 트리거 콜라이더 결합(없으면 자체→자식 순으로 1회 검색)
            if (!_triggerRange)
            {
                _triggerRange = GetComponent<Collider2D>();
                if (!_triggerRange) _triggerRange = GetComponentInChildren<Collider2D>();
            }

            if (!_triggerRange)
            {
                GcLogger.LogError("[TrapTriggerDetector] Trigger Collider2D가 없습니다.");
                enabled = false;
                return;
            }

            // 반드시 Trigger로 동작
            SetTriggerEnabled(true);
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
            {
                GcLogger.LogWarning("[TrapTriggerDetector] targetTrap이 할당되지 않았습니다. 이벤트가 전달되지 않습니다.");
                return;
            }
        }

        private void OnDisable()
        {
            // 비활성화 시 충돌 이벤트 차단
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
            if (!forwardOnEnter) return;
            if (!PassesFilter(other)) return;
            if (!IsPlayerHitArea(other, out var player)) return;
            
            // Stay 체크를 위해서 플레이어의 Rigidbody의 Sleep 모드를 변경한다. 
            if (forwardOnStay)
            {
                _targetTrap.OnStay(true, player);
            }
            
            HandleTrigger(other);
        }

        private void OnTriggerStay2D(Collider2D other)
        {
            if (!forwardOnStay) return;
            if (!PassesFilter(other)) return;
            if (!IsPlayerHitArea(other, out var player)) return;
            
            HandleTrigger(other);
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            if (!forwardOnExit) return;
            if (!PassesFilter(other)) return;
            if (!IsPlayerHitArea(other, out var player)) return;
            
            if (forwardOnStay)
            {
                _targetTrap.OnStay(false,null);
            }
            HandleTrigger(other);
        }

        // ----------------------------
        // Core Control
        // ----------------------------

        /// <summary>
        /// 필터 통과 후, 지정한 actionType에 따라 Trap에 제어를 전달합니다.
        /// - Start: 기본 OnTrigger 호출로 시작을 요청(기존 호환)
        /// - End: ITrapExternalControl 구현 시 RequestEnd 호출
        /// - Toggle: ITrapExternalControl의 IsActive 기준으로 Start/End 분기
        ///   (인터페이스 미구현 시: Start로 폴백, End/Toggle은 경고 로그)
        /// </summary>
        private void HandleTrigger(Collider2D other)
        {
            if (!_targetTrap)
            {
                GcLogger.LogWarning("[TrapTriggerDetector] targetTrap 미할당. 호출이 무시되었습니다.");
                return;
            }

            var external = _targetTrap as ITrapTriggerController;

            switch (actionType)
            {
                case TriggerActionType.Start:
                    // 기존 규약: 외부 트리거로 '시작'을 요청할 때 OnTrigger 사용
                    _targetTrap.OnTrigger(other);
                    break;

                case TriggerActionType.End:
                    if (external != null)
                    {
                        external.RequestEnd();
                    }
                    else
                    {
                        // 폴백: 지원하지 않으면 경고(필요 시 targetTrap에 전용 API 추가 권장)
                        GcLogger.LogWarning("[TrapTriggerDetector] Target trap이 End 제어(ITrapExternalControl)를 지원하지 않습니다.");
                    }
                    break;

                case TriggerActionType.Toggle:
                    if (external != null)
                    {
                        if (external.IsActive)
                            external.RequestEnd();
                        else
                            external.RequestStart(other);
                    }
                    else
                    {
                        // 폴백: 상태 질의가 불가하므로 Start만 시도
                        GcLogger.LogWarning("[TrapTriggerDetector] Target trap이 Toggle 제어를 위한 상태 질의(ITrapExternalControl.IsActive)를 지원하지 않습니다. Start로 폴백합니다.");
                        _targetTrap.OnTrigger(other);
                    }
                    break;
            }
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
            if (!other.CompareTag(ConfigTags.GetValue(_requiredTag)))
                return false;

            return true;
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
