using GGemCo2DCore;
using UnityEditor;

namespace GGemCo2DCoreEditor
{
    /// <summary>
    /// <see cref="GGemCoPlayerSettings"/> 전용 인스펙터.
    /// - 기본 Inspector(UI Toolkit ListView) 대신 IMGUI 기반으로 렌더링하여
    ///   Play Mode 종료 시 SerializedObjectList 관련 예외를 회피합니다.
    /// </summary>
    [CustomEditor(typeof(GGemCoPlayerSettings))]
    public sealed class GGemCoPlayerSettingsEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            DrawPropertiesExcluding(serializedObject, "m_Script");
            serializedObject.ApplyModifiedProperties();
        }
    }
}
