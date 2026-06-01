#nullable enable

using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

namespace GGemCo2DCoreEditor
{
    /// <summary>
    /// 검색 가능한 커스텀 드롭다운(팝업) 유틸.
    /// - 높이(표시 개수) 제한
    /// - Key/Value/Both 검색 지원
    /// - 제네릭 데이터 페이로드 지원
    /// </summary>
    public static partial class SearchableDropdownUtility
    {
        /// <summary>
        /// 드롭다운 항목.
        /// </summary>
        public readonly struct Option<T>
        {
            public readonly string Key;
            public readonly string Value;
            public readonly T Data;

            internal readonly string KeyLower;
            internal readonly string ValueLower;

            public Option(string key, string value, T data)
            {
                Key = key ?? string.Empty;
                Value = value ?? string.Empty;
                Data = data;

                // 검색 성능: 소문자 캐시(매번 ToLowerInvariant 비용 방지)
                KeyLower = Key.ToLowerInvariant();
                ValueLower = Value.ToLowerInvariant();
            }

            public override string ToString()
            {
                if (string.IsNullOrEmpty(Key))
                    return Value;

                if (string.IsNullOrEmpty(Value))
                    return Key;

                return $"{Key}  |  {Value}";
            }
        }



        /// <summary>
        /// 탭으로 구분된 드롭다운 옵션 묶음.
        /// </summary>
        public readonly struct OptionTab<T>
        {
            public readonly string Id;
            public readonly string Label;
            public readonly IReadOnlyList<Option<T>> Options;

            public OptionTab(string id, string label, IReadOnlyList<Option<T>> options)
            {
                Id = string.IsNullOrWhiteSpace(id) ? "default" : id;
                Label = string.IsNullOrWhiteSpace(label) ? Id : label;
                Options = options ?? Array.Empty<Option<T>>();
            }
        }

        public enum SearchMode
        {
            Key = 0,
            Value = 1,
            Both = 2
        }

        /// <summary>
        /// 드롭다운이 처음 열릴 때 선택 항목을 기준으로 스크롤 위치를 정하는 정책입니다.
        /// </summary>
        public enum InitialScrollPolicy
        {
            /// <summary>
            /// 초기 스크롤을 보정하지 않습니다.
            /// </summary>
            None = 0,

            /// <summary>
            /// 선택 항목이 보이도록 필요한 만큼만 스크롤합니다.
            /// </summary>
            EnsureSelectedVisible = 1,

            /// <summary>
            /// 선택 항목이 포함된 MaxVisibleItems 단위 페이지의 시작 위치로 스크롤합니다.
            /// 예: MaxVisibleItems가 10이고 13번째 항목이 선택되어 있으면 11번째 항목부터 표시합니다.
            /// </summary>
            PageStart = 2,

            /// <summary>
            /// 선택 항목이 가능한 한 중앙에 오도록 스크롤합니다.
            /// </summary>
            Center = 3
        }

        /// <summary>
        /// 선택 항목과 표시 개수를 기준으로 드롭다운이 처음 보여줄 첫 번째 항목 인덱스를 계산합니다.
        /// </summary>
        /// <param name="selectedFilteredIndex">필터링된 목록 기준 선택 항목 인덱스입니다.</param>
        /// <param name="filteredCount">필터링된 항목 개수입니다.</param>
        /// <param name="maxVisibleItems">한 번에 보여줄 최대 항목 개수입니다.</param>
        /// <param name="policy">초기 스크롤 정책입니다.</param>
        /// <returns>처음 표시할 항목 인덱스입니다.</returns>
        private static int CalculateInitialFirstVisibleIndex(
            int selectedFilteredIndex,
            int filteredCount,
            int maxVisibleItems,
            InitialScrollPolicy policy)
        {
            if (policy == InitialScrollPolicy.None || selectedFilteredIndex < 0 || filteredCount <= 0 || maxVisibleItems <= 0)
                return 0;

            int maxFirstVisibleIndex = Mathf.Max(0, filteredCount - maxVisibleItems);

            int firstVisibleIndex = policy switch
            {
                InitialScrollPolicy.PageStart =>
                    (selectedFilteredIndex / maxVisibleItems) * maxVisibleItems,

                InitialScrollPolicy.Center =>
                    selectedFilteredIndex - (maxVisibleItems / 2),

                InitialScrollPolicy.EnsureSelectedVisible =>
                    selectedFilteredIndex - maxVisibleItems + 1,

                _ => 0
            };

            return Mathf.Clamp(firstVisibleIndex, 0, maxFirstVisibleIndex);
        }

