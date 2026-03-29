using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.Timeline;

namespace GGemCo2DCoreEditor
{
    /// <summary>
    /// TimelineAsset 선택과 JSON 저장 관련 작업을 수행하는 에디터 패널을 제공합니다.
    /// </summary>
    internal sealed class CutsceneTimelinePanel
    {
        /// <summary>
        /// Timeline 선택 UI와 저장 및 에셋 선택 액션 버튼을 그립니다.
        /// </summary>
        /// <param name="state">현재 선택된 TimelineAsset을 포함한 에디터 상태 객체입니다.</param>
        /// <param name="exportSelectedTimelineToCutsceneJson">선택한 TimelineAsset을 컷신 JSON으로 저장하는 콜백입니다.</param>
        /// <param name="pingSelectedTimeline">선택된 TimelineAsset을 프로젝트 창에서 표시하는 콜백입니다.</param>
        public void Draw(
            CutsceneEditorState state,
            Action exportSelectedTimelineToCutsceneJson,
            Action pingSelectedTimeline)
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField("연출 타임라인", EditorStyles.boldLabel);
                EditorGUILayout.HelpBox("선택한 TimelineAsset을 현재 cutscene 테이블 행의 FileName.json으로 바로 내보낼 수 있습니다.", MessageType.Info);

                // 사용자가 내보내기 대상으로 사용할 TimelineAsset을 선택합니다.
                state.SelectedTimelineAsset = (TimelineAsset)EditorGUILayout.ObjectField(
                    "Timeline Asset",
                    state.SelectedTimelineAsset,
                    typeof(TimelineAsset),
                    false);

                using (new EditorGUILayout.HorizontalScope())
                {
                    // 현재 선택된 TimelineAsset을 컷신 JSON으로 저장합니다.
                    if (GUILayout.Button("선택한 Timeline 등록(Json 저장)", GUILayout.Height(24)))
                    {
                        exportSelectedTimelineToCutsceneJson?.Invoke();
                    }

                    // 현재 선택된 TimelineAsset을 프로젝트 창에서 강조 표시합니다.
                    if (GUILayout.Button("Timeline 에셋 선택", GUILayout.Height(24)))
                    {
                        pingSelectedTimeline?.Invoke();
                    }
                }
            }
        }
    }
}