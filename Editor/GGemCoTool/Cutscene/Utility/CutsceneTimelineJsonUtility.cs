using GGemCo2DCore;
using UnityEditor;
using UnityEngine;
using UnityEngine.Timeline;

namespace GGemCo2DCoreEditor
{
    /// <summary>
    /// Cutscene Timeline <-> Json 변환 공용 진입 유틸리티입니다.
    /// 실제 구현은 Import / Export / Validation / Clone 유틸리티로 분리합니다.
    /// </summary>
    internal static class CutsceneTimelineJsonUtility
    {
        public static bool TryCreateTimelineFromJsonAsset(TextAsset jsonAsset, string timelineAssetPath, out TimelineAsset timelineAsset, out string error)
        {
            return CutsceneTimelineImportService.TryCreateTimelineFromJsonAsset(jsonAsset, timelineAssetPath, out timelineAsset, out error);
        }

        public static bool TryCreateTimelineFromData(CutsceneData cutsceneData, string timelineAssetPath, out TimelineAsset timelineAsset, out string error)
        {
            return CutsceneTimelineImportService.TryCreateTimelineFromData(cutsceneData, timelineAssetPath, out timelineAsset, out error);
        }

        public static bool TryExportTimelineToJson(TimelineAsset timeline, string jsonPath, out CutsceneData data, out string error)
        {
            return CutsceneTimelineExportService.TryExportTimelineToJson(timeline, jsonPath, out data, out error);
        }
    }
}
