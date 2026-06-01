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
        /// 버튼을 그리고, 클릭 시 검색 가능한 드롭다운 팝업을 버튼 바로 아래에 표시합니다.
        /// (GUILayout 기반 Rect 계산/이벤트 단계 이슈를 유틸 내부에서 해결)
        /// </summary>
        /// <param name="initialScrollPolicy">드롭다운이 처음 열릴 때 적용할 선택 항목 기준 스크롤 정책입니다.</param>
        /// <param name="selectedKey">UID처럼 옵션 index가 아닌 <see cref="Option{T}.Key"/> 기준으로 선택 항목을 찾을 때 사용하는 값입니다.</param>
        private static bool DrawButtonAndShow<T>(
            GUIContent buttonLabel,
            IReadOnlyList<Option<T>> options,
            int selectedIndex,
            Action<int, Option<T>> onSelected,
            GUIStyle? buttonStyle = null,
            float buttonHeight = EditorConstants.SearchableDropdownUtility.ButtonHeight,
            int maxVisibleItems = EditorConstants.SearchableDropdownUtility.MaxVisibleItems,
            float rowHeight = EditorConstants.SearchableDropdownUtility.RowHeight,
            float popupWidth = EditorConstants.SearchableDropdownUtility.PopupWidth,
            SearchMode defaultSearchMode = SearchMode.Both,
            float verticalOffset = EditorConstants.SearchableDropdownUtility.VerticalOffset,
            bool disabled = false,
            InitialScrollPolicy initialScrollPolicy = InitialScrollPolicy.PageStart,
            string? selectedKey = null)
        {
            if (buttonLabel == null) throw new ArgumentNullException(nameof(buttonLabel));
            if (options == null) throw new ArgumentNullException(nameof(options));
            if (onSelected == null) throw new ArgumentNullException(nameof(onSelected));

            buttonStyle ??= new GUIStyle(GUI.skin.button);
            buttonStyle.alignment = TextAnchor.MiddleLeft;

            using (new EditorGUI.DisabledScope(disabled))
            {
                // (중요) GetLastRect() 사용 금지: Layout/Repaint 타이밍 이슈 방지
                Rect btnRect = GUILayoutUtility.GetRect(
                    buttonLabel,
                    buttonStyle,
                    GUILayout.Height(buttonHeight),
                    GUILayout.ExpandWidth(true));

                bool clicked = GUI.Button(btnRect, buttonLabel, buttonStyle);

                if (!clicked || disabled)
                    return false;

                Rect screenRect = GUIUtility.GUIToScreenRect(btnRect);
                screenRect.x = btnRect.x;
                screenRect.y = btnRect.y;

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
        /// 문자열 라벨 버튼을 그리고, 클릭 시 검색 가능한 드롭다운 팝업을 표시합니다.
        /// </summary>
        /// <param name="selectedKey">UID처럼 옵션 index가 아닌 <see cref="Option{T}.Key"/> 기준으로 선택 항목을 찾을 때 사용하는 값입니다.</param>
        public static bool DrawButtonAndShow<T>(
            string buttonText,
            IReadOnlyList<Option<T>> options,
            int selectedIndex,
            Action<int, Option<T>> onSelected,
            GUIStyle? buttonStyle = null,
            float buttonHeight = EditorConstants.SearchableDropdownUtility.ButtonHeight,
            int maxVisibleItems = EditorConstants.SearchableDropdownUtility.MaxVisibleItems,
            float rowHeight = EditorConstants.SearchableDropdownUtility.RowHeight,
            float popupWidth = EditorConstants.SearchableDropdownUtility.PopupWidth,
            SearchMode defaultSearchMode = SearchMode.Both,
            float verticalOffset = EditorConstants.SearchableDropdownUtility.VerticalOffset,
            bool disabled = false,
            InitialScrollPolicy initialScrollPolicy = InitialScrollPolicy.PageStart,
            string? selectedKey = null)
        {
            return DrawButtonAndShow(
                new GUIContent(buttonText ?? string.Empty),
                options,
                selectedIndex,
                onSelected,
                buttonStyle,
                buttonHeight,
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
