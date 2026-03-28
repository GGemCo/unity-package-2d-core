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
            var uiWindowVisibilityProp = property.FindPropertyRelative("uiWindowVisibility");
            var timeScaleProp = property.FindPropertyRelative("timeScale");
            
            var characterAnimationTimeScaleProp = property.FindPropertyRelative("characterAnimationTimeScale");

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
                case CutsceneEventType.CharacterAnimationTimeScale:
                    DrawCharacterAnimationTimeScaleProperty(line, characterAnimationTimeScaleProp);
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
                case CutsceneEventType.UiWindowVisibility:
                    EditorGUI.PropertyField(line, uiWindowVisibilityProp, true);
                    break;
                case CutsceneEventType.TimeScale:
                    DrawTimeScaleProperty(line, timeScaleProp);
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
                case CutsceneEventType.CharacterAnimationTimeScale:
                    return baseHeight + GetCharacterAnimationTimeScalePropertyHeight(property.FindPropertyRelative("characterAnimationTimeScale"));
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
                case CutsceneEventType.UiWindowVisibility:
                    return baseHeight + EditorGUI.GetPropertyHeight(property.FindPropertyRelative("uiWindowVisibility"), true);
                case CutsceneEventType.TimeScale:
                    return baseHeight + GetTimeScalePropertyHeight(property.FindPropertyRelative("timeScale"));
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


        private static void DrawCharacterAnimationTimeScaleProperty(Rect position, SerializedProperty timeScaleProp)
        {
            if (timeScaleProp == null)
            {
                return;
            }

            Rect current = position;
            current.height = EditorGUIUtility.singleLineHeight;

            EditorGUI.LabelField(current, timeScaleProp.displayName, EditorStyles.boldLabel);
            current.y += current.height + VerticalSpacing;

            int originalIndent = EditorGUI.indentLevel;
            EditorGUI.indentLevel++;

            var characterTypeProp = timeScaleProp.FindPropertyRelative("characterType");
            var characterUidProp = timeScaleProp.FindPropertyRelative("characterUid");
            var actionModeProp = timeScaleProp.FindPropertyRelative("actionMode");
            var fromScaleProp = timeScaleProp.FindPropertyRelative("fromScale");
            var toScaleProp = timeScaleProp.FindPropertyRelative("toScale");
            var restoreScaleProp = timeScaleProp.FindPropertyRelative("restoreScale");
            var easingProp = timeScaleProp.FindPropertyRelative("easing");
            var useUnscaledTimeProp = timeScaleProp.FindPropertyRelative("useUnscaledTime");
            var captureOriginalOnTriggerProp = timeScaleProp.FindPropertyRelative("captureOriginalOnTrigger");
            var useCapturedScaleForRestoreProp = timeScaleProp.FindPropertyRelative("useCapturedScaleForRestore");
            var restoreOnCutsceneEndProp = timeScaleProp.FindPropertyRelative("restoreOnCutsceneEnd");

            DrawPropertyLine(ref current, characterTypeProp);
            if ((CharacterConstants.Type)characterTypeProp.enumValueIndex != CharacterConstants.Type.Player)
            {
                DrawPropertyLine(ref current, characterUidProp);
            }

            DrawPropertyLine(ref current, actionModeProp);
            var actionMode = (CharacterAnimationTimeScaleActionMode)actionModeProp.enumValueIndex;
            switch (actionMode)
            {
                case CharacterAnimationTimeScaleActionMode.BlendAndHold:
                    DrawPropertyLine(ref current, captureOriginalOnTriggerProp);
                    if (!captureOriginalOnTriggerProp.boolValue)
                    {
                        DrawPropertyLine(ref current, fromScaleProp);
                    }
                    DrawPropertyLine(ref current, toScaleProp);
                    DrawPropertyLine(ref current, easingProp);
                    DrawPropertyLine(ref current, useUnscaledTimeProp);
                    DrawCharacterAnimationTimeScaleWarnings(ref current, actionMode, toScaleProp, useUnscaledTimeProp, restoreOnCutsceneEndProp);
                    DrawPropertyLine(ref current, restoreOnCutsceneEndProp);
                    break;

                case CharacterAnimationTimeScaleActionMode.SetAndHold:
                    DrawPropertyLine(ref current, captureOriginalOnTriggerProp);
                    DrawPropertyLine(ref current, toScaleProp);
                    DrawCharacterAnimationTimeScaleWarnings(ref current, actionMode, toScaleProp, useUnscaledTimeProp, restoreOnCutsceneEndProp);
                    DrawPropertyLine(ref current, restoreOnCutsceneEndProp);
                    break;

                case CharacterAnimationTimeScaleActionMode.Restore:
                    DrawPropertyLine(ref current, useCapturedScaleForRestoreProp);
                    if (!useCapturedScaleForRestoreProp.boolValue)
                    {
                        DrawPropertyLine(ref current, restoreScaleProp);
                    }
                    DrawPropertyLine(ref current, easingProp);
                    DrawPropertyLine(ref current, useUnscaledTimeProp);
                    DrawPropertyLine(ref current, restoreOnCutsceneEndProp);
                    break;
            }

            EditorGUI.indentLevel = originalIndent;
        }

        private static float GetCharacterAnimationTimeScalePropertyHeight(SerializedProperty timeScaleProp)
        {
            if (timeScaleProp == null)
            {
                return EditorGUIUtility.singleLineHeight;
            }

            float height = EditorGUIUtility.singleLineHeight + VerticalSpacing;
            var characterTypeProp = timeScaleProp.FindPropertyRelative("characterType");
            var actionModeProp = timeScaleProp.FindPropertyRelative("actionMode");
            var captureOriginalOnTriggerProp = timeScaleProp.FindPropertyRelative("captureOriginalOnTrigger");
            var useCapturedScaleForRestoreProp = timeScaleProp.FindPropertyRelative("useCapturedScaleForRestore");
            var restoreOnCutsceneEndProp = timeScaleProp.FindPropertyRelative("restoreOnCutsceneEnd");
            var toScaleProp = timeScaleProp.FindPropertyRelative("toScale");
            var useUnscaledTimeProp = timeScaleProp.FindPropertyRelative("useUnscaledTime");

            height += EditorGUI.GetPropertyHeight(characterTypeProp, true) + VerticalSpacing;
            if ((CharacterConstants.Type)characterTypeProp.enumValueIndex != CharacterConstants.Type.Player)
            {
                height += EditorGUI.GetPropertyHeight(timeScaleProp.FindPropertyRelative("characterUid"), true) + VerticalSpacing;
            }

            height += EditorGUI.GetPropertyHeight(actionModeProp, true) + VerticalSpacing;

            var actionMode = (CharacterAnimationTimeScaleActionMode)actionModeProp.enumValueIndex;
            switch (actionMode)
            {
                case CharacterAnimationTimeScaleActionMode.BlendAndHold:
                    height += EditorGUI.GetPropertyHeight(captureOriginalOnTriggerProp, true) + VerticalSpacing;
                    if (!captureOriginalOnTriggerProp.boolValue)
                    {
                        height += EditorGUI.GetPropertyHeight(timeScaleProp.FindPropertyRelative("fromScale"), true) + VerticalSpacing;
                    }
                    height += EditorGUI.GetPropertyHeight(toScaleProp, true) + VerticalSpacing;
                    height += EditorGUI.GetPropertyHeight(timeScaleProp.FindPropertyRelative("easing"), true) + VerticalSpacing;
                    height += EditorGUI.GetPropertyHeight(useUnscaledTimeProp, true) + VerticalSpacing;
                    height += GetCharacterAnimationTimeScaleWarningHeight(actionMode, toScaleProp, useUnscaledTimeProp, restoreOnCutsceneEndProp);
                    height += EditorGUI.GetPropertyHeight(restoreOnCutsceneEndProp, true) + VerticalSpacing;
                    break;

                case CharacterAnimationTimeScaleActionMode.SetAndHold:
                    height += EditorGUI.GetPropertyHeight(captureOriginalOnTriggerProp, true) + VerticalSpacing;
                    height += EditorGUI.GetPropertyHeight(toScaleProp, true) + VerticalSpacing;
                    height += GetCharacterAnimationTimeScaleWarningHeight(actionMode, toScaleProp, useUnscaledTimeProp, restoreOnCutsceneEndProp);
                    height += EditorGUI.GetPropertyHeight(restoreOnCutsceneEndProp, true) + VerticalSpacing;
                    break;

                case CharacterAnimationTimeScaleActionMode.Restore:
                    height += EditorGUI.GetPropertyHeight(useCapturedScaleForRestoreProp, true) + VerticalSpacing;
                    if (!useCapturedScaleForRestoreProp.boolValue)
                    {
                        height += EditorGUI.GetPropertyHeight(timeScaleProp.FindPropertyRelative("restoreScale"), true) + VerticalSpacing;
                    }
                    height += EditorGUI.GetPropertyHeight(timeScaleProp.FindPropertyRelative("easing"), true) + VerticalSpacing;
                    height += EditorGUI.GetPropertyHeight(useUnscaledTimeProp, true) + VerticalSpacing;
                    height += EditorGUI.GetPropertyHeight(restoreOnCutsceneEndProp, true) + VerticalSpacing;
                    break;
            }

            return height;
        }

        private static void DrawCharacterAnimationTimeScaleWarnings(ref Rect current, CharacterAnimationTimeScaleActionMode actionMode, SerializedProperty toScaleProp,
            SerializedProperty useUnscaledTimeProp, SerializedProperty restoreOnCutsceneEndProp)
        {
            if (toScaleProp == null || !Mathf.Approximately(toScaleProp.floatValue, 0f))
            {
                return;
            }

            if (actionMode == CharacterAnimationTimeScaleActionMode.BlendAndHold && !useUnscaledTimeProp.boolValue)
            {
                DrawHelpBox(ref current, "animation time scale을 0으로 만들면 애니메이션은 멈춰 보이지만, 이 이벤트 duration은 Time.deltaTime 기준일 경우 진행이 멈출 수 있습니다. Use Unscaled Time 활성화를 권장합니다.", MessageType.Warning);
            }

            if (actionMode == CharacterAnimationTimeScaleActionMode.SetAndHold && !restoreOnCutsceneEndProp.boolValue)
            {
                DrawHelpBox(ref current, "SetAndHold + toScale 0 + Restore On Cutscene End 비활성 상태입니다. 별도 Restore 이벤트가 없으면 컷씬 종료 후에도 애니메이션이 멈춘 상태로 남을 수 있습니다.", MessageType.Info);
            }
        }

        private static float GetCharacterAnimationTimeScaleWarningHeight(CharacterAnimationTimeScaleActionMode actionMode, SerializedProperty toScaleProp,
            SerializedProperty useUnscaledTimeProp, SerializedProperty restoreOnCutsceneEndProp)
        {
            if (toScaleProp == null || !Mathf.Approximately(toScaleProp.floatValue, 0f))
            {
                return 0f;
            }

            float height = 0f;
            if (actionMode == CharacterAnimationTimeScaleActionMode.BlendAndHold && !useUnscaledTimeProp.boolValue)
            {
                height += GetHelpBoxHeight("animation time scale을 0으로 만들면 애니메이션은 멈춰 보이지만, 이 이벤트 duration은 Time.deltaTime 기준일 경우 진행이 멈출 수 있습니다. Use Unscaled Time 활성화를 권장합니다.");
            }

            if (actionMode == CharacterAnimationTimeScaleActionMode.SetAndHold && !restoreOnCutsceneEndProp.boolValue)
            {
                height += GetHelpBoxHeight("SetAndHold + toScale 0 + Restore On Cutscene End 비활성 상태입니다. 별도 Restore 이벤트가 없으면 컷씬 종료 후에도 애니메이션이 멈춘 상태로 남을 수 있습니다.");
            }

            return height;
        }

        private static void DrawTimeScaleProperty(Rect position, SerializedProperty timeScaleProp)
        {
            if (timeScaleProp == null)
            {
                return;
            }

            Rect current = position;
            current.height = EditorGUIUtility.singleLineHeight;

            EditorGUI.LabelField(current, timeScaleProp.displayName, EditorStyles.boldLabel);
            current.y += current.height + VerticalSpacing;

            int originalIndent = EditorGUI.indentLevel;
            EditorGUI.indentLevel++;

            var actionModeProp = timeScaleProp.FindPropertyRelative("actionMode");
            var fromScaleProp = timeScaleProp.FindPropertyRelative("fromScale");
            var toScaleProp = timeScaleProp.FindPropertyRelative("toScale");
            var restoreScaleProp = timeScaleProp.FindPropertyRelative("restoreScale");
            var easingProp = timeScaleProp.FindPropertyRelative("easing");
            var useUnscaledTimeProp = timeScaleProp.FindPropertyRelative("useUnscaledTime");
            var timelineModeProp = timeScaleProp.FindPropertyRelative("timelineMode");
            var useCapturedScaleForRestoreProp = timeScaleProp.FindPropertyRelative("useCapturedScaleForRestore");
            var restoreOnCutsceneEndProp = timeScaleProp.FindPropertyRelative("restoreOnCutsceneEnd");
            var affectFixedDeltaTimeProp = timeScaleProp.FindPropertyRelative("affectFixedDeltaTime");
            var minimumScaleForFixedDeltaTimeProp = timeScaleProp.FindPropertyRelative("minimumScaleForFixedDeltaTime");

            DrawPropertyLine(ref current, actionModeProp);

            var actionMode = (TimeScaleActionMode)actionModeProp.enumValueIndex;
            switch (actionMode)
            {
                case TimeScaleActionMode.BlendAndHold:
                    DrawPropertyLine(ref current, fromScaleProp);
                    DrawPropertyLine(ref current, toScaleProp);
                    DrawPropertyLine(ref current, easingProp);
                    DrawPropertyLine(ref current, useUnscaledTimeProp);
                    DrawPropertyLine(ref current, timelineModeProp);
                    DrawTimeScaleWarnings(ref current, actionMode, toScaleProp, useUnscaledTimeProp, timelineModeProp, restoreOnCutsceneEndProp);
                    DrawPropertyLine(ref current, restoreOnCutsceneEndProp);
                    DrawPropertyLine(ref current, affectFixedDeltaTimeProp);
                    if (affectFixedDeltaTimeProp.boolValue)
                    {
                        DrawPropertyLine(ref current, minimumScaleForFixedDeltaTimeProp);
                    }
                    break;

                case TimeScaleActionMode.SetAndHold:
                    DrawPropertyLine(ref current, toScaleProp);
                    DrawPropertyLine(ref current, timelineModeProp);
                    DrawTimeScaleWarnings(ref current, actionMode, toScaleProp, useUnscaledTimeProp, timelineModeProp, restoreOnCutsceneEndProp);
                    DrawPropertyLine(ref current, restoreOnCutsceneEndProp);
                    DrawPropertyLine(ref current, affectFixedDeltaTimeProp);
                    if (affectFixedDeltaTimeProp.boolValue)
                    {
                        DrawPropertyLine(ref current, minimumScaleForFixedDeltaTimeProp);
                    }
                    break;

                case TimeScaleActionMode.Restore:
                    DrawPropertyLine(ref current, useCapturedScaleForRestoreProp);
                    if (!useCapturedScaleForRestoreProp.boolValue)
                    {
                        DrawPropertyLine(ref current, restoreScaleProp);
                    }
                    DrawPropertyLine(ref current, easingProp);
                    DrawPropertyLine(ref current, useUnscaledTimeProp);
                    DrawPropertyLine(ref current, affectFixedDeltaTimeProp);
                    if (affectFixedDeltaTimeProp.boolValue)
                    {
                        DrawPropertyLine(ref current, minimumScaleForFixedDeltaTimeProp);
                    }
                    break;
            }

            EditorGUI.indentLevel = originalIndent;
        }

        private static float GetTimeScalePropertyHeight(SerializedProperty timeScaleProp)
        {
            if (timeScaleProp == null)
            {
                return EditorGUIUtility.singleLineHeight;
            }

            float height = EditorGUIUtility.singleLineHeight + VerticalSpacing;
            var actionModeProp = timeScaleProp.FindPropertyRelative("actionMode");
            height += EditorGUI.GetPropertyHeight(actionModeProp, true) + VerticalSpacing;

            var affectFixedDeltaTimeProp = timeScaleProp.FindPropertyRelative("affectFixedDeltaTime");
            var useCapturedScaleForRestoreProp = timeScaleProp.FindPropertyRelative("useCapturedScaleForRestore");
            var actionMode = (TimeScaleActionMode)actionModeProp.enumValueIndex;

            switch (actionMode)
            {
                case TimeScaleActionMode.BlendAndHold:
                    height += EditorGUI.GetPropertyHeight(timeScaleProp.FindPropertyRelative("fromScale"), true) + VerticalSpacing;
                    height += EditorGUI.GetPropertyHeight(timeScaleProp.FindPropertyRelative("toScale"), true) + VerticalSpacing;
                    height += EditorGUI.GetPropertyHeight(timeScaleProp.FindPropertyRelative("easing"), true) + VerticalSpacing;
                    height += EditorGUI.GetPropertyHeight(timeScaleProp.FindPropertyRelative("useUnscaledTime"), true) + VerticalSpacing;
                    height += EditorGUI.GetPropertyHeight(timeScaleProp.FindPropertyRelative("timelineMode"), true) + VerticalSpacing;
                    height += GetTimeScaleWarningHeight(actionMode, timeScaleProp.FindPropertyRelative("toScale"), timeScaleProp.FindPropertyRelative("useUnscaledTime"), timeScaleProp.FindPropertyRelative("timelineMode"), timeScaleProp.FindPropertyRelative("restoreOnCutsceneEnd"));
                    height += EditorGUI.GetPropertyHeight(timeScaleProp.FindPropertyRelative("restoreOnCutsceneEnd"), true) + VerticalSpacing;
                    height += EditorGUI.GetPropertyHeight(affectFixedDeltaTimeProp, true) + VerticalSpacing;
                    if (affectFixedDeltaTimeProp.boolValue)
                    {
                        height += EditorGUI.GetPropertyHeight(timeScaleProp.FindPropertyRelative("minimumScaleForFixedDeltaTime"), true) + VerticalSpacing;
                    }
                    break;

                case TimeScaleActionMode.SetAndHold:
                    height += EditorGUI.GetPropertyHeight(timeScaleProp.FindPropertyRelative("toScale"), true) + VerticalSpacing;
                    height += EditorGUI.GetPropertyHeight(timeScaleProp.FindPropertyRelative("timelineMode"), true) + VerticalSpacing;
                    height += GetTimeScaleWarningHeight(actionMode, timeScaleProp.FindPropertyRelative("toScale"), timeScaleProp.FindPropertyRelative("useUnscaledTime"), timeScaleProp.FindPropertyRelative("timelineMode"), timeScaleProp.FindPropertyRelative("restoreOnCutsceneEnd"));
                    height += EditorGUI.GetPropertyHeight(timeScaleProp.FindPropertyRelative("restoreOnCutsceneEnd"), true) + VerticalSpacing;
                    height += EditorGUI.GetPropertyHeight(affectFixedDeltaTimeProp, true) + VerticalSpacing;
                    if (affectFixedDeltaTimeProp.boolValue)
                    {
                        height += EditorGUI.GetPropertyHeight(timeScaleProp.FindPropertyRelative("minimumScaleForFixedDeltaTime"), true) + VerticalSpacing;
                    }
                    break;

                case TimeScaleActionMode.Restore:
                    height += EditorGUI.GetPropertyHeight(useCapturedScaleForRestoreProp, true) + VerticalSpacing;
                    if (!useCapturedScaleForRestoreProp.boolValue)
                    {
                        height += EditorGUI.GetPropertyHeight(timeScaleProp.FindPropertyRelative("restoreScale"), true) + VerticalSpacing;
                    }
                    height += EditorGUI.GetPropertyHeight(timeScaleProp.FindPropertyRelative("easing"), true) + VerticalSpacing;
                    height += EditorGUI.GetPropertyHeight(timeScaleProp.FindPropertyRelative("useUnscaledTime"), true) + VerticalSpacing;
                    height += EditorGUI.GetPropertyHeight(affectFixedDeltaTimeProp, true) + VerticalSpacing;
                    if (affectFixedDeltaTimeProp.boolValue)
                    {
                        height += EditorGUI.GetPropertyHeight(timeScaleProp.FindPropertyRelative("minimumScaleForFixedDeltaTime"), true) + VerticalSpacing;
                    }
                    break;
            }

            return height;
        }

        private static void DrawTimeScaleWarnings(ref Rect current, TimeScaleActionMode actionMode, SerializedProperty toScaleProp,
            SerializedProperty useUnscaledTimeProp, SerializedProperty timelineModeProp, SerializedProperty restoreOnCutsceneEndProp)
        {
            if (toScaleProp == null || !Mathf.Approximately(toScaleProp.floatValue, 0f))
            {
                return;
            }

            if (actionMode == TimeScaleActionMode.BlendAndHold && !useUnscaledTimeProp.boolValue)
            {
                DrawHelpBox(ref current, "timeScale이 0이면 Time.deltaTime도 0이 되므로, 이 이벤트 duration 진행이 멈출 수 있습니다. Use Unscaled Time 활성화를 권장합니다.", MessageType.Warning);
            }

            if ((CutsceneTimeScaleTimelineMode)timelineModeProp.enumValueIndex != CutsceneTimeScaleTimelineMode.KeepRunningWhenTimeScaleIsZero)
            {
                DrawHelpBox(ref current, "timeScale이 0일 때 컷신 타임라인도 같이 멈출 수 있습니다. 후속 이벤트를 계속 진행하려면 Timeline Mode를 KeepRunningWhenTimeScaleIsZero로 설정하세요.", MessageType.Warning);
            }

            if (actionMode == TimeScaleActionMode.SetAndHold && !restoreOnCutsceneEndProp.boolValue)
            {
                DrawHelpBox(ref current, "SetAndHold + toScale 0 + Restore On Cutscene End 비활성 상태입니다. 별도 Restore 이벤트가 없으면 컷신 종료 후에도 게임이 멈춘 상태로 남을 수 있습니다.", MessageType.Info);
            }
        }

        private static float GetTimeScaleWarningHeight(TimeScaleActionMode actionMode, SerializedProperty toScaleProp,
            SerializedProperty useUnscaledTimeProp, SerializedProperty timelineModeProp, SerializedProperty restoreOnCutsceneEndProp)
        {
            if (toScaleProp == null || !Mathf.Approximately(toScaleProp.floatValue, 0f))
            {
                return 0f;
            }

            float height = 0f;
            if (actionMode == TimeScaleActionMode.BlendAndHold && !useUnscaledTimeProp.boolValue)
            {
                height += GetHelpBoxHeight("timeScale이 0이면 Time.deltaTime도 0이 되므로, 이 이벤트 duration 진행이 멈출 수 있습니다. Use Unscaled Time 활성화를 권장합니다.");
            }

            if ((CutsceneTimeScaleTimelineMode)timelineModeProp.enumValueIndex != CutsceneTimeScaleTimelineMode.KeepRunningWhenTimeScaleIsZero)
            {
                height += GetHelpBoxHeight("timeScale이 0일 때 컷신 타임라인도 같이 멈출 수 있습니다. 후속 이벤트를 계속 진행하려면 Timeline Mode를 KeepRunningWhenTimeScaleIsZero로 설정하세요.");
            }

            if (actionMode == TimeScaleActionMode.SetAndHold && !restoreOnCutsceneEndProp.boolValue)
            {
                height += GetHelpBoxHeight("SetAndHold + toScale 0 + Restore On Cutscene End 비활성 상태입니다. 별도 Restore 이벤트가 없으면 컷신 종료 후에도 게임이 멈춘 상태로 남을 수 있습니다.");
            }

            return height;
        }

        private static float GetHelpBoxHeight(string message)
        {
            return EditorStyles.helpBox.CalcHeight(new GUIContent(message), EditorGUIUtility.currentViewWidth - 80f) + VerticalSpacing;
        }

        private static void DrawHelpBox(ref Rect current, string message, MessageType messageType)
        {
            float height = EditorStyles.helpBox.CalcHeight(new GUIContent(message), current.width);
            current.height = height;
            EditorGUI.HelpBox(current, message, messageType);
            current.y += height + VerticalSpacing;
            current.height = EditorGUIUtility.singleLineHeight;
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
            EnsureManagedReference(property.FindPropertyRelative("characterAnimationTimeScale"), eventType == CutsceneEventType.CharacterAnimationTimeScale, typeof(CharacterAnimationTimeScaleData));
            EnsureManagedReference(property.FindPropertyRelative("dialogueBalloon"), eventType == CutsceneEventType.DialogueBalloon, typeof(DialogueBalloonData));
            EnsureManagedReference(property.FindPropertyRelative("screenFade"), eventType == CutsceneEventType.ScreenFade, typeof(ScreenFadeData));
            EnsureManagedReference(property.FindPropertyRelative("overlayText"), eventType == CutsceneEventType.OverlayText, typeof(OverlayTextData));
            EnsureManagedReference(property.FindPropertyRelative("characterWhiteOverlay"), eventType == CutsceneEventType.CharacterWhiteOverlay, typeof(CharacterWhiteOverlayData));
            EnsureManagedReference(property.FindPropertyRelative("uiPanel"), eventType == CutsceneEventType.UiPanel, typeof(UiPanelData));
            EnsureManagedReference(property.FindPropertyRelative("uiWindowVisibility"), eventType == CutsceneEventType.UiWindowVisibility, typeof(UiWindowVisibilityData));
            EnsureManagedReference(property.FindPropertyRelative("timeScale"), eventType == CutsceneEventType.TimeScale, typeof(TimeScaleData));

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
