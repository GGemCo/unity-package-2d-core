using GGemCo2DCore;
using UnityEditor;
using UnityEngine;

namespace GGemCo2DCoreEditor
{
    internal sealed class CutsceneTimeScaleEventTypeDrawer : ICutsceneEventTypeDrawer
    {
        public CutsceneEventType EventType => CutsceneEventType.TimeScale;

        public void Draw(Rect position, SerializedProperty eventProperty)
        {
            var timeScaleProp = eventProperty.FindPropertyRelative("timeScale");
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

            CutsceneEventDrawerUiUtil.DrawPropertyLine(ref current, actionModeProp);

            var actionMode = (TimeScaleActionMode)actionModeProp.enumValueIndex;
            switch (actionMode)
            {
                case TimeScaleActionMode.BlendAndHold:
                    CutsceneEventDrawerUiUtil.DrawPropertyLine(ref current, fromScaleProp);
                    CutsceneEventDrawerUiUtil.DrawPropertyLine(ref current, toScaleProp);
                    CutsceneEventDrawerUiUtil.DrawPropertyLine(ref current, easingProp);
                    CutsceneEventDrawerUiUtil.DrawPropertyLine(ref current, useUnscaledTimeProp);
                    CutsceneEventDrawerUiUtil.DrawPropertyLine(ref current, timelineModeProp);
                    DrawWarnings(ref current, actionMode, toScaleProp, useUnscaledTimeProp, timelineModeProp, restoreOnCutsceneEndProp);
                    CutsceneEventDrawerUiUtil.DrawPropertyLine(ref current, restoreOnCutsceneEndProp);
                    CutsceneEventDrawerUiUtil.DrawPropertyLine(ref current, affectFixedDeltaTimeProp);
                    if (affectFixedDeltaTimeProp.boolValue)
                    {
                        CutsceneEventDrawerUiUtil.DrawPropertyLine(ref current, minimumScaleForFixedDeltaTimeProp);
                    }
                    break;

                case TimeScaleActionMode.SetAndHold:
                    CutsceneEventDrawerUiUtil.DrawPropertyLine(ref current, toScaleProp);
                    CutsceneEventDrawerUiUtil.DrawPropertyLine(ref current, timelineModeProp);
                    DrawWarnings(ref current, actionMode, toScaleProp, useUnscaledTimeProp, timelineModeProp, restoreOnCutsceneEndProp);
                    CutsceneEventDrawerUiUtil.DrawPropertyLine(ref current, restoreOnCutsceneEndProp);
                    CutsceneEventDrawerUiUtil.DrawPropertyLine(ref current, affectFixedDeltaTimeProp);
                    if (affectFixedDeltaTimeProp.boolValue)
                    {
                        CutsceneEventDrawerUiUtil.DrawPropertyLine(ref current, minimumScaleForFixedDeltaTimeProp);
                    }
                    break;

                case TimeScaleActionMode.Restore:
                    CutsceneEventDrawerUiUtil.DrawPropertyLine(ref current, useCapturedScaleForRestoreProp);
                    if (!useCapturedScaleForRestoreProp.boolValue)
                    {
                        CutsceneEventDrawerUiUtil.DrawPropertyLine(ref current, restoreScaleProp);
                    }
                    CutsceneEventDrawerUiUtil.DrawPropertyLine(ref current, easingProp);
                    CutsceneEventDrawerUiUtil.DrawPropertyLine(ref current, useUnscaledTimeProp);
                    CutsceneEventDrawerUiUtil.DrawPropertyLine(ref current, affectFixedDeltaTimeProp);
                    if (affectFixedDeltaTimeProp.boolValue)
                    {
                        CutsceneEventDrawerUiUtil.DrawPropertyLine(ref current, minimumScaleForFixedDeltaTimeProp);
                    }
                    break;
            }

            EditorGUI.indentLevel = originalIndent;
        }

