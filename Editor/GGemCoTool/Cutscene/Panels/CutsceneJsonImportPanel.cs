using System;
using UnityEditor;
using UnityEngine;

namespace GGemCo2DCoreEditor
{
    /// <summary>
    /// 임의 JSON을 Temp Timeline으로 변환하는 섹션 전용 패널입니다.
    /// </summary>
    internal sealed class CutsceneJsonImportPanel
    {
        public void Draw(CutsceneEditorState state, Action<TextAsset> importJsonToTempTimeline)
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField("JSON -> Timeline 생성", EditorStyles.boldLabel);
                EditorGUILayout.HelpBox("임의의 Json 파일을 선택해 Temp Timeline으로 변환할 수 있습니다.", MessageType.None);

                state.SelectedJson = (TextAsset)EditorGUILayout.ObjectField(
                    "JSON 파일",
                    state.SelectedJson,
                    typeof(TextAsset),
                    false);

                if (GUILayout.Button("선택한 Json으로 Temp Timeline 생성", GUILayout.Height(24)))
                {
                    importJsonToTempTimeline?.Invoke(state.SelectedJson);
                }
            }
        }
    }
}
