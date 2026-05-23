using System;
using System.Collections.Generic;
using GGemCo2DCore;
using UnityEditor;

namespace GGemCo2DCoreEditor
{
    /// <summary>
    /// <see cref="UIWindowInteractionDialogue"/> 전용 인스펙터입니다.
    /// DialogueVisualMode 값에 따라 현재 모드에 필요한 필드만 표시합니다.
    /// </summary>
    [CustomEditor(typeof(UIWindowInteractionDialogue))]
    public sealed class UIWindowInteractionDialogueEditor : Editor
    {
        /// <summary>
        /// enum 이름 비교 시 사용할 말풍선 모드 식별자입니다.
        /// </summary>
        private const string SpeechBubbleModeName = "SpeechBubble";

        /// <summary>
        /// 대화 박스 모드에서 숨길 말풍선 전용 프로퍼티 목록입니다.
        /// </summary>
        private static readonly HashSet<string> SpeechBubbleOnlyPropertyNames = new()
        {
            "useLegacyThumbnailFallbackForNone",
            "thumbnailGapPx",
            "textPaddingOnNonThumbnailSidePx",
            "textPaddingOnThumbnailSidePx",
            "useSymmetricLayoutByTail",
            "tailForwardOffsetPx",
            "minHalfExtentByTailPx",
            "imageTail",
            "thumbnailFlipPolicy",
            "thumbnailSourceFacing",
        };

        private SerializedProperty _dialogueVisualMode;

        /// <summary>
        /// 인스펙터 초기화 시 모드 판별용 프로퍼티를 캐시합니다.
        /// </summary>
        private void OnEnable()
        {
            _dialogueVisualMode = serializedObject.FindProperty("dialogueVisualMode");
        }

        /// <summary>
        /// 모드별 필드 필터링 규칙을 적용해 인스펙터를 렌더링합니다.
        /// </summary>
        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            bool isSpeechBubbleMode = IsSpeechBubbleMode();
            DrawFilteredProperties(isSpeechBubbleMode);

            serializedObject.ApplyModifiedProperties();
        }

        /// <summary>
        /// 현재 모드에 맞게 표시 대상을 필터링해 모든 직렬화 프로퍼티를 그립니다.
        /// </summary>
        /// <param name="isSpeechBubbleMode">말풍선 모드 여부입니다.</param>
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
                        EditorGUILayout.PropertyField(iterator, includeChildren: true);
                    }

                    continue;
                }

                if (ShouldHideProperty(iterator.name, isSpeechBubbleMode))
                {
                    continue;
                }

                EditorGUILayout.PropertyField(iterator, includeChildren: true);
            }
        }

        /// <summary>
        /// 현재 선택된 DialogueVisualMode가 SpeechBubble인지 확인합니다.
        /// </summary>
        /// <returns>말풍선 모드면 <see langword="true"/>를 반환합니다.</returns>
        private bool IsSpeechBubbleMode()
        {
            if (_dialogueVisualMode == null || _dialogueVisualMode.propertyType != SerializedPropertyType.Enum)
            {
                return false;
            }

            int enumIndex = _dialogueVisualMode.enumValueIndex;
            if (enumIndex < 0 || enumIndex >= _dialogueVisualMode.enumNames.Length)
            {
                return false;
            }

            string currentModeName = _dialogueVisualMode.enumNames[enumIndex];
            return string.Equals(currentModeName, SpeechBubbleModeName, StringComparison.Ordinal);
        }

        /// <summary>
        /// 현재 모드에서 특정 프로퍼티를 숨겨야 하는지 판단합니다.
        /// </summary>
        /// <param name="propertyName">검사할 프로퍼티 이름입니다.</param>
        /// <param name="isSpeechBubbleMode">말풍선 모드 여부입니다.</param>
        /// <returns>숨겨야 하면 <see langword="true"/>를 반환합니다.</returns>
        private static bool ShouldHideProperty(string propertyName, bool isSpeechBubbleMode)
        {
            if (isSpeechBubbleMode)
            {
                return false;
            }

            return SpeechBubbleOnlyPropertyNames.Contains(propertyName);
        }
    }
}
