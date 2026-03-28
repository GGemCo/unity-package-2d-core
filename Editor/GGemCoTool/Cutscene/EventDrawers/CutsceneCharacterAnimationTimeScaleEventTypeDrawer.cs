using GGemCo2DCore;
using UnityEditor;
using UnityEngine;

namespace GGemCo2DCoreEditor
{
    internal sealed class CutsceneCharacterAnimationTimeScaleEventTypeDrawer : ICutsceneEventTypeDrawer
    {
        public CutsceneEventType EventType => CutsceneEventType.CharacterAnimationTimeScale;

        public void Draw(Rect position, SerializedProperty eventProperty)
        {
            var timeScaleProp = eventProperty.FindPropertyRelative("characterAnimationTimeScale");
            if (timeScaleProp == null)
            {
                return;
            }

            Rect current = position;
            current.height = EditorGUIUtility.singleLineHeight;

            EditorGUI.LabelField(current, timeScaleProp.displayName, EditorStyles.boldLabel);
            current.y += current.height + CutsceneEventDrawerUiUtil.VerticalSpacing;

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

            CutsceneEventDrawerUiUtil.DrawPropertyLine(ref current, characterTypeProp);
            if ((CharacterConstants.Type)characterTypeProp.enumValueIndex != CharacterConstants.Type.Player)
            {
                CutsceneEventDrawerUiUtil.DrawPropertyLine(ref current, characterUidProp);
            }

            CutsceneEventDrawerUiUtil.DrawPropertyLine(ref current, actionModeProp);
            var actionMode = (CharacterAnimationTimeScaleActionMode)actionModeProp.enumValueIndex;
            switch (actionMode)
            {
                case CharacterAnimationTimeScaleActionMode.BlendAndHold:
                    CutsceneEventDrawerUiUtil.DrawPropertyLine(ref current, captureOriginalOnTriggerProp);
                    if (!captureOriginalOnTriggerProp.boolValue)
                    {
                        CutsceneEventDrawerUiUtil.DrawPropertyLine(ref current, fromScaleProp);
                    }
                    CutsceneEventDrawerUiUtil.DrawPropertyLine(ref current, toScaleProp);
                    CutsceneEventDrawerUiUtil.DrawPropertyLine(ref current, easingProp);
                    CutsceneEventDrawerUiUtil.DrawPropertyLine(ref current, useUnscaledTimeProp);
                    DrawWarnings(ref current, actionMode, toScaleProp, useUnscaledTimeProp, restoreOnCutsceneEndProp);
                    CutsceneEventDrawerUiUtil.DrawPropertyLine(ref current, restoreOnCutsceneEndProp);
                    break;

                case CharacterAnimationTimeScaleActionMode.SetAndHold:
                    CutsceneEventDrawerUiUtil.DrawPropertyLine(ref current, captureOriginalOnTriggerProp);
                    CutsceneEventDrawerUiUtil.DrawPropertyLine(ref current, toScaleProp);
                    DrawWarnings(ref current, actionMode, toScaleProp, useUnscaledTimeProp, restoreOnCutsceneEndProp);
                    CutsceneEventDrawerUiUtil.DrawPropertyLine(ref current, restoreOnCutsceneEndProp);
                    break;

                case CharacterAnimationTimeScaleActionMode.Restore:
                    CutsceneEventDrawerUiUtil.DrawPropertyLine(ref current, useCapturedScaleForRestoreProp);
                    if (!useCapturedScaleForRestoreProp.boolValue)
                    {
                        CutsceneEventDrawerUiUtil.DrawPropertyLine(ref current, restoreScaleProp);
                    }
                    CutsceneEventDrawerUiUtil.DrawPropertyLine(ref current, easingProp);
                    CutsceneEventDrawerUiUtil.DrawPropertyLine(ref current, useUnscaledTimeProp);
                    CutsceneEventDrawerUiUtil.DrawPropertyLine(ref current, restoreOnCutsceneEndProp);
                    break;
            }

            EditorGUI.indentLevel = originalIndent;
        }

