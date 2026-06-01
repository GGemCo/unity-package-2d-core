#nullable enable

using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace GGemCo2DCoreEditor
{
    public static partial class SearchableDropdownUtility
    {
        /// <summary>
        /// Inspector 스타일: 라벨(Prefix) + 드롭다운 필드(버튼) 형태를 렌더링하고,
        /// 클릭 시 검색 가능한 드롭다운 팝업을 버튼 바로 아래에 표시합니다.
        /// </summary>
        /// <param name="initialScrollPolicy">드롭다운이 처음 열릴 때 적용할 선택 항목 기준 스크롤 정책입니다.</param>
        /// <param name="selectedKey">UID처럼 옵션 index가 아닌 <see cref="Option{T}.Key"/> 기준으로 선택 항목을 찾을 때 사용하는 값입니다.</param>
        private static bool DrawLabeledFieldAndShow<T>(
            GUIContent label,
            GUIContent fieldContent,
            IReadOnlyList<Option<T>> options,
            int selectedIndex,
            Action<int, Option<T>> onSelected,
            GUILayoutOption[]? layoutOptions = null,
            GUIStyle? fieldStyle = null,
            float fieldHeight = EditorConstants.SearchableDropdownUtility.FieldHeight,
            int maxVisibleItems = EditorConstants.SearchableDropdownUtility.MaxVisibleItems,
            float rowHeight = EditorConstants.SearchableDropdownUtility.RowHeight,
            float popupWidth = EditorConstants.SearchableDropdownUtility.PopupWidth,
            SearchMode defaultSearchMode = SearchMode.Both,
            float verticalOffset = EditorConstants.SearchableDropdownUtility.VerticalOffset,
            bool disabled = false,
            InitialScrollPolicy initialScrollPolicy = InitialScrollPolicy.PageStart,
            string? selectedKey = null)
        {
            if (label == null) throw new ArgumentNullException(nameof(label));
            if (fieldContent == null) throw new ArgumentNullException(nameof(fieldContent));
            if (options == null) throw new ArgumentNullException(nameof(options));
            if (onSelected == null) throw new ArgumentNullException(nameof(onSelected));

            fieldStyle ??= EditorStyles.popup;

            using (new EditorGUILayout.HorizontalScope(layoutOptions ?? Array.Empty<GUILayoutOption>()))
            using (new EditorGUI.DisabledScope(disabled))
            {
                Rect rowRect = EditorGUILayout.GetControlRect(true, fieldHeight);
                Rect fieldRect = EditorGUI.PrefixLabel(rowRect, label);

                bool clicked = GUI.Button(fieldRect, fieldContent, fieldStyle);

                if (!clicked || disabled)
                    return false;

                Rect screenRect = GUIUtility.GUIToScreenRect(fieldRect);
                screenRect.x = fieldRect.x;
                screenRect.y = fieldRect.y;

                Show(
                    activatorRectScreen: screenRect,
                    options: options,
                    selectedIndex: selectedIndex,
                    onSelected: onSelected,
                    maxVisibleItems: maxVisibleItems,
                    rowHeight: rowHeight,
                    popupWidth: popupWidth,
                    defaultSearchMode: defaultSearchMode,
                    initialScrollPolicy: initialScrollPolicy,
                    selectedKey: selectedKey);

                return true;
            }
        }

        /// <summary>
        /// selectedIndex를 기반으로 fieldContent를 자동 생성하는 편의 오버로드.
        /// </summary>
        /// <param name="selectedKey">UID처럼 옵션 index가 아닌 <see cref="Option{T}.Key"/> 기준으로 선택 항목을 찾을 때 사용하는 값입니다.</param>
        public static bool DrawLabeledFieldAndShow<T>(
            GUIContent label,
            IReadOnlyList<Option<T>> options,
            int selectedIndex,
            Action<int, Option<T>> onSelected,
            string noneText = "(Select...)",
            GUILayoutOption[]? layoutOptions = null,
            GUIStyle? fieldStyle = null,
            float fieldHeight = EditorConstants.SearchableDropdownUtility.FieldHeight,
            int maxVisibleItems = EditorConstants.SearchableDropdownUtility.MaxVisibleItems,
            float rowHeight = EditorConstants.SearchableDropdownUtility.RowHeight,
            float popupWidth = EditorConstants.SearchableDropdownUtility.PopupWidth,
            SearchMode defaultSearchMode = SearchMode.Both,
            float verticalOffset = EditorConstants.SearchableDropdownUtility.VerticalOffset,
            bool disabled = false,
            InitialScrollPolicy initialScrollPolicy = InitialScrollPolicy.PageStart,
            string? selectedKey = null)
        {
            string text = (selectedIndex >= 0 && selectedIndex < options.Count)
                ? options[selectedIndex].ToString()
                : (noneText ?? string.Empty);

            return DrawLabeledFieldAndShow(
                label,
                new GUIContent(text),
                options,
                selectedIndex,
                onSelected,
                layoutOptions,
                fieldStyle,
                fieldHeight,
                maxVisibleItems,
                rowHeight,
                popupWidth,
                defaultSearchMode,
                verticalOffset,
                disabled,
                initialScrollPolicy,
                selectedKey);
        }

        /// <summary>
        /// 문자열 라벨로 Inspector 스타일 드롭다운 필드를 표시합니다.
        /// </summary>
        /// <param name="selectedKey">UID처럼 옵션 index가 아닌 <see cref="Option{T}.Key"/> 기준으로 선택 항목을 찾을 때 사용하는 값입니다.</param>
        public static bool DrawLabeledFieldAndShow<T>(
            string labelText,
            IReadOnlyList<Option<T>> options,
            int selectedIndex,
            Action<int, Option<T>> onSelected,
            string noneText = "(Select...)",
            GUILayoutOption[]? layoutOptions = null,
            GUIStyle? fieldStyle = null,
            float fieldHeight = EditorConstants.SearchableDropdownUtility.FieldHeight,
            int maxVisibleItems = EditorConstants.SearchableDropdownUtility.MaxVisibleItems,
            float rowHeight = EditorConstants.SearchableDropdownUtility.RowHeight,
            float popupWidth = EditorConstants.SearchableDropdownUtility.PopupWidth,
            SearchMode defaultSearchMode = SearchMode.Both,
            float verticalOffset = EditorConstants.SearchableDropdownUtility.VerticalOffset,
            bool disabled = false,
            InitialScrollPolicy initialScrollPolicy = InitialScrollPolicy.PageStart,
            string? selectedKey = null)
        {
            return DrawLabeledFieldAndShow(
                new GUIContent(labelText ?? string.Empty),
                options,
                selectedIndex,
                onSelected,
                noneText,
                layoutOptions,
                fieldStyle,
                fieldHeight,
                maxVisibleItems,
                rowHeight,
                popupWidth,
                defaultSearchMode,
                verticalOffset,
                disabled,
                initialScrollPolicy,
                selectedKey);
        }
    }
}
