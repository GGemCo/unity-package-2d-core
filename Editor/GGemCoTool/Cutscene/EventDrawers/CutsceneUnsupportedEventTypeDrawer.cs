using GGemCo2DCore;
using UnityEditor;
using UnityEngine;

namespace GGemCo2DCoreEditor
{
    internal sealed class CutsceneUnsupportedEventTypeDrawer : ICutsceneEventTypeDrawer
    {
        public CutsceneEventType EventType => (CutsceneEventType)(-1);

        public void Draw(Rect position, SerializedProperty eventProperty)
        {
            EditorGUI.HelpBox(position, "지원되지 않는 CutsceneEventType 입니다.", MessageType.Warning);
        }

        public float GetHeight(SerializedProperty eventProperty)
        {
            return EditorStyles.helpBox.CalcHeight(new GUIContent("지원되지 않는 CutsceneEventType 입니다."), EditorGUIUtility.currentViewWidth - 80f);
        }
    }
}
