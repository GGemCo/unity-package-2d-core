using UnityEditor;
using UnityEngine;

namespace GGemCo2DCoreEditor
{
    public sealed class ExampleWindow : EditorWindow
    {
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
        private int _selected = -1;

        // [MenuItem("Tools/GGemCo/Example/Searchable Dropdown")]
        public static void Open() => GetWindow<ExampleWindow>("Dropdown Example");

        private void OnGUI()
        {
            SearchableDropdownUtility.DrawLabeledFieldAndShow(
                labelText: "Item",
                options: _options,
                selectedIndex: _selected,
                // onSelected: (_, opt) => Debug.Log($"Selected: {opt.Key} / {opt.Value}"),
                onSelected: Test1,
                popupWidth: 420f,
                maxVisibleItems: 3
            );
            SearchableDropdownUtility.DrawButtonAndShow(
                buttonText: "Item",
                options: _options,
                selectedIndex: _selected,
                // onSelected: (_, opt) => Debug.Log($"Selected: {opt.Key} / {opt.Value}"),
                onSelected: Test1,
                popupWidth: 420f,
                maxVisibleItems: 4
            );
        }

        private void Test1(int index, SearchableDropdownUtility.Option<int> opt)
        {
            Debug.Log($"_selected: {_selected}");
            Debug.Log($"opt key: {opt.Key} / value: {opt.Value}");
        }
    }
}