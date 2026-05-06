using System;
using TMPro;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.UI;

namespace GGemCo2DCore
{
    /// <summary>
    /// 언어 선택 토글 1개를 표시하고 선택 이벤트를 Locale 단위로 전달합니다.
    /// </summary>
    public class UIToggleLanguage : MonoBehaviour
    {
        [SerializeField] private TMP_Text textSubject;
        
        private Toggle _toggle;
        private Action<Locale> _onSelected;
        private Locale _locale;

        private void Awake()
        {
            EnsureToggle();
        }
        
        /// <summary>
        /// 오브젝트가 제거될 때 Toggle 이벤트 연결을 해제합니다.
        /// </summary>
        private void OnDestroy()
        {
            if (_toggle != null)
            {
                _toggle.onValueChanged.RemoveListener(OnToggleValueChanged);
            }
        }
        
        /// <summary>
        /// 토글에 표시할 Locale 정보와 선택 이벤트를 초기화합니다.
        /// </summary>
        /// <param name="locale">이 토글이 담당할 Locale입니다.</param>
        /// <param name="toggleGroup">언어 토글을 하나의 선택 그룹으로 묶을 ToggleGroup입니다.</param>
        /// <param name="onSelected">토글이 선택되었을 때 호출할 콜백입니다.</param>
        public void Initialize(Locale locale, ToggleGroup toggleGroup, Action<Locale> onSelected)
        {
            EnsureToggle();
            _locale = locale;
            _onSelected = onSelected;
            
            if (textSubject)
                textSubject.text = LocalizationConstants.GetName(locale);
            
            if (_toggle == null)
            {
                return;
            }

            _toggle.group = toggleGroup;
            _toggle.onValueChanged.RemoveListener(OnToggleValueChanged);
            _toggle.onValueChanged.AddListener(OnToggleValueChanged);
        }
        
        /// <summary>
        /// 외부 동기화로 토글 선택 상태를 갱신합니다.
        /// 콜백을 발생시키지 않아 저장값 반영과 사용자 입력이 서로 재귀 호출되지 않게 합니다.
        /// </summary>
        /// <param name="selected">선택 상태로 표시하면 true입니다.</param>
        public void SetSelectedWithoutNotify(bool selected)
        {
            EnsureToggle();
            if (_toggle != null)
            {
                _toggle.SetIsOnWithoutNotify(selected);
            }
        }
        
        /// <summary>
        /// Toggle 선택 이벤트를 언어 선택 콜백으로 전달합니다.
        /// 선택 해제 이벤트는 ToggleGroup에서 동반 발생하는 상태 변경이므로 무시합니다.
        /// </summary>
        /// <param name="isOn">Toggle이 선택되면 true입니다.</param>
        private void OnToggleValueChanged(bool isOn)
        {
            if (!isOn)
            {
                return;
            }

            _onSelected?.Invoke(_locale);
        }
        
        /// <summary>
        /// Toggle 컴포넌트를 지연 조회합니다.
        /// Initialize가 Awake보다 먼저 호출되는 경우에도 동일하게 동작하게 합니다.
        /// </summary>
        private void EnsureToggle()
        {
            if (_toggle == null)
            {
                _toggle = GetComponent<Toggle>();
            }
        }
    }
}
