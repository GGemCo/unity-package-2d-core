using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.UI;

namespace GGemCo2DCore
{
    /// <summary>
    /// ToggleGroup과 UIToggleLanguage 프리팹을 사용해 언어 목록을 표시하고 선택 Locale을 관리하는 정책입니다.
    /// </summary>
    public class UIToggleLanguageSelectionPolicy : UILanguageSelectionPolicy
    {
        [Header(UIWindowConstants.TitleHeaderIndividual)]
        [Tooltip("언어 토글을 묶을 ToggleGroup입니다.")]
        [SerializeField] private ToggleGroup toggleGroupLanguage;
        [Tooltip("언어 토글로 사용할 프리팹입니다.")]
        [SerializeField] private GameObject prefabToggleLanguage;
        [Tooltip("생성한 언어 토글을 배치할 부모 Transform입니다.")]
        [SerializeField] private Transform containerToggleLanguage;

        private readonly List<Locale> _locales = new();
        private readonly List<GameObject> _createdToggleObjects = new();
        private readonly Dictionary<string, UIToggleLanguage> _toggleByLocaleCode = new();
        private Locale _selectedLocale;

        /// <summary>
        /// 컴포넌트가 제거될 때 생성한 토글과 내부 상태를 정리합니다.
        /// </summary>
        private void OnDestroy()
        {
            Clear();
        }

        /// <summary>
        /// 기존 UIPanelOptionDefault 필드와의 호환을 위해 토글 UI 참조를 외부에서 주입합니다.
        /// 새 프리팹에서는 인스펙터에서 직접 연결하는 것을 권장합니다.
        /// </summary>
        /// <param name="toggleGroup">언어 토글을 묶을 ToggleGroup입니다.</param>
        /// <param name="prefabToggle">언어 토글로 생성할 프리팹입니다.</param>
        /// <param name="container">생성한 언어 토글을 배치할 부모 Transform입니다.</param>
        public void Configure(ToggleGroup toggleGroup, GameObject prefabToggle, Transform container)
        {
            toggleGroupLanguage = toggleGroup;
            prefabToggleLanguage = prefabToggle;
            containerToggleLanguage = container;
        }

        /// <summary>
        /// Locale 목록을 기준으로 언어 토글을 생성하고 선택 이벤트를 연결합니다.
        /// </summary>
        /// <param name="locales">토글로 표시할 Locale 목록입니다.</param>
        public override void Initialize(IReadOnlyList<Locale> locales)
        {
            ClearGeneratedToggles();
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

            CreateLanguageToggles();

            if (_selectedLocale != null)
            {
                SetSelectedLocaleWithoutNotify(_selectedLocale);
            }
        }

        /// <summary>
        /// 현재 토글 정책에서 선택한 Locale을 반환합니다.
        /// 선택값이 아직 없으면 첫 번째 Locale을 기본 선택값으로 사용합니다.
        /// </summary>
        /// <returns>현재 선택된 Locale입니다.</returns>
        public override Locale GetSelectedLocale()
        {
            if (_selectedLocale == null && _locales.Count > 0)
            {
                _selectedLocale = _locales[0];
            }

            return _selectedLocale;
        }

        /// <summary>
        /// 지정한 Locale에 맞춰 토글 선택 상태를 이벤트 없이 갱신합니다.
        /// </summary>
        /// <param name="locale">선택 상태로 표시할 Locale입니다.</param>
        public override void SetSelectedLocaleWithoutNotify(Locale locale)
        {
            _selectedLocale = locale;

            foreach (Locale availableLocale in _locales)
            {
                string code = GetLocaleCode(availableLocale);
                if (string.IsNullOrEmpty(code) || !_toggleByLocaleCode.TryGetValue(code, out UIToggleLanguage toggle))
                {
                    continue;
                }

                toggle.SetSelectedWithoutNotify(IsSameLocale(availableLocale, locale));
            }
        }

        /// <summary>
        /// 생성한 토글과 내부 Locale 목록, 선택 상태를 모두 정리합니다.
        /// </summary>
        public override void Clear()
        {
            ClearGeneratedToggles();
            _locales.Clear();
            _toggleByLocaleCode.Clear();
            _selectedLocale = null;
        }

        /// <summary>
        /// Locale 목록을 순회하며 토글 프리팹을 생성합니다.
        /// 생성된 토글은 Locale과 함께 초기화되어 선택 시 정책으로 이벤트를 전달합니다.
        /// </summary>
        private void CreateLanguageToggles()
        {
            if (_locales.Count == 0)
            {
                return;
            }

            if (GcLogger.IsNull(prefabToggleLanguage, nameof(prefabToggleLanguage)))
            {
                return;
            }

            if (GcLogger.IsNull(containerToggleLanguage, nameof(containerToggleLanguage)))
            {
                return;
            }

            for (int i = 0; i < _locales.Count; i++)
            {
                Locale locale = _locales[i];
                GameObject toggleObject = Instantiate(prefabToggleLanguage, containerToggleLanguage);
                if (toggleObject == null)
                {
                    continue;
                }

                _createdToggleObjects.Add(toggleObject);

                UIToggleLanguage toggleLanguage = toggleObject.GetComponent<UIToggleLanguage>();
                if (GcLogger.IsNull(toggleLanguage, nameof(UIToggleLanguage)))
                {
                    continue;
                }

                toggleLanguage.Initialize(locale, toggleGroupLanguage, OnToggleSelected);

                string code = GetLocaleCode(locale);
                if (!string.IsNullOrEmpty(code))
                {
                    _toggleByLocaleCode[code] = toggleLanguage;
                }
            }
        }

        /// <summary>
        /// 정책이 직접 생성한 토글 오브젝트만 제거합니다.
        /// 프리팹에 미리 배치된 다른 UI 요소는 건드리지 않습니다.
        /// </summary>
        private void ClearGeneratedToggles()
        {
            for (int i = 0; i < _createdToggleObjects.Count; i++)
            {
                GameObject toggleObject = _createdToggleObjects[i];
                if (toggleObject == null)
                {
                    continue;
                }

                if (Application.isPlaying)
                {
                    Destroy(toggleObject);
                }
                else
                {
                    DestroyImmediate(toggleObject);
                }
            }

            _createdToggleObjects.Clear();
            _toggleByLocaleCode.Clear();
        }

        /// <summary>
        /// 사용자가 언어 토글을 선택했을 때 선택 Locale을 갱신하고 이벤트를 전달합니다.
        /// </summary>
        /// <param name="locale">사용자가 선택한 Locale입니다.</param>
        private void OnToggleSelected(Locale locale)
        {
            _selectedLocale = locale;
            NotifySelectedLocaleChanged(locale);
        }

        /// <summary>
        /// Locale에서 사전 키로 사용할 언어 코드를 가져옵니다.
        /// </summary>
        /// <param name="locale">언어 코드를 조회할 Locale입니다.</param>
        /// <returns>Locale의 Identifier.Code 값입니다. Locale이 없으면 빈 문자열입니다.</returns>
        private static string GetLocaleCode(Locale locale)
        {
            return locale != null ? locale.Identifier.Code : string.Empty;
        }
    }
}
