using System;
using System.Collections.Generic;
using System.IO;
using GGemCo2DCore;
using Newtonsoft.Json;
using UnityEditor;
using UnityEngine;
using UnityEngine.Timeline;

namespace GGemCo2DCoreEditor
{
    /// <summary>
    /// Cutscene Timeline <-> Json 변환 공용 유틸리티입니다.
    /// </summary>
    internal static class CutsceneTimelineJsonUtility
    {
        public static bool TryCreateTimelineFromJsonAsset(TextAsset jsonAsset, string timelineAssetPath, out TimelineAsset timelineAsset, out string error)
        {
            timelineAsset = null;
            error = null;

            if (jsonAsset == null)
            {
                error = "JSON 파일이 선택되지 않았습니다.";
                return false;
            }

            try
            {
                var cutsceneData = JsonConvert.DeserializeObject<CutsceneData>(jsonAsset.text);
                if (cutsceneData == null)
                {
                    error = "Json 파싱 결과가 비어 있습니다.";
                    return false;
                }

                return TryCreateTimelineFromData(cutsceneData, timelineAssetPath, out timelineAsset, out error);
            }
            catch (Exception e)
            {
                error = $"Json 파싱 실패: {e.Message}";
                return false;
            }
        }

        public static bool TryCreateTimelineFromData(CutsceneData cutsceneData, string timelineAssetPath, out TimelineAsset timelineAsset, out string error)
        {
            timelineAsset = null;
            error = null;

            if (cutsceneData == null)
            {
                error = "CutsceneData가 null 입니다.";
                return false;
            }

            if (cutsceneData.events == null)
            {
                error = "이벤트 목록이 없습니다.";
                return false;
            }

            try
            {
                EnsureFolderExistsForAssetPath(timelineAssetPath);
                DeleteAssetIfExists(timelineAssetPath);

                timelineAsset = ScriptableObject.CreateInstance<TimelineAsset>();
                AssetDatabase.CreateAsset(timelineAsset, timelineAssetPath);

                var trackMap = new Dictionary<CutsceneEventType, TrackAsset>();
                foreach (var cutsceneEvent in cutsceneData.events)
                {
                    if (cutsceneEvent == null)
                    {
                        continue;
                    }

                    cutsceneEvent.EnsureDataForType();

                    TrackAsset track;
                    if (!trackMap.TryGetValue(cutsceneEvent.type, out track))
                    {
                        track = timelineAsset.CreateTrack<CutsceneEventTrack>(null, $"{cutsceneEvent.type} Track");
                        trackMap.Add(cutsceneEvent.type, track);
                    }

                    var clip = track.CreateClip<CutsceneEventClip>();
                    clip.start = cutsceneEvent.time;
                    clip.duration = cutsceneEvent.duration > 0f ? cutsceneEvent.duration : 1.0f;

                    var clipAsset = clip.asset as CutsceneEventClip;
                    if (clipAsset == null)
                    {
                        continue;
                    }

                    clipAsset.events.Clear();
                    clipAsset.SetEvent(CloneEvent(cutsceneEvent));
                    EditorUtility.SetDirty(clipAsset);
                }

                EditorUtility.SetDirty(timelineAsset);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                return true;
            }
            catch (Exception e)
            {
                error = $"Timeline 생성 실패: {e.Message}";
                return false;
            }
        }

