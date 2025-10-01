using UnityEngine;

namespace GGemCo2DCore
{
    /// <summary>
    /// 외부 트리거(Trigger Collider)에서 발생한 접촉 이벤트를 Trap으로 전달하고,
    /// 지정한 동작 타입(Start/End/Toggle)에 따라 트랩을 제어합니다.
    /// - 감지만 담당하며, 실제 상태 전이는 Trap(DefaultObjectTrap 파생)이 처리합니다.
    /// - Layer/Tag 필터, Enter/Stay/Exit 포워딩 옵션 제공
    /// </summary>
    public sealed class TrapTriggerDetector : MonoBehaviour
    {
        private enum TriggerActionType { Start, End, Toggle }

        [Header("타깃 Trap")]
        [Tooltip("트리거 이벤트를 전달받아 동작할 Trap (DefaultObjectTrap 파생)")]
        private DefaultObjectTrap _targetTrap;

        [Header("트리거 콜라이더")]
        [Tooltip("감지 영역으로 사용할 Trigger Collider2D. 비워두면 자체/자식에서 자동 검색(최초 1회)")]
        private Collider2D _triggerRange;

        [Space(6)]
        [Header("동작 타입")]
        [Tooltip("Start: OnTrigger 시 트랩 시작만 요청\nEnd: 트랩 종료만 요청 (ITrapTriggerController 필요)\nToggle: 동작 중이면 종료, 정지 중이면 시작")]
        [SerializeField] private TriggerActionType actionType = TriggerActionType.Start;

        [Space(6)]
        [Header("전달 옵션")]
        [Tooltip("OnTriggerEnter2D 발생 시 Trap으로 이벤트를 전달할지 여부")]
        [SerializeField] private bool forwardOnEnter = true;

        [Tooltip("OnTriggerStay2D 발생 시 Trap으로 이벤트를 전달할지 여부\n활성화 시 Enter 시점에 수면 방지(NeverSleep) 처리")]
        [SerializeField] private bool forwardOnStay;

        [Tooltip("OnTriggerExit2D 발생 시 Trap으로 이벤트를 전달할지 여부\n활성화 시 Exit 시점에 수면 모드 원복(StartAwake) 처리")]
        [SerializeField] private bool forwardOnExit;

        [Space(6)]
        [Header("필터 옵션")]
        [Tooltip("이 레이어 마스크에 포함된 Collider만 허용 (기본: 전체)")]
        private LayerMask _layerMask = ~0;

        [Tooltip("허용 태그 (비워둘 수 없음). 해당 태그의 Collider만 통과\n프로젝트 공용 ConfigTags.Keys 사용")]
        private const ConfigTags.Keys RequiredTag = ConfigTags.Keys.Player;

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
                GcLogger.LogError("[TrapTriggerDetector] Trigger Collider2D가 없습니다.");
                enabled = false; return;
            }
            SetTriggerEnabled(true);
        }
        private void OnEnable() => SetTriggerEnabled(true);
        private void Start()
        {
            if (!_targetTrap)
                GcLogger.LogWarning("[TrapTriggerDetector] targetTrap 미할당. 이벤트가 전달되지 않습니다.");
        }
        private void OnDisable() => SetTriggerEnabled(false);
#if UNITY_EDITOR
        private void OnValidate() { if (_triggerRange) _triggerRange.isTrigger = true; }
#endif

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!forwardOnEnter) return;
            if (!PassesFilter(other)) return;
            if (!IsPlayerHitArea(other, out var player)) return;

            if (forwardOnStay) _targetTrap?.OnStay(true, player); // 수면 방지
            HandleTrigger(other);
        }
        private void OnTriggerStay2D(Collider2D other)
        {
            if (!forwardOnStay) return;
            if (!PassesFilter(other)) return;
            if (!IsPlayerHitArea(other, out var _)) return;
            HandleTrigger(other);
        }
        private void OnTriggerExit2D(Collider2D other)
        {
            if (!forwardOnExit) return;
            if (!PassesFilter(other)) return;
            if (!IsPlayerHitArea(other, out var _)) return;

            if (forwardOnStay) _targetTrap?.OnStay(false, null); // 슬립 모드 원복
            HandleTrigger(other);
        }

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
                    _targetTrap.OnTrigger(other); break;
                case TriggerActionType.End:
                    if (external != null) external.RequestEnd();
                    else GcLogger.LogWarning("[TrapTriggerDetector] End 제어 미지원(ITrapTriggerController).");
                    break;
                case TriggerActionType.Toggle:
                    if (external != null)
                    {
                        if (external.IsActive) external.RequestEnd(); else external.RequestStart(other);
                    }
                    else
                    {
                        GcLogger.LogWarning("[TrapTriggerDetector] Toggle 제어 미지원(ITrapTriggerController). Start로 폴백.");
                        _targetTrap.OnTrigger(other);
                    }
                    break;
            }
        }

        private bool PassesFilter(Collider2D other)
        {
            if (!other) return false;
            if (((_layerMask.value >> other.gameObject.layer) & 1) == 0) return false;
            if (!other.CompareTag(ConfigTags.GetValue(RequiredTag))) return false;
            return true;
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