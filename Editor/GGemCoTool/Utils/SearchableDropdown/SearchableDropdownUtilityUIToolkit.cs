#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace GGemCo2DCoreEditor
{
    /// <summary>
    /// SearchableDropdownUtility (UI Toolkit)
    /// - DropDown EditorWindow + UI Toolkit 기반 검색/선택 팝업
    /// - CreateGUI 초기화 중 selection 콜백 발생/Close 호출 NRE 방지(억제 플래그 + delayClose)
    /// - 탭 분리 검색 지원
    /// </summary>
    public static partial class SearchableDropdownUtility
    {
        private const string DefaultTabId = "default";
        private const string DefaultTabLabel = "All";

        /// <summary>
        /// UI Toolkit용 검색 드롭다운을 DropDown EditorWindow로 표시합니다.
        /// </summary>
        public static void ShowUiToolkit<T>(
            EditorWindow owner,
            Rect activatorRectScreen,
            IReadOnlyList<Option<T>> options,
            int selectedIndex,
            Action<int, Option<T>> onSelected,
            int maxVisibleItems = EditorConstants.SearchableDropdownUtility.MaxVisibleItems,
            float rowHeight = EditorConstants.SearchableDropdownUtility.RowHeight,
            float popupWidth = EditorConstants.SearchableDropdownUtility.PopupWidth,
            SearchMode defaultSearchMode = SearchMode.Both,
            float verticalOffset = EditorConstants.SearchableDropdownUtility.VerticalOffset)
        {
            if (owner == null) throw new ArgumentNullException(nameof(owner));
            if (options == null) throw new ArgumentNullException(nameof(options));
            if (onSelected == null) throw new ArgumentNullException(nameof(onSelected));

            OptionTab<T>[] tabs =
            {
                new OptionTab<T>(DefaultTabId, DefaultTabLabel, options),
            };

            ShowUiToolkit(
                owner,
                activatorRectScreen,
                tabs,
                DefaultTabId,
                selectedIndex,
                (selectedTab, selectedOptionIndex, option) => onSelected(selectedOptionIndex, option),
                maxVisibleItems,
                rowHeight,
                popupWidth,
                defaultSearchMode,
                verticalOffset);
        }

        /// <summary>
        /// UI Toolkit용 검색 드롭다운을 탭 기반으로 표시합니다.
        /// </summary>
        public static void ShowUiToolkit<T>(
            EditorWindow owner,
            Rect activatorRectScreen,
            IReadOnlyList<OptionTab<T>> tabs,
            string selectedTabId,
            int selectedIndex,
            Action<OptionTab<T>, int, Option<T>> onSelected,
            int maxVisibleItems = EditorConstants.SearchableDropdownUtility.MaxVisibleItems,
            float rowHeight = EditorConstants.SearchableDropdownUtility.RowHeight,
            float popupWidth = EditorConstants.SearchableDropdownUtility.PopupWidth,
            SearchMode defaultSearchMode = SearchMode.Both,
            float verticalOffset = EditorConstants.SearchableDropdownUtility.VerticalOffset)
        {
            if (owner == null) throw new ArgumentNullException(nameof(owner));
            if (tabs == null) throw new ArgumentNullException(nameof(tabs));
            if (onSelected == null) throw new ArgumentNullException(nameof(onSelected));

            maxVisibleItems = Mathf.Clamp(maxVisibleItems, 3, 40);
            rowHeight = Mathf.Clamp(rowHeight, 18f, 28f);
            popupWidth = Mathf.Clamp(popupWidth, 240f, 900f);

            activatorRectScreen.y = activatorRectScreen.yMax + verticalOffset;

            SearchableDropdownUiToolkitWindow wnd = ScriptableObject.CreateInstance<SearchableDropdownUiToolkitWindow>();
            if (wnd == null)
            {
                Debug.LogError("[SearchableDropdownUiToolkit] Failed to create window instance.");
                return;
            }

            List<SearchableDropdownUiToolkitWindow.TabInfo> tabInfos = new List<SearchableDropdownUiToolkitWindow.TabInfo>(tabs.Count);
            List<SearchableDropdownUiToolkitWindow.Entry> entries = new List<SearchableDropdownUiToolkitWindow.Entry>();

            for (int tabIndex = 0; tabIndex < tabs.Count; tabIndex++)
            {
                OptionTab<T> tab = tabs[tabIndex];
                string tabId = string.IsNullOrWhiteSpace(tab.Id) ? $"tab_{tabIndex}" : tab.Id;
                string tabLabel = string.IsNullOrWhiteSpace(tab.Label) ? tabId : tab.Label;
                IReadOnlyList<Option<T>> options = tab.Options ?? Array.Empty<Option<T>>();

                tabInfos.Add(new SearchableDropdownUiToolkitWindow.TabInfo(tabId, tabLabel));

                for (int optionIndex = 0; optionIndex < options.Count; optionIndex++)
                {
                    Option<T> opt = options[optionIndex];
                    entries.Add(new SearchableDropdownUiToolkitWindow.Entry(
                        tabId: tabId,
                        tabLabel: tabLabel,
                        optionIndex: optionIndex,
                        key: opt.Key,
                        value: opt.Value,
                        display: opt.ToString()));
                }
            }

            string initialTabId = ResolveInitialTabId(tabInfos, selectedTabId);

            wnd.Initialize(
                entries: entries,
                tabs: tabInfos,
                selectedTabId: initialTabId,
                selectedOptionIndex: selectedIndex,
                onSelectedEntry: entry =>
                {
                    OptionTab<T> selectedTab = FindTab(tabs, entry.TabId);
                    IReadOnlyList<Option<T>> options = selectedTab.Options ?? Array.Empty<Option<T>>();
                    if (entry.OptionIndex < 0 || entry.OptionIndex >= options.Count)
                        return;

                    onSelected(selectedTab, entry.OptionIndex, options[entry.OptionIndex]);
                },
                maxVisibleItems: maxVisibleItems,
                rowHeight: rowHeight,
                popupWidth: popupWidth,
                defaultMode: defaultSearchMode,
                showTabs: tabInfos.Count > 1);

            int visibleCount = Mathf.Min(entries.Count, maxVisibleItems);
            visibleCount = Mathf.Max(visibleCount, 3);

            float headerHeight = tabInfos.Count > 1 ? 84f : 56f;
            float listHeight = visibleCount * rowHeight;
            float height = headerHeight + listHeight + 10f;

            wnd.ShowAsDropDown(activatorRectScreen, new Vector2(popupWidth, height));
        }

        private static string ResolveInitialTabId(IReadOnlyList<SearchableDropdownUiToolkitWindow.TabInfo> tabs, string requestedTabId)
        {
            if (tabs == null || tabs.Count == 0)
                return DefaultTabId;

            if (!string.IsNullOrWhiteSpace(requestedTabId))
            {
                for (int i = 0; i < tabs.Count; i++)
                {
                    if (string.Equals(tabs[i].Id, requestedTabId, StringComparison.OrdinalIgnoreCase))
                        return tabs[i].Id;
                }
            }

            return tabs[0].Id;
        }

        private static OptionTab<T> FindTab<T>(IReadOnlyList<OptionTab<T>> tabs, string tabId)
        {
            if (tabs != null)
            {
                for (int i = 0; i < tabs.Count; i++)
                {
                    OptionTab<T> tab = tabs[i];
                    if (string.Equals(tab.Id, tabId, StringComparison.OrdinalIgnoreCase))
                        return tab;
                }

                if (tabs.Count > 0)
                    return tabs[0];
            }

            return new OptionTab<T>(DefaultTabId, DefaultTabLabel, Array.Empty<Option<T>>());
        }

        /// <summary>
        /// UI Toolkit VisualElement(worldBound)를 EditorWindow screen 좌표 Rect로 변환합니다.
        /// </summary>
        public static Rect GetScreenRect(EditorWindow owner, VisualElement element)
        {
            if (owner == null) throw new ArgumentNullException(nameof(owner));
            if (element == null) throw new ArgumentNullException(nameof(element));

            Rect wb = element.worldBound;
            Rect ow = owner.position;

            return new Rect(ow.x + wb.x, ow.y + wb.y, wb.width, wb.height);
        }

        private sealed class SearchableDropdownUiToolkitWindow : EditorWindow
        {
            internal readonly struct Entry
            {
                public readonly string TabId;
                public readonly string TabLabel;
                public readonly int OptionIndex;
                public readonly string Key;
                public readonly string Value;
                public readonly string Display;

                public readonly string KeyLower;
                public readonly string ValueLower;

                public Entry(string tabId, string tabLabel, int optionIndex, string key, string value, string display)
                {
                    TabId = string.IsNullOrWhiteSpace(tabId) ? DefaultTabId : tabId;
                    TabLabel = string.IsNullOrWhiteSpace(tabLabel) ? TabId : tabLabel;
                    OptionIndex = optionIndex;
                    Key = key ?? string.Empty;
                    Value = value ?? string.Empty;
                    Display = display ?? string.Empty;

                    KeyLower = Key.ToLowerInvariant();
                    ValueLower = Value.ToLowerInvariant();
                }
            }

            internal readonly struct TabInfo
            {
                public readonly string Id;
                public readonly string Label;

                public TabInfo(string id, string label)
                {
                    Id = string.IsNullOrWhiteSpace(id) ? DefaultTabId : id;
                    Label = string.IsNullOrWhiteSpace(label) ? Id : label;
                }
            }

            private List<Entry> _entries = new();
            private List<TabInfo> _tabs = new();
            private Action<Entry>? _onSelectedEntry;

            private string _selectedTabId = DefaultTabId;
            private int _selectedOptionIndex;
            private int _maxVisibleItems;
            private float _rowHeight;
            private float _popupWidth;
            private SearchMode _mode;
            private bool _showTabs;

            private string _query = string.Empty;

            private readonly List<int> _filteredEntryIndices = new(256);
            private readonly List<string> _display = new(256);

            private ToolbarSearchField? _searchField;
            private ToolbarMenu? _modeMenu;
            private ListView? _listView;
            private VisualElement? _tabRow;
            private const float HeaderWithoutTabsHeight = 44f;
            private const float HeaderWithTabsHeight = 72f;
            private const float WindowPadding = 6f;

            private bool _suppressSelectionCallback;
            private static MethodInfo? _setSelectionWithoutNotifyInt;

            public void Initialize(
                List<Entry> entries,
                List<TabInfo> tabs,
                string selectedTabId,
                int selectedOptionIndex,
                Action<Entry> onSelectedEntry,
                int maxVisibleItems,
                float rowHeight,
                float popupWidth,
                SearchMode defaultMode,
                bool showTabs)
            {
                _entries = entries ?? new List<Entry>();
                _tabs = tabs ?? new List<TabInfo>();
                _selectedTabId = string.IsNullOrWhiteSpace(selectedTabId) ? ResolveDefaultTabId(_tabs) : selectedTabId;
                _selectedOptionIndex = selectedOptionIndex;
                _onSelectedEntry = onSelectedEntry ?? throw new ArgumentNullException(nameof(onSelectedEntry));
                _maxVisibleItems = maxVisibleItems;
                _rowHeight = rowHeight;
                _popupWidth = popupWidth;
                _mode = defaultMode;
                _showTabs = showTabs && _tabs.Count > 1;

                RebuildFilter();
            }

            private static string ResolveDefaultTabId(IReadOnlyList<TabInfo> tabs)
            {
                return tabs != null && tabs.Count > 0 ? tabs[0].Id : DefaultTabId;
            }

            private void CreateGUI()
            {
                rootVisualElement.style.flexDirection = FlexDirection.Column;
                rootVisualElement.style.paddingLeft = WindowPadding;
                rootVisualElement.style.paddingRight = WindowPadding;
                rootVisualElement.style.paddingTop = WindowPadding;
                rootVisualElement.style.paddingBottom = WindowPadding;
                rootVisualElement.style.backgroundColor = new Color(0.3f, 0.3f, 0.3f);

                VisualElement header = new VisualElement
                {
                    style =
                    {
                        flexDirection = FlexDirection.Column,
                        flexShrink = 0,
                        height = _showTabs ? HeaderWithTabsHeight : HeaderWithoutTabsHeight
                    }
                };

                if (_showTabs)
                {
                    _tabRow = new VisualElement
                    {
                        style =
                        {
                            flexDirection = FlexDirection.Row,
                            flexWrap = Wrap.NoWrap,
                            marginBottom = 4f,
                            flexShrink = 0
                        }
                    };
                    header.Add(_tabRow);
                    RebuildTabRow();
                }

                _searchField = new ToolbarSearchField { value = _query };
                _searchField.style.flexShrink = 0;
                _searchField.RegisterValueChangedCallback(evt =>
                {
                    _query = evt.newValue ?? string.Empty;
                    RebuildFilter();
                    RefreshListView();
                });

                VisualElement modeRow = new VisualElement
                {
                    style =
                    {
                        flexDirection = FlexDirection.Row,
                        justifyContent = Justify.SpaceBetween,
                        alignItems = Align.Center,
                        flexShrink = 0
                    }
                };

                Label modeLabel = new Label("Search:")
                {
                    style = { marginRight = 6 }
                };

                _modeMenu = new ToolbarMenu { text = ModeToText(_mode) };
                _modeMenu.menu.AppendAction("Key", _ => SetMode(SearchMode.Key),
                    a => _mode == SearchMode.Key ? DropdownMenuAction.Status.Checked : DropdownMenuAction.Status.Normal);
                _modeMenu.menu.AppendAction("Value", _ => SetMode(SearchMode.Value),
                    a => _mode == SearchMode.Value ? DropdownMenuAction.Status.Checked : DropdownMenuAction.Status.Normal);
                _modeMenu.menu.AppendAction("Both", _ => SetMode(SearchMode.Both),
                    a => _mode == SearchMode.Both ? DropdownMenuAction.Status.Checked : DropdownMenuAction.Status.Normal);

                modeRow.Add(modeLabel);
                modeRow.Add(_modeMenu);

                header.Add(_searchField);
                header.Add(modeRow);

                _listView = new ListView
                {
                    virtualizationMethod = CollectionVirtualizationMethod.FixedHeight,
                    fixedItemHeight = _rowHeight,
                    selectionType = SelectionType.Single,
                    itemsSource = _display
                };

                _listView.style.flexGrow = 0;
                _listView.style.flexShrink = 0;

                _listView.makeItem = () =>
                {
                    Label item = new Label
                    {
                        style =
                        {
                            unityTextAlign = TextAnchor.MiddleLeft,
                            paddingLeft = 4,
                            paddingRight = 4
                        }
                    };
                    return item;
                };

                _listView.bindItem = (ve, i) =>
                {
                    ((Label)ve).text = (i >= 0 && i < _display.Count) ? _display[i] : string.Empty;
                };

                _listView.selectionChanged += _ =>
                {
                    if (_suppressSelectionCallback || _listView == null)
                        return;

                    int selectedFilteredIndex = _listView.selectedIndex;
                    if (selectedFilteredIndex < 0 || selectedFilteredIndex >= _filteredEntryIndices.Count)
                        return;

                    int entryIndex = _filteredEntryIndices[selectedFilteredIndex];
                    Select(_entries[entryIndex]);
                };

                _listView.RegisterCallback<KeyDownEvent>(evt =>
                {
                    if (_listView == null)
                        return;

                    if (evt.keyCode == KeyCode.Return || evt.keyCode == KeyCode.KeypadEnter)
                    {
                        int selectedFilteredIndex = _listView.selectedIndex;
                        if (selectedFilteredIndex >= 0 && selectedFilteredIndex < _filteredEntryIndices.Count)
                        {
                            int entryIndex = _filteredEntryIndices[selectedFilteredIndex];
                            Select(_entries[entryIndex]);
                            evt.StopPropagation();
                        }
                    }
                    else if (evt.keyCode == KeyCode.Escape)
                    {
                        DelayClose();
                        evt.StopPropagation();
                    }
                });

                rootVisualElement.Add(header);
                rootVisualElement.Add(_listView);

                RefreshListView();
                _searchField.Focus();
            }

            private void RebuildTabRow()
            {
                if (_tabRow == null)
                    return;

                _tabRow.Clear();

                for (int i = 0; i < _tabs.Count; i++)
                {
                    TabInfo tab = _tabs[i];
                    Button button = new Button(() => SetActiveTab(tab.Id))
                    {
                        text = tab.Label
                    };

                    bool isActive = string.Equals(_selectedTabId, tab.Id, StringComparison.OrdinalIgnoreCase);
                    button.style.flexGrow = 1f;
                    button.style.marginRight = i < _tabs.Count - 1 ? 4f : 0f;
                    button.style.unityFontStyleAndWeight = isActive ? FontStyle.Bold : FontStyle.Normal;
                    button.style.opacity = isActive ? 1f : 0.75f;
                    _tabRow.Add(button);
                }
            }

            private void SetActiveTab(string tabId)
            {
                if (string.IsNullOrWhiteSpace(tabId) || string.Equals(_selectedTabId, tabId, StringComparison.OrdinalIgnoreCase))
                    return;

                _selectedTabId = tabId;
                _query = string.Empty;
                if (_searchField != null)
                    _searchField.SetValueWithoutNotify(_query);

                RebuildTabRow();
                RebuildFilter();
                RefreshListView();
                _searchField?.Focus();
            }

            private void SetMode(SearchMode mode)
            {
                if (_mode == mode)
                    return;

                _mode = mode;
                if (_modeMenu != null)
                    _modeMenu.text = ModeToText(_mode);

                RebuildFilter();
                RefreshListView();
            }

            private static string ModeToText(SearchMode mode)
            {
                return mode switch
                {
                    SearchMode.Key => "Key",
                    SearchMode.Value => "Value",
                    _ => "Both"
                };
            }

            private void RefreshListView()
            {
                _display.Clear();

                for (int i = 0; i < _filteredEntryIndices.Count; i++)
                    _display.Add(_entries[_filteredEntryIndices[i]].Display);

                _listView?.Rebuild();

                int idxInFiltered = FindFilteredIndex(_selectedTabId, _selectedOptionIndex);
                int targetSelection =
                    idxInFiltered >= 0 ? idxInFiltered :
                    (_filteredEntryIndices.Count > 0 ? 0 : -1);

                if (_listView != null && targetSelection >= 0 && _selectedOptionIndex > -1)
                    SetSelectionWithoutNotifySafe(_listView, targetSelection);

                int visibleCount = Mathf.Min(_filteredEntryIndices.Count, _maxVisibleItems);
                visibleCount = Mathf.Max(visibleCount, 3);

                float listHeight = visibleCount * _rowHeight;

                if (_listView != null)
                    _listView.style.height = listHeight;

                float headerHeight = _showTabs ? HeaderWithTabsHeight : HeaderWithoutTabsHeight;
                float height = (WindowPadding * 2f) + headerHeight + listHeight + 4f;

                EditorApplication.delayCall += () =>
                {
                    if (this == null)
                        return;
                    minSize = maxSize = new Vector2(_popupWidth, height);
                };
            }

            private int FindFilteredIndex(string tabId, int optionIndex)
            {
                for (int i = 0; i < _filteredEntryIndices.Count; i++)
                {
                    Entry entry = _entries[_filteredEntryIndices[i]];
                    if (string.Equals(entry.TabId, tabId, StringComparison.OrdinalIgnoreCase) && entry.OptionIndex == optionIndex)
                        return i;
                }

                return -1;
            }

            private void SetSelectionWithoutNotifySafe(ListView listView, int index)
            {
                _suppressSelectionCallback = true;
                try
                {
                    if (_setSelectionWithoutNotifyInt == null)
                    {
                        _setSelectionWithoutNotifyInt = listView
                            .GetType()
                            .GetMethod("SetSelectionWithoutNotify", new[] { typeof(int) });
                    }

                    if (_setSelectionWithoutNotifyInt != null)
                    {
                        _setSelectionWithoutNotifyInt.Invoke(listView, new object[] { index });
                    }
                    else
                    {
                        listView.SetSelection(index);
                    }
                }
                finally
                {
                    _suppressSelectionCallback = false;
                }
            }

            private void Select(Entry entry)
            {
                _selectedTabId = entry.TabId;
                _selectedOptionIndex = entry.OptionIndex;
                _onSelectedEntry?.Invoke(entry);
                DelayClose();
            }

            private void DelayClose()
            {
                EditorApplication.delayCall += () =>
                {
                    if (this == null)
                        return;

                    try
                    {
                        Close();
                    }
                    catch
                    {
                    }
                };
            }

            private void RebuildFilter()
            {
                _filteredEntryIndices.Clear();

                if (_entries.Count == 0)
                    return;

                string activeTabId = string.IsNullOrWhiteSpace(_selectedTabId)
                    ? ResolveDefaultTabId(_tabs)
                    : _selectedTabId;
                string query = (_query ?? string.Empty).Trim();
                string queryLower = query.ToLowerInvariant();

                for (int i = 0; i < _entries.Count; i++)
                {
                    Entry entry = _entries[i];
                    if (_showTabs && !string.Equals(entry.TabId, activeTabId, StringComparison.OrdinalIgnoreCase))
                        continue;

                    bool match = query.Length == 0 || _mode switch
                    {
                        SearchMode.Key => entry.KeyLower.Contains(queryLower),
                        SearchMode.Value => entry.ValueLower.Contains(queryLower),
                        _ => entry.KeyLower.Contains(queryLower) || entry.ValueLower.Contains(queryLower)
                    };

                    if (match)
                        _filteredEntryIndices.Add(i);
                }
            }
        }
    }
}
