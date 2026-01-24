#nullable enable

using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace GGemCo2DCoreEditor
{
    public sealed class ExampleUiToolkitDropdownWindow : EditorWindow
    {
        private int _selectedIndex = -1;

        private readonly SearchableDropdownUtility.Option<int>[] _options =
        {
            new SearchableDropdownUtility.Option<int>("10001", "Potion_HealSmall", 10001),
            new SearchableDropdownUtility.Option<int>("10002", "Potion_HealLarge", 10002),
            new SearchableDropdownUtility.Option<int>("20001", "Sword_Iron", 20001),
            new SearchableDropdownUtility.Option<int>("30001", "Affix_AttackSpeed1", 30001),
            new SearchableDropdownUtility.Option<int>("30002", "Affix_AttackSpeed2", 30002),
            new SearchableDropdownUtility.Option<int>("30003", "Affix_AttackSpeed3", 30003),
            new SearchableDropdownUtility.Option<int>("30004", "Affix_AttackSpeed4", 30004),
            new SearchableDropdownUtility.Option<int>("30005", "Affix_AttackSpeed4", 30005),
            new SearchableDropdownUtility.Option<int>("30006", "Affix_AttackSpeed4", 30006),
            new SearchableDropdownUtility.Option<int>("30007", "Affix_AttackSpeed4", 30007),
            new SearchableDropdownUtility.Option<int>("30008", "Affix_AttackSpeed4", 30008),
            new SearchableDropdownUtility.Option<int>("30009", "Affix_AttackSpeed4", 30009),
            new SearchableDropdownUtility.Option<int>("30010", "Affix_AttackSpeed4", 30010),
        };

        // [MenuItem("Tools/GGemCo/Example/UI Toolkit Searchable Dropdown")]
        public static void Open() => GetWindow<ExampleUiToolkitDropdownWindow>("UITK Dropdown");

        public void CreateGUI()
        {
            var root = rootVisualElement;
            root.style.paddingLeft = 10;
            root.style.paddingTop = 10;

            var title = new Label("UI Toolkit Searchable Dropdown")
            {
                style =
                {
                    unityFontStyleAndWeight = FontStyle.Bold
                }
            };
            root.Add(title);

            var button = new Button
            {
                text = GetLabel()
            };
            root.Add(button);

            // 바인딩: 클릭 시 드롭다운 표시
            SearchableDropdownUtility.BindUiToolkitButton(
                owner: this,
                button: button,
                options: _options,
                getSelectedIndex: () => _selectedIndex,
                onSelected: (idx, opt) =>
                {
                    _selectedIndex = idx;
                    button.text = GetLabel();
                    Debug.Log($"Selected: {opt.Key} / {opt.Value} / data={opt.Data}");
                },
                maxVisibleItems: 10,
                popupWidth: 420f
            );
        }

        private string GetLabel()
        {
            return (_selectedIndex >= 0 && _selectedIndex < _options.Length)
                ? _options[_selectedIndex].ToString()
                : "(Select...)";
        }
    }
}