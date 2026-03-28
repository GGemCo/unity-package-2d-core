using System;
using System.Collections.Generic;
using GGemCo2DCore;
using Newtonsoft.Json;
using UnityEditor;
using UnityEngine;
using UnityEngine.Timeline;

namespace GGemCo2DCoreEditor
{
    internal static class CutsceneTimelineImportService
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
                var cutsceneData = JsonConvert.DeserializeObject<CutsceneData>(jsonAsset.text, CutsceneJsonSettingsUtility.CutsceneJsonSettings);
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
                CutsceneTimelineAssetUtility.EnsureFolderExistsForAssetPath(timelineAssetPath);
                CutsceneTimelineAssetUtility.DeleteAssetIfExists(timelineAssetPath);

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

                    if (!trackMap.TryGetValue(cutsceneEvent.type, out var track))
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
                    clipAsset.SetEvent(CutsceneTimelineCloneUtility.CloneEvent(cutsceneEvent));
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
    }
}
