using GGemCo2DCore;
using UnityEditor;
using UnityEngine;

namespace GGemCo2DCoreEditor
{
    internal sealed class CutsceneDefaultEventTypeDrawer : ICutsceneEventTypeDrawer
    {
        private readonly string _propertyName;

        public CutsceneEventType EventType { get; }

        public CutsceneDefaultEventTypeDrawer(CutsceneEventType eventType, string propertyName)
        {
            EventType = eventType;
            _propertyName = propertyName;
        }

        public void Draw(Rect position, SerializedProperty eventProperty)
        {
            var payloadProperty = eventProperty.FindPropertyRelative(_propertyName);
            if (payloadProperty == null)
            {
                return;
            }

            EditorGUI.PropertyField(position, payloadProperty, true);
        }

        public float GetHeight(SerializedProperty eventProperty)
        {
            var payloadProperty = eventProperty.FindPropertyRelative(_propertyName);
            return payloadProperty != null
                ? EditorGUI.GetPropertyHeight(payloadProperty, true)
                : EditorGUIUtility.singleLineHeight;
        }
    }
}