        public float GetHeight(SerializedProperty eventProperty)
        {
            var timeScaleProp = eventProperty.FindPropertyRelative("timeScale");
            if (timeScaleProp == null)
            {
                return EditorGUIUtility.singleLineHeight;
            }

            float height = CutsceneEventDrawerUiUtil.GetLabeledGroupBaseHeight();
            var actionModeProp = timeScaleProp.FindPropertyRelative("actionMode");
            height += EditorGUI.GetPropertyHeight(actionModeProp, true) + CutsceneEventDrawerUiUtil.VerticalSpacing;

            var affectFixedDeltaTimeProp = timeScaleProp.FindPropertyRelative("affectFixedDeltaTime");
            var useCapturedScaleForRestoreProp = timeScaleProp.FindPropertyRelative("useCapturedScaleForRestore");
            var actionMode = (TimeScaleActionMode)actionModeProp.enumValueIndex;

            switch (actionMode)
            {
                case TimeScaleActionMode.BlendAndHold:
                    height += EditorGUI.GetPropertyHeight(timeScaleProp.FindPropertyRelative("fromScale"), true) + CutsceneEventDrawerUiUtil.VerticalSpacing;
                    height += EditorGUI.GetPropertyHeight(timeScaleProp.FindPropertyRelative("toScale"), true) + CutsceneEventDrawerUiUtil.VerticalSpacing;
                    height += EditorGUI.GetPropertyHeight(timeScaleProp.FindPropertyRelative("easing"), true) + CutsceneEventDrawerUiUtil.VerticalSpacing;
                    height += EditorGUI.GetPropertyHeight(timeScaleProp.FindPropertyRelative("useUnscaledTime"), true) + CutsceneEventDrawerUiUtil.VerticalSpacing;
                    height += EditorGUI.GetPropertyHeight(timeScaleProp.FindPropertyRelative("timelineMode"), true) + CutsceneEventDrawerUiUtil.VerticalSpacing;
                    height += GetWarningHeight(actionMode, timeScaleProp.FindPropertyRelative("toScale"), timeScaleProp.FindPropertyRelative("useUnscaledTime"), timeScaleProp.FindPropertyRelative("timelineMode"), timeScaleProp.FindPropertyRelative("restoreOnCutsceneEnd"));
                    height += EditorGUI.GetPropertyHeight(timeScaleProp.FindPropertyRelative("restoreOnCutsceneEnd"), true) + CutsceneEventDrawerUiUtil.VerticalSpacing;
                    height += EditorGUI.GetPropertyHeight(affectFixedDeltaTimeProp, true) + CutsceneEventDrawerUiUtil.VerticalSpacing;
                    if (affectFixedDeltaTimeProp.boolValue)
                    {
                        height += EditorGUI.GetPropertyHeight(timeScaleProp.FindPropertyRelative("minimumScaleForFixedDeltaTime"), true) + CutsceneEventDrawerUiUtil.VerticalSpacing;
                    }
                    break;

                case TimeScaleActionMode.SetAndHold:
                    height += EditorGUI.GetPropertyHeight(timeScaleProp.FindPropertyRelative("toScale"), true) + CutsceneEventDrawerUiUtil.VerticalSpacing;
                    height += EditorGUI.GetPropertyHeight(timeScaleProp.FindPropertyRelative("timelineMode"), true) + CutsceneEventDrawerUiUtil.VerticalSpacing;
                    height += GetWarningHeight(actionMode, timeScaleProp.FindPropertyRelative("toScale"), timeScaleProp.FindPropertyRelative("useUnscaledTime"), timeScaleProp.FindPropertyRelative("timelineMode"), timeScaleProp.FindPropertyRelative("restoreOnCutsceneEnd"));
                    height += EditorGUI.GetPropertyHeight(timeScaleProp.FindPropertyRelative("restoreOnCutsceneEnd"), true) + CutsceneEventDrawerUiUtil.VerticalSpacing;
                    height += EditorGUI.GetPropertyHeight(affectFixedDeltaTimeProp, true) + CutsceneEventDrawerUiUtil.VerticalSpacing;
                    if (affectFixedDeltaTimeProp.boolValue)
                    {
                        height += EditorGUI.GetPropertyHeight(timeScaleProp.FindPropertyRelative("minimumScaleForFixedDeltaTime"), true) + CutsceneEventDrawerUiUtil.VerticalSpacing;
                    }
                    break;

                case TimeScaleActionMode.Restore:
                    height += EditorGUI.GetPropertyHeight(useCapturedScaleForRestoreProp, true) + CutsceneEventDrawerUiUtil.VerticalSpacing;
                    if (!useCapturedScaleForRestoreProp.boolValue)
                    {
                        height += EditorGUI.GetPropertyHeight(timeScaleProp.FindPropertyRelative("restoreScale"), true) + CutsceneEventDrawerUiUtil.VerticalSpacing;
                    }
                    height += EditorGUI.GetPropertyHeight(timeScaleProp.FindPropertyRelative("easing"), true) + CutsceneEventDrawerUiUtil.VerticalSpacing;
                    height += EditorGUI.GetPropertyHeight(timeScaleProp.FindPropertyRelative("useUnscaledTime"), true) + CutsceneEventDrawerUiUtil.VerticalSpacing;
                    height += EditorGUI.GetPropertyHeight(affectFixedDeltaTimeProp, true) + CutsceneEventDrawerUiUtil.VerticalSpacing;
                    if (affectFixedDeltaTimeProp.boolValue)
                    {
                        height += EditorGUI.GetPropertyHeight(timeScaleProp.FindPropertyRelative("minimumScaleForFixedDeltaTime"), true) + CutsceneEventDrawerUiUtil.VerticalSpacing;
                    }
                    break;
            }

            return height;
        }

