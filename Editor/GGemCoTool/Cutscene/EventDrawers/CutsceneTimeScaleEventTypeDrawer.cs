using GGemCo2DCore;
using UnityEditor;
using UnityEngine;

namespace GGemCo2DCoreEditor
{
    /// <summary>
    /// TimeScale 이벤트에 대한 Inspector UI를 렌더링하는 Drawer입니다.
    /// 게임의 timeScale 변경 방식(Blend, Set, Restore)에 따라 UI를 동적으로 구성합니다.
    /// </summary>
    internal sealed class CutsceneTimeScaleEventTypeDrawer : ICutsceneEventTypeDrawer
    {
        /// <summary>
        /// 이 Drawer가 담당하는 이벤트 타입입니다.
        /// </summary>
        public CutsceneEventType EventType => CutsceneEventType.TimeScale;

        /// <summary>
        /// Inspector에서 TimeScale 이벤트 UI를 그립니다.
        /// </summary>
        /// <param name="position">UI를 그릴 영역(Rect)</param>
        /// <param name="eventProperty">컷씬 이벤트의 SerializedProperty</param>
        public void Draw(Rect position, SerializedProperty eventProperty)
        {
            var timeScaleProp = eventProperty.FindPropertyRelative("timeScale");
            if (timeScaleProp == null)
            {
                // TODO: 데이터 구조 변경 또는 propertyName 불일치 가능성
                return;
            }

            Rect current = position;
            current.height = EditorGUIUtility.singleLineHeight;

            // 그룹 제목
            EditorGUI.LabelField(current, timeScaleProp.displayName, EditorStyles.boldLabel);
            current.y += current.height + CutsceneEventDrawerUiUtil.VerticalSpacing;

            int originalIndent = EditorGUI.indentLevel;
            EditorGUI.indentLevel++;

            // 주요 프로퍼티 캐싱
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

            // 동작 모드 선택
            CutsceneEventDrawerUiUtil.DrawPropertyLine(ref current, actionModeProp);

            var actionMode = (TimeScaleActionMode)actionModeProp.enumValueIndex;

            switch (actionMode)
            {
                case TimeScaleActionMode.BlendAndHold:
                    // 기존 → 목표 값으로 보간 후 유지
                    CutsceneEventDrawerUiUtil.DrawPropertyLine(ref current, fromScaleProp);
                    CutsceneEventDrawerUiUtil.DrawPropertyLine(ref current, toScaleProp);
                    CutsceneEventDrawerUiUtil.DrawPropertyLine(ref current, easingProp);
                    CutsceneEventDrawerUiUtil.DrawPropertyLine(ref current, useUnscaledTimeProp);
                    CutsceneEventDrawerUiUtil.DrawPropertyLine(ref current, timelineModeProp);

                    // 위험 설정 경고
                    DrawWarnings(ref current, actionMode, toScaleProp, useUnscaledTimeProp, timelineModeProp, restoreOnCutsceneEndProp);

                    CutsceneEventDrawerUiUtil.DrawPropertyLine(ref current, restoreOnCutsceneEndProp);

                    // FixedUpdate 영향 여부
                    CutsceneEventDrawerUiUtil.DrawPropertyLine(ref current, affectFixedDeltaTimeProp);
                    if (affectFixedDeltaTimeProp.boolValue)
                    {
                        CutsceneEventDrawerUiUtil.DrawPropertyLine(ref current, minimumScaleForFixedDeltaTimeProp);
                    }
                    break;

                case TimeScaleActionMode.SetAndHold:
                    // 즉시 설정 후 유지
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
                    // 이전 값으로 복구
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

        /// <summary>
        /// TimeScale 이벤트 UI의 전체 높이를 계산합니다.
        /// Draw와 동일한 분기 구조를 따라야 정확한 레이아웃이 유지됩니다.
        /// </summary>
        /// <param name="eventProperty">컷씬 이벤트의 SerializedProperty</param>
        /// <returns>렌더링에 필요한 전체 높이</returns>
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

                    height += GetWarningHeight(actionMode,
                        timeScaleProp.FindPropertyRelative("toScale"),
                        timeScaleProp.FindPropertyRelative("useUnscaledTime"),
                        timeScaleProp.FindPropertyRelative("timelineMode"),
                        timeScaleProp.FindPropertyRelative("restoreOnCutsceneEnd"));

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

                    height += GetWarningHeight(actionMode,
                        timeScaleProp.FindPropertyRelative("toScale"),
                        timeScaleProp.FindPropertyRelative("useUnscaledTime"),
                        timeScaleProp.FindPropertyRelative("timelineMode"),
                        timeScaleProp.FindPropertyRelative("restoreOnCutsceneEnd"));

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

        /// <summary>
        /// 특정 설정 조합에서 발생할 수 있는 문제를 경고 메시지로 표시합니다.
        /// </summary>
        private static void DrawWarnings(
            ref Rect current,
            TimeScaleActionMode actionMode,
            SerializedProperty toScaleProp,
            SerializedProperty useUnscaledTimeProp,
            SerializedProperty timelineModeProp,
            SerializedProperty restoreOnCutsceneEndProp)
        {
            // toScale이 0이 아니면 경고 없음
            if (toScaleProp == null || !Mathf.Approximately(toScaleProp.floatValue, 0f))
            {
                return;
            }

            // deltaTime 기반이면 이벤트 진행 정지 가능
            if (actionMode == TimeScaleActionMode.BlendAndHold && !useUnscaledTimeProp.boolValue)
            {
                CutsceneEventDrawerUiUtil.DrawHelpBox(ref current,
                    "timeScale이 0이면 Time.deltaTime도 0이 되므로, 이 이벤트 duration 진행이 멈출 수 있습니다. Use Unscaled Time 활성화를 권장합니다.",
                    MessageType.Warning);
            }

            // 타임라인도 같이 멈출 위험
            if ((CutsceneTimeScaleTimelineMode)timelineModeProp.enumValueIndex != CutsceneTimeScaleTimelineMode.KeepRunningWhenTimeScaleIsZero)
            {
                CutsceneEventDrawerUiUtil.DrawHelpBox(ref current,
                    "timeScale이 0일 때 컷신 타임라인도 같이 멈출 수 있습니다. 후속 이벤트를 계속 진행하려면 Timeline Mode를 KeepRunningWhenTimeScaleIsZero로 설정하세요.",
                    MessageType.Warning);
            }

            // 복구 설정 없음 → 영구 정지 위험
            if (actionMode == TimeScaleActionMode.SetAndHold && !restoreOnCutsceneEndProp.boolValue)
            {
                CutsceneEventDrawerUiUtil.DrawHelpBox(ref current,
                    "SetAndHold + toScale 0 + Restore On Cutscene End 비활성 상태입니다. 별도 Restore 이벤트가 없으면 컷신 종료 후에도 게임이 멈춘 상태로 남을 수 있습니다.",
                    MessageType.Info);
            }
        }

        /// <summary>
        /// 경고 메시지에 필요한 UI 높이를 계산합니다.
        /// </summary>
        private static float GetWarningHeight(
            TimeScaleActionMode actionMode,
            SerializedProperty toScaleProp,
            SerializedProperty useUnscaledTimeProp,
            SerializedProperty timelineModeProp,
            SerializedProperty restoreOnCutsceneEndProp)
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