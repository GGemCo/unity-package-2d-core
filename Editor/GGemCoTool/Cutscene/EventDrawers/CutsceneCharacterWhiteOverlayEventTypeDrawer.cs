using GGemCo2DCore;
using UnityEditor;
using UnityEngine;

namespace GGemCo2DCoreEditor
{
    /// <summary>
    /// CharacterWhiteOverlay 이벤트에 대한 Inspector UI를 렌더링하는 Drawer입니다.
    /// 캐릭터 대상 지정 모드에 따라 고정 대상 또는 런타임 키 입력 필드를 동적으로 구성합니다.
    /// </summary>
    internal sealed class CutsceneCharacterWhiteOverlayEventTypeDrawer : ICutsceneEventTypeDrawer
    {
        public CutsceneEventType EventType => CutsceneEventType.CharacterWhiteOverlay;

        public void Draw(Rect position, SerializedProperty eventProperty)
        {
            var whiteOverlayProp = eventProperty.FindPropertyRelative("characterWhiteOverlay");
            if (whiteOverlayProp == null)
            {
                return;
            }

            var targetProp = whiteOverlayProp.FindPropertyRelative("target");
            if (targetProp == null)
            {
                EditorGUI.PropertyField(position, whiteOverlayProp, true);
                return;
            }

            Rect current = position;
            current.height = EditorGUIUtility.singleLineHeight;

            EditorGUI.LabelField(current, whiteOverlayProp.displayName, EditorStyles.boldLabel);
            current.y += current.height + CutsceneEventDrawerUiUtil.VerticalSpacing;

            int originalIndent = EditorGUI.indentLevel;
            EditorGUI.indentLevel++;

            var sourceModeProp = targetProp.FindPropertyRelative("sourceMode");
            var characterTypeProp = targetProp.FindPropertyRelative("characterType");
            var characterUidProp = targetProp.FindPropertyRelative("characterUid");
            var runtimeTargetKeyProp = targetProp.FindPropertyRelative("runtimeTargetKey");

            CutsceneEventDrawerUiUtil.DrawPropertyLine(ref current, sourceModeProp);

            var sourceMode = (CutsceneCharacterTargetSourceMode)sourceModeProp.enumValueIndex;
            if (sourceMode == CutsceneCharacterTargetSourceMode.RuntimeOverride)
            {
                CutsceneEventDrawerUiUtil.DrawPropertyLine(ref current, runtimeTargetKeyProp);
            }
            else
            {
                CutsceneEventDrawerUiUtil.DrawPropertyLine(ref current, characterTypeProp);
                if ((CharacterConstants.Type)characterTypeProp.enumValueIndex != CharacterConstants.Type.Player)
                {
                    CutsceneEventDrawerUiUtil.DrawPropertyLine(ref current, characterUidProp);
                }
            }

            CutsceneEventDrawerUiUtil.DrawPropertyLine(ref current, whiteOverlayProp.FindPropertyRelative("color"));
            CutsceneEventDrawerUiUtil.DrawPropertyLine(ref current, whiteOverlayProp.FindPropertyRelative("fromStrength"));
            CutsceneEventDrawerUiUtil.DrawPropertyLine(ref current, whiteOverlayProp.FindPropertyRelative("toStrength"));
            CutsceneEventDrawerUiUtil.DrawPropertyLine(ref current, whiteOverlayProp.FindPropertyRelative("restoreOnStop"));
            CutsceneEventDrawerUiUtil.DrawPropertyLine(ref current, whiteOverlayProp.FindPropertyRelative("refreshTargetsOnTrigger"));
            CutsceneEventDrawerUiUtil.DrawPropertyLine(ref current, whiteOverlayProp.FindPropertyRelative("useUnscaledTime"));
            CutsceneEventDrawerUiUtil.DrawPropertyLine(ref current, whiteOverlayProp.FindPropertyRelative("easing"));

            EditorGUI.indentLevel = originalIndent;
        }

        public float GetHeight(SerializedProperty eventProperty)
        {
            var whiteOverlayProp = eventProperty.FindPropertyRelative("characterWhiteOverlay");
            if (whiteOverlayProp == null)
            {
                return EditorGUIUtility.singleLineHeight;
            }

            var targetProp = whiteOverlayProp.FindPropertyRelative("target");
            if (targetProp == null)
            {
                return EditorGUI.GetPropertyHeight(whiteOverlayProp, true);
            }

            float height = CutsceneEventDrawerUiUtil.GetLabeledGroupBaseHeight();

            var sourceModeProp = targetProp.FindPropertyRelative("sourceMode");
            var characterTypeProp = targetProp.FindPropertyRelative("characterType");
            var runtimeTargetKeyProp = targetProp.FindPropertyRelative("runtimeTargetKey");
            var characterUidProp = targetProp.FindPropertyRelative("characterUid");

            height += EditorGUI.GetPropertyHeight(sourceModeProp, true) + CutsceneEventDrawerUiUtil.VerticalSpacing;

            var sourceMode = (CutsceneCharacterTargetSourceMode)sourceModeProp.enumValueIndex;
            if (sourceMode == CutsceneCharacterTargetSourceMode.RuntimeOverride)
            {
                height += EditorGUI.GetPropertyHeight(runtimeTargetKeyProp, true) + CutsceneEventDrawerUiUtil.VerticalSpacing;
            }
            else
            {
                height += EditorGUI.GetPropertyHeight(characterTypeProp, true) + CutsceneEventDrawerUiUtil.VerticalSpacing;
                if ((CharacterConstants.Type)characterTypeProp.enumValueIndex != CharacterConstants.Type.Player)
                {
                    height += EditorGUI.GetPropertyHeight(characterUidProp, true) + CutsceneEventDrawerUiUtil.VerticalSpacing;
                }
            }

            height += EditorGUI.GetPropertyHeight(whiteOverlayProp.FindPropertyRelative("color"), true) + CutsceneEventDrawerUiUtil.VerticalSpacing;
            height += EditorGUI.GetPropertyHeight(whiteOverlayProp.FindPropertyRelative("fromStrength"), true) + CutsceneEventDrawerUiUtil.VerticalSpacing;
            height += EditorGUI.GetPropertyHeight(whiteOverlayProp.FindPropertyRelative("toStrength"), true) + CutsceneEventDrawerUiUtil.VerticalSpacing;
            height += EditorGUI.GetPropertyHeight(whiteOverlayProp.FindPropertyRelative("restoreOnStop"), true) + CutsceneEventDrawerUiUtil.VerticalSpacing;
            height += EditorGUI.GetPropertyHeight(whiteOverlayProp.FindPropertyRelative("refreshTargetsOnTrigger"), true) + CutsceneEventDrawerUiUtil.VerticalSpacing;
            height += EditorGUI.GetPropertyHeight(whiteOverlayProp.FindPropertyRelative("useUnscaledTime"), true) + CutsceneEventDrawerUiUtil.VerticalSpacing;
            height += EditorGUI.GetPropertyHeight(whiteOverlayProp.FindPropertyRelative("easing"), true);

            return height;
        }
    }
}
