using GGemCo2DCore;
using UnityEditor;

namespace GGemCo2DCoreEditor
{
    /// <summary>
    /// <see cref="GGemCoPlayerStatSettings"/> 전용 인스펙터입니다.
    /// </summary>
    [CustomEditor(typeof(GGemCoPlayerStatSettings))]
    public sealed class GGemCoPlayerStatSettingsEditor : UnityEditor.Editor
    {
        /// <summary>
        /// 플레이어 스탯 설정을 기본 IMGUI Inspector로 출력합니다.
        /// </summary>
        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            DrawPropertiesExcluding(serializedObject, "m_Script");
            serializedObject.ApplyModifiedProperties();
        }
    }
}
