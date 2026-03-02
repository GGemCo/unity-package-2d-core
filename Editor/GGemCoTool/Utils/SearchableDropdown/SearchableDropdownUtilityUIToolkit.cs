#nullable enable

using System;
using System.Collections.Generic;
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
    /// </summary>
    public static partial class SearchableDropdownUtility
    {
        // --------------------------------------------------------------------
        // NOTE:
        // - Option<T>, SearchMode는 기존 코어(SearchableDropdownUtility.cs)에 있다고 가정합니다.
        // - 네임스페이스는 사용 중인 프로젝트에 맞춰 조정하세요.
        // --------------------------------------------------------------------

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

            maxVisibleItems = Mathf.Clamp(maxVisibleItems, 3, 40);
            rowHeight = Mathf.Clamp(rowHeight, 18f, 28f);
            popupWidth = Mathf.Clamp(popupWidth, 240f, 900f);

            // 버튼/필드 바로 아래로 앵커 이동
            activatorRectScreen.y = activatorRectScreen.yMax + verticalOffset;

            var wnd = ScriptableObject.CreateInstance<SearchableDropdownUiToolkitWindow>();
            if (wnd == null)
            {
                Debug.LogError("[SearchableDropdownUiToolkit] Failed to create window instance.");
                return;
            }

            // 제네릭 옵션을 비제네릭 엔트리로 변환
            var entries = new List<SearchableDropdownUiToolkitWindow.Entry>(options.Count);
            for (int i = 0; i < options.Count; i++)
            {
                var opt = options[i];
                entries.Add(new SearchableDropdownUiToolkitWindow.Entry(
                    optionIndex: i,
                    key: opt.Key,
                    value: opt.Value,
                    display: opt.ToString()));
            }

            wnd.Initialize(
                entries: entries,
                selectedOptionIndex: selectedIndex,
                onSelectedOptionIndex: optionIndex =>
                {
                    if (optionIndex < 0 || optionIndex >= options.Count)
                        return;

                    onSelected(optionIndex, options[optionIndex]);
                },
                maxVisibleItems: maxVisibleItems,
                rowHeight: rowHeight,
                popupWidth: popupWidth,
                defaultMode: defaultSearchMode);

            // DropDown 크기(헤더 + 리스트)
            int visibleCount = Mathf.Min(entries.Count, maxVisibleItems);
            visibleCount = Mathf.Max(visibleCount, 3);

            float headerHeight = 56f;
            float listHeight = visibleCount * rowHeight;
            float height = headerHeight + listHeight + 10f;

            wnd.ShowAsDropDown(activatorRectScreen, new Vector2(popupWidth, height));
        }

        /// <summary>
        /// UI Toolkit VisualElement(worldBound)를 EditorWindow screen 좌표 Rect로 변환합니다.
        /// </summary>
        public static Rect GetScreenRect(EditorWindow owner, VisualElement element)
        {
            if (owner == null) throw new ArgumentNullException(nameof(owner));
            if (element == null) throw new ArgumentNullException(nameof(element));

            Rect wb = element.worldBound; // panel 좌표
            Rect ow = owner.position;     // screen 좌표

            return new Rect(ow.x + wb.x, ow.y + wb.y, wb.width, wb.height);
        }

        // --------------------------------------------------------------------
        // DropDown Window (non-generic)
        // --------------------------------------------------------------------
        private sealed class SearchableDropdownUiToolkitWindow : EditorWindow
        {
            internal readonly struct Entry
            {
                public readonly int OptionIndex;
                public readonly string Key;
                public readonly string Value;
                public readonly string Display;

                public readonly string KeyLower;
                public readonly string ValueLower;

                public Entry(int optionIndex, string key, string value, string display)
                {
                    OptionIndex = optionIndex;
                    Key = key ?? string.Empty;
                    Value = value ?? string.Empty;
                    Display = display ?? string.Empty;

                    KeyLower = Key.ToLowerInvariant();
                    ValueLower = Value.ToLowerInvariant();
                }
            }

            private List<Entry> _entries = new();
            private Action<int>? _onSelectedOptionIndex;

            private int _selectedOptionIndex;
            private int _maxVisibleItems;
            private float _rowHeight;
            private float _popupWidth;
            private SearchMode _mode;

            private string _query = string.Empty;

            // filteredEntryIndices: _entries 인덱스 목록
            private readonly List<int> _filteredEntryIndices = new(256);
            private readonly List<string> _display = new(256);

            // UI
            private ToolbarSearchField? _searchField;
            private ToolbarMenu? _modeMenu;
            private ListView? _listView;
            private const float HeaderFixedHeight = 44f; // 검색(1줄) + 모드(1줄) + 여유
            private const float WindowPadding = 6f;

            // 핵심: 초기/리프레시 선택 세팅 시 selectionChange 콜백 억제
            private bool _suppressSelectionCallback;

            // 리플렉션 캐시 (Unity 버전에 따라 SetSelectionWithoutNotify 존재 여부가 다릅니다)
            private static MethodInfo? _setSelectionWithoutNotifyInt;

            public void Initialize(
                List<Entry> entries,
                int selectedOptionIndex,
                Action<int> onSelectedOptionIndex,
                int maxVisibleItems,
                float rowHeight,
                float popupWidth,
                SearchMode defaultMode)
            {
                _entries = entries ?? new List<Entry>();
                _selectedOptionIndex = selectedOptionIndex;
                _onSelectedOptionIndex = onSelectedOptionIndex ?? throw new ArgumentNullException(nameof(onSelectedOptionIndex));

                _maxVisibleItems = maxVisibleItems;
                _rowHeight = rowHeight;
                _popupWidth = popupWidth;
                _mode = defaultMode;

                RebuildFilter();
            }

            private void CreateGUI()
            {
                rootVisualElement.style.flexDirection = FlexDirection.Column;
                rootVisualElement.style.paddingLeft = 6;
                rootVisualElement.style.paddingRight = 6;
                rootVisualElement.style.paddingTop = 6;
                rootVisualElement.style.paddingBottom = 6;
                rootVisualElement.style.backgroundColor = new Color(0.3f, 0.3f, 0.3f);
                
                // Header
                var header = new VisualElement
                {
                    style =
                    {
                        flexDirection = FlexDirection.Column,
                        // gap = 4,
                        flexShrink = 0,                 // 헤더가 줄어들지 않게
                        height = HeaderFixedHeight      // 고정 높이
                    }
                };
                
                // (선택) ToolbarSearchField 높이를 명확히 하고 싶다면
                _searchField = new ToolbarSearchField { value = _query };
                _searchField.style.flexShrink = 0;
                _searchField.RegisterValueChangedCallback(evt =>
                {
                    _query = evt.newValue ?? string.Empty;
                    RebuildFilter();
                    RefreshListView();
                });

                // modeRow도 shrink 방지
                var modeRow = new VisualElement
                {
                    style =
                    {
                        flexDirection = FlexDirection.Row,
                        justifyContent = Justify.SpaceBetween,
                        alignItems = Align.Center,
                        flexShrink = 0
                    }
                };

                var modeLabel = new Label("Search:")
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

                // ListView
                _listView = new ListView
                {
                    virtualizationMethod = CollectionVirtualizationMethod.FixedHeight,
                    fixedItemHeight = _rowHeight,
                    selectionType = SelectionType.Single,
                    itemsSource = _display
                };

                // ListView가 헤더를 침범하지 않도록 명확히
                _listView.style.flexGrow = 0;   // 높이를 우리가 직접 지정할 것이므로 grow는 0
                _listView.style.flexShrink = 0;

                _listView.makeItem = () =>
                {
                    var item = new Label
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

                // 선택 변경: suppress 플래그로 초기화/리프레시 시 이벤트 무시
                _listView.selectionChanged += _ =>
                {
                    if (_suppressSelectionCallback)
                        return;

                    if (_listView == null) return;

                    int sel = _listView.selectedIndex;
                    if (sel < 0 || sel >= _filteredEntryIndices.Count) return;

                    int entryIndex = _filteredEntryIndices[sel];
                    int optionIndex = _entries[entryIndex].OptionIndex;
                    Select(optionIndex);
                };

                // 키보드 처리 (Enter/Escape)
                _listView.RegisterCallback<KeyDownEvent>(evt =>
                {
                    if (_listView == null) return;

                    if (evt.keyCode == KeyCode.Return || evt.keyCode == KeyCode.KeypadEnter)
                    {
                        int sel = _listView.selectedIndex;
                        if (sel >= 0 && sel < _filteredEntryIndices.Count)
                        {
                            int entryIndex = _filteredEntryIndices[sel];
                            Select(_entries[entryIndex].OptionIndex);
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

            private void SetMode(SearchMode mode)
            {
                if (_mode == mode) return;

                _mode = mode;
                if (_modeMenu != null) _modeMenu.text = ModeToText(_mode);

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

                // 선택 유지 시도 (초기화 중에는 알림 없이 선택 설정)
                int idxInFiltered = FindFilteredIndexByOptionIndex(_selectedOptionIndex);
                int targetSelection =
                    idxInFiltered >= 0 ? idxInFiltered :
                    (_filteredEntryIndices.Count > 0 ? 0 : -1);

                if (_listView != null && targetSelection >= 0 && _selectedOptionIndex > -1)
                    SetSelectionWithoutNotifySafe(_listView, targetSelection);

                // Window 고정 크기 (표시 개수 제한)
                int visibleCount = Mathf.Min(_filteredEntryIndices.Count, _maxVisibleItems);
                visibleCount = Mathf.Max(visibleCount, 3);

                float listHeight = visibleCount * _rowHeight;

                // (핵심) ListView 높이를 명시적으로 지정
                if (_listView != null)
                {
                    _listView.style.height = listHeight;
                }

                // DropDown Window 전체 높이 = padding(top/bottom) + header + list + padding 여유
                float height = (WindowPadding * 2f) + HeaderFixedHeight + listHeight + 4f;

                // DropDown은 크기 변경에 민감하므로 min/max를 동일하게 유지
                EditorApplication.delayCall += () =>
                {
                    if (this == null) return;
                    minSize = maxSize = new Vector2(_popupWidth, height);
                };
            }

            private int FindFilteredIndexByOptionIndex(int optionIndex)
            {
                for (int i = 0; i < _filteredEntryIndices.Count; i++)
                {
                    int entryIndex = _filteredEntryIndices[i];
                    if (_entries[entryIndex].OptionIndex == optionIndex)
                        return i;
                }
                return -1;
            }

            /// <summary>
            /// Unity 버전에 따라 SetSelectionWithoutNotify가 없을 수 있으므로,
            /// suppress 플래그 + reflection fallback으로 안전하게 선택을 설정합니다.
            /// </summary>
            private void SetSelectionWithoutNotifySafe(ListView listView, int index)
            {
                _suppressSelectionCallback = true;
                try
                {
                    // reflection 캐시
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
                        // 대안: 일반 SetSelection 사용(하지만 suppress로 콜백은 무시)
                        listView.SetSelection(index);
                    }
                }
                finally
                {
                    _suppressSelectionCallback = false;
                }
            }

            private void Select(int optionIndex)
            {
                _selectedOptionIndex = optionIndex;

                // 콜백 먼저 실행 (호출부에서 상태 갱신)
                _onSelectedOptionIndex?.Invoke(optionIndex);

                // CreateGUI/초기화 중 Close 호출 NRE 방지: 다음 틱에 닫기
                DelayClose();
            }

            private void DelayClose()
            {
                EditorApplication.delayCall += () =>
                {
                    // 이미 파괴/닫힘 상태일 수 있으므로 방어
                    if (this == null) return;
                    try
                    {
                        Close();
                    }
                    catch
                    {
                        // Close 내부가 Unity 상태에 따라 예외를 낼 수 있어 방어적으로 무시
                    }
                };
            }

            private void RebuildFilter()
            {
                _filteredEntryIndices.Clear();

                if (_entries.Count == 0)
                    return;

                string q = (_query ?? string.Empty).Trim();
                if (q.Length == 0)
                {
                    for (int i = 0; i < _entries.Count; i++)
                        _filteredEntryIndices.Add(i);
                    return;
                }

                string qLower = q.ToLowerInvariant();

                for (int i = 0; i < _entries.Count; i++)
                {
                    Entry e = _entries[i];

                    bool match = _mode switch
                    {
                        SearchMode.Key => e.KeyLower.Contains(qLower),
                        SearchMode.Value => e.ValueLower.Contains(qLower),
                        _ => e.KeyLower.Contains(qLower) || e.ValueLower.Contains(qLower)
                    };

                    if (match)
                        _filteredEntryIndices.Add(i);
                }
            }
        }
    }
}
