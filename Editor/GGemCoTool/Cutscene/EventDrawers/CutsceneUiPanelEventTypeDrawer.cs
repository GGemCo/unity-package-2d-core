using GGemCo2DCore;
using UnityEditor;
using UnityEngine;

namespace GGemCo2DCoreEditor
{
    /// <summary>
    /// UI Panel 이벤트의 레이아웃 모드에 맞춰 필요한 Inspector 필드를 구성하는 Drawer입니다.
    /// Stretch 축과 기존 위치·크기 축의 역할을 구분하여 잘못된 조합 입력을 줄입니다.
    /// </summary>
    internal sealed class CutsceneUiPanelEventTypeDrawer : ICutsceneEventTypeDrawer
    {
        private const string HorizontalStretchHelp =
            "가로축은 부모 너비에 맞춰 자동으로 늘어납니다. 위치와 크기에서는 Y와 Height만 적용됩니다.";
        private const string VerticalStretchHelp =
            "세로축은 부모 높이에 맞춰 자동으로 늘어납니다. 위치와 크기에서는 X와 Width만 적용됩니다.";
        private const string BothStretchHelp =
            "가로와 세로가 부모 전체 영역에 맞춰 자동으로 늘어납니다. 위치와 크기 대신 Stretch 여백이 적용됩니다.";

        private static readonly GUIContent StretchOffsetMinLabel = new("Stretch Left / Bottom");
        private static readonly GUIContent StretchOffsetMaxLabel = new("Stretch Right / Top");
        private static readonly GUIContent FromPositionYLabel = new("From Position (Y)");
        private static readonly GUIContent ToPositionYLabel = new("To Position (Y)");
        private static readonly GUIContent FromSizeHeightLabel = new("From Size (Height)");
        private static readonly GUIContent ToSizeHeightLabel = new("To Size (Height)");
        private static readonly GUIContent FromPositionXLabel = new("From Position (X)");
        private static readonly GUIContent ToPositionXLabel = new("To Position (X)");
        private static readonly GUIContent FromSizeWidthLabel = new("From Size (Width)");
        private static readonly GUIContent ToSizeWidthLabel = new("To Size (Width)");

        private static readonly string[] IdentityPropertyNames =
            { "panelId", "createIfMissing", "destroyOnStop", "hideOnStop" };
        private static readonly string[] CustomAnchorPropertyNames = { "anchorMin", "anchorMax" };
        private static readonly string[] StretchOffsetPropertyNames = { "stretchOffsetMin", "stretchOffsetMax" };
        private static readonly string[] AnimatedLayoutPropertyNames =
            { "fromAnchoredPosition", "toAnchoredPosition", "fromSizeDelta", "toSizeDelta" };
        private static readonly string[] RenderPropertyNames =
            { "renderMode", "useIndependentCanvasSorting", "sortingLayerName", "orderInLayer", "planeDistance" };
        private static readonly string[] VisualPropertyNames =
            { "fromColor", "toColor", "fromAlpha", "toAlpha", "raycastTarget" };
        private static readonly string[] PlaybackPropertyNames = { "easing", "useUnscaledTime" };

        /// <summary>
        /// 이 Drawer가 담당하는 이벤트 타입입니다.
        /// </summary>
        public CutsceneEventType EventType => CutsceneEventType.UiPanel;

