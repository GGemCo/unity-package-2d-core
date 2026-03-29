using GGemCo2DCore;
using UnityEditor;
using UnityEditor.Timeline;
using UnityEngine;
using UnityEngine.Timeline;

namespace GGemCo2DCoreEditor
{
    /// <summary>
    /// <see cref="CutsceneEventClip"/> 전용 커스텀 인스펙터입니다.
    /// </summary>
    /// <remarks>
    /// 클립 편집 시 이벤트 Payload가 현재 데이터 구조와 일치하도록 보정하고,
    /// 인스펙터에서 해당 클립이 포함된 Timeline을 JSON으로 내보내는 기능을 제공합니다.
    /// </remarks>
    [CustomEditor(typeof(CutsceneEventClip))]
    public class CutsceneEventClipEditor : Editor
    {
        private const string Title = "연출툴";
        private SerializedProperty _eventsProp;

        /// <summary>
        /// 인스펙터가 활성화될 때 호출되며, Payload 초기화 및 직렬화 프로퍼티를 준비합니다.
        /// </summary>
        private void OnEnable()
        {
            if (CutsceneEventPayloadEditorUtility.EnsurePayloadsForClip(target as CutsceneEventClip))
            {
                EditorUtility.SetDirty(target);
            }

            serializedObject.Update();
            _eventsProp = serializedObject.FindProperty("events");
        }

        /// <summary>
        /// 커스텀 인스펙터 GUI를 그립니다.
        /// </summary>
        /// <remarks>
        /// 이벤트 목록을 편집 가능하게 표시하고,
        /// 현재 클립이 포함된 Timeline을 JSON으로 저장하는 버튼을 제공합니다.
        /// </remarks>
        public override void OnInspectorGUI()
        {
            CutsceneEventPayloadEditorUtility.EnsurePayloadsForClip(target as CutsceneEventClip);
            serializedObject.Update();

            EditorGUILayout.PropertyField(_eventsProp, true);

            EditorGUILayout.HelpBox(
                $"{CutsceneEditorWindow.TempImportFolder} 폴더에 생성된 타임라인 파일을 Hierarchy 탭에 임시로 오브젝트를 생성해야 Json 으로 저장할 수 있습니다.",
                MessageType.Info);

            if (GUILayout.Button("이 클립이 포함된 타임라인을 JSON으로 저장"))
            {
                serializedObject.ApplyModifiedProperties();
                ExportTimelineFromClip();
                return;
            }

            serializedObject.ApplyModifiedProperties();
        }

        /// <summary>
        /// 현재 편집 중인 클립이 포함된 <see cref="TimelineAsset"/>을 JSON 파일로 내보냅니다.
        /// </summary>
        /// <remarks>
        /// Timeline 에디터에서 현재 inspectedDirector를 기준으로 Timeline을 찾고,
        /// 타임라인 이름을 사용해 컷신 JSON 경로를 구성합니다.
        /// </remarks>
        private void ExportTimelineFromClip()
        {
            var timeline = FindTimelineAsset();
            if (timeline == null)
            {
                Debug.LogWarning("TimelineAsset을 찾을 수 없습니다.");
                return;
            }

            var path = $"{ConfigAddressablePath.Narrative.Cutscene}/{timeline.name}.json";
            CutsceneData data;
            string error;

            if (!CutsceneTimelineJsonUtility.TryExportTimelineToJson(timeline, path, out data, out error))
            {
                EditorUtility.DisplayDialog(Title, error, "OK");
                return;
            }

            Debug.Log($"Saved cutscene to: {path}");
            EditorUtility.DisplayDialog(Title, "Json 저장하기 완료", "OK");
        }

        /// <summary>
        /// 현재 Timeline Editor에서 검사 중인 <see cref="TimelineAsset"/>을 찾습니다.
        /// </summary>
        /// <returns>
        /// 현재 inspectedDirector가 보유한 Timeline 에셋입니다.
        /// 찾지 못한 경우 null을 반환합니다.
        /// </returns>
        private TimelineAsset FindTimelineAsset()
        {
            var director = TimelineEditor.inspectedDirector;
            if (director != null && director.playableAsset is TimelineAsset timelineAsset)
            {
                return timelineAsset;
            }

            return null;
        }
    }
}