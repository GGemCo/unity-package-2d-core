using UnityEditor;
using UnityEngine;

namespace GGemCo2DCoreEditor
{
    internal static class CutsceneEventDrawerUiUtil
    {
        public static float VerticalSpacing => CutsceneEventDrawer.VerticalSpacing;

        public static void DrawPropertyLine(ref Rect current, SerializedProperty property, GUIContent label = null)
        {
            float height = EditorGUI.GetPropertyHeight(property, true);
            current.height = height;

            if (label == null)
            {
                EditorGUI.PropertyField(current, property, true);
            }
            else
            {
                EditorGUI.PropertyField(current, property, label, true);
            }

            current.y += height + VerticalSpacing;
            current.height = EditorGUIUtility.singleLineHeight;
        }

        public static void DrawHelpBox(ref Rect current, string message, MessageType messageType)
        {
            float height = EditorStyles.helpBox.CalcHeight(new GUIContent(message), current.width);
            current.height = height;
            EditorGUI.HelpBox(current, message, messageType);
            current.y += height + VerticalSpacing;
            current.height = EditorGUIUtility.singleLineHeight;
        }

        public static float GetHelpBoxHeight(string message)
        {
            return EditorStyles.helpBox.CalcHeight(new GUIContent(message), EditorGUIUtility.currentViewWidth - 80f) + VerticalSpacing;
        }

        public static float GetLabeledGroupBaseHeight()
        {
            return EditorGUIUtility.singleLineHeight + VerticalSpacing;
        }
    }
}
