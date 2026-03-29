using UnityEditor;
using UnityEngine;

namespace GGemCo2DCoreEditor
{
    /// <summary>
    /// Cutscene 이벤트 Drawer에서 공통적으로 사용하는 UI 유틸리티 메서드를 제공합니다.
    /// PropertyField, HelpBox 등의 반복 로직을 단순화합니다.
    /// </summary>
    internal static class CutsceneEventDrawerUiUtil
    {
        /// <summary>
        /// 각 UI 요소 사이의 기본 세로 간격을 반환합니다.
        /// </summary>
        public static float VerticalSpacing => CutsceneEventDrawer.VerticalSpacing;

        /// <summary>
        /// SerializedProperty 한 줄을 그린 후, 다음 위치로 Rect를 이동시킵니다.
        /// </summary>
        /// <param name="current">현재 그리기 위치 (다음 위치로 갱신됨)</param>
        /// <param name="property">렌더링할 SerializedProperty</param>
        /// <param name="label">사용할 라벨 (null이면 기본 라벨 사용)</param>
        public static void DrawPropertyLine(ref Rect current, SerializedProperty property, GUIContent label = null)
        {
            float height = EditorGUI.GetPropertyHeight(property, true);
            current.height = height;

            // 라벨이 지정된 경우와 기본 라벨 사용 분기
            if (label == null)
            {
                EditorGUI.PropertyField(current, property, true);
            }
            else
            {
                EditorGUI.PropertyField(current, property, label, true);
            }

            // 다음 줄 위치로 이동
            current.y += height + VerticalSpacing;
            current.height = EditorGUIUtility.singleLineHeight;
        }

        /// <summary>
        /// HelpBox를 그리고, 다음 위치로 Rect를 이동시킵니다.
        /// </summary>
        /// <param name="current">현재 그리기 위치 (다음 위치로 갱신됨)</param>
        /// <param name="message">표시할 메시지</param>
        /// <param name="messageType">메시지 유형 (Info, Warning, Error)</param>
        public static void DrawHelpBox(ref Rect current, string message, MessageType messageType)
        {
            float height = EditorStyles.helpBox.CalcHeight(new GUIContent(message), current.width);
            current.height = height;

            EditorGUI.HelpBox(current, message, messageType);

            // 다음 줄 위치로 이동
            current.y += height + VerticalSpacing;
            current.height = EditorGUIUtility.singleLineHeight;
        }

        /// <summary>
        /// HelpBox를 그릴 때 필요한 높이를 계산합니다.
        /// </summary>
        /// <param name="message">표시할 메시지</param>
        /// <returns>HelpBox의 높이 + 기본 간격</returns>
        public static float GetHelpBoxHeight(string message)
        {
            // NOTE: 여유 padding(80f)은 Inspector 여백을 고려한 값
            return EditorStyles.helpBox.CalcHeight(
                       new GUIContent(message),
                       EditorGUIUtility.currentViewWidth - 80f)
                   + VerticalSpacing;
        }

        /// <summary>
        /// 라벨이 있는 그룹(UI 블록)의 기본 높이를 반환합니다.
        /// </summary>
        /// <returns>한 줄 높이 + 간격</returns>
        public static float GetLabeledGroupBaseHeight()
        {
            return EditorGUIUtility.singleLineHeight + VerticalSpacing;
        }
    }
}