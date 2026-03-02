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
            float verticalOffset = EditorConstants.SearchableDropdownUtility.VerticalOffset)
        {
            if (owner == null) throw new ArgumentNullException(nameof(owner));
            if (button == null) throw new ArgumentNullException(nameof(button));
            if (options == null) throw new ArgumentNullException(nameof(options));
            if (getSelectedIndex == null) throw new ArgumentNullException(nameof(getSelectedIndex));
            if (onSelected == null) throw new ArgumentNullException(nameof(onSelected));

            button.clicked += () =>
            {
                int selectedIndex = getSelectedIndex.Invoke();

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
                    verticalOffset);
            };
        }

        /// <summary>
        /// UI Toolkit VisualElement(예: clickable container)에 검색 드롭다운을 바인딩합니다.
        /// </summary>
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
            float verticalOffset = EditorConstants.SearchableDropdownUtility.VerticalOffset)
        {
            if (owner == null) throw new ArgumentNullException(nameof(owner));
            if (element == null) throw new ArgumentNullException(nameof(element));
            if (options == null) throw new ArgumentNullException(nameof(options));
            if (getSelectedIndex == null) throw new ArgumentNullException(nameof(getSelectedIndex));
            if (onSelected == null) throw new ArgumentNullException(nameof(onSelected));

            // ClickEvent는 버튼이 아니면 안 올 수 있어, PointerDown으로 처리
            element.RegisterCallback<PointerDownEvent>(evt =>
            {
                if (evt.button != 0) return; // left click only

                int selectedIndex = getSelectedIndex.Invoke();
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
                    verticalOffset);

                evt.StopPropagation();
            });
        }
    }
}
