using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.Timeline;

namespace GGemCo2DCoreEditor
{
    /// <summary>
    /// Timeline 선택/등록 관련 섹션만 담당하는 패널입니다.
    /// </summary>
    internal sealed class CutsceneTimelinePanel
    {
        public void Draw(
            CutsceneEditorState state,
            Action exportSelectedTimelineToCutsceneJson,
            Action pingSelectedTimeline)
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField("연출 타임라인", EditorStyles.boldLabel);
                EditorGUILayout.HelpBox("선택한 TimelineAsset을 현재 cutscene 테이블 행의 FileName.json으로 바로 내보낼 수 있습니다.", MessageType.Info);

                state.SelectedTimelineAsset = (TimelineAsset)EditorGUILayout.ObjectField(
                    "Timeline Asset",
                    state.SelectedTimelineAsset,
                    typeof(TimelineAsset),
                    false);

                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("선택한 Timeline 등록(Json 저장)", GUILayout.Height(24)))
                    {
                        exportSelectedTimelineToCutsceneJson?.Invoke();
                    }

                    if (GUILayout.Button("Timeline 에셋 선택", GUILayout.Height(24)))
                    {
                        pingSelectedTimeline?.Invoke();
                    }
                }
            }
        }
    }
}
