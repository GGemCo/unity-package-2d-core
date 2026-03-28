using GGemCo2DCore;
using UnityEditor;
using UnityEditor.Timeline;
using UnityEngine;
using UnityEngine.Timeline;

namespace GGemCo2DCoreEditor
{
    [CustomEditor(typeof(CutsceneEventClip))]
    public class CutsceneEventClipEditor : Editor
    {
        private const string Title = "연출툴";
        private SerializedProperty _eventsProp;

        private void OnEnable()
        {
            if (CutsceneEventPayloadEditorUtility.EnsurePayloadsForClip(target as CutsceneEventClip))
            {
                EditorUtility.SetDirty(target);
            }

            serializedObject.Update();
            _eventsProp = serializedObject.FindProperty("events");
        }

        public override void OnInspectorGUI()
        {
            if (CutsceneEventPayloadEditorUtility.EnsurePayloadsForClip(target as CutsceneEventClip))
            {
                serializedObject.Update();
            }
            else
            {
                serializedObject.Update();
            }

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
