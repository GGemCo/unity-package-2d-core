using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Localization;

namespace GGemCo2DCore
{
    /// <summary>
    /// 옵션 패널에서 사용할 언어 선택 UI 정책의 공통 기능을 정의합니다.
    /// Dropdown, Toggle 등 표현 방식이 달라도 패널은 이 추상화만 통해 선택 언어를 읽고 씁니다.
    /// </summary>
    public abstract class UILanguageSelectionPolicy : MonoBehaviour
    {
        /// <summary>
        /// 사용자가 언어 선택 UI에서 Locale을 변경했을 때 발생합니다.
        /// 외부 동기화로 값을 맞추는 경우에는 호출하지 않습니다.
        /// </summary>
        public event Action<Locale> OnSelectedLocaleChanged;

        /// <summary>
        /// 선택 가능한 Locale 목록으로 언어 선택 UI를 초기화합니다.
        /// </summary>
        /// <param name="locales">화면에 표시할 Locale 목록입니다.</param>
        public abstract void Initialize(IReadOnlyList<Locale> locales);

        /// <summary>
        /// 현재 UI에서 선택된 Locale을 반환합니다.
        /// </summary>
        /// <returns>선택된 Locale입니다. 선택값이 없으면 null을 반환할 수 있습니다.</returns>
        public abstract Locale GetSelectedLocale();

        /// <summary>
        /// 외부 모델 값에 맞춰 선택 상태를 갱신합니다.
        /// 이 메서드는 변경 이벤트를 발생시키지 않아야 합니다.
        /// </summary>
        /// <param name="locale">선택 상태로 표시할 Locale입니다.</param>
        public abstract void SetSelectedLocaleWithoutNotify(Locale locale);

        /// <summary>
        /// 정책이 생성한 UI 항목과 내부 선택 상태를 정리합니다.
        /// </summary>
        public abstract void Clear();

        /// <summary>
        /// 선택 변경 이벤트를 구독자에게 전달합니다.
        /// 구현체는 실제 사용자 입력이 발생했을 때만 이 메서드를 호출합니다.
        /// </summary>
        /// <param name="locale">사용자가 선택한 Locale입니다.</param>
        protected void NotifySelectedLocaleChanged(Locale locale)
        {
            OnSelectedLocaleChanged?.Invoke(locale);
        }

        /// <summary>
        /// 두 Locale이 같은 언어 코드를 가리키는지 확인합니다.
        /// Unity Locale 인스턴스가 달라도 Identifier.Code가 같으면 같은 선택값으로 취급합니다.
        /// </summary>
        /// <param name="left">비교할 첫 번째 Locale입니다.</param>
        /// <param name="right">비교할 두 번째 Locale입니다.</param>
        /// <returns>두 Locale의 언어 코드가 같으면 true입니다.</returns>
        protected static bool IsSameLocale(Locale left, Locale right)
        {
            if (left == null || right == null)
            {
                return false;
            }

            return string.Equals(
                left.Identifier.Code,
                right.Identifier.Code,
                StringComparison.Ordinal);
        }
    }
}