        /// <summary>
        /// UI Panel 이벤트의 Inspector UI를 레이아웃 모드에 따라 그립니다.
        /// </summary>
        /// <param name="position">UI를 그릴 전체 영역입니다.</param>
        /// <param name="eventProperty">UI Panel 데이터를 포함한 컷신 이벤트 프로퍼티입니다.</param>
        public void Draw(Rect position, SerializedProperty eventProperty)
        {
            SerializedProperty uiPanelProperty = eventProperty.FindPropertyRelative("uiPanel");
            if (uiPanelProperty == null)
            {
                return;
            }

            Rect current = position;
            current.height = EditorGUIUtility.singleLineHeight;
            EditorGUI.LabelField(current, uiPanelProperty.displayName, EditorStyles.boldLabel);
            current.y += current.height + CutsceneEventDrawerUiUtil.VerticalSpacing;

            int originalIndent = EditorGUI.indentLevel;
            EditorGUI.indentLevel++;

            DrawSectionHeader(ref current, "Identity");
            DrawProperty(ref current, uiPanelProperty, "panelId");
            DrawProperty(ref current, uiPanelProperty, "createIfMissing");
            DrawProperty(ref current, uiPanelProperty, "destroyOnStop");
            DrawProperty(ref current, uiPanelProperty, "hideOnStop");

            DrawSectionHeader(ref current, "Layout");
            SerializedProperty layoutModeProperty = uiPanelProperty.FindPropertyRelative("layoutMode");
            CutsceneEventDrawerUiUtil.DrawPropertyLine(ref current, layoutModeProperty);
            UiPanelLayoutMode layoutMode = (UiPanelLayoutMode)layoutModeProperty.enumValueIndex;

            if (layoutMode == UiPanelLayoutMode.Custom)
            {
                DrawProperty(ref current, uiPanelProperty, "anchorMin");
                DrawProperty(ref current, uiPanelProperty, "anchorMax");
            }
            else
            {
                DrawProperty(ref current, uiPanelProperty, "stretchOffsetMin", StretchOffsetMinLabel);
                DrawProperty(ref current, uiPanelProperty, "stretchOffsetMax", StretchOffsetMaxLabel);
                CutsceneEventDrawerUiUtil.DrawHelpBox(
                    ref current,
                    GetStretchHelp(layoutMode),
                    MessageType.Info);
            }

            DrawProperty(ref current, uiPanelProperty, "pivot");
            DrawAnimatedLayoutFields(ref current, uiPanelProperty, layoutMode);
            DrawProperty(ref current, uiPanelProperty, "siblingIndex");

            DrawSectionHeader(ref current, "Render");
            DrawProperty(ref current, uiPanelProperty, "renderMode");
            DrawProperty(ref current, uiPanelProperty, "useIndependentCanvasSorting");
            DrawProperty(ref current, uiPanelProperty, "sortingLayerName");
            DrawProperty(ref current, uiPanelProperty, "orderInLayer");
            DrawProperty(ref current, uiPanelProperty, "planeDistance");

            DrawSectionHeader(ref current, "Visual");
            DrawProperty(ref current, uiPanelProperty, "fromColor");
            DrawProperty(ref current, uiPanelProperty, "toColor");
            DrawProperty(ref current, uiPanelProperty, "fromAlpha");
            DrawProperty(ref current, uiPanelProperty, "toAlpha");
            DrawProperty(ref current, uiPanelProperty, "raycastTarget");

            DrawSectionHeader(ref current, "Playback");
            DrawProperty(ref current, uiPanelProperty, "easing");
            DrawProperty(ref current, uiPanelProperty, "useUnscaledTime");

            EditorGUI.indentLevel = originalIndent;
        }

