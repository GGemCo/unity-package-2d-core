using GGemCo2DCore;
using UnityEditor;
using UnityEngine;

namespace GGemCo2DCoreEditor
{
    /// <summary>
    /// 연출 클립 inspector 
    /// </summary>
    [CustomPropertyDrawer(typeof(CutsceneEvent))]
    public class CutsceneEventDrawer : PropertyDrawer
    {
        private const float VerticalSpacing = 2f;

        public override void OnGUI(Rect pos, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(pos, label, property);

            var typeProp = property.FindPropertyRelative("type");
            CutsceneEventType cutsceneEventType = (CutsceneEventType)typeProp.enumValueIndex;

            EnsureEventData(property, cutsceneEventType);

            var cameraMoveProp = property.FindPropertyRelative("cameraMove");
            var cameraZoomProp = property.FindPropertyRelative("cameraZoom");
            var cameraShakeProp = property.FindPropertyRelative("cameraShake");
            var cameraChangeTargetProp = property.FindPropertyRelative("cameraChangeTarget");

            var characterMoveProp = property.FindPropertyRelative("characterMove");
            var characterAnimationProp = property.FindPropertyRelative("characterAnimation");

            var dialogueBalloonProp = property.FindPropertyRelative("dialogueBalloon");
            var screenFadeProp = property.FindPropertyRelative("screenFade");
            var overlayTextProp = property.FindPropertyRelative("overlayText");
            var characterWhiteOverlayProp = property.FindPropertyRelative("characterWhiteOverlay");
            var uiPanelProp = property.FindPropertyRelative("uiPanel");

            var line = pos;
            line.height = EditorGUIUtility.singleLineHeight;

            EditorGUI.BeginChangeCheck();
            EditorGUI.PropertyField(line, typeProp);
            if (EditorGUI.EndChangeCheck())
            {
                cutsceneEventType = (CutsceneEventType)typeProp.enumValueIndex;
                EnsureEventData(property, cutsceneEventType);
            }

            line.y += line.height + VerticalSpacing;

            switch (cutsceneEventType)
            {
                case CutsceneEventType.CameraMove:
                    EditorGUI.PropertyField(line, cameraMoveProp, true);
                    break;
                case CutsceneEventType.CameraZoom:
                    EditorGUI.PropertyField(line, cameraZoomProp, true);
                    break;
                case CutsceneEventType.CameraShake:
                    EditorGUI.PropertyField(line, cameraShakeProp, true);
                    break;
                case CutsceneEventType.CameraChangeTarget:
                    EditorGUI.PropertyField(line, cameraChangeTargetProp, true);
                    break;
                case CutsceneEventType.CharacterMove:
                    EditorGUI.PropertyField(line, characterMoveProp, true);
                    break;
                case CutsceneEventType.CharacterAnimation:
                    EditorGUI.PropertyField(line, characterAnimationProp, true);
                    break;
                case CutsceneEventType.DialogueBalloon:
                    EditorGUI.PropertyField(line, dialogueBalloonProp, true);
                    break;
                case CutsceneEventType.ScreenFade:
                    EditorGUI.PropertyField(line, screenFadeProp, true);
                    break;
                case CutsceneEventType.OverlayText:
                    DrawOverlayTextProperty(line, overlayTextProp);
                    break;
                case CutsceneEventType.CharacterWhiteOverlay:
                    EditorGUI.PropertyField(line, characterWhiteOverlayProp, true);
                    break;
                case CutsceneEventType.UiPanel:
                    EditorGUI.PropertyField(line, uiPanelProp, true);
                    break;
            }

            EditorGUI.EndProperty();
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            var typeProp = property.FindPropertyRelative("type");
            CutsceneEventType cutsceneEventType = (CutsceneEventType)typeProp.enumValueIndex;

            EnsureEventData(property, cutsceneEventType);

            float baseHeight = EditorGUIUtility.singleLineHeight * 2f + 6f;
            switch (cutsceneEventType)
            {
                case CutsceneEventType.CameraMove:
                    return baseHeight + EditorGUI.GetPropertyHeight(property.FindPropertyRelative("cameraMove"), true);
                case CutsceneEventType.CameraZoom:
                    return baseHeight + EditorGUI.GetPropertyHeight(property.FindPropertyRelative("cameraZoom"), true);
                case CutsceneEventType.CameraShake:
                    return baseHeight + EditorGUI.GetPropertyHeight(property.FindPropertyRelative("cameraShake"), true);
                case CutsceneEventType.CameraChangeTarget:
                    return baseHeight + EditorGUI.GetPropertyHeight(property.FindPropertyRelative("cameraChangeTarget"), true);
                case CutsceneEventType.CharacterMove:
                    return baseHeight + EditorGUI.GetPropertyHeight(property.FindPropertyRelative("characterMove"), true);
                case CutsceneEventType.CharacterAnimation:
                    return baseHeight + EditorGUI.GetPropertyHeight(property.FindPropertyRelative("characterAnimation"), true);
                case CutsceneEventType.DialogueBalloon:
                    return baseHeight + EditorGUI.GetPropertyHeight(property.FindPropertyRelative("dialogueBalloon"), true);
                case CutsceneEventType.ScreenFade:
                    return baseHeight + EditorGUI.GetPropertyHeight(property.FindPropertyRelative("screenFade"), true);
                case CutsceneEventType.OverlayText:
                    return baseHeight + GetOverlayTextPropertyHeight(property.FindPropertyRelative("overlayText"));
                case CutsceneEventType.CharacterWhiteOverlay:
                    return baseHeight + EditorGUI.GetPropertyHeight(property.FindPropertyRelative("characterWhiteOverlay"), true);
                case CutsceneEventType.UiPanel:
                    return baseHeight + EditorGUI.GetPropertyHeight(property.FindPropertyRelative("uiPanel"), true);
                default:
                    return baseHeight;
            }
        }