        public float GetHeight(SerializedProperty eventProperty)
        {
            var timeScaleProp = eventProperty.FindPropertyRelative("characterAnimationTimeScale");
            if (timeScaleProp == null)
            {
                return EditorGUIUtility.singleLineHeight;
            }

            float height = CutsceneEventDrawerUiUtil.GetLabeledGroupBaseHeight();
            var characterTypeProp = timeScaleProp.FindPropertyRelative("characterType");
            var actionModeProp = timeScaleProp.FindPropertyRelative("actionMode");
            var captureOriginalOnTriggerProp = timeScaleProp.FindPropertyRelative("captureOriginalOnTrigger");
            var useCapturedScaleForRestoreProp = timeScaleProp.FindPropertyRelative("useCapturedScaleForRestore");
            var restoreOnCutsceneEndProp = timeScaleProp.FindPropertyRelative("restoreOnCutsceneEnd");
            var toScaleProp = timeScaleProp.FindPropertyRelative("toScale");
            var useUnscaledTimeProp = timeScaleProp.FindPropertyRelative("useUnscaledTime");

            height += EditorGUI.GetPropertyHeight(characterTypeProp, true) + CutsceneEventDrawerUiUtil.VerticalSpacing;
            if ((CharacterConstants.Type)characterTypeProp.enumValueIndex != CharacterConstants.Type.Player)
            {
                height += EditorGUI.GetPropertyHeight(timeScaleProp.FindPropertyRelative("characterUid"), true) + CutsceneEventDrawerUiUtil.VerticalSpacing;
            }

            height += EditorGUI.GetPropertyHeight(actionModeProp, true) + CutsceneEventDrawerUiUtil.VerticalSpacing;

            var actionMode = (CharacterAnimationTimeScaleActionMode)actionModeProp.enumValueIndex;
            switch (actionMode)
            {
                case CharacterAnimationTimeScaleActionMode.BlendAndHold:
                    height += EditorGUI.GetPropertyHeight(captureOriginalOnTriggerProp, true) + CutsceneEventDrawerUiUtil.VerticalSpacing;
                    if (!captureOriginalOnTriggerProp.boolValue)
                    {
                        height += EditorGUI.GetPropertyHeight(timeScaleProp.FindPropertyRelative("fromScale"), true) + CutsceneEventDrawerUiUtil.VerticalSpacing;
                    }
                    height += EditorGUI.GetPropertyHeight(toScaleProp, true) + CutsceneEventDrawerUiUtil.VerticalSpacing;
                    height += EditorGUI.GetPropertyHeight(timeScaleProp.FindPropertyRelative("easing"), true) + CutsceneEventDrawerUiUtil.VerticalSpacing;
                    height += EditorGUI.GetPropertyHeight(useUnscaledTimeProp, true) + CutsceneEventDrawerUiUtil.VerticalSpacing;
                    height += GetWarningHeight(actionMode, toScaleProp, useUnscaledTimeProp, restoreOnCutsceneEndProp);
                    height += EditorGUI.GetPropertyHeight(restoreOnCutsceneEndProp, true) + CutsceneEventDrawerUiUtil.VerticalSpacing;
                    break;

                case CharacterAnimationTimeScaleActionMode.SetAndHold:
                    height += EditorGUI.GetPropertyHeight(captureOriginalOnTriggerProp, true) + CutsceneEventDrawerUiUtil.VerticalSpacing;
                    height += EditorGUI.GetPropertyHeight(toScaleProp, true) + CutsceneEventDrawerUiUtil.VerticalSpacing;
                    height += GetWarningHeight(actionMode, toScaleProp, useUnscaledTimeProp, restoreOnCutsceneEndProp);
                    height += EditorGUI.GetPropertyHeight(restoreOnCutsceneEndProp, true) + CutsceneEventDrawerUiUtil.VerticalSpacing;
                    break;

                case CharacterAnimationTimeScaleActionMode.Restore:
                    height += EditorGUI.GetPropertyHeight(useCapturedScaleForRestoreProp, true) + CutsceneEventDrawerUiUtil.VerticalSpacing;
                    if (!useCapturedScaleForRestoreProp.boolValue)
                    {
                        height += EditorGUI.GetPropertyHeight(timeScaleProp.FindPropertyRelative("restoreScale"), true) + CutsceneEventDrawerUiUtil.VerticalSpacing;
                    }
                    height += EditorGUI.GetPropertyHeight(timeScaleProp.FindPropertyRelative("easing"), true) + CutsceneEventDrawerUiUtil.VerticalSpacing;
                    height += EditorGUI.GetPropertyHeight(useUnscaledTimeProp, true) + CutsceneEventDrawerUiUtil.VerticalSpacing;
                    height += EditorGUI.GetPropertyHeight(restoreOnCutsceneEndProp, true) + CutsceneEventDrawerUiUtil.VerticalSpacing;
                    break;
            }

            return height;
        }