        /// <summary>
        /// 검색 가능한 드롭다운 팝업을 표시합니다.
        /// </summary>
        /// <param name="activatorRectScreen">버튼/필드의 Screen Rect. (GUIUtility.GUIToScreenRect 결과)</param>
        /// <param name="options">옵션 목록</param>
        /// <param name="selectedIndex">현재 선택 인덱스(없으면 -1)</param>
        /// <param name="onSelected">선택 콜백(인덱스, 옵션)</param>
        /// <param name="maxVisibleItems">팝업에서 한 번에 보여줄 최대 항목 수</param>
        /// <param name="rowHeight">항목 높이</param>
        /// <param name="popupWidth">팝업 너비</param>
        /// <param name="defaultSearchMode">기본 검색 모드</param>
        /// <param name="initialScrollPolicy">드롭다운이 처음 열릴 때 적용할 선택 항목 기준 스크롤 정책</param>
        public static void Show<T>(
            Rect activatorRectScreen,
            IReadOnlyList<Option<T>> options,
            int selectedIndex,
            Action<int, Option<T>> onSelected,
            int maxVisibleItems = EditorConstants.SearchableDropdownUtility.MaxVisibleItems,
            float rowHeight = EditorConstants.SearchableDropdownUtility.RowHeight,
            float popupWidth = EditorConstants.SearchableDropdownUtility.PopupWidth,
            SearchMode defaultSearchMode = SearchMode.Both,
            InitialScrollPolicy initialScrollPolicy = InitialScrollPolicy.PageStart)
        {
            if (options == null) throw new ArgumentNullException(nameof(options));
            if (onSelected == null) throw new ArgumentNullException(nameof(onSelected));

            maxVisibleItems = Mathf.Clamp(maxVisibleItems, 3, 40);
            rowHeight = Mathf.Clamp(rowHeight, 16f, 28f);
            popupWidth = Mathf.Clamp(popupWidth, 220f, 800f);

            PopupWindow.Show(
                activatorRectScreen,
                new SearchableDropdownPopup<T>(
                    options,
                    selectedIndex,
                    onSelected,
                    maxVisibleItems,
                    rowHeight,
                    popupWidth,
                    defaultSearchMode,
                    initialScrollPolicy));
        }

        private sealed class SearchableDropdownPopup<T> : PopupWindowContent
        {
            private readonly IReadOnlyList<Option<T>> _options;
            private readonly Action<int, Option<T>> _onSelected;

            private readonly int _maxVisibleItems;
            private readonly float _rowHeight;
            private readonly float _popupWidth;
            private readonly InitialScrollPolicy _initialScrollPolicy;

            private readonly SearchField _searchField = new SearchField();
            private readonly List<int> _filtered = new List<int>(256);

            private int _selectedIndex;
            private int _hoverIndexInFiltered = -1;
            private Vector2 _scroll;
            private bool _initialScrollApplied;

            private string _query = string.Empty;
            private SearchMode _mode;

            // Layout constants
            private const float Padding = 6f;
            private const float ModeBarHeight = 18f;

            public SearchableDropdownPopup(
                IReadOnlyList<Option<T>> options,
                int selectedIndex,
                Action<int, Option<T>> onSelected,
                int maxVisibleItems,
                float rowHeight,
                float popupWidth,
                SearchMode defaultMode,
                InitialScrollPolicy initialScrollPolicy)
            {
                _options = options;
                _selectedIndex = selectedIndex;
                _onSelected = onSelected;

                _maxVisibleItems = maxVisibleItems;
                _rowHeight = rowHeight;
                _popupWidth = popupWidth;
                _initialScrollPolicy = initialScrollPolicy;

                _mode = defaultMode;

                RebuildFilter();
            }

            public override void OnOpen()
            {
                _searchField.SetFocus();
            }