        private static void DrawOverlayTextProperty(Rect position, SerializedProperty overlayTextProp)
        {
            if (overlayTextProp == null)
            {
                return;
            }

            Rect current = position;
            current.height = EditorGUIUtility.singleLineHeight;

            EditorGUI.LabelField(current, overlayTextProp.displayName, EditorStyles.boldLabel);
            current.y += current.height + VerticalSpacing;

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

            DrawPropertyLine(ref current, sourceModeProp);

            var sourceMode = (OverlayTextSourceMode)sourceModeProp.enumValueIndex;
            if (sourceMode == OverlayTextSourceMode.RuntimeOverride)
            {
                DrawPropertyLine(ref current, runtimeTextKeyProp);
                DrawPropertyLine(ref current, textProp, new GUIContent("Fallback Text"));
            }
            else
            {
                DrawPropertyLine(ref current, textProp);
            }

            DrawPropertyLine(ref current, anchoredPositionProp);
            DrawPropertyLine(ref current, sizeDeltaProp);
            DrawPropertyLine(ref current, fontSizeProp);
            DrawPropertyLine(ref current, textColorProp);
            DrawPropertyLine(ref current, maxAlphaProp);
            DrawPropertyLine(ref current, fadeInProp);
            DrawPropertyLine(ref current, fadeOutProp);
            DrawPropertyLine(ref current, easingProp);
            DrawPropertyLine(ref current, useUnscaledTimeProp);

            EditorGUI.indentLevel = originalIndent;
        }

        private static float GetOverlayTextPropertyHeight(SerializedProperty overlayTextProp)
        {
            if (overlayTextProp == null)
            {
                return EditorGUIUtility.singleLineHeight;
            }

            float height = EditorGUIUtility.singleLineHeight + VerticalSpacing;
            height += EditorGUI.GetPropertyHeight(overlayTextProp.FindPropertyRelative("sourceMode"), true) + VerticalSpacing;

            var sourceModeProp = overlayTextProp.FindPropertyRelative("sourceMode");
            var sourceMode = (OverlayTextSourceMode)sourceModeProp.enumValueIndex;
            if (sourceMode == OverlayTextSourceMode.RuntimeOverride)
            {
                height += EditorGUI.GetPropertyHeight(overlayTextProp.FindPropertyRelative("runtimeTextKey"), true) + VerticalSpacing;
                height += EditorGUI.GetPropertyHeight(overlayTextProp.FindPropertyRelative("text"), true) + VerticalSpacing;
            }
            else
            {
                height += EditorGUI.GetPropertyHeight(overlayTextProp.FindPropertyRelative("text"), true) + VerticalSpacing;
            }

            height += EditorGUI.GetPropertyHeight(overlayTextProp.FindPropertyRelative("anchoredPosition"), true) + VerticalSpacing;
            height += EditorGUI.GetPropertyHeight(overlayTextProp.FindPropertyRelative("sizeDelta"), true) + VerticalSpacing;
            height += EditorGUI.GetPropertyHeight(overlayTextProp.FindPropertyRelative("fontSize"), true) + VerticalSpacing;
            height += EditorGUI.GetPropertyHeight(overlayTextProp.FindPropertyRelative("textColor"), true) + VerticalSpacing;
            height += EditorGUI.GetPropertyHeight(overlayTextProp.FindPropertyRelative("maxAlpha"), true) + VerticalSpacing;
            height += EditorGUI.GetPropertyHeight(overlayTextProp.FindPropertyRelative("fadeIn"), true) + VerticalSpacing;
            height += EditorGUI.GetPropertyHeight(overlayTextProp.FindPropertyRelative("fadeOut"), true) + VerticalSpacing;
            height += EditorGUI.GetPropertyHeight(overlayTextProp.FindPropertyRelative("easing"), true) + VerticalSpacing;
            height += EditorGUI.GetPropertyHeight(overlayTextProp.FindPropertyRelative("useUnscaledTime"), true);

            return height;
        }

