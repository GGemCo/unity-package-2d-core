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
    public class ClickSoundEventBroadcaster : MonoBehaviour, IClickSoundEventTrigger, IPointerClickHandler, ISubmitHandler
    {
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

        /// <summary>
        /// 같은 GameObject의 Button 또는 Toggle을 캐시하고 설정된 입력 모드에 맞춰 이벤트를 연결합니다.
        /// </summary>
        private void Awake()
        {
            _button = GetComponent<Button>();
            _toggle = GetComponent<Toggle>();
            _selectable = _toggle != null ? _toggle : _button;

            if (triggerOnlyOnUserInteraction)
            {
                // 사용자 입력 전용 모드에서는 Pointer/Submit 이벤트만 사용하여 코드에서 발생한 값 변경을 무음 처리합니다.
                return;
            }

            if (_button != null)
            {
                _button.onClick.AddListener(TryDispatchClickSound);
            }

            if (_toggle != null)
            {
                _toggle.onValueChanged.AddListener(OnToggleChanged);
            }
        }

        /// <summary>
        /// Button과 Toggle에 등록한 클릭 사운드 이벤트를 해제합니다.
        /// </summary>
        private void OnDestroy()
        {
            if (_button != null) _button.onClick.RemoveListener(TryDispatchClickSound);
            if (_toggle != null) _toggle.onValueChanged.RemoveListener(OnToggleChanged);
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

            if (_toggle != null)
            {
                if (ShouldPlayForToggleState(_toggle.isOn))
                {
                    TryDispatchClickSound();
                }
            }
            else
            {
                TryDispatchClickSound();
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

            if (_toggle != null)
            {
                if (ShouldPlayForToggleState(_toggle.isOn))
                {
                    TryDispatchClickSound();
                }
            }
            else
            {
                TryDispatchClickSound();
            }
        }

        /// <summary>
        /// Toggle 값 변경 이벤트를 현재 On/Off 재생 정책에 따라 처리합니다.
        /// 사용자 입력 전용 모드가 아닐 때는 프로그래밍 방식의 값 변경도 이 경로로 들어옵니다.
        /// </summary>
        /// <param name="isOn">변경된 Toggle 선택 상태입니다.</param>
        private void OnToggleChanged(bool isOn)
        {
            if (ShouldPlayForToggleState(isOn))
            {
                TryDispatchClickSound();
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
        /// 연결된 Selectable이 현재 사용자 상호작용과 클릭 사운드를 허용하는 상태인지 확인합니다.
        /// Button/Toggle의 enabled, interactable 및 상위 CanvasGroup 상호작용 정책을 함께 반영합니다.
        /// </summary>
        /// <returns>클릭 사운드를 발행할 수 있으면 <see langword="true"/>입니다.</returns>
        private bool CanDispatchClickSound()
        {
            return isActiveAndEnabled &&
                   _selectable != null &&
                   _selectable.IsActive() &&
                   _selectable.IsInteractable();
        }

        /// <summary>
        /// 현재 UI 상호작용 상태가 유효한 경우에만 클릭 사운드 이벤트를 발행합니다.
        /// 모든 입력 경로가 이 메서드를 거치게 하여 비활성 UI의 사운드 재생을 일관되게 차단합니다.
        /// </summary>
        private void TryDispatchClickSound()
        {
            if (!CanDispatchClickSound())
            {
                return;
            }

            ClickSoundEventDispatcher.Dispatch(this);
        }
    }
}
