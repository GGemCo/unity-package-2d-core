using GGemCo2DCore;
using UnityEditor;
using UnityEngine;

namespace GGemCo2DCoreEditor
{
    internal interface ICutsceneEventTypeDrawer
    {
        CutsceneEventType EventType { get; }
        void Draw(Rect position, SerializedProperty eventProperty);
        float GetHeight(SerializedProperty eventProperty);
    }
}
