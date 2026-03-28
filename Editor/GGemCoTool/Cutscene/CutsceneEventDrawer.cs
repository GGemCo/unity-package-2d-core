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
        internal const float VerticalSpacing = 2f;
        private const string MissingPayloadMessage = "현재 이벤트 타입에 필요한 데이터 payload 가 없습니다. 클립을 다시 선택하거나 타입을 다시 설정해서 payload 를 초기화해주세요.";

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            var typeProp = property.FindPropertyRelative("type");
            var eventType = (CutsceneEventType)typeProp.enumValueIndex;

            Rect current = position;
            current.height = EditorGUIUtility.singleLineHeight;

            EditorGUI.BeginChangeCheck();
            EditorGUI.PropertyField(current, typeProp);
            if (EditorGUI.EndChangeCheck())
            {
                property.serializedObject.ApplyModifiedProperties();
                eventType = (CutsceneEventType)typeProp.enumValueIndex;
                CutsceneEventPayloadEditorUtility.EnsurePayloadForTypeChange(property, eventType);
            }

            current.y += current.height + VerticalSpacing;

            if (!CutsceneEventPayloadEditorUtility.HasActivePayload(property, eventType))
            {
                EditorGUI.HelpBox(current, MissingPayloadMessage, MessageType.Warning);
                EditorGUI.EndProperty();
                return;
            }

            var drawer = CutsceneEventDrawerRegistry.Get(eventType);
            drawer.Draw(current, property);

            EditorGUI.EndProperty();
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            var typeProp = property.FindPropertyRelative("type");
            var eventType = (CutsceneEventType)typeProp.enumValueIndex;

            float baseHeight = EditorGUIUtility.singleLineHeight * 2f + 6f;
            if (!CutsceneEventPayloadEditorUtility.HasActivePayload(property, eventType))
            {
                return baseHeight + EditorStyles.helpBox.CalcHeight(new GUIContent(MissingPayloadMessage), EditorGUIUtility.currentViewWidth - 80f);
            }

            return baseHeight + CutsceneEventDrawerRegistry.Get(eventType).GetHeight(property);
        }
    }
}