            public override Vector2 GetWindowSize()
            {
                int visibleCount = Mathf.Min(_filtered.Count, _maxVisibleItems);
                visibleCount = Mathf.Max(visibleCount, 3);

                float toolbarHeight = EditorStyles.toolbar.fixedHeight > 0
                    ? EditorStyles.toolbar.fixedHeight
                    : 20f;

                float listHeight = visibleCount * _rowHeight;

                // padding + toolbar + modebar + list + padding
                float height = Padding + toolbarHeight + 4f + ModeBarHeight + 4f + listHeight + Padding;

                return new Vector2(_popupWidth, height);
            }

            public override void OnGUI(Rect rect)
            {
                HandleKeyboard();

                GUILayout.Space(Padding);
                DrawSearchBar();
                GUILayout.Space(4f);
                DrawModeBar();
                GUILayout.Space(4f);
                DrawList();
                GUILayout.Space(Padding);
            }

            private void DrawSearchBar()
            {
                using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
                {
                    string newQuery = _searchField.OnToolbarGUI(_query);
                    if (!string.Equals(newQuery, _query, StringComparison.Ordinal))
                    {
                        _query = newQuery ?? string.Empty;
                        RebuildFilter();
                        _scroll = Vector2.zero;
                        _hoverIndexInFiltered = -1;
                        editorWindow?.Repaint();
                    }
                }
            }

            private void DrawModeBar()
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    GUILayout.Label("Search:", GUILayout.Width(48));

                    int newMode = GUILayout.Toolbar(
                        (int)_mode,
                        new[] { "Key", "Value", "Both" },
                        GUILayout.Height(ModeBarHeight));

                    if (newMode != (int)_mode)
                    {
                        _mode = (SearchMode)newMode;
                        RebuildFilter();
                        _scroll = Vector2.zero;
                        _hoverIndexInFiltered = -1;
                        editorWindow?.Repaint();
                    }
                }
            }

            private void DrawList()
            {
                int visibleCount = Mathf.Min(_filtered.Count, _maxVisibleItems);
                visibleCount = Mathf.Max(visibleCount, 3);

                float viewHeight = visibleCount * _rowHeight;

                ApplyInitialScrollIfNeeded();

                _scroll = GUILayout.BeginScrollView(_scroll, false, true, GUILayout.Height(viewHeight));

                if (_filtered.Count == 0)
                {
                    GUILayout.Label("No results.", EditorStyles.miniLabel);
                    GUILayout.EndScrollView();
                    return;
                }

                for (int i = 0; i < _filtered.Count; i++)
                {
                    int optionIndex = _filtered[i];
                    Option<T> option = _options[optionIndex];

                    Rect rowRect = GUILayoutUtility.GetRect(0, _rowHeight, GUILayout.ExpandWidth(true));

                    bool isSelected = optionIndex == _selectedIndex;
                    bool isHover = i == _hoverIndexInFiltered;

                    if (Event.current.type == EventType.Repaint)
                    {
                        // 스타일 키는 Unity 버전에 따라 다를 수 있으므로, 안전 fallback 적용
                        GUIStyle style = GetRowBackgroundStyle(isSelected, isHover);
                        style.Draw(rowRect, GUIContent.none, false, false, isSelected, false);
                    }

                    if (GUI.Button(rowRect, option.ToString(), EditorStyles.label))
                    {
                        Select(optionIndex);
                        GUILayout.EndScrollView();
                        return;
                    }

                    if (rowRect.Contains(Event.current.mousePosition))
                        _hoverIndexInFiltered = i;
                }

                GUILayout.EndScrollView();
            }

            /// <summary>
            /// 드롭다운 최초 표시 시 현재 선택 항목이 포함된 위치로 스크롤을 보정합니다.
            /// 검색어/검색 모드 변경 이후에는 사용자의 스크롤 조작을 방해하지 않도록 한 번만 실행합니다.
            /// </summary>
            private void ApplyInitialScrollIfNeeded()
            {
                if (_initialScrollApplied)
                    return;

                _initialScrollApplied = true;

                int selectedFilteredIndex = FindFilteredIndexByOptionIndex(_selectedIndex);
                if (selectedFilteredIndex < 0)
                    return;

                _hoverIndexInFiltered = selectedFilteredIndex;

                int firstVisibleIndex = CalculateInitialFirstVisibleIndex(
                    selectedFilteredIndex,
                    _filtered.Count,
                    _maxVisibleItems,
                    _initialScrollPolicy);

                _scroll.y = firstVisibleIndex * _rowHeight;
            }