        public static bool TryExportTimelineToJson(TimelineAsset timeline, string jsonPath, out CutsceneData data, out string error)
        {
            data = null;
            error = null;

            if (timeline == null)
            {
                error = "TimelineAsset이 선택되지 않았습니다.";
                return false;
            }

            if (string.IsNullOrWhiteSpace(jsonPath))
            {
                error = "Json 저장 경로가 비어 있습니다.";
                return false;
            }

            try
            {
                var events = CollectEventsFromTimeline(timeline, out error);
                if (events == null)
                {
                    return false;
                }

                data = new CutsceneData
                {
                    duration = events.Count > 0 ? events[events.Count - 1].time + events[events.Count - 1].duration : 0f,
                    events = events
                };

                var directory = Path.GetDirectoryName(jsonPath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                var json = JsonConvert.SerializeObject(
                    data,
                    Formatting.Indented,
                    new JsonSerializerSettings
                    {
                        NullValueHandling = NullValueHandling.Ignore
                    });

                File.WriteAllText(jsonPath, json);
                AssetDatabase.Refresh();
                return true;
            }
            catch (Exception e)
            {
                error = $"Json 저장 실패: {e.Message}";
                return false;
            }
        }

        private static List<CutsceneEvent> CollectEventsFromTimeline(TimelineAsset timeline, out string error)
        {
            error = null;
            var events = new List<CutsceneEvent>();

            foreach (var track in timeline.GetOutputTracks())
            {
                if (!(track is CutsceneEventTrack) || track.muted)
                {
                    continue;
                }

                foreach (var clip in track.GetClips())
                {
                    var cutsceneClip = clip.asset as CutsceneEventClip;
                    if (cutsceneClip == null || cutsceneClip.events == null)
                    {
                        continue;
                    }

                    foreach (var cutsceneEvent in cutsceneClip.events)
                    {
                        if (cutsceneEvent == null)
                        {
                            continue;
                        }

                        cutsceneEvent.EnsureDataForType();
                        if (!ValidateEvent(cutsceneEvent, out error))
                        {
                            return null;
                        }

                        var copy = CloneEvent(cutsceneEvent);
                        copy.time = (float)clip.start;
                        copy.duration = (float)clip.duration;
                        events.Add(copy);
                    }
                }
            }

            events.Sort((a, b) => a.time.CompareTo(b.time));
            return events;
        }

        private static bool ValidateEvent(CutsceneEvent cutsceneEvent, out string error)
        {
            error = null;

            if (cutsceneEvent.type == CutsceneEventType.CharacterMove &&
                cutsceneEvent.characterMove != null &&
                cutsceneEvent.characterMove.characterType == CharacterConstants.Type.None)
            {
                error = $"type: {cutsceneEvent.type} / 캐릭터 타입을 정하지 않았습니다.";
                Debug.LogError(error);
                return false;
            }

            if (cutsceneEvent.type == CutsceneEventType.CameraChangeTarget &&
                cutsceneEvent.cameraChangeTarget != null &&
                cutsceneEvent.cameraChangeTarget.characterType == CharacterConstants.Type.None)
            {
                error = $"type: {cutsceneEvent.type} / 캐릭터 타입을 정하지 않았습니다.";
                Debug.LogError(error);
                return false;
            }

            return true;
        }

        private static CutsceneEvent CloneEvent(CutsceneEvent source)
        {
            if (source == null)
            {
                return null;
            }

            var clone = new CutsceneEvent
            {
                time = source.time,
                duration = source.duration,
                type = source.type,
                cameraMove = source.type == CutsceneEventType.CameraMove ? CloneData(source.cameraMove) : null,
                cameraZoom = source.type == CutsceneEventType.CameraZoom ? CloneData(source.cameraZoom) : null,
                cameraShake = source.type == CutsceneEventType.CameraShake ? CloneData(source.cameraShake) : null,
                cameraChangeTarget = source.type == CutsceneEventType.CameraChangeTarget ? CloneData(source.cameraChangeTarget) : null,
                characterMove = source.type == CutsceneEventType.CharacterMove ? CloneData(source.characterMove) : null,
                characterAnimation = source.type == CutsceneEventType.CharacterAnimation ? CloneData(source.characterAnimation) : null,
                dialogueBalloon = source.type == CutsceneEventType.DialogueBalloon ? CloneData(source.dialogueBalloon) : null,
                screenFade = source.type == CutsceneEventType.ScreenFade ? CloneData(source.screenFade) : null,
                overlayText = source.type == CutsceneEventType.OverlayText ? CloneData(source.overlayText) : null,
                characterWhiteOverlay = source.type == CutsceneEventType.CharacterWhiteOverlay ? CloneData(source.characterWhiteOverlay) : null,
            };

            clone.EnsureDataForType();
            return clone;
        }

        private static T CloneData<T>(T source) where T : class
        {
            if (source == null)
            {
                return null;
            }

            var json = JsonConvert.SerializeObject(source);
            return JsonConvert.DeserializeObject<T>(json);
        }

        private static void EnsureFolderExistsForAssetPath(string assetPath)
        {
            if (string.IsNullOrWhiteSpace(assetPath))
            {
                return;
            }

            var normalizedPath = assetPath.Replace("\\", "/");
            var directoryPath = Path.GetDirectoryName(normalizedPath);
            if (string.IsNullOrWhiteSpace(directoryPath))
            {
                return;
            }

            var segments = directoryPath.Replace("\\", "/").Split('/');
            if (segments.Length == 0 || segments[0] != "Assets")
            {
                return;
            }

            var current = segments[0];
            for (var i = 1; i < segments.Length; i++)
            {
                var next = segments[i];
                var combined = $"{current}/{next}";
                if (!AssetDatabase.IsValidFolder(combined))
                {
                    AssetDatabase.CreateFolder(current, next);
                }

                current = combined;
            }
        }

        private static void DeleteAssetIfExists(string assetPath)
        {
            if (string.IsNullOrWhiteSpace(assetPath))
            {
                return;
            }

            var existing = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(assetPath);
            if (existing != null)
            {
                AssetDatabase.DeleteAsset(assetPath);
            }
        }
    }
}
