using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace GGemCo2DCore
{
    /// <summary>
    /// 전환 시 확인이 필요한 Toggle.
    /// 실제 전환은 외부 컨트롤러에서 수행.
    /// </summary>
    public class UIToggleConfirmable : Toggle
    {
        /// <summary>이 토글로 전환을 시도할 때 발생</summary>
        public event Action<UIToggleConfirmable> OnConfirmRequested;

        [Tooltip("이 토글로 전환할 때 확인이 필요한지 여부")]
        [SerializeField] private bool requireConfirm = true;

        // 내부적으로 프로그램으로 상태를 바꿀 때 확인 절차를 생략하기 위한 가드
        internal bool SuppressConfirm { get; set; }

        public bool RequireConfirm
        {
            get => requireConfirm;
            set => requireConfirm = value;
        }

        public override void OnPointerClick(PointerEventData eventData)
        {
            if (!IsActive() || !IsInteractable())
                return;

            // 이미 On 상태면(= 같은 탭 재클릭) 기본 동작 유지
            if (isOn)
            {
                base.OnPointerClick(eventData);
                return;
            }

            // 외부 전환(코드) 또는 확인 불필요면 기본 동작
            if (SuppressConfirm || !requireConfirm)
            {
                base.OnPointerClick(eventData);
                return;
            }

            // 여기서 기본 토글 동작을 막고 확인 요청만 보냄
            OnConfirmRequested?.Invoke(this);
        }

        public override void OnSubmit(BaseEventData eventData)
        {
            // 키보드/패드 Submit에도 동일한 정책 적용
            if (!isOn && !SuppressConfirm && requireConfirm)
            {
                OnConfirmRequested?.Invoke(this);
                return;
            }
            base.OnSubmit(eventData);
        }
    }
}