        private static void DrawWarnings(ref Rect current, CharacterAnimationTimeScaleActionMode actionMode, SerializedProperty toScaleProp,
            SerializedProperty useUnscaledTimeProp, SerializedProperty restoreOnCutsceneEndProp)
        {
            if (toScaleProp == null || !Mathf.Approximately(toScaleProp.floatValue, 0f))
            {
                return;
            }

            if (actionMode == CharacterAnimationTimeScaleActionMode.BlendAndHold && !useUnscaledTimeProp.boolValue)
            {
                CutsceneEventDrawerUiUtil.DrawHelpBox(ref current,
                    "animation time scale을 0으로 만들면 애니메이션은 멈춰 보이지만, 이 이벤트 duration은 Time.deltaTime 기준일 경우 진행이 멈출 수 있습니다. Use Unscaled Time 활성화를 권장합니다.",
                    MessageType.Warning);
            }

            if (actionMode == CharacterAnimationTimeScaleActionMode.SetAndHold && !restoreOnCutsceneEndProp.boolValue)
            {
                CutsceneEventDrawerUiUtil.DrawHelpBox(ref current,
                    "SetAndHold + toScale 0 + Restore On Cutscene End 비활성 상태입니다. 별도 Restore 이벤트가 없으면 컷씬 종료 후에도 애니메이션이 멈춘 상태로 남을 수 있습니다.",
                    MessageType.Info);
            }
        }

        private static float GetWarningHeight(CharacterAnimationTimeScaleActionMode actionMode, SerializedProperty toScaleProp,
            SerializedProperty useUnscaledTimeProp, SerializedProperty restoreOnCutsceneEndProp)
        {
            if (toScaleProp == null || !Mathf.Approximately(toScaleProp.floatValue, 0f))
            {
                return 0f;
            }

            float height = 0f;
            if (actionMode == CharacterAnimationTimeScaleActionMode.BlendAndHold && !useUnscaledTimeProp.boolValue)
            {
                height += CutsceneEventDrawerUiUtil.GetHelpBoxHeight(
                    "animation time scale을 0으로 만들면 애니메이션은 멈춰 보이지만, 이 이벤트 duration은 Time.deltaTime 기준일 경우 진행이 멈출 수 있습니다. Use Unscaled Time 활성화를 권장합니다.");
            }

            if (actionMode == CharacterAnimationTimeScaleActionMode.SetAndHold && !restoreOnCutsceneEndProp.boolValue)
            {
                height += CutsceneEventDrawerUiUtil.GetHelpBoxHeight(
                    "SetAndHold + toScale 0 + Restore On Cutscene End 비활성 상태입니다. 별도 Restore 이벤트가 없으면 컷씬 종료 후에도 애니메이션이 멈춘 상태로 남을 수 있습니다.");
            }

            return height;
        }
    }
}
