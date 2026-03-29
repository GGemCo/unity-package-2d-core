using GGemCo2DCore;
using UnityEditor;
using UnityEngine;

namespace GGemCo2DCoreEditor
{
    /// <summary>
    /// OverlayText 이벤트에 대한 Inspector UI를 렌더링하는 Drawer입니다.
    /// 텍스트 소스 모드에 따라 입력 필드를 동적으로 구성합니다.
    /// </summary>
    internal sealed class CutsceneOverlayTextEventTypeDrawer : ICutsceneEventTypeDrawer
    {
        /// <summary>
        /// 이 Drawer가 담당하는 이벤트 타입입니다.
        /// </summary>
        public CutsceneEventType EventType => CutsceneEventType.OverlayText;

        /// <summary>
        /// Inspector에서 OverlayText 이벤트 UI를 그립니다.
        /// </summary>
        /// <param name="position">UI를 그릴 영역(Rect)</param>
        /// <param name="eventProperty">컷씬 이벤트의 SerializedProperty</param>
        public void Draw(Rect position, SerializedProperty eventProperty)
        {
            var overlayTextProp = eventProperty.FindPropertyRelative("overlayText");
            if (overlayTextProp == null)
            {
                // TODO: 데이터 구조 변경 또는 propertyName 불일치 가능성
                return;
            }

            Rect current = position;
            current.height = EditorGUIUtility.singleLineHeight;

            // 그룹 제목 출력
            EditorGUI.LabelField(current, overlayTextProp.displayName, EditorStyles.boldLabel);
            current.y += current.height + CutsceneEventDrawerUiUtil.VerticalSpacing;

            int originalIndent = EditorGUI.indentLevel;
            EditorGUI.indentLevel++;

            // 주요 프로퍼티 캐싱
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

            // 텍스트 소스 모드 선택
            CutsceneEventDrawerUiUtil.DrawPropertyLine(ref current, sourceModeProp);

            var sourceMode = (OverlayTextSourceMode)sourceModeProp.enumValueIndex;

            // 런타임 텍스트 오버라이드 모드
            if (sourceMode == OverlayTextSourceMode.RuntimeOverride)
            {
                // 런타임 키로 텍스트를 가져오고, 실패 시 fallback 텍스트 사용
                CutsceneEventDrawerUiUtil.DrawPropertyLine(ref current, runtimeTextKeyProp);
                CutsceneEventDrawerUiUtil.DrawPropertyLine(ref current, textProp, new GUIContent("Fallback Text"));
            }
            else
            {
                // 정적 텍스트 입력
                CutsceneEventDrawerUiUtil.DrawPropertyLine(ref current, textProp);
            }

            // 레이아웃 및 스타일 설정
            CutsceneEventDrawerUiUtil.DrawPropertyLine(ref current, anchoredPositionProp);
            CutsceneEventDrawerUiUtil.DrawPropertyLine(ref current, sizeDeltaProp);
            CutsceneEventDrawerUiUtil.DrawPropertyLine(ref current, fontSizeProp);
            CutsceneEventDrawerUiUtil.DrawPropertyLine(ref current, textColorProp);

            // 알파 및 페이드 설정
            CutsceneEventDrawerUiUtil.DrawPropertyLine(ref current, maxAlphaProp);
            CutsceneEventDrawerUiUtil.DrawPropertyLine(ref current, fadeInProp);
            CutsceneEventDrawerUiUtil.DrawPropertyLine(ref current, fadeOutProp);

            // 애니메이션 보간 및 시간 설정
            CutsceneEventDrawerUiUtil.DrawPropertyLine(ref current, easingProp);
            CutsceneEventDrawerUiUtil.DrawPropertyLine(ref current, useUnscaledTimeProp);

            EditorGUI.indentLevel = originalIndent;
        }

        /// <summary>
        /// OverlayText 이벤트 UI의 전체 높이를 계산합니다.
        /// sourceMode에 따라 동적으로 높이가 달라집니다.
        /// </summary>
        /// <param name="eventProperty">컷씬 이벤트의 SerializedProperty</param>
        /// <returns>렌더링에 필요한 전체 높이</returns>
        public float GetHeight(SerializedProperty eventProperty)
        {
            var overlayTextProp = eventProperty.FindPropertyRelative("overlayText");
            if (overlayTextProp == null)
            {
                return EditorGUIUtility.singleLineHeight;
            }

            float height = CutsceneEventDrawerUiUtil.GetLabeledGroupBaseHeight();

            var sourceModeProp = overlayTextProp.FindPropertyRelative("sourceMode");
            height += EditorGUI.GetPropertyHeight(sourceModeProp, true) + CutsceneEventDrawerUiUtil.VerticalSpacing;

            var sourceMode = (OverlayTextSourceMode)sourceModeProp.enumValueIndex;

            // 텍스트 입력 영역 (모드에 따라 분기)
            if (sourceMode == OverlayTextSourceMode.RuntimeOverride)
            {
                height += EditorGUI.GetPropertyHeight(overlayTextProp.FindPropertyRelative("runtimeTextKey"), true) + CutsceneEventDrawerUiUtil.VerticalSpacing;
                height += EditorGUI.GetPropertyHeight(overlayTextProp.FindPropertyRelative("text"), true) + CutsceneEventDrawerUiUtil.VerticalSpacing;
            }
            else
            {
                height += EditorGUI.GetPropertyHeight(overlayTextProp.FindPropertyRelative("text"), true) + CutsceneEventDrawerUiUtil.VerticalSpacing;
            }

            // 공통 속성들
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