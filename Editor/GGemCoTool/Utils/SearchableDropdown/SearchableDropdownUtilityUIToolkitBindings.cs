#nullable enable

using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace GGemCo2DCoreEditor
{
    public static partial class SearchableDropdownUtility
    {
        /// <summary>
        /// UI Toolkit Button에 검색 드롭다운을 바인딩합니다.
        /// 버튼 텍스트는 호출부에서 필요 시 갱신하세요.
        /// </summary>
        /// <param name="initialScrollPolicy">드롭다운이 처음 열릴 때 적용할 선택 항목 기준 스크롤 정책입니다.</param>
        /// <param name="getSelectedKey">UID처럼 옵션 index가 아닌 <see cref="Option{T}.Key"/> 기준 선택 값을 반환하는 함수입니다.</param>
        public static void BindUiToolkitButton<T>(
            EditorWindow owner,
            Button button,
            IReadOnlyList<Option<T>> options,
            Func<int> getSelectedIndex,
            Action<int, Option<T>> onSelected,
            int maxVisibleItems = EditorConstants.SearchableDropdownUtility.MaxVisibleItems,
            float rowHeight = EditorConstants.SearchableDropdownUtility.RowHeight,
            float popupWidth = EditorConstants.SearchableDropdownUtility.PopupWidth,
            SearchMode defaultSearchMode = SearchMode.Both,
            float verticalOffset = EditorConstants.SearchableDropdownUtility.VerticalOffset,
            InitialScrollPolicy initialScrollPolicy = InitialScrollPolicy.PageStart,
            Func<string>? getSelectedKey = null)
        {
            if (owner == null) throw new ArgumentNullException(nameof(owner));
            if (button == null) throw new ArgumentNullException(nameof(button));
            if (options == null) throw new ArgumentNullException(nameof(options));
            if (getSelectedIndex == null) throw new ArgumentNullException(nameof(getSelectedIndex));
            if (onSelected == null) throw new ArgumentNullException(nameof(onSelected));

            button.clicked += () =>
            {
                int selectedIndex = getSelectedIndex.Invoke();
                string selectedKey = getSelectedKey?.Invoke() ?? string.Empty;
                Rect screenRect = GetScreenRect(owner, button);

                ShowUiToolkit(
                    owner,
                    screenRect,
                    options,
                    selectedIndex,
                    onSelected,
                    maxVisibleItems,
                    rowHeight,
                    popupWidth,
                    defaultSearchMode,
                    verticalOffset,
                    initialScrollPolicy,
                    selectedKey);
            };
        }

        /// <summary>
        /// UI Toolkit Button에 탭 기반 검색 드롭다운을 바인딩합니다.
        /// </summary>
        /// <param name="initialScrollPolicy">드롭다운이 처음 열릴 때 적용할 선택 항목 기준 스크롤 정책입니다.</param>
        /// <param name="getSelectedKey">탭 ID를 받아 <see cref="Option{T}.Key"/> 기준 선택 값을 반환하는 함수입니다.</param>
        public static void BindUiToolkitButton<T>(
            EditorWindow owner,
            Button button,
            IReadOnlyList<OptionTab<T>> tabs,
            Func<string> getSelectedTabId,
            Func<string, int> getSelectedIndex,
            Action<OptionTab<T>, int, Option<T>> onSelected,
            int maxVisibleItems = EditorConstants.SearchableDropdownUtility.MaxVisibleItems,
            float rowHeight = EditorConstants.SearchableDropdownUtility.RowHeight,
            float popupWidth = EditorConstants.SearchableDropdownUtility.PopupWidth,
            SearchMode defaultSearchMode = SearchMode.Both,
            float verticalOffset = EditorConstants.SearchableDropdownUtility.VerticalOffset,
            InitialScrollPolicy initialScrollPolicy = InitialScrollPolicy.PageStart,
            Func<string, string>? getSelectedKey = null)
        {
            if (owner == null) throw new ArgumentNullException(nameof(owner));
            if (button == null) throw new ArgumentNullException(nameof(button));
            if (tabs == null) throw new ArgumentNullException(nameof(tabs));
            if (getSelectedTabId == null) throw new ArgumentNullException(nameof(getSelectedTabId));
            if (getSelectedIndex == null) throw new ArgumentNullException(nameof(getSelectedIndex));
            if (onSelected == null) throw new ArgumentNullException(nameof(onSelected));

            button.clicked += () =>
            {
                string selectedTabId = getSelectedTabId.Invoke();
                int selectedIndex = getSelectedIndex.Invoke(selectedTabId);
                string selectedKey = getSelectedKey?.Invoke(selectedTabId) ?? string.Empty;
                Rect screenRect = GetScreenRect(owner, button);

                ShowUiToolkit(
                    owner,
                    screenRect,
                    tabs,
                    selectedTabId,
                    selectedIndex,
                    onSelected,
                    maxVisibleItems,
                    rowHeight,
                    popupWidth,
                    defaultSearchMode,
                    verticalOffset,
                    initialScrollPolicy,
                    selectedKey);
            };
        }

        /// <summary>
        /// UI Toolkit VisualElement(예: clickable container)에 검색 드롭다운을 바인딩합니다.
        /// </summary>
        /// <param name="initialScrollPolicy">드롭다운이 처음 열릴 때 적용할 선택 항목 기준 스크롤 정책입니다.</param>
        /// <param name="getSelectedKey">UID처럼 옵션 index가 아닌 <see cref="Option{T}.Key"/> 기준 선택 값을 반환하는 함수입니다.</param>
        public static void BindUiToolkitClickable<T>(
            EditorWindow owner,
            VisualElement element,
            IReadOnlyList<Option<T>> options,
            Func<int> getSelectedIndex,
            Action<int, Option<T>> onSelected,
            int maxVisibleItems = EditorConstants.SearchableDropdownUtility.MaxVisibleItems,
            float rowHeight = EditorConstants.SearchableDropdownUtility.RowHeight,
            float popupWidth = EditorConstants.SearchableDropdownUtility.PopupWidth,
            SearchMode defaultSearchMode = SearchMode.Both,
            float verticalOffset = EditorConstants.SearchableDropdownUtility.VerticalOffset,
            InitialScrollPolicy initialScrollPolicy = InitialScrollPolicy.PageStart,
            Func<string>? getSelectedKey = null)
        {
            if (owner == null) throw new ArgumentNullException(nameof(owner));
            if (element == null) throw new ArgumentNullException(nameof(element));
            if (options == null) throw new ArgumentNullException(nameof(options));
            if (getSelectedIndex == null) throw new ArgumentNullException(nameof(getSelectedIndex));
            if (onSelected == null) throw new ArgumentNullException(nameof(onSelected));

            element.RegisterCallback<PointerDownEvent>(evt =>
            {
                if (evt.button != 0)
                    return;

                int selectedIndex = getSelectedIndex.Invoke();
                string selectedKey = getSelectedKey?.Invoke() ?? string.Empty;
                Rect screenRect = GetScreenRect(owner, element);

                ShowUiToolkit(
                    owner,
                    screenRect,
                    options,
                    selectedIndex,
                    onSelected,
                    maxVisibleItems,
                    rowHeight,
                    popupWidth,
                    defaultSearchMode,
                    verticalOffset,
                    initialScrollPolicy,
                    selectedKey);

                evt.StopPropagation();
            });
        }
    }
}
