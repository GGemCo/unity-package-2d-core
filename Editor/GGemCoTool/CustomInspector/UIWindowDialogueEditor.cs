using System;
using System.Collections.Generic;
using GGemCo2DCore;
using UnityEditor;

namespace GGemCo2DCoreEditor
{
    /// <summary>
    /// <see cref="UIWindowDialogue"/>의 시각 모드에 필요한 속성만 표시하는 전용 인스펙터입니다.
    /// </summary>
    [CustomEditor(typeof(UIWindowDialogue))]
    public sealed class UIWindowDialogueEditor : Editor
    {
        private const string SpeechBubbleModeName = "SpeechBubble";

        private static readonly HashSet<string> SpeechBubbleOnlyPropertyNames = new()
        {
            "speechBubbleWorldOffset",
            "speechBubbleWorldOffsetXPolicy",
            "useProjectEnterIndicatorDefaultsInSpeechBubble",
            "imageEnter",
            "enterIndicatorSpriteOverride",
            "enterIndicatorGapPx",
            "enterIndicatorBlinkHz",
            "enterIndicatorMinAlpha",
            "useLegacyThumbnailFallbackForNone",
            "thumbnailGapPx",
            "textPaddingOnNonThumbnailSidePx",
            "textPaddingOnThumbnailSidePx",
            "useSymmetricLayoutByTail",
            "tailForwardOffsetPx",
            "minHalfExtentByTailPx",
            "imageTail",
        };

        private SerializedProperty _dialogueVisualMode;

        /// <summary>
        /// 직렬화된 시각 모드 속성을 캐시합니다.
        /// </summary>
        private void OnEnable()
        {
            _dialogueVisualMode = serializedObject.FindProperty("dialogueVisualMode");
        }

        /// <summary>
        /// 현재 시각 모드에 맞춰 직렬화된 속성을 필터링하여 표시합니다.
        /// </summary>
        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            DrawFilteredProperties(IsSpeechBubbleMode());
            serializedObject.ApplyModifiedProperties();
        }

        /// <summary>
        /// 말풍선 전용 속성의 표시 여부를 적용하여 인스펙터를 그립니다.
        /// </summary>
        /// <param name="isSpeechBubbleMode">현재 말풍선 모드인지 여부입니다.</param>
        private void DrawFilteredProperties(bool isSpeechBubbleMode)
        {
            SerializedProperty iterator = serializedObject.GetIterator();
            bool enterChildren = true;
            while (iterator.NextVisible(enterChildren))
            {
                enterChildren = false;
                if (iterator.name == "m_Script")
                {
                    using (new EditorGUI.DisabledScope(true))
                    {
                        EditorGUILayout.PropertyField(iterator, true);
                    }

                    continue;
                }

                if (!isSpeechBubbleMode &&
                    SpeechBubbleOnlyPropertyNames.Contains(iterator.name))
                {
                    continue;
                }

                EditorGUILayout.PropertyField(iterator, true);
            }
        }

        /// <summary>
        /// 현재 직렬화된 시각 모드가 SpeechBubble인지 확인합니다.
        /// </summary>
        /// <returns>말풍선 모드이면 <see langword="true"/>입니다.</returns>
        private bool IsSpeechBubbleMode()
        {
            if (_dialogueVisualMode == null ||
                _dialogueVisualMode.propertyType != SerializedPropertyType.Enum)
            {
                return false;
            }

            int enumIndex = _dialogueVisualMode.enumValueIndex;
            return enumIndex >= 0 &&
                   enumIndex < _dialogueVisualMode.enumNames.Length &&
                   string.Equals(
                       _dialogueVisualMode.enumNames[enumIndex],
                       SpeechBubbleModeName,
                       StringComparison.Ordinal);
        }
    }
}
