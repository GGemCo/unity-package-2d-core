using GGemCo2DCore;
using UnityEditor;
using UnityEngine;

namespace GGemCo2DCoreEditor
{
    /// <summary>
    /// CharacterAnimationTimeScale 이벤트 타입에 대한 Inspector UI를 그리는 Drawer입니다.
    /// 이벤트 설정에 따라 표시되는 필드를 동적으로 구성합니다.
    /// </summary>
    internal sealed class CutsceneCharacterAnimationTimeScaleEventTypeDrawer : ICutsceneEventTypeDrawer
    {
        /// <summary>
        /// 이 Drawer가 담당하는 이벤트 타입을 반환합니다.
        /// </summary>
        public CutsceneEventType EventType => CutsceneEventType.CharacterAnimationTimeScale;

        /// <summary>
        /// Inspector에서 이벤트 프로퍼티를 렌더링합니다.
        /// </summary>
        /// <param name="position">그리기를 수행할 영역(Rect)</param>
        /// <param name="eventProperty">이벤트 SerializedProperty</param>
        public void Draw(Rect position, SerializedProperty eventProperty)
        {
            var timeScaleProp = eventProperty.FindPropertyRelative("characterAnimationTimeScale");
            if (timeScaleProp == null)
            {
                return;
            }

            Rect current = position;
            current.height = EditorGUIUtility.singleLineHeight;

            // 그룹 제목 출력
            EditorGUI.LabelField(current, timeScaleProp.displayName, EditorStyles.boldLabel);
            current.y += current.height + CutsceneEventDrawerUiUtil.VerticalSpacing;

            int originalIndent = EditorGUI.indentLevel;
            EditorGUI.indentLevel++;

            // 주요 속성들 캐싱
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

            // 캐릭터 타입
            CutsceneEventDrawerUiUtil.DrawPropertyLine(ref current, characterTypeProp);

            // Player가 아닌 경우 UID 표시
            if ((CharacterConstants.Type)characterTypeProp.enumValueIndex != CharacterConstants.Type.Player)
            {
                CutsceneEventDrawerUiUtil.DrawPropertyLine(ref current, characterUidProp);
            }

            // 동작 모드
            CutsceneEventDrawerUiUtil.DrawPropertyLine(ref current, actionModeProp);
            var actionMode = (CharacterAnimationTimeScaleActionMode)actionModeProp.enumValueIndex;

            switch (actionMode)
            {
                case CharacterAnimationTimeScaleActionMode.BlendAndHold:
                    // 기존 값 캡처 여부
                    CutsceneEventDrawerUiUtil.DrawPropertyLine(ref current, captureOriginalOnTriggerProp);

                    // 캡처하지 않으면 시작값 입력
                    if (!captureOriginalOnTriggerProp.boolValue)
                    {
                        CutsceneEventDrawerUiUtil.DrawPropertyLine(ref current, fromScaleProp);
                    }

                    CutsceneEventDrawerUiUtil.DrawPropertyLine(ref current, toScaleProp);
                    CutsceneEventDrawerUiUtil.DrawPropertyLine(ref current, easingProp);
                    CutsceneEventDrawerUiUtil.DrawPropertyLine(ref current, useUnscaledTimeProp);

                    // 위험 설정 경고 표시
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
                    // 캡처된 값 사용 여부
                    CutsceneEventDrawerUiUtil.DrawPropertyLine(ref current, useCapturedScaleForRestoreProp);

                    // 캡처값을 사용하지 않으면 직접 입력
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

        /// <summary>
        /// 현재 이벤트 설정에 따라 Inspector UI의 전체 높이를 계산합니다.
        /// </summary>
        /// <param name="eventProperty">이벤트 SerializedProperty</param>
        /// <returns>필요한 UI 높이</returns>
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

            // 공통 필드 높이 계산
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

        /// <summary>
        /// 특정 설정 조합에서 발생할 수 있는 문제를 Inspector에 경고 메시지로 표시합니다.
        /// </summary>
        /// <param name="current">현재 그리기 위치</param>
        /// <param name="actionMode">현재 액션 모드</param>
        /// <param name="toScaleProp">목표 타임스케일</param>
        /// <param name="useUnscaledTimeProp">Unscaled Time 사용 여부</param>
        /// <param name="restoreOnCutsceneEndProp">컷씬 종료 시 복구 여부</param>
        private static void DrawWarnings(
            ref Rect current,
            CharacterAnimationTimeScaleActionMode actionMode,
            SerializedProperty toScaleProp,
            SerializedProperty useUnscaledTimeProp,
            SerializedProperty restoreOnCutsceneEndProp)
        {
            // toScale이 0이 아닐 경우 경고 없음
            if (toScaleProp == null || !Mathf.Approximately(toScaleProp.floatValue, 0f))
            {
                return;
            }

            // BlendAndHold + deltaTime 사용 시 이벤트 진행 정지 가능성
            if (actionMode == CharacterAnimationTimeScaleActionMode.BlendAndHold && !useUnscaledTimeProp.boolValue)
            {
                CutsceneEventDrawerUiUtil.DrawHelpBox(ref current,
                    "animation time scale을 0으로 만들면 애니메이션은 멈춰 보이지만, 이 이벤트 duration은 Time.deltaTime 기준일 경우 진행이 멈출 수 있습니다. Use Unscaled Time 활성화를 권장합니다.",
                    MessageType.Warning);
            }

            // SetAndHold + restore 없음 → 영구 정지 가능성
            if (actionMode == CharacterAnimationTimeScaleActionMode.SetAndHold && !restoreOnCutsceneEndProp.boolValue)
            {
                CutsceneEventDrawerUiUtil.DrawHelpBox(ref current,
                    "SetAndHold + toScale 0 + Restore On Cutscene End 비활성 상태입니다. 별도 Restore 이벤트가 없으면 컷씬 종료 후에도 애니메이션이 멈춘 상태로 남을 수 있습니다.",
                    MessageType.Info);
            }
        }

        /// <summary>
        /// 경고 메시지가 표시될 경우 필요한 UI 높이를 계산합니다.
        /// </summary>
        private static float GetWarningHeight(
            CharacterAnimationTimeScaleActionMode actionMode,
            SerializedProperty toScaleProp,
            SerializedProperty useUnscaledTimeProp,
            SerializedProperty restoreOnCutsceneEndProp)
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