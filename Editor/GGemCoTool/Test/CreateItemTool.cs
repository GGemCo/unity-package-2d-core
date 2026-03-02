using System;
using System.Collections.Generic;
using GGemCo2DCore;
using UnityEditor;
using UnityEngine;

namespace GGemCo2DCoreEditor
{
    public class CreateItemTool : DefaultEditorWindow
    {
        private const string Title = "아이템 생성툴";
        private const float SectionSpacing = 10f;

        private TableItem _tableItem;
        private int _selectedItemIndex;
        private int _makeItemCount;
        private int _makeGoldCount;
        private int _makeSilverCount;
        private Vector2 _scroll;

        private bool _foldoutItem = true;
        private bool _foldoutCurrency = true;
        private bool _foldoutMaintenance = true;

        private readonly List<string> _itemNames = new List<string>();
        private readonly List<int> _itemUids = new List<int>();
        private Dictionary<int, StruckTableItem> _itemDictionary;
        private readonly List<SearchableDropdownUtility.Option<int>> _itemOptions = new();

        [MenuItem(ConfigEditor.NameToolCreateItem, false, (int)ConfigEditor.ToolOrdering.CreateItem)]
        public static void ShowWindow()
        {
            GetWindow<CreateItemTool>(Title);
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            _selectedItemIndex = 0;
            _tableItem = TableLoaderManager.LoadItemTable();
            _itemDictionary = _tableItem.GetDatas();
            LoadItemInfoData();
        }
        private void OnGUI()
        {
            if (_selectedItemIndex >= _itemNames.Count)
            {
                _selectedItemIndex = 0;
            }

            DrawHeader();

            using (var scroll = new EditorGUILayout.ScrollViewScope(_scroll))
            {
                _scroll = scroll.scrollPosition;

                using (new EditorGUI.DisabledScope(!EditorApplication.isPlaying))
                {
                    DrawItemSection();
                    GUILayout.Space(SectionSpacing);
                    DrawCurrencySection();
                    GUILayout.Space(SectionSpacing);
                    DrawMaintenanceSection();
                }
            }
        }

        private void DrawHeader()
        {
            EditorGUILayout.Space(4);
            EditorGUILayout.LabelField(Title, EditorStyles.boldLabel);

            if (!EditorApplication.isPlaying)
            {
                EditorGUILayout.HelpBox(
                    "플레이 모드에서만 사용할 수 있습니다.\n게임을 실행한 뒤 다시 시도해주세요.",
                    MessageType.Info);
            }
            else
            {
                // SceneGame 이 아직 초기화되지 않은 프레임이 있을 수 있으므로, 상태를 명확히 보여준다.
                var isReady = SceneGame.Instance != null;
                EditorGUILayout.HelpBox(
                    isReady ? "SceneGame 준비됨" : "SceneGame 초기화 대기 중...",
                    isReady ? MessageType.None : MessageType.Warning);
            }

            EditorGUILayout.Space(2);
        }

        private void DrawItemSection()
        {
            _foldoutItem = EditorGUILayout.Foldout(_foldoutItem, "아이템", true);
            if (!_foldoutItem) return;

            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                DrawItemDropdown();

                using (new EditorGUILayout.HorizontalScope())
                {
                    _makeItemCount = EditorGUILayout.IntField("추가할 개수", _makeItemCount);
                    _makeItemCount = Mathf.Max(0, _makeItemCount);

                    GUILayout.FlexibleSpace();
                    if (GUILayout.Button("+1", GUILayout.Width(40))) _makeItemCount += 1;
                    if (GUILayout.Button("+10", GUILayout.Width(45))) _makeItemCount += 10;
                    if (GUILayout.Button("초기화", GUILayout.Width(60))) _makeItemCount = 0;
                }

                EditorGUILayout.Space(4);
                using (new EditorGUI.DisabledScope(!CanOperateOnRuntime()))
                {
                    if (GUILayout.Button("인벤토리에 아이템 추가", GUILayout.Height(26)))
                    {
                        AddItem();
                    }
                }
            }
        }

        private void DrawItemDropdown()
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.PrefixLabel("아이템");

                string currentText = (_selectedItemIndex >= 0 && _selectedItemIndex < _itemOptions.Count)
                    ? _itemOptions[_selectedItemIndex].ToString()
                    : "Select...";

