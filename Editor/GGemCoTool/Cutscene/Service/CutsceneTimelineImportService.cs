using System;
using System.Collections.Generic;
using GGemCo2DCore;
using Newtonsoft.Json;
using UnityEditor;
using UnityEngine;
using UnityEngine.Timeline;

namespace GGemCo2DCoreEditor
{
    /// <summary>
    /// 컷신 JSON 또는 <see cref="CutsceneData"/>를 기반으로 TimelineAsset을 생성하는 기능을 제공합니다.
    /// </summary>
    internal static class CutsceneTimelineImportService
    {
        /// <summary>
        /// JSON 에셋을 역직렬화하여 TimelineAsset을 생성합니다.
        /// </summary>
        /// <param name="jsonAsset">컷신 데이터가 포함된 JSON 텍스트 에셋입니다.</param>
        /// <param name="timelineAssetPath">생성할 TimelineAsset의 저장 경로입니다.</param>
        /// <param name="timelineAsset">생성에 성공한 TimelineAsset입니다.</param>
        /// <param name="error">실패 시 반환할 오류 메시지입니다.</param>
        /// <returns>Timeline 생성에 성공하면 <see langword="true"/>, 그렇지 않으면 <see langword="false"/>입니다.</returns>
        /// <exception cref="JsonException">JSON 역직렬화 중 형식 오류가 있을 경우 발생할 수 있습니다.</exception>
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
                // JSON 텍스트를 컷신 데이터 객체로 변환합니다.
                var cutsceneData = JsonConvert.DeserializeObject<CutsceneData>(jsonAsset.text, CutsceneJsonSettingsUtility.CutsceneJsonSettings);
                if (cutsceneData == null)
                {
                    error = "Json 파싱 결과가 비어 있습니다.";
                    return false;
                }

                // 역직렬화한 데이터를 기반으로 TimelineAsset 생성을 위임합니다.
                return TryCreateTimelineFromData(cutsceneData, timelineAssetPath, out timelineAsset, out error);
            }
            catch (Exception e)
            {
                error = $"Json 파싱 실패: {e.Message}";
                return false;
            }
        }

        /// <summary>
        /// 컷신 데이터의 이벤트 목록을 기반으로 TimelineAsset과 이벤트 트랙 및 클립을 생성합니다.
        /// </summary>
        /// <param name="cutsceneData">타임라인으로 변환할 컷신 데이터입니다.</param>
        /// <param name="timelineAssetPath">생성할 TimelineAsset의 저장 경로입니다.</param>
        /// <param name="timelineAsset">생성에 성공한 TimelineAsset입니다.</param>
        /// <param name="error">실패 시 반환할 오류 메시지입니다.</param>
        /// <returns>Timeline 생성에 성공하면 <see langword="true"/>, 그렇지 않으면 <see langword="false"/>입니다.</returns>
        /// <exception cref="InvalidOperationException">에셋 생성 또는 트랙/클립 생성 과정에서 Unity 에디터 상태에 따라 발생할 수 있습니다.</exception>
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
                // 타임라인 에셋이 저장될 폴더를 보장하고, 같은 경로의 기존 에셋이 있으면 제거합니다.
                CutsceneTimelineAssetUtility.EnsureFolderExistsForAssetPath(timelineAssetPath);
                CutsceneTimelineAssetUtility.DeleteAssetIfExists(timelineAssetPath);

                // 새 TimelineAsset을 생성하여 지정한 경로에 저장합니다.
                timelineAsset = ScriptableObject.CreateInstance<TimelineAsset>();
                AssetDatabase.CreateAsset(timelineAsset, timelineAssetPath);

                // 이벤트 타입별로 하나의 트랙을 재사용하기 위한 맵입니다.
                var trackMap = new Dictionary<CutsceneEventType, TrackAsset>();
                foreach (var cutsceneEvent in cutsceneData.events)
                {
                    if (cutsceneEvent == null)
                    {
                        continue;
                    }

                    // 이벤트 타입에 맞는 내부 데이터를 보정합니다.
                    cutsceneEvent.EnsureDataForType();

                    // 같은 타입의 이벤트는 동일한 트랙에 배치합니다.
                    if (!trackMap.TryGetValue(cutsceneEvent.type, out var track))
                    {
                        track = timelineAsset.CreateTrack<CutsceneEventTrack>(null, $"{cutsceneEvent.type} Track");
                        trackMap.Add(cutsceneEvent.type, track);
                    }

                    // 이벤트 하나를 하나의 타임라인 클립으로 생성합니다.
                    var clip = track.CreateClip<CutsceneEventClip>();
                    clip.start = cutsceneEvent.time;
                    clip.duration = cutsceneEvent.duration > 0f ? cutsceneEvent.duration : 1.0f;

                    var clipAsset = clip.asset as CutsceneEventClip;
                    if (clipAsset == null)
                    {
                        continue;
                    }

                    // 클립 내부 이벤트 목록을 초기화하고 현재 이벤트를 복제하여 설정합니다.
                    clipAsset.events.Clear();
                    clipAsset.SetEvent(CutsceneTimelineCloneUtility.CloneEvent(cutsceneEvent));
                    EditorUtility.SetDirty(clipAsset);
                }

                // 생성된 타임라인과 하위 에셋 변경 사항을 저장합니다.
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