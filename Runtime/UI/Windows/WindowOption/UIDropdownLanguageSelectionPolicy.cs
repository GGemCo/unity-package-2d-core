using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Localization;

namespace GGemCo2DCore
{
    /// <summary>
    /// TMP_Dropdown을 사용해 언어 목록을 표시하고 선택 Locale을 관리하는 정책입니다.
    /// </summary>
    public class UIDropdownLanguageSelectionPolicy : UILanguageSelectionPolicy
    {
        [Header(UIWindowConstants.TitleHeaderIndividual)]
        [Tooltip("언어 선택 드롭다운입니다.")]
        [SerializeField] private TMP_Dropdown dropdownLanguage;

        private readonly List<Locale> _locales = new();
        private Locale _selectedLocale;

        /// <summary>
        /// 컴포넌트가 제거될 때 드롭다운 이벤트를 해제해 중복 호출과 참조 누수를 방지합니다.
        /// </summary>
        private void OnDestroy()
        {
            UnbindDropdown();
        }

        /// <summary>
        /// 기존 UIPanelOptionDefault 필드와의 호환을 위해 드롭다운 참조를 외부에서 주입합니다.
        /// 새 프리팹에서는 인스펙터에서 직접 연결하는 것을 권장합니다.
        /// </summary>
        /// <param name="dropdown">언어 선택에 사용할 TMP_Dropdown입니다.</param>
        public void Configure(TMP_Dropdown dropdown)
        {
            if (dropdownLanguage == dropdown)
            {
                return;
            }

            UnbindDropdown();
            dropdownLanguage = dropdown;
        }

        /// <summary>
        /// Locale 목록을 드롭다운 옵션으로 변환하고 사용자 입력 이벤트를 연결합니다.
        /// </summary>
        /// <param name="locales">드롭다운에 표시할 Locale 목록입니다.</param>
        public override void Initialize(IReadOnlyList<Locale> locales)
        {
            _locales.Clear();

            if (locales != null)
            {
                for (int i = 0; i < locales.Count; i++)
                {
                    if (locales[i] != null)
                    {
                        _locales.Add(locales[i]);
                    }
                }
            }

            BindDropdown();
            RefreshOptions();

            if (_selectedLocale != null)
            {
                SetSelectedLocaleWithoutNotify(_selectedLocale);
            }
        }

        /// <summary>
        /// 현재 드롭다운 값에 해당하는 Locale을 반환합니다.
        /// 드롭다운 선택값이 유효하지 않으면 마지막으로 동기화된 Locale을 사용합니다.
        /// </summary>
        /// <returns>현재 선택된 Locale입니다.</returns>
        public override Locale GetSelectedLocale()
        {
            if (dropdownLanguage != null && TryGetLocaleByIndex(dropdownLanguage.value, out Locale locale))
            {
                _selectedLocale = locale;
            }

            return _selectedLocale;
        }

        /// <summary>
        /// 지정한 Locale에 맞춰 드롭다운 선택 인덱스를 이벤트 없이 갱신합니다.
        /// </summary>
        /// <param name="locale">선택 상태로 표시할 Locale입니다.</param>
        public override void SetSelectedLocaleWithoutNotify(Locale locale)
        {
            _selectedLocale = locale;

            if (dropdownLanguage == null)
            {
                return;
            }

            int index = IndexOfLocale(locale);
            if (index >= 0)
            {
                dropdownLanguage.SetValueWithoutNotify(index);
            }
        }

        /// <summary>
        /// 드롭다운 옵션과 내부 Locale 목록을 초기 상태로 되돌립니다.
        /// </summary>
        public override void Clear()
        {
            _locales.Clear();
            _selectedLocale = null;
            dropdownLanguage?.ClearOptions();
        }

        /// <summary>
        /// 드롭다운 옵션을 현재 Locale 목록 기준으로 다시 구성합니다.
        /// 표시명은 LocalizationConstants의 언어 이름 매핑을 사용합니다.
        /// </summary>
        private void RefreshOptions()
        {
            if (dropdownLanguage == null)
            {
                return;
            }

            dropdownLanguage.ClearOptions();

            if (_locales.Count == 0)
            {
                return;
            }

            List<TMP_Dropdown.OptionData> options = new(_locales.Count);
            for (int i = 0; i < _locales.Count; i++)
            {
                options.Add(new TMP_Dropdown.OptionData(LocalizationConstants.GetName(_locales[i])));
            }

            dropdownLanguage.AddOptions(options);
        }

        /// <summary>
        /// 드롭다운 변경 이벤트를 한 번만 연결합니다.
        /// </summary>
        private void BindDropdown()
        {
            if (dropdownLanguage == null)
            {
                return;
            }

            dropdownLanguage.onValueChanged.RemoveListener(OnDropdownValueChanged);
            dropdownLanguage.onValueChanged.AddListener(OnDropdownValueChanged);
        }

        /// <summary>
        /// 드롭다운 변경 이벤트 연결을 해제합니다.
        /// </summary>
        private void UnbindDropdown()
        {
            if (dropdownLanguage == null)
            {
                return;
            }

            dropdownLanguage.onValueChanged.RemoveListener(OnDropdownValueChanged);
        }

        /// <summary>
        /// 사용자가 드롭다운에서 언어를 선택했을 때 선택 Locale을 갱신하고 이벤트를 전달합니다.
        /// </summary>
        /// <param name="index">드롭다운에서 선택된 옵션 인덱스입니다.</param>
        private void OnDropdownValueChanged(int index)
        {
            if (!TryGetLocaleByIndex(index, out Locale locale))
            {
                return;
            }

            _selectedLocale = locale;
            NotifySelectedLocaleChanged(locale);
        }

        /// <summary>
        /// Locale 목록에서 지정한 인덱스의 Locale을 조회합니다.
        /// </summary>
        /// <param name="index">조회할 Locale 인덱스입니다.</param>
        /// <param name="locale">조회된 Locale입니다.</param>
        /// <returns>유효한 Locale을 찾았으면 true입니다.</returns>
        private bool TryGetLocaleByIndex(int index, out Locale locale)
        {
            if (index >= 0 && index < _locales.Count)
            {
                locale = _locales[index];
                return true;
            }

            locale = null;
            return false;
        }

        /// <summary>
        /// 지정한 Locale과 같은 언어 코드를 가진 항목의 인덱스를 찾습니다.
        /// </summary>
        /// <param name="locale">찾을 Locale입니다.</param>
        /// <returns>일치하는 인덱스입니다. 없으면 -1을 반환합니다.</returns>
        private int IndexOfLocale(Locale locale)
        {
            for (int i = 0; i < _locales.Count; i++)
            {
                if (IsSameLocale(_locales[i], locale))
                {
                    return i;
                }
            }

            return -1;
        }
    }
}
