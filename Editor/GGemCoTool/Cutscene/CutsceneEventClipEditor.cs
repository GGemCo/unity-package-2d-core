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
            EditorGUILayout.PropertyField(_eventsProp, true);

            EditorGUILayout.HelpBox(
                $"{CutsceneEditorWindowWindow.TempImportFolder} 폴더에 생성된 타임라인 파일을 Hierarchy 탭에 임시로 오브젝트를 생성해야 Json 으로 저장할 수 있습니다.",
                MessageType.Info);
            if (GUILayout.Button("이 클립이 포함된 타임라인을 JSON으로 저장"))
            {
                ExportTimelineFromClip();
            }
            
            /*
            var clip = (CutsceneEventClip)target;

            EditorGUILayout.LabelField("Drag a Character object here:");

            Rect dropArea = GUILayoutUtility.GetRect(0, 50, GUILayout.ExpandWidth(true));
            GUI.Box(dropArea, "Drop Character Here", EditorStyles.helpBox);

            Event evt = Event.current;
            if (evt.type == EventType.DragUpdated || evt.type == EventType.DragPerform)
            {
                if (dropArea.Contains(evt.mousePosition))
                {
                    DragAndDrop.visualMode = DragAndDropVisualMode.Copy;

                    if (evt.type == EventType.DragPerform)
                    {
                        DragAndDrop.AcceptDrag();

                        foreach (var obj in DragAndDrop.objectReferences)
                        {
                            GameObject go = obj as GameObject;
                            if (go != null && go.GetComponent<CharacterBase>() != null)
                            {
                                // clip.characterObject.exposedName = UnityEditor.GUID.Generate().ToString();
                                // var director = TimelineEditor.inspectedDirector;
                                // if (director != null)
                                // {
                                //     director.SetReferenceValue(clip.characterObject.exposedName, go);
                                //     Debug.Log("Character assigned: " + go.name);
                                // }

                                EditorUtility.SetDirty(clip);
                            }
                        }

                        evt.Use();
                    }
                }
            }
            */
            serializedObject.ApplyModifiedProperties();
        }
        private void ExportTimelineFromClip()
        {
            // 타임라인 찾기
            TimelineAsset timeline = FindTimelineAsset();
            if (timeline == null)
            {
                Debug.LogWarning("TimelineAsset을 찾을 수 없습니다.");
                return;
            }

            // JSON 저장
            Debug.Log(timeline);
            ExportToJson(timeline);
        }
        private TimelineAsset FindTimelineAsset()
        {
            // 현재 열린 Timeline에서 찾아봄
            var director = TimelineEditor.inspectedDirector;
            if (director != null && director.playableAsset is TimelineAsset timelineAsset)
            {
                return timelineAsset;
            }
    
            // 대체 방법: 강제로 Timeline을 검색하거나 연결
            return null;
        }
        
        private void ExportToJson(TimelineAsset timeline)
        {
            var events = new List<GGemCo2DCore.CutsceneEvent>();

            foreach (var track in timeline.GetOutputTracks())
            {
                if (!(track is GGemCo2DCore.CutsceneEventTrack)) continue;
                
                if (track.muted) continue;
                
                foreach (var clip in track.GetClips())
                {
                    if (clip.asset is GGemCo2DCore.CutsceneEventClip cutsceneClip)
                    {
                        foreach (var e in cutsceneClip.events)
                        {
                            if (e != null &&
                                ((e.type == GGemCo2DCore.CutsceneEventType.CharacterMove &&
                                  e.characterMove.characterType == GGemCo2DCore.CharacterConstants.Type.None)
                                 || (e.type == GGemCo2DCore.CutsceneEventType.CameraChangeTarget &&
                                     e.cameraChangeTarget.characterType == GGemCo2DCore.CharacterConstants.Type.None)
                                ))
                            {
                                Debug.LogError($"type: {e.type} / 캐릭터 타입을 정하지 않았습니다.");
                                continue;
                            }

                            var evtCopy = new GGemCo2DCore.CutsceneEvent
                            {
                                time = (float)(clip.start),
                                duration = (float)clip.duration,
                                type = e.type,
                                cameraMove = e.type == GGemCo2DCore.CutsceneEventType.CameraMove ? e.cameraMove : null,
                                cameraZoom = e.type == GGemCo2DCore.CutsceneEventType.CameraZoom ? e.cameraZoom : null,
                                cameraShake = e.type == GGemCo2DCore.CutsceneEventType.CameraShake ? e.cameraShake : null,
                                cameraChangeTarget = e.type == GGemCo2DCore.CutsceneEventType.CameraChangeTarget ? e.cameraChangeTarget : null,
                                
                                characterMove = e.type == GGemCo2DCore.CutsceneEventType.CharacterMove ? e.characterMove : null,
                                characterAnimation = e.type == GGemCo2DCore.CutsceneEventType.CharacterAnimation ? e.characterAnimation : null,
                                
                                dialogueBalloon = e.type == GGemCo2DCore.CutsceneEventType.DialogueBalloon ? e.dialogueBalloon : null,
                            };
                            events.Add(evtCopy);
                        }
                    }
                }
            }

            events.Sort((a, b) => a.time.CompareTo(b.time));

            var data = new GGemCo2DCore.CutsceneData
            {
                duration = events.Count > 0 ? (events[^1].time + events[^1].duration) : 0f,
                events = events
            };
            
            string json = JsonConvert.SerializeObject(data, Formatting.Indented,
                new JsonSerializerSettings
                {
                    NullValueHandling = NullValueHandling.Ignore
                });

            string path = $"{GGemCo2DCore.ConfigAddressables.PathJsonCutscene}/{timeline.name}.json";
                
            File.WriteAllText(path, json);
            Debug.Log($"Saved cutscene to: {path}");
            AssetDatabase.Refresh();
            EditorUtility.DisplayDialog(Title, "Json 저장하기 완료", "OK");
        }
    }
}