        private static void DrawWarnings(ref Rect current, TimeScaleActionMode actionMode, SerializedProperty toScaleProp,
            SerializedProperty useUnscaledTimeProp, SerializedProperty timelineModeProp, SerializedProperty restoreOnCutsceneEndProp)
        {
            if (toScaleProp == null || !Mathf.Approximately(toScaleProp.floatValue, 0f))
            {
                return;
            }

            if (actionMode == TimeScaleActionMode.BlendAndHold && !useUnscaledTimeProp.boolValue)
            {
                CutsceneEventDrawerUiUtil.DrawHelpBox(ref current,
                    "timeScale이 0이면 Time.deltaTime도 0이 되므로, 이 이벤트 duration 진행이 멈출 수 있습니다. Use Unscaled Time 활성화를 권장합니다.",
                    MessageType.Warning);
            }

            if ((CutsceneTimeScaleTimelineMode)timelineModeProp.enumValueIndex != CutsceneTimeScaleTimelineMode.KeepRunningWhenTimeScaleIsZero)
            {
                CutsceneEventDrawerUiUtil.DrawHelpBox(ref current,
                    "timeScale이 0일 때 컷신 타임라인도 같이 멈출 수 있습니다. 후속 이벤트를 계속 진행하려면 Timeline Mode를 KeepRunningWhenTimeScaleIsZero로 설정하세요.",
                    MessageType.Warning);
            }

            if (actionMode == TimeScaleActionMode.SetAndHold && !restoreOnCutsceneEndProp.boolValue)
            {
                CutsceneEventDrawerUiUtil.DrawHelpBox(ref current,
                    "SetAndHold + toScale 0 + Restore On Cutscene End 비활성 상태입니다. 별도 Restore 이벤트가 없으면 컷신 종료 후에도 게임이 멈춘 상태로 남을 수 있습니다.",
                    MessageType.Info);
            }
        }

        private static float GetWarningHeight(TimeScaleActionMode actionMode, SerializedProperty toScaleProp,
            SerializedProperty useUnscaledTimeProp, SerializedProperty timelineModeProp, SerializedProperty restoreOnCutsceneEndProp)
        {
            if (toScaleProp == null || !Mathf.Approximately(toScaleProp.floatValue, 0f))
            {
                return 0f;
            }

            float height = 0f;
            if (actionMode == TimeScaleActionMode.BlendAndHold && !useUnscaledTimeProp.boolValue)
            {
                height += CutsceneEventDrawerUiUtil.GetHelpBoxHeight(
                    "timeScale이 0이면 Time.deltaTime도 0이 되므로, 이 이벤트 duration 진행이 멈출 수 있습니다. Use Unscaled Time 활성화를 권장합니다.");
            }

            if ((CutsceneTimeScaleTimelineMode)timelineModeProp.enumValueIndex != CutsceneTimeScaleTimelineMode.KeepRunningWhenTimeScaleIsZero)
            {
                height += CutsceneEventDrawerUiUtil.GetHelpBoxHeight(
                    "timeScale이 0일 때 컷신 타임라인도 같이 멈출 수 있습니다. 후속 이벤트를 계속 진행하려면 Timeline Mode를 KeepRunningWhenTimeScaleIsZero로 설정하세요.");
            }

            if (actionMode == TimeScaleActionMode.SetAndHold && !restoreOnCutsceneEndProp.boolValue)
            {
                height += CutsceneEventDrawerUiUtil.GetHelpBoxHeight(
                    "SetAndHold + toScale 0 + Restore On Cutscene End 비활성 상태입니다. 별도 Restore 이벤트가 없으면 컷신 종료 후에도 게임이 멈춘 상태로 남을 수 있습니다.");
            }

            return height;
        }
    }
}
