using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace GGemCo2DCore
{
    /// <summary>
    /// Button/Toggle 공용 클릭 사운드 브로드캐스터
    /// - Button: onClick에 반응
    /// - Toggle: onValueChanged에 반응(옵션으로 On/Off 선택)
    /// - triggerOnlyOnUserInteraction=true면 IPointerClick/ISubmit 기반으로만 재생
    /// </summary>
    [DisallowMultipleComponent]
    public class ClickSoundEventBroadcaster : MonoBehaviour, IClickSoundEventTrigger,
        IPointerDownHandler, IPointerClickHandler, ISubmitHandler
    {
        private const int InvalidPointerId = int.MinValue;

        [Header("Sound")]
        [Tooltip("사운드 고유 ID (우선순위: 이 값이 있을 경우 우선 적용)")]
        public int soundUid;

        [Tooltip("Sound Type Enum (soundUid가 없을 경우 사용)")]
        public SoundConstants.UIButtonType type = SoundConstants.UIButtonType.Default;

        [Header("Toggle Options")]
        [Tooltip("Toggle일 때 선택(On)될 때 재생")]
        public bool playOnToggleOn = true;

        [Tooltip("Toggle일 때 해제(Off)될 때도 재생")]
        public bool playOnToggleOff = false;

        [Header("Trigger Mode")]
        [Tooltip("사용자 상호작용(클릭/Submit)에만 반응. 프로그래매틱 변경에는 반응하지 않음")]
        [SerializeField] private bool triggerOnlyOnUserInteraction = true;

        private Button _button;
        private Toggle _toggle;
        private Selectable _selectable;
        private bool _controlEventsRegistered;
        private bool _pointerInteractionAccepted;
        private bool _acceptedInteractionSoundDispatched;
        private int _acceptedPointerId = InvalidPointerId;
        private int _lastControlActivationFrame = -1;
        private bool _lastObservedToggleState;

        /// <summary>
        /// 같은 GameObject의 Button 또는 Toggle 참조를 최초로 준비합니다.
        /// </summary>
        private void Awake()
        {
            CacheSelectableReferences();
        }

        /// <summary>
        /// 재활성화 또는 Domain Reload 이후 Selectable 참조와 제어 이벤트 구독을 복구합니다.
        /// </summary>
        private void OnEnable()
        {
            CacheSelectableReferences();
            ResetInteractionState();
            RegisterControlEvents();
        }

        /// <summary>
        /// 비활성화될 때 제어 이벤트 구독을 해제합니다.
        /// 클릭 콜백 자체가 비활성화를 발생시킨 경우를 위해 현재 입력 승인은 다음 활성화 전까지 보존합니다.
        /// </summary>
        private void OnDisable()
        {
            UnregisterControlEvents();
            // 클릭 콜백이 GameObject를 비활성화해도 현재 승인된 클릭 사운드는 이어서 발행될 수 있어야 합니다.
            // 남은 상태는 다음 OnEnable에서 초기화하여 이전 입력이 재활성화 이후로 넘어가지 않게 합니다.
        }

        /// <summary>
        /// 파괴 시점에도 남아 있을 수 있는 제어 이벤트 구독을 안전하게 해제합니다.
        /// </summary>
        private void OnDestroy()
        {
            UnregisterControlEvents();
        }

        /// <summary>
        /// 같은 GameObject에서 Button, Toggle 및 대표 Selectable 참조를 다시 조회합니다.
        /// Toggle과 Button이 함께 있으면 기존 Toggle 우선 처리 정책을 유지합니다.
        /// </summary>
        private void CacheSelectableReferences()
        {
            _button = GetComponent<Button>();
            _toggle = GetComponent<Toggle>();
            _selectable = _toggle != null ? _toggle : _button;
        }

        /// <summary>
        /// Button과 Toggle이 실제 활성 동작을 발생시킨 시점을 확인하기 위한 이벤트를 중복 없이 구독합니다.
        /// 사용자 입력 전용 모드에서는 승인 사실만 기록하고, 일반 모드에서는 해당 이벤트에서 바로 재생합니다.
        /// </summary>
        private void RegisterControlEvents()
        {
            if (_controlEventsRegistered)
            {
                return;
            }

            if (_button != null)
            {
                _button.onClick.AddListener(OnButtonActivated);
            }

            if (_toggle != null)
            {
                _toggle.onValueChanged.AddListener(OnToggleChanged);
            }

            _controlEventsRegistered = _button != null || _toggle != null;
        }

        /// <summary>
        /// Button과 Toggle에 등록한 제어 이벤트를 해제합니다.
        /// </summary>
        private void UnregisterControlEvents()
        {
            if (!_controlEventsRegistered)
            {
                return;
            }

            if (_button != null)
            {
                _button.onClick.RemoveListener(OnButtonActivated);
            }

            if (_toggle != null)
            {
                _toggle.onValueChanged.RemoveListener(OnToggleChanged);
            }

            _controlEventsRegistered = false;
        }

        /// <summary>
        /// 직접 지정된 클릭 사운드 UID를 반환합니다.
        /// </summary>
        /// <returns>직접 지정된 사운드 UID이며, 지정되지 않았으면 0입니다.</returns>
        public int GetSoundId() => soundUid;

        /// <summary>
        /// 사운드 UID가 없을 때 사용할 UI 버튼 사운드 타입을 반환합니다.
        /// </summary>
        /// <returns>현재 설정된 UI 버튼 사운드 타입입니다.</returns>
        public SoundConstants.UIButtonType GetSoundType() => type;

        /// <summary>
        /// 포인터를 누른 시점에 Button 또는 Toggle이 입력 가능한 상태였는지 기록합니다.
        /// 클릭 콜백이 이후 interactable을 변경하더라도 시작 시점의 유효성을 보존하기 위해 사용합니다.
        /// </summary>
        /// <param name="eventData">포인터 식별자와 버튼 정보를 포함한 이벤트 데이터입니다.</param>
        public void OnPointerDown(PointerEventData eventData)
        {
            ResetPointerInteractionState();
            if (!triggerOnlyOnUserInteraction ||
                eventData == null ||
                eventData.button != PointerEventData.InputButton.Left ||
                !CanBeginUserInteraction())
            {
                return;
            }

            _pointerInteractionAccepted = true;
            _acceptedPointerId = eventData.pointerId;
        }

        /// <summary>
        /// 사용자 포인터 클릭을 받아 활성화된 Button 또는 Toggle의 클릭 사운드를 요청합니다.
        /// Unity Button/Toggle과 동일하게 왼쪽 버튼 입력만 유효한 클릭으로 처리합니다.
        /// </summary>
        /// <param name="eventData">포인터 버튼 정보를 포함한 이벤트 데이터입니다.</param>
        public void OnPointerClick(PointerEventData eventData)
        {
            if (!triggerOnlyOnUserInteraction ||
                eventData == null ||
                eventData.button != PointerEventData.InputButton.Left)
            {
                return;
            }

            bool pointerInteractionAccepted =
                _pointerInteractionAccepted &&
                _acceptedPointerId == eventData.pointerId;
            bool soundAlreadyDispatched = _acceptedInteractionSoundDispatched;
            ResetPointerInteractionState();
            if (!pointerInteractionAccepted ||
                soundAlreadyDispatched ||
                !WasControlActivationAccepted())
            {
                return;
            }

            if (_toggle != null)
            {
                if (ShouldPlayForToggleState(ResolveObservedToggleState()))
                {
                    DispatchClickSound();
                }
            }
            else
            {
                DispatchClickSound();
            }
        }

        /// <summary>
        /// 키보드 또는 게임패드 Submit 입력을 받아 활성화된 Button 또는 Toggle의 클릭 사운드를 요청합니다.
        /// </summary>
        /// <param name="eventData">Submit 입력 이벤트 데이터입니다.</param>
        public void OnSubmit(BaseEventData eventData)
        {
            if (!triggerOnlyOnUserInteraction)
            {
                return;
            }

            if (!WasControlActivationAccepted())
            {
                return;
            }

            if (_toggle != null)
            {
                if (ShouldPlayForToggleState(ResolveObservedToggleState()))
                {
                    DispatchClickSound();
                }
            }
            else
            {
                DispatchClickSound();
            }
        }

        /// <summary>
        /// Button이 실제 onClick을 발생시킨 프레임을 기록하거나 일반 트리거 모드에서 사운드를 즉시 발행합니다.
        /// Button 콜백이 interactable을 변경해도 onClick이 발생했다는 사실을 승인 근거로 사용합니다.
        /// </summary>
        private void OnButtonActivated()
        {
            _lastControlActivationFrame = Time.frameCount;
            if (!triggerOnlyOnUserInteraction)
            {
                DispatchClickSound();
                return;
            }

            if (_pointerInteractionAccepted && !_acceptedInteractionSoundDispatched)
            {
                DispatchClickSound();
                _acceptedInteractionSoundDispatched = true;
            }
        }

        /// <summary>
        /// Toggle 값 변경 이벤트를 현재 On/Off 재생 정책에 따라 처리합니다.
        /// 사용자 입력 전용 모드가 아닐 때는 프로그래밍 방식의 값 변경도 이 경로로 들어옵니다.
        /// </summary>
        /// <param name="isOn">변경된 Toggle 선택 상태입니다.</param>
        private void OnToggleChanged(bool isOn)
        {
            _lastControlActivationFrame = Time.frameCount;
            _lastObservedToggleState = isOn;
            if (!ShouldPlayForToggleState(isOn))
            {
                return;
            }

            if (!triggerOnlyOnUserInteraction)
            {
                DispatchClickSound();
                return;
            }

            if (_pointerInteractionAccepted && !_acceptedInteractionSoundDispatched)
            {
                DispatchClickSound();
                _acceptedInteractionSoundDispatched = true;
            }
        }

        /// <summary>
        /// 현재 Toggle 상태가 설정된 클릭 사운드 재생 대상인지 확인합니다.
        /// </summary>
        /// <param name="isOn">확인할 Toggle 선택 상태입니다.</param>
        /// <returns>현재 상태에서 클릭 사운드를 재생해야 하면 <see langword="true"/>입니다.</returns>
        private bool ShouldPlayForToggleState(bool isOn)
        {
            return isOn ? playOnToggleOn : playOnToggleOff;
        }

        /// <summary>
        /// 연결된 Selectable이 새로운 사용자 상호작용을 시작할 수 있는 상태인지 확인합니다.
        /// Button/Toggle의 enabled, interactable 및 상위 CanvasGroup 상호작용 정책을 함께 반영합니다.
        /// </summary>
        /// <returns>사용자 상호작용을 시작할 수 있으면 <see langword="true"/>입니다.</returns>
        private bool CanBeginUserInteraction()
        {
            return isActiveAndEnabled &&
                   _selectable != null &&
                   _selectable.IsActive() &&
                   _selectable.IsInteractable();
        }

        /// <summary>
        /// 현재 프레임에 Button/Toggle 활성 이벤트가 확인되었거나 아직 Selectable이 입력 가능한지 확인합니다.
        /// 제어 콜백이 먼저 실행되어 interactable을 끈 경우에도 활성 이벤트 기록으로 정상 클릭을 인정합니다.
        /// </summary>
        /// <returns>현재 사용자 입력을 정상 활성 동작으로 인정할 수 있으면 <see langword="true"/>입니다.</returns>
        private bool WasControlActivationAccepted()
        {
            return _lastControlActivationFrame == Time.frameCount ||
                   CanBeginUserInteraction();
        }

        /// <summary>
        /// 현재 프레임에 관찰된 Toggle 상태가 있으면 해당 상태를 반환하고, 없으면 현재 Toggle 상태를 반환합니다.
        /// </summary>
        /// <returns>클릭 사운드 On/Off 정책 판정에 사용할 Toggle 상태입니다.</returns>
        private bool ResolveObservedToggleState()
        {
            return _lastControlActivationFrame == Time.frameCount
                ? _lastObservedToggleState
                : _toggle != null && _toggle.isOn;
        }

        /// <summary>
        /// 포인터 입력 승인 상태를 초기화합니다.
        /// </summary>
        private void ResetPointerInteractionState()
        {
            _pointerInteractionAccepted = false;
            _acceptedInteractionSoundDispatched = false;
            _acceptedPointerId = InvalidPointerId;
        }

        /// <summary>
        /// 비활성화와 재활성화 경계에서 이전 입력 및 제어 활성 기록을 모두 초기화합니다.
        /// </summary>
        private void ResetInteractionState()
        {
            ResetPointerInteractionState();
            _lastControlActivationFrame = -1;
            _lastObservedToggleState = false;
        }

        /// <summary>
        /// 이미 승인된 Button 또는 Toggle 동작에 대한 클릭 사운드 이벤트를 발행합니다.
        /// 승인 후 콜백에서 interactable이 변경될 수 있으므로 이 단계에서는 현재 상태를 다시 검사하지 않습니다.
        /// </summary>
        private void DispatchClickSound()
        {
            ClickSoundEventDispatcher.Dispatch(this);
        }
    }
}
