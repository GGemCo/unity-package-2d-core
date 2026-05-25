using System;
using System.Collections.Generic;
using System.IO;
using GGemCo2DCore;
using Newtonsoft.Json;
using UnityEditor;
using UnityEngine.Timeline;

namespace GGemCo2DCoreEditor
{
    /// <summary>
    /// TimelineAsset의 컷신 트랙 정보를 수집하여 컷신 JSON으로 내보내는 기능을 제공합니다.
    /// </summary>
    internal static class CutsceneTimelineExportService
    {
        /// <summary>
        /// 지정한 TimelineAsset을 분석하여 컷신 JSON 파일로 저장합니다.
        /// </summary>
        /// <param name="timeline">내보낼 대상 TimelineAsset입니다.</param>
        /// <param name="jsonPath">생성할 JSON 파일의 저장 경로입니다.</param>
        /// <param name="data">내보내기 성공 시 생성된 컷신 데이터입니다.</param>
        /// <param name="error">실패 시 반환할 오류 메시지입니다.</param>
        /// <param name="cutsceneInfo">Localization 컬렉션 이름과 Key 생성에 사용할 컷신 테이블 정보입니다.</param>
        /// <returns>내보내기에 성공하면 <see langword="true"/>, 그렇지 않으면 <see langword="false"/>입니다.</returns>
        /// <exception cref="IOException">JSON 파일 쓰기 또는 디렉터리 생성 중 I/O 오류가 발생할 수 있습니다.</exception>
        /// <exception cref="UnauthorizedAccessException">지정한 경로에 대한 쓰기 권한이 없을 경우 발생할 수 있습니다.</exception>
        public static bool TryExportTimelineToJson(
            TimelineAsset timeline,
            string jsonPath,
            out CutsceneData data,
            out string error,
            StruckTableCutscene cutsceneInfo = null)
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
                // 타임라인에서 컷신 이벤트를 수집하고 유효성을 검사합니다.
                var events = CollectEventsFromTimeline(timeline, out error);
                if (events == null)
                {
                    return false;
                }

                // DialogueBalloon 메시지는 GUID 기반 Localization Key로 변환하고 String Table Collection을 갱신합니다.
                string cutsceneToken = ResolveCutsceneLocalizationToken(cutsceneInfo, timeline, jsonPath);
                var localizationExportService = new CutsceneLocalizationExportService();
                localizationExportService.Export(cutsceneToken, events);

                // 마지막 이벤트의 종료 시점을 기준으로 컷신 전체 길이를 계산합니다.
                data = new CutsceneData
                {
                    duration = events.Count > 0 ? events[events.Count - 1].time + events[events.Count - 1].duration : 0f,
                    events = events,
                };

                // 저장 대상 디렉터리가 없으면 생성합니다.
                var directory = Path.GetDirectoryName(jsonPath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                // 컷신 데이터를 JSON으로 직렬화하여 파일로 저장한 뒤 에셋 데이터베이스를 갱신합니다.
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


        /// <summary>
        /// 컷신 말풍선 Localization 컬렉션과 Key에 사용할 컷신 식별 토큰을 결정합니다.
        /// cutscene.txt 정보가 있으면 Uid를 우선 사용하고, 없으면 저장 파일명 또는 Timeline 이름으로 보정합니다.
        /// </summary>
        /// <param name="cutsceneInfo">선택된 컷신 테이블 정보입니다.</param>
        /// <param name="timeline">내보낼 Timeline 에셋입니다.</param>
        /// <param name="jsonPath">JSON 저장 경로입니다.</param>
        /// <returns>Localization Key 생성에 사용할 컷신 식별 토큰입니다.</returns>
        private static string ResolveCutsceneLocalizationToken(StruckTableCutscene cutsceneInfo, TimelineAsset timeline, string jsonPath)
        {
            if (cutsceneInfo != null && cutsceneInfo.Uid > 0)
            {
                return cutsceneInfo.Uid.ToString();
            }

            string fileName = !string.IsNullOrWhiteSpace(jsonPath)
                ? Path.GetFileNameWithoutExtension(jsonPath)
                : string.Empty;
            if (!string.IsNullOrWhiteSpace(fileName))
            {
                return fileName;
            }

            return timeline != null ? timeline.name : string.Empty;
        }

        /// <summary>
        /// 타임라인의 활성 컷신 이벤트 트랙에서 이벤트를 수집하고 시간순으로 정렬합니다.
        /// </summary>
        /// <param name="timeline">이벤트를 수집할 대상 TimelineAsset입니다.</param>
        /// <param name="error">유효성 검사 실패 시 반환할 오류 메시지입니다.</param>
        /// <returns>수집된 컷신 이벤트 목록이며, 실패하면 <see langword="null"/>입니다.</returns>
        private static List<CutsceneEvent> CollectEventsFromTimeline(TimelineAsset timeline, out string error)
        {
            error = null;
            var events = new List<CutsceneEvent>();

            foreach (var track in timeline.GetOutputTracks())
            {
                // 컷신 이벤트 트랙만 처리하며 음소거된 트랙은 제외합니다.
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

                        // 이벤트 타입에 필요한 데이터 구조를 보정한 뒤 유효성을 검사합니다.
                        cutsceneEvent.EnsureDataForType();
                        if (!CutsceneTimelineValidationUtility.ValidateEvent(cutsceneEvent, out error))
                        {
                            return null;
                        }

                        // 원본 이벤트를 복제하여 클립의 시간 정보와 함께 결과 목록에 추가합니다.
                        var copy = CutsceneTimelineCloneUtility.CloneEvent(cutsceneEvent);
                        copy.time = (float)clip.start;
                        copy.duration = (float)clip.duration;
                        events.Add(copy);
                    }
                }
            }

            // 내보내기 결과가 시간 순서를 따르도록 정렬합니다.
            events.Sort((a, b) => a.time.CompareTo(b.time));
            return events;
        }
    }
}