            /// <summary>
            /// 원본 옵션 인덱스를 필터링된 목록 기준 인덱스로 변환합니다.
            /// </summary>
            /// <param name="optionIndex">원본 옵션 인덱스입니다.</param>
            /// <returns>필터링된 목록 기준 인덱스입니다. 찾지 못하면 -1을 반환합니다.</returns>
            private int FindFilteredIndexByOptionIndex(int optionIndex)
            {
                if (optionIndex < 0)
                    return -1;

                for (int i = 0; i < _filtered.Count; i++)
                {
                    if (_filtered[i] == optionIndex)
                        return i;
                }

                return -1;
            }

            private static GUIStyle GetRowBackgroundStyle(bool selected, bool hover)
            {
                // 선택 > 호버 > 기본 우선순위
                if (selected)
                {
                    var s = GUI.skin.FindStyle("PR Label") ?? EditorStyles.helpBox;
                    return s;
                }

                if (hover)
                {
                    var s = GUI.skin.FindStyle("CN EntryBackEven") ?? EditorStyles.helpBox;
                    return s;
                }

                return GUI.skin.FindStyle("Label") ?? EditorStyles.label;
            }

            private void HandleKeyboard()
            {
                Event e = Event.current;
                if (e.type != EventType.KeyDown)
                    return;

                if (e.keyCode == KeyCode.DownArrow)
                {
                    if (_filtered.Count > 0)
                    {
                        _hoverIndexInFiltered = Mathf.Clamp(_hoverIndexInFiltered + 1, 0, _filtered.Count - 1);
                        EnsureHoverVisible();
                        e.Use();
                    }
                }
                else if (e.keyCode == KeyCode.UpArrow)
                {
                    if (_filtered.Count > 0)
                    {
                        _hoverIndexInFiltered = Mathf.Clamp(_hoverIndexInFiltered - 1, 0, _filtered.Count - 1);
                        EnsureHoverVisible();
                        e.Use();
                    }
                }
                else if (e.keyCode == KeyCode.Return || e.keyCode == KeyCode.KeypadEnter)
                {
                    if (_filtered.Count > 0)
                    {
                        int idxInFiltered = _hoverIndexInFiltered < 0 ? 0 : _hoverIndexInFiltered;
                        idxInFiltered = Mathf.Clamp(idxInFiltered, 0, _filtered.Count - 1);
                        Select(_filtered[idxInFiltered]);
                        e.Use();
                    }
                }
                else if (e.keyCode == KeyCode.Escape)
                {
                    editorWindow?.Close();
                    e.Use();
                }
            }

            private void EnsureHoverVisible()
            {
                if (_hoverIndexInFiltered < 0)
                    return;

                float top = _hoverIndexInFiltered * _rowHeight;
                float bottom = top + _rowHeight;

                float viewTop = _scroll.y;
                float viewBottom = _scroll.y + (_maxVisibleItems * _rowHeight);

                if (top < viewTop) _scroll.y = top;
                else if (bottom > viewBottom) _scroll.y = bottom - (_maxVisibleItems * _rowHeight);

                editorWindow?.Repaint();
            }

            private void Select(int optionIndex)
            {
                _selectedIndex = optionIndex;
                _onSelected.Invoke(optionIndex, _options[optionIndex]);
                editorWindow?.Close();
            }

            private void RebuildFilter()
            {
                _filtered.Clear();

                if (_options.Count == 0)
                    return;

                string q = (_query ?? string.Empty).Trim();
                if (q.Length == 0)
                {
                    for (int i = 0; i < _options.Count; i++)
                        _filtered.Add(i);
                    return;
                }

                string qLower = q.ToLowerInvariant();

                for (int i = 0; i < _options.Count; i++)
                {
                    Option<T> opt = _options[i];

                    bool match = _mode switch
                    {
                        SearchMode.Key => opt.KeyLower.Contains(qLower),
                        SearchMode.Value => opt.ValueLower.Contains(qLower),
                        _ => opt.KeyLower.Contains(qLower) || opt.ValueLower.Contains(qLower)
                    };

                    if (match)
                        _filtered.Add(i);
                }
            }
        }
    }
}
