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
        public static bool DrawButtonAndShow<T>(
            GUIContent buttonLabel,
            IReadOnlyList<Option<T>> options,
            int selectedIndex,
            Action<int, Option<T>> onSelected,
            GUIStyle? buttonStyle = null,
            float buttonHeight = 22f,
            int maxVisibleItems = 10,
            float rowHeight = 18f,
            float popupWidth = 320f,
            SearchMode defaultSearchMode = SearchMode.Both,
            float verticalOffset = 2f,
            bool disabled = false)
        {
            if (buttonLabel == null) throw new ArgumentNullException(nameof(buttonLabel));
            if (options == null) throw new ArgumentNullException(nameof(options));
            if (onSelected == null) throw new ArgumentNullException(nameof(onSelected));

            buttonStyle ??= GUI.skin.button;

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
                screenRect.x = 0;
                screenRect.y = btnRect.y;

                Show(
                    activatorRectScreen: screenRect,
                    options: options,
                    selectedIndex: selectedIndex,
                    onSelected: onSelected,
                    maxVisibleItems: maxVisibleItems,
                    rowHeight: rowHeight,
                    popupWidth: popupWidth,
                    defaultSearchMode: defaultSearchMode);

                return true;
            }
        }

        public static bool DrawButtonAndShow<T>(
            string buttonText,
            IReadOnlyList<Option<T>> options,
            int selectedIndex,
            Action<int, Option<T>> onSelected,
            GUIStyle? buttonStyle = null,
            float buttonHeight = 22f,
            int maxVisibleItems = 10,
            float rowHeight = 18f,
            float popupWidth = 320f,
            SearchMode defaultSearchMode = SearchMode.Both,
            float verticalOffset = 2f,
            bool disabled = false)
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
                disabled);
        }
    }
}
