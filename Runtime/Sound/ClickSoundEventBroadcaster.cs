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
        private bool triggerOnlyOnUserInteraction;

        private Button _button;
        private Toggle _toggle;

        private void Awake()
        {
            triggerOnlyOnUserInteraction = true;
            _button = GetComponent<Button>();
            _toggle = GetComponent<Toggle>();

            if (triggerOnlyOnUserInteraction)
            {
                // 사용자 입력 이벤트(IPointerClick/ISubmit)로만 처리
                // Button/Toggle의 이벤트에는 구독하지 않음 → 프로그래매틱 변경 시 무음
                return;
            }

            if (_button != null)
            {
                _button.onClick.AddListener(OnPlay);
            }

            if (_toggle != null)
            {
                _toggle.onValueChanged.AddListener(OnToggleChanged);
            }
        }

        private void OnDestroy()
        {
            if (_button != null) _button.onClick.RemoveListener(OnPlay);
            if (_toggle != null) _toggle.onValueChanged.RemoveListener(OnToggleChanged);
        }

        // --- IClickSoundEventTrigger ---
        public int GetSoundId() => soundUid;
        public SoundConstants.UIButtonType GetSoundType() => type;

        // --- Pointer/Submit(사용자 상호작용 전용 모드) ---
        public void OnPointerClick(PointerEventData eventData)
        {
            if (!triggerOnlyOnUserInteraction) return;
            // Toggle인 경우: 현재 상태에 따라 On/Off 정책 적용
            if (_toggle != null)
            {
                // 클릭 직후의 값(_toggle.isOn)에 대해 설정을 평가
                if ((_toggle.isOn && playOnToggleOn) || (!_toggle.isOn && playOnToggleOff))
                    OnPlay();
            }
            else
            {
                OnPlay();
            }
        }

        public void OnSubmit(BaseEventData eventData)
        {
            if (!triggerOnlyOnUserInteraction) return;
            // 키보드/패드 Submit에도 동일 정책 적용
            if (_toggle != null)
            {
                if ((_toggle.isOn && playOnToggleOn) || (!_toggle.isOn && playOnToggleOff))
                    OnPlay();
            }
            else
            {
                OnPlay();
            }
        }

        // --- 내부 처리 ---
        private void OnToggleChanged(bool isOn)
        {
            // 프로그래매틱 변경 포함(기본 정책)
            // SetIsOnWithoutNotify 사용 시엔 호출되지 않음 → 초기화 시 무음 처리에 유용
            if ((isOn && playOnToggleOn) || (!isOn && playOnToggleOff))
                OnPlay();
        }

        private void OnPlay()
        {
            ClickSoundEventDispatcher.Dispatch(this);
        }
    }
}
