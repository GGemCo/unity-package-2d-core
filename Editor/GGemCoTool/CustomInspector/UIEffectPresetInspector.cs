using GGemCo2DCore;
using UnityEditor;
using UnityEngine;

namespace GGemCo2DCoreEditor
{
    /// <summary>
    /// UIEffectPreset 인스펙터에서 전용 편집 툴을 빠르게 열 수 있도록 지원합니다.
    /// </summary>
    [CustomEditor(typeof(UIEffectPreset))]
    public sealed class UIEffectPresetInspector : Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            EditorGUILayout.Space(8f);
            if (GUILayout.Button("UI 효과 프리셋 편집툴 열기"))
            {
                CreateUIEffectPresetWindow.Open((UIEffectPreset)target);
            }
        }
    }
}
