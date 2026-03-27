using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using UnityEditor;
using UnityEditor.Timeline;
using UnityEngine;
using UnityEngine.Timeline;

namespace GGemCo2DCoreEditor
{
    [CustomEditor(typeof(GGemCo2DCore.CutsceneEventClip))]
    public class CutsceneEventClipEditor : Editor
    {
        private const string Title = "연출툴";
        private SerializedProperty _eventsProp;

        private void OnEnable()
        {
            serializedObject.Update();
            _eventsProp = serializedObject.FindProperty("events");
        }

        public override void OnInspectorGUI()
        {
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

        private void ExportTimelineFromClip()
        {
            TimelineAsset timeline = FindTimelineAsset();
            if (timeline == null)
            {
                Debug.LogWarning("TimelineAsset을 찾을 수 없습니다.");
                return;
            }

            ExportToJson(timeline);
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

        private void ExportToJson(TimelineAsset timeline)
        {
            var events = new List<GGemCo2DCore.CutsceneEvent>();

            foreach (var track in timeline.GetOutputTracks())
            {
                if (track is not GGemCo2DCore.CutsceneEventTrack)
                {
                    continue;
                }

                if (track.muted)
                {
                    continue;
                }

                foreach (var clip in track.GetClips())
                {
                    if (clip.asset is not GGemCo2DCore.CutsceneEventClip cutsceneClip)
                    {
                        continue;
                    }

                    foreach (var e in cutsceneClip.events)
                    {
                        if (e == null)
                        {
                            continue;
                        }

                        e.EnsureDataForType();

                        if (!ValidateEvent(e))
                        {
                            continue;
                        }

                        var evtCopy = new GGemCo2DCore.CutsceneEvent
                        {
                            time = (float)clip.start,
                            duration = (float)clip.duration,
                            type = e.type,
                            cameraMove = e.type == GGemCo2DCore.CutsceneEventType.CameraMove ? CloneData(e.cameraMove) : null,
                            cameraZoom = e.type == GGemCo2DCore.CutsceneEventType.CameraZoom ? CloneData(e.cameraZoom) : null,
                            cameraShake = e.type == GGemCo2DCore.CutsceneEventType.CameraShake ? CloneData(e.cameraShake) : null,
                            cameraChangeTarget = e.type == GGemCo2DCore.CutsceneEventType.CameraChangeTarget ? CloneData(e.cameraChangeTarget) : null,
                            characterMove = e.type == GGemCo2DCore.CutsceneEventType.CharacterMove ? CloneData(e.characterMove) : null,
                            characterAnimation = e.type == GGemCo2DCore.CutsceneEventType.CharacterAnimation ? CloneData(e.characterAnimation) : null,
                            dialogueBalloon = e.type == GGemCo2DCore.CutsceneEventType.DialogueBalloon ? CloneData(e.dialogueBalloon) : null,
                            screenFade = e.type == GGemCo2DCore.CutsceneEventType.ScreenFade ? CloneData(e.screenFade) : null,
                            overlayText = e.type == GGemCo2DCore.CutsceneEventType.OverlayText ? CloneData(e.overlayText) : null,
                            characterWhiteOverlay = e.type == GGemCo2DCore.CutsceneEventType.CharacterWhiteOverlay ? CloneData(e.characterWhiteOverlay) : null,
                        };

                        evtCopy.EnsureDataForType();
                        events.Add(evtCopy);
                    }
                }
            }

            events.Sort((a, b) => a.time.CompareTo(b.time));

            var data = new GGemCo2DCore.CutsceneData
            {
                duration = events.Count > 0 ? events[^1].time + events[^1].duration : 0f,
                events = events
            };

            string json = JsonConvert.SerializeObject(
                data,
                Formatting.Indented,
                new JsonSerializerSettings
                {
                    NullValueHandling = NullValueHandling.Ignore
                });

            string path = $"{GGemCo2DCore.ConfigAddressablePath.Narrative.Cutscene}/{timeline.name}.json";

            File.WriteAllText(path, json);
            Debug.Log($"Saved cutscene to: {path}");
            AssetDatabase.Refresh();
            EditorUtility.DisplayDialog(Title, "Json 저장하기 완료", "OK");
        }

        private static bool ValidateEvent(GGemCo2DCore.CutsceneEvent cutsceneEvent)
        {
            if (cutsceneEvent.type == GGemCo2DCore.CutsceneEventType.CharacterMove &&
                cutsceneEvent.characterMove != null &&
                cutsceneEvent.characterMove.characterType == GGemCo2DCore.CharacterConstants.Type.None)
            {
                Debug.LogError($"type: {cutsceneEvent.type} / 캐릭터 타입을 정하지 않았습니다.");
                return false;
            }

            if (cutsceneEvent.type == GGemCo2DCore.CutsceneEventType.CameraChangeTarget &&
                cutsceneEvent.cameraChangeTarget != null &&
                cutsceneEvent.cameraChangeTarget.characterType == GGemCo2DCore.CharacterConstants.Type.None)
            {
                Debug.LogError($"type: {cutsceneEvent.type} / 캐릭터 타입을 정하지 않았습니다.");
                return false;
            }

            return true;
        }

        private static T CloneData<T>(T source) where T : class
        {
            if (source == null)
            {
                return null;
            }

            string json = JsonConvert.SerializeObject(source);
            return JsonConvert.DeserializeObject<T>(json);
        }
    }
}
