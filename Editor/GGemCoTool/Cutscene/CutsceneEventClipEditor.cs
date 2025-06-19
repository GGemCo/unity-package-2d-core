using System.Collections.Generic;
using System.IO;
using GGemCo.Scripts;
using Newtonsoft.Json;
using UnityEditor;
using UnityEditor.Timeline;
using UnityEngine;
using UnityEngine.Timeline;

namespace GGemCo.Editor
{
    [CustomEditor(typeof(CutsceneEventClip))]
    public class CutsceneEventClipEditor : UnityEditor.Editor
    {
        private const string Title = "연출툴";
        private SerializedProperty eventsProp;
        private readonly string jsonFolderPath = Application.dataPath+"/Resources/Cutscene/";

        private void OnEnable()
        {
            serializedObject.Update();
            eventsProp = serializedObject.FindProperty("events");
        }

        public override void OnInspectorGUI()
        {
            EditorGUILayout.PropertyField(eventsProp, true);
            
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
                GcLogger.LogWarning("TimelineAsset을 찾을 수 없습니다.");
                return;
            }

            // JSON 저장
            GcLogger.Log(timeline);
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
            var events = new List<CutsceneEvent>();

            foreach (var track in timeline.GetOutputTracks())
            {
                if (!(track is CutsceneEventTrack)) continue;
                
                if (track.muted) continue;
                
                foreach (var clip in track.GetClips())
                {
                    if (clip.asset is CutsceneEventClip cutsceneClip)
                    {
                        foreach (var e in cutsceneClip.events)
                        {
                            if (e != null &&
                                ((e.type == CutsceneEventType.CharacterMove &&
                                  e.characterMove.characterType == CharacterConstants.Type.None)
                                 || (e.type == CutsceneEventType.CameraChangeTarget &&
                                     e.cameraChangeTarget.characterType == CharacterConstants.Type.None)
                                ))
                            {
                                GcLogger.LogError($"type: {e.type} / 캐릭터 타입을 정하지 않았습니다.");
                                continue;
                            }

                            var evtCopy = new CutsceneEvent
                            {
                                time = (float)(clip.start),
                                duration = (float)clip.duration,
                                type = e.type,
                                cameraMove = e.type == CutsceneEventType.CameraMove ? e.cameraMove : null,
                                cameraZoom = e.type == CutsceneEventType.CameraZoom ? e.cameraZoom : null,
                                cameraShake = e.type == CutsceneEventType.CameraShake ? e.cameraShake : null,
                                cameraChangeTarget = e.type == CutsceneEventType.CameraChangeTarget ? e.cameraChangeTarget : null,
                                
                                characterMove = e.type == CutsceneEventType.CharacterMove ? e.characterMove : null,
                                characterAnimation = e.type == CutsceneEventType.CharacterAnimation ? e.characterAnimation : null,
                                
                                dialogueBalloon = e.type == CutsceneEventType.DialogueBalloon ? e.dialogueBalloon : null,
                            };
                            events.Add(evtCopy);
                        }
                    }
                }
            }

            events.Sort((a, b) => a.time.CompareTo(b.time));

            var data = new CutsceneData
            {
                duration = events.Count > 0 ? (events[^1].time + events[^1].duration) : 0f,
                events = events
            };
            
            string json = JsonConvert.SerializeObject(data, Formatting.Indented,
                new JsonSerializerSettings
                {
                    NullValueHandling = NullValueHandling.Ignore
                });

            // string path = EditorUtility.SaveFilePanel("Save Cutscene JSON", "", "Cutscene.json", "json");
            string path = Path.Combine(jsonFolderPath, timeline.name + ".json");
            File.WriteAllText(path, json);
            Debug.Log($"Saved cutscene to: {path}");
            AssetDatabase.Refresh();
            EditorUtility.DisplayDialog(Title, "Json 저장하기 완료", "OK");
        }
    }
}