        /// <summary>
        /// 현재 레이아웃 모드에 필요한 UI Panel Inspector 전체 높이를 계산합니다.
        /// </summary>
        /// <param name="eventProperty">UI Panel 데이터를 포함한 컷신 이벤트 프로퍼티입니다.</param>
        /// <returns>Inspector 렌더링에 필요한 높이입니다.</returns>
        public float GetHeight(SerializedProperty eventProperty)
        {
            SerializedProperty uiPanelProperty = eventProperty.FindPropertyRelative("uiPanel");
            if (uiPanelProperty == null)
            {
                return EditorGUIUtility.singleLineHeight;
            }

            float height = CutsceneEventDrawerUiUtil.GetLabeledGroupBaseHeight();
            height += GetSectionHeaderHeight();
            height += GetPropertiesHeight(uiPanelProperty, IdentityPropertyNames);

            height += GetSectionHeaderHeight();
            SerializedProperty layoutModeProperty = uiPanelProperty.FindPropertyRelative("layoutMode");
            height += GetPropertyHeight(layoutModeProperty);
            UiPanelLayoutMode layoutMode = (UiPanelLayoutMode)layoutModeProperty.enumValueIndex;

            if (layoutMode == UiPanelLayoutMode.Custom)
            {
                height += GetPropertiesHeight(uiPanelProperty, CustomAnchorPropertyNames);
            }
            else
            {
                height += GetPropertiesHeight(uiPanelProperty, StretchOffsetPropertyNames);
                height += CutsceneEventDrawerUiUtil.GetHelpBoxHeight(GetStretchHelp(layoutMode));
            }

            height += GetPropertyHeight(uiPanelProperty.FindPropertyRelative("pivot"));
            height += GetAnimatedLayoutFieldsHeight(uiPanelProperty, layoutMode);
            height += GetPropertyHeight(uiPanelProperty.FindPropertyRelative("siblingIndex"));

            height += GetSectionHeaderHeight();
            height += GetPropertiesHeight(uiPanelProperty, RenderPropertyNames);

            height += GetSectionHeaderHeight();
            height += GetPropertiesHeight(uiPanelProperty, VisualPropertyNames);

            height += GetSectionHeaderHeight();
            height += GetPropertiesHeight(uiPanelProperty, PlaybackPropertyNames);
            return height;
        }

        /// <summary>
        /// Stretch하지 않는 축에서 사용할 시작·종료 위치와 크기 필드를 그립니다.
        /// </summary>
        /// <param name="current">현재 그리기 위치이며 다음 위치로 갱신됩니다.</param>
        /// <param name="uiPanelProperty">UI Panel 데이터 프로퍼티입니다.</param>
        /// <param name="layoutMode">현재 선택된 레이아웃 모드입니다.</param>
        private static void DrawAnimatedLayoutFields(
            ref Rect current,
            SerializedProperty uiPanelProperty,
            UiPanelLayoutMode layoutMode)
        {
            if (layoutMode == UiPanelLayoutMode.StretchBoth)
            {
                return;
            }

            GUIContent fromPositionLabel = null;
            GUIContent toPositionLabel = null;
            GUIContent fromSizeLabel = null;
            GUIContent toSizeLabel = null;

            if (layoutMode == UiPanelLayoutMode.StretchHorizontal)
            {
                fromPositionLabel = FromPositionYLabel;
                toPositionLabel = ToPositionYLabel;
                fromSizeLabel = FromSizeHeightLabel;
                toSizeLabel = ToSizeHeightLabel;
            }
            else if (layoutMode == UiPanelLayoutMode.StretchVertical)
            {
                fromPositionLabel = FromPositionXLabel;
                toPositionLabel = ToPositionXLabel;
                fromSizeLabel = FromSizeWidthLabel;
                toSizeLabel = ToSizeWidthLabel;
            }

            DrawProperty(ref current, uiPanelProperty, "fromAnchoredPosition", fromPositionLabel);
            DrawProperty(ref current, uiPanelProperty, "toAnchoredPosition", toPositionLabel);
            DrawProperty(ref current, uiPanelProperty, "fromSizeDelta", fromSizeLabel);
            DrawProperty(ref current, uiPanelProperty, "toSizeDelta", toSizeLabel);
        }

        /// <summary>
        /// 현재 레이아웃 모드에서 위치·크기 필드가 차지하는 높이를 계산합니다.
        /// </summary>
        /// <param name="uiPanelProperty">UI Panel 데이터 프로퍼티입니다.</param>
        /// <param name="layoutMode">현재 선택된 레이아웃 모드입니다.</param>
        /// <returns>위치·크기 필드가 차지하는 높이입니다.</returns>
        private static float GetAnimatedLayoutFieldsHeight(
            SerializedProperty uiPanelProperty,
            UiPanelLayoutMode layoutMode)
        {
            return layoutMode == UiPanelLayoutMode.StretchBoth
                ? 0f
                : GetPropertiesHeight(uiPanelProperty, AnimatedLayoutPropertyNames);
        }