                SearchableDropdownUtility.DrawButtonAndShow(
                    buttonText: currentText,
                    options: _itemOptions,
                    selectedIndex: _selectedItemIndex,
                    onSelected: (idx, opt) =>
                    {
                        _selectedItemIndex = idx;
                        Repaint();
                    },
                    buttonHeight: EditorConstants.SearchableDropdownUtility.ButtonHeight,
                    maxVisibleItems: EditorConstants.SearchableDropdownUtility.MaxVisibleItems,
                    popupWidth: EditorConstants.SearchableDropdownUtility.PopupWidth,
                    defaultSearchMode: SearchableDropdownUtility.SearchMode.Both);
            }

            if (_selectedItemIndex <= 0)
            {
                EditorGUILayout.HelpBox("아이템을 선택해주세요.", MessageType.None);
            }
        }

        private void DrawCurrencySection()
        {
            _foldoutCurrency = EditorGUILayout.Foldout(_foldoutCurrency, "재화", true);
            if (!_foldoutCurrency) return;

            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                // 한 화면에서 보기 쉽도록 2열 구성
                using (new EditorGUILayout.HorizontalScope())
                {
                    DrawSingleCurrencyBlock(
                        title: "골드",
                        fieldLabel: "추가할 골드",
                        valueGetter: () => _makeGoldCount,
                        valueSetter: v => _makeGoldCount = v,
                        addButtonLabel: "골드 추가",
                        onAdd: () => AddCurrency(CurrencyConstants.Type.Gold),
                        removeButtonLabel: "골드 삭제",
                        onRemove: () => RemoveCurrency(CurrencyConstants.Type.Gold));

                    GUILayout.Space(8);

                    DrawSingleCurrencyBlock(
                        title: "실버",
                        fieldLabel: "추가할 실버",
                        valueGetter: () => _makeSilverCount,
                        valueSetter: v => _makeSilverCount = v,
                        addButtonLabel: "실버 추가",
                        onAdd: () => AddCurrency(CurrencyConstants.Type.Silver),
                        removeButtonLabel: "실버 삭제",
                        onRemove: () => RemoveCurrency(CurrencyConstants.Type.Silver));
                }
            }
        }

        private void DrawSingleCurrencyBlock(
            string title,
            string fieldLabel,
            Func<int> valueGetter,
            Action<int> valueSetter,
            string addButtonLabel,
            Action onAdd,
            string removeButtonLabel,
            Action onRemove)
        {
            using (new EditorGUILayout.VerticalScope(GUILayout.MinWidth(240)))
            {
                EditorGUILayout.LabelField(title, EditorStyles.boldLabel);

                int v = EditorGUILayout.IntField(fieldLabel, valueGetter());
                valueSetter(Mathf.Max(0, v));

                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("+1", GUILayout.Width(40)))
                    {
                        valueSetter(valueGetter() + 1);
                        GUI.FocusControl(null);
                    }

                    if (GUILayout.Button("+10", GUILayout.Width(45)))
                    {
                        valueSetter(valueGetter() + 10);
                        GUI.FocusControl(null);
                    }
                    if (GUILayout.Button("초기화", GUILayout.Width(60))) valueSetter(0);
                }

                EditorGUILayout.Space(2);

                using (new EditorGUI.DisabledScope(!CanOperateOnRuntime()))
                {
                    if (GUILayout.Button($"인벤토리에 {addButtonLabel}", GUILayout.Height(24)))
                    {
                        onAdd?.Invoke();
                    }
                }

                EditorGUILayout.Space(2);

                using (new EditorGUI.DisabledScope(!CanOperateOnRuntime()))
                {
                    if (GUILayout.Button(removeButtonLabel))
                    {
                        if (EditorUtility.DisplayDialog(Title, $"{title}를 모두 삭제하시겠습니까?", "삭제", "취소"))
                        {
                            onRemove?.Invoke();
                        }
                    }
                }
            }
        }

        private void DrawMaintenanceSection()
        {
            _foldoutMaintenance = EditorGUILayout.Foldout(_foldoutMaintenance, "정리", true);
            if (!_foldoutMaintenance) return;

            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.HelpBox(
                    "주의: 아래 기능은 현재 인벤토리 데이터를 직접 변경합니다.",
                    MessageType.Warning);

                using (new EditorGUI.DisabledScope(!CanOperateOnRuntime()))
                {
                    if (GUILayout.Button("인벤토리 모든 아이템 삭제", GUILayout.Height(26)))
                    {
                        if (EditorUtility.DisplayDialog(Title, "인벤토리의 모든 아이템을 삭제하시겠습니까?", "삭제", "취소"))
                        {
                            RemoveAllInventoryItem();
                        }
                    }
                }
            }
        }

        private static bool CanOperateOnRuntime()
        {
            return EditorApplication.isPlaying && SceneGame.Instance != null;
        }

        private void RemoveCurrency(CurrencyConstants.Type type)
        {
            if (!SceneGame.Instance)
            {
                EditorUtility.DisplayDialog(Title, "게임을 실행해주세요.", "OK");
                return;
            }

            var player = SceneGame.Instance.saveDataManager.Player;

            if (type == CurrencyConstants.Type.Gold)
            {
                player.MinusCurrency(type, player.CurrentGold);
            }
            else if (type == CurrencyConstants.Type.Silver)
            {
                player.MinusCurrency(type, player.CurrentSilver);
            }
        }

        private void AddCurrency(CurrencyConstants.Type type)
        {
            if (!SceneGame.Instance)
            {
                EditorUtility.DisplayDialog(Title, "게임을 실행해주세요.", "OK");
                return;
            }

            if (type == CurrencyConstants.Type.Gold && _makeGoldCount <= 0)
            {
                EditorUtility.DisplayDialog(Title, "골드 수량을 입력해주세요.", "OK");
                return;
            }

            if (type == CurrencyConstants.Type.Silver && _makeSilverCount <= 0)
            {
                EditorUtility.DisplayDialog(Title, "실버 수량을 입력해주세요.", "OK");
                return;
            }

            int uid = (type == CurrencyConstants.Type.Gold)
                ? CurrencyConstants.ItemUidGold
                : CurrencyConstants.ItemUidSilver;

            int count = (type == CurrencyConstants.Type.Gold) ? _makeGoldCount : _makeSilverCount;

            SceneGame.Instance.saveDataManager.Inventory.AddItem(uid, count);
        }

        private void RemoveAllInventoryItem()
        {
            if (!SceneGame.Instance)
            {
                EditorUtility.DisplayDialog(Title, "게임을 실행해주세요.", "OK");
                return;
            }

            SceneGame.Instance.saveDataManager.Inventory.RemoveAllItems();

            var inventory = SceneGame.Instance.uIWindowManager.GetUIWindowByUid<UIWindowInventory>(UIWindowConstants.WindowUid.Inventory);
            if (!inventory) return;
            inventory.LoadIcons();
        }

        private void AddItem()
        {
            if (!SceneGame.Instance)
            {
                EditorUtility.DisplayDialog(Title, "게임을 실행해주세요.", "OK");
                return;
            }

            if (_makeItemCount <= 0)
            {
                EditorUtility.DisplayDialog(Title, "생성할 아이템 개수를 입력해주세요.", "OK");
                return;
            }

            if (_selectedItemIndex <= 0 || _selectedItemIndex >= _itemUids.Count)
            {
                EditorUtility.DisplayDialog(Title, "생성할 아이템을 선택해주세요.", "OK");
                return;
            }

            int itemUid = _itemUids[_selectedItemIndex];
            if (itemUid <= 0)
            {
                EditorUtility.DisplayDialog(Title, "생성할 아이템을 선택해주세요.", "OK");
                return;
            }

            var result = SceneGame.Instance.saveDataManager.Inventory.AddItem(itemUid, _makeItemCount);
            var inventory = SceneGame.Instance.uIWindowManager.GetUIWindowByUid<UIWindowInventory>(UIWindowConstants.WindowUid.Inventory);
            if (!inventory) return;
            inventory.SetIcons(result);
        }

        private void LoadItemInfoData()
        {
            _itemNames.Clear();
            _itemUids.Clear();
            _itemOptions.Clear();
            
            _itemOptions.Add(new SearchableDropdownUtility.Option<int>("0", "Select...", 0));
            
            if (_tableItem == null)
            {
                _selectedItemIndex = 0;
                return;
            }

            foreach (var kvp in _itemDictionary)
            {
                var info = kvp.Value;
                if (info.Uid <= 0) continue;

                _itemNames.Add($"{info.Uid} - {info.Name}");
                _itemUids.Add(info.Uid);
                
                // Key(Uid) + Value(Name) 형태로 표시되며, 검색은 Key/Value 모두 지원
                _itemOptions.Add(new SearchableDropdownUtility.Option<int>(
                    key: info.Uid.ToString(),
                    value: $"{info.Name}",
                    data: info.Uid));
            }
            _selectedItemIndex = 0; // 추가
        }
    }
}
