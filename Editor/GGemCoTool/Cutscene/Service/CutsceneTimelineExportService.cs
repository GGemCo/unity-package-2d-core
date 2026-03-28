using System;
using System.Collections.Generic;
using System.IO;
using GGemCo2DCore;
using Newtonsoft.Json;
using UnityEditor;
using UnityEngine.Timeline;

namespace GGemCo2DCoreEditor
{
    internal static class CutsceneTimelineExportService
    {
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
                    events = events,
                };

                var directory = Path.GetDirectoryName(jsonPath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                var json = JsonConvert.SerializeObject(data, Formatting.Indented, CutsceneJsonSettingsUtility.CutsceneJsonSettings);
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
                        if (!CutsceneTimelineValidationUtility.ValidateEvent(cutsceneEvent, out error))
                        {
                            return null;
                        }

                        var copy = CutsceneTimelineCloneUtility.CloneEvent(cutsceneEvent);
                        copy.time = (float)clip.start;
                        copy.duration = (float)clip.duration;
                        events.Add(copy);
                    }
                }
            }

            events.Sort((a, b) => a.time.CompareTo(b.time));
            return events;
        }
    }
}