        private static void DrawPropertyLine(ref Rect current, SerializedProperty property, GUIContent label = null)
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

        private static void EnsureEventData(SerializedProperty property, CutsceneEventType eventType)
        {
            EnsureManagedReference(property.FindPropertyRelative("cameraMove"), eventType == CutsceneEventType.CameraMove, typeof(CameraMoveData));
            EnsureManagedReference(property.FindPropertyRelative("cameraZoom"), eventType == CutsceneEventType.CameraZoom, typeof(CameraZoomData));
            EnsureManagedReference(property.FindPropertyRelative("cameraShake"), eventType == CutsceneEventType.CameraShake, typeof(CameraShakeData));
            EnsureManagedReference(property.FindPropertyRelative("cameraChangeTarget"), eventType == CutsceneEventType.CameraChangeTarget, typeof(CameraChangeTargetData));
            EnsureManagedReference(property.FindPropertyRelative("characterMove"), eventType == CutsceneEventType.CharacterMove, typeof(CharacterMoveData));
            EnsureManagedReference(property.FindPropertyRelative("characterAnimation"), eventType == CutsceneEventType.CharacterAnimation, typeof(CharacterAnimationData));
            EnsureManagedReference(property.FindPropertyRelative("dialogueBalloon"), eventType == CutsceneEventType.DialogueBalloon, typeof(DialogueBalloonData));
            EnsureManagedReference(property.FindPropertyRelative("screenFade"), eventType == CutsceneEventType.ScreenFade, typeof(ScreenFadeData));
            EnsureManagedReference(property.FindPropertyRelative("overlayText"), eventType == CutsceneEventType.OverlayText, typeof(OverlayTextData));
            EnsureManagedReference(property.FindPropertyRelative("characterWhiteOverlay"), eventType == CutsceneEventType.CharacterWhiteOverlay, typeof(CharacterWhiteOverlayData));
            EnsureManagedReference(property.FindPropertyRelative("uiPanel"), eventType == CutsceneEventType.UiPanel, typeof(UiPanelData));

            property.serializedObject.ApplyModifiedProperties();
        }

        private static void EnsureManagedReference(SerializedProperty dataProperty, bool shouldCreate, System.Type dataType)
        {
            if (dataProperty == null || dataProperty.propertyType != SerializedPropertyType.Generic)
            {
                return;
            }

            if (!shouldCreate || dataProperty.hasVisibleChildren)
            {
                return;
            }

            object boxedValue = GetBoxedValue(dataProperty);
            if (boxedValue != null)
            {
                return;
            }

            SetBoxedValue(dataProperty, System.Activator.CreateInstance(dataType));
        }

        private static object GetBoxedValue(SerializedProperty property)
        {
#if UNITY_2023_1_OR_NEWER
            return property.boxedValue;
#else
            return null;
#endif
        }

        private static void SetBoxedValue(SerializedProperty property, object value)
        {
#if UNITY_2023_1_OR_NEWER
            property.boxedValue = value;
#else
            // Unity 2022+ 환경을 주 대상으로 하지만, boxedValue 미지원 환경에서는
            // CutsceneEvent 기본 생성자/Export 보정으로 null 직렬화 누락을 최소화합니다.
#endif
        }
    }
}
