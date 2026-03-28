using GGemCo2DCore;
using UnityEditor;
using UnityEngine;

namespace GGemCo2DCoreEditor
{
    internal sealed class CutsceneOverlayTextEventTypeDrawer : ICutsceneEventTypeDrawer
    {
        public CutsceneEventType EventType => CutsceneEventType.OverlayText;

        public void Draw(Rect position, SerializedProperty eventProperty)
        {
            var overlayTextProp = eventProperty.FindPropertyRelative("overlayText");
            if (overlayTextProp == null)
            {
                return;
            }

            Rect current = position;
            current.height = EditorGUIUtility.singleLineHeight;

            EditorGUI.LabelField(current, overlayTextProp.displayName, EditorStyles.boldLabel);
            current.y += current.height + CutsceneEventDrawerUiUtil.VerticalSpacing;

            int originalIndent = EditorGUI.indentLevel;
            EditorGUI.indentLevel++;

            var sourceModeProp = overlayTextProp.FindPropertyRelative("sourceMode");
            var textProp = overlayTextProp.FindPropertyRelative("text");
            var runtimeTextKeyProp = overlayTextProp.FindPropertyRelative("runtimeTextKey");
            var anchoredPositionProp = overlayTextProp.FindPropertyRelative("anchoredPosition");
            var sizeDeltaProp = overlayTextProp.FindPropertyRelative("sizeDelta");
            var fontSizeProp = overlayTextProp.FindPropertyRelative("fontSize");
            var textColorProp = overlayTextProp.FindPropertyRelative("textColor");
            var maxAlphaProp = overlayTextProp.FindPropertyRelative("maxAlpha");
            var fadeInProp = overlayTextProp.FindPropertyRelative("fadeIn");
            var fadeOutProp = overlayTextProp.FindPropertyRelative("fadeOut");
            var easingProp = overlayTextProp.FindPropertyRelative("easing");
            var useUnscaledTimeProp = overlayTextProp.FindPropertyRelative("useUnscaledTime");

            CutsceneEventDrawerUiUtil.DrawPropertyLine(ref current, sourceModeProp);

            var sourceMode = (OverlayTextSourceMode)sourceModeProp.enumValueIndex;
            if (sourceMode == OverlayTextSourceMode.RuntimeOverride)
            {
                CutsceneEventDrawerUiUtil.DrawPropertyLine(ref current, runtimeTextKeyProp);
                CutsceneEventDrawerUiUtil.DrawPropertyLine(ref current, textProp, new GUIContent("Fallback Text"));
            }
            else
            {
                CutsceneEventDrawerUiUtil.DrawPropertyLine(ref current, textProp);
            }

            CutsceneEventDrawerUiUtil.DrawPropertyLine(ref current, anchoredPositionProp);
            CutsceneEventDrawerUiUtil.DrawPropertyLine(ref current, sizeDeltaProp);
            CutsceneEventDrawerUiUtil.DrawPropertyLine(ref current, fontSizeProp);
            CutsceneEventDrawerUiUtil.DrawPropertyLine(ref current, textColorProp);
            CutsceneEventDrawerUiUtil.DrawPropertyLine(ref current, maxAlphaProp);
            CutsceneEventDrawerUiUtil.DrawPropertyLine(ref current, fadeInProp);
            CutsceneEventDrawerUiUtil.DrawPropertyLine(ref current, fadeOutProp);
            CutsceneEventDrawerUiUtil.DrawPropertyLine(ref current, easingProp);
            CutsceneEventDrawerUiUtil.DrawPropertyLine(ref current, useUnscaledTimeProp);

            EditorGUI.indentLevel = originalIndent;
        }

        public float GetHeight(SerializedProperty eventProperty)
        {
            var overlayTextProp = eventProperty.FindPropertyRelative("overlayText");
            if (overlayTextProp == null)
            {
                return EditorGUIUtility.singleLineHeight;
            }

            float height = CutsceneEventDrawerUiUtil.GetLabeledGroupBaseHeight();
            height += EditorGUI.GetPropertyHeight(overlayTextProp.FindPropertyRelative("sourceMode"), true) + CutsceneEventDrawerUiUtil.VerticalSpacing;

            var sourceModeProp = overlayTextProp.FindPropertyRelative("sourceMode");
            var sourceMode = (OverlayTextSourceMode)sourceModeProp.enumValueIndex;
            if (sourceMode == OverlayTextSourceMode.RuntimeOverride)
            {
                height += EditorGUI.GetPropertyHeight(overlayTextProp.FindPropertyRelative("runtimeTextKey"), true) + CutsceneEventDrawerUiUtil.VerticalSpacing;
                height += EditorGUI.GetPropertyHeight(overlayTextProp.FindPropertyRelative("text"), true) + CutsceneEventDrawerUiUtil.VerticalSpacing;
            }
            else
            {
                height += EditorGUI.GetPropertyHeight(overlayTextProp.FindPropertyRelative("text"), true) + CutsceneEventDrawerUiUtil.VerticalSpacing;
            }

            height += EditorGUI.GetPropertyHeight(overlayTextProp.FindPropertyRelative("anchoredPosition"), true) + CutsceneEventDrawerUiUtil.VerticalSpacing;
            height += EditorGUI.GetPropertyHeight(overlayTextProp.FindPropertyRelative("sizeDelta"), true) + CutsceneEventDrawerUiUtil.VerticalSpacing;
            height += EditorGUI.GetPropertyHeight(overlayTextProp.FindPropertyRelative("fontSize"), true) + CutsceneEventDrawerUiUtil.VerticalSpacing;
            height += EditorGUI.GetPropertyHeight(overlayTextProp.FindPropertyRelative("textColor"), true) + CutsceneEventDrawerUiUtil.VerticalSpacing;
            height += EditorGUI.GetPropertyHeight(overlayTextProp.FindPropertyRelative("maxAlpha"), true) + CutsceneEventDrawerUiUtil.VerticalSpacing;
            height += EditorGUI.GetPropertyHeight(overlayTextProp.FindPropertyRelative("fadeIn"), true) + CutsceneEventDrawerUiUtil.VerticalSpacing;
            height += EditorGUI.GetPropertyHeight(overlayTextProp.FindPropertyRelative("fadeOut"), true) + CutsceneEventDrawerUiUtil.VerticalSpacing;
            height += EditorGUI.GetPropertyHeight(overlayTextProp.FindPropertyRelative("easing"), true) + CutsceneEventDrawerUiUtil.VerticalSpacing;
            height += EditorGUI.GetPropertyHeight(overlayTextProp.FindPropertyRelative("useUnscaledTime"), true);
            return height;
        }
    }
}
