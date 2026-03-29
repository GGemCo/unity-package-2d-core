using System;
using UnityEditor;
using UnityEngine;

namespace GGemCo2DCoreEditor
{
    /// <summary>
    /// 임의의 JSON 파일을 선택하여 Temp Timeline으로 변환하는 에디터 패널을 제공합니다.
    /// </summary>
    internal sealed class CutsceneJsonImportPanel
    {
        /// <summary>
        /// JSON 선택 및 Temp Timeline 생성 UI를 그립니다.
        /// </summary>
        /// <param name="state">에디터 상태로, 선택된 JSON 파일을 저장 및 참조합니다.</param>
        /// <param name="importJsonToTempTimeline">선택된 JSON을 Temp Timeline으로 변환하는 콜백입니다.</param>
        public void Draw(CutsceneEditorState state, Action<TextAsset> importJsonToTempTimeline)
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                // 패널 제목
                EditorGUILayout.LabelField("JSON -> Timeline 생성", EditorStyles.boldLabel);

                // 기능 설명
                EditorGUILayout.HelpBox("임의의 Json 파일을 선택해 Temp Timeline으로 변환할 수 있습니다.", MessageType.None);

                // JSON 파일 선택 필드
                state.SelectedJson = (TextAsset)EditorGUILayout.ObjectField(
                    "JSON 파일",
                    state.SelectedJson,
                    typeof(TextAsset),
                    false);

                // 변환 실행 버튼
                using (new EditorGUI.DisabledScope(state.SelectedJson == null))
                {
                    if (GUILayout.Button("선택한 Json으로 Temp Timeline 생성", GUILayout.Height(24)))
                    {
                        // 선택된 JSON을 기반으로 Temp Timeline 생성 콜백 실행
                        importJsonToTempTimeline?.Invoke(state.SelectedJson);
                    }
                }
            }
        }
    }
}