        /// <summary>
        /// 레이아웃 모드에 대응하는 Stretch 동작 안내 문구를 반환합니다.
        /// </summary>
        /// <param name="layoutMode">안내할 UI Panel 레이아웃 모드입니다.</param>
        /// <returns>선택한 Stretch 모드의 축별 적용 규칙을 설명하는 문구입니다.</returns>
        private static string GetStretchHelp(UiPanelLayoutMode layoutMode)
        {
            return layoutMode switch
            {
                UiPanelLayoutMode.StretchHorizontal => HorizontalStretchHelp,
                UiPanelLayoutMode.StretchVertical => VerticalStretchHelp,
                UiPanelLayoutMode.StretchBoth => BothStretchHelp,
                _ => string.Empty,
            };
        }

        /// <summary>
        /// 지정한 상대 경로의 프로퍼티를 한 줄 그리고 다음 위치로 이동합니다.
        /// </summary>
        /// <param name="current">현재 그리기 위치이며 다음 위치로 갱신됩니다.</param>
        /// <param name="parent">대상 프로퍼티를 포함한 부모 프로퍼티입니다.</param>
        /// <param name="relativeName">부모 기준 상대 프로퍼티 이름입니다.</param>
        /// <param name="label">기본 표시 이름을 대체할 라벨입니다.</param>
        private static void DrawProperty(
            ref Rect current,
            SerializedProperty parent,
            string relativeName,
            GUIContent label = null)
        {
            CutsceneEventDrawerUiUtil.DrawPropertyLine(
                ref current,
                parent.FindPropertyRelative(relativeName),
                label);
        }

        /// <summary>
        /// Inspector 설정 구간 제목을 그리고 다음 위치로 이동합니다.
        /// </summary>
        /// <param name="current">현재 그리기 위치이며 다음 위치로 갱신됩니다.</param>
        /// <param name="label">표시할 설정 구간 제목입니다.</param>
        private static void DrawSectionHeader(ref Rect current, string label)
        {
            current.height = EditorGUIUtility.singleLineHeight;
            EditorGUI.LabelField(current, label, EditorStyles.boldLabel);
            current.y += current.height + CutsceneEventDrawerUiUtil.VerticalSpacing;
        }

        /// <summary>
        /// 지정한 프로퍼티 한 개가 차지하는 높이와 기본 간격을 계산합니다.
        /// </summary>
        /// <param name="property">높이를 계산할 프로퍼티입니다.</param>
        /// <returns>프로퍼티 높이와 기본 세로 간격의 합입니다.</returns>
        private static float GetPropertyHeight(SerializedProperty property)
        {
            return EditorGUI.GetPropertyHeight(property, true) + CutsceneEventDrawerUiUtil.VerticalSpacing;
        }

        /// <summary>
        /// 여러 상대 프로퍼티가 차지하는 전체 높이를 계산합니다.
        /// </summary>
        /// <param name="parent">대상 프로퍼티를 포함한 부모 프로퍼티입니다.</param>
        /// <param name="relativeNames">높이를 합산할 상대 프로퍼티 이름 목록입니다.</param>
        /// <returns>모든 프로퍼티 높이와 세로 간격의 합입니다.</returns>
        private static float GetPropertiesHeight(SerializedProperty parent, string[] relativeNames)
        {
            float height = 0f;
            for (int i = 0; i < relativeNames.Length; i++)
            {
                height += GetPropertyHeight(parent.FindPropertyRelative(relativeNames[i]));
            }

            return height;
        }

        /// <summary>
        /// 설정 구간 제목 한 줄이 차지하는 높이를 반환합니다.
        /// </summary>
        /// <returns>제목 한 줄 높이와 기본 세로 간격의 합입니다.</returns>
        private static float GetSectionHeaderHeight()
        {
            return EditorGUIUtility.singleLineHeight + CutsceneEventDrawerUiUtil.VerticalSpacing;
        }
    }
}
