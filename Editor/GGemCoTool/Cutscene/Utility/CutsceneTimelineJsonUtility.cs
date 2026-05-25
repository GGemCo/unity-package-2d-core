using GGemCo2DCore;
using UnityEngine;
using UnityEngine.Timeline;

namespace GGemCo2DCoreEditor
{
    /// <summary>
    /// Cutscene Timeline과 JSON 데이터 간 변환을 위한 공용 진입 유틸리티입니다.
    /// </summary>
    /// <remarks>
    /// 실제 변환 로직은 Import, Export 전용 서비스에 위임하며,
    /// 이 클래스는 외부 호출 지점을 단순화하는 파사드(Facade) 역할을 담당합니다.
    /// </remarks>
    internal static class CutsceneTimelineJsonUtility
    {
        /// <summary>
        /// JSON 에셋으로부터 컷신 데이터를 읽어 <see cref="TimelineAsset"/>을 생성합니다.
        /// </summary>
        /// <param name="jsonAsset">컷신 JSON 문자열을 포함하는 텍스트 에셋입니다.</param>
        /// <param name="timelineAssetPath">생성할 Timeline 에셋의 저장 경로입니다.</param>
        /// <param name="timelineAsset">생성에 성공한 Timeline 에셋입니다. 실패 시 null입니다.</param>
        /// <param name="error">생성 실패 시 오류 메시지입니다. 성공 시 null 또는 빈 문자열일 수 있습니다.</param>
        /// <returns>Timeline 생성에 성공하면 true, 실패하면 false를 반환합니다.</returns>
        public static bool TryCreateTimelineFromJsonAsset(TextAsset jsonAsset, string timelineAssetPath, out TimelineAsset timelineAsset, out string error)
        {
            return CutsceneTimelineImportService.TryCreateTimelineFromJsonAsset(jsonAsset, timelineAssetPath, out timelineAsset, out error);
        }

        /// <summary>
        /// 컷신 데이터 객체로부터 <see cref="TimelineAsset"/>을 생성합니다.
        /// </summary>
        /// <param name="cutsceneData">Timeline으로 변환할 컷신 데이터입니다.</param>
        /// <param name="timelineAssetPath">생성할 Timeline 에셋의 저장 경로입니다.</param>
        /// <param name="timelineAsset">생성에 성공한 Timeline 에셋입니다. 실패 시 null입니다.</param>
        /// <param name="error">생성 실패 시 오류 메시지입니다. 성공 시 null 또는 빈 문자열일 수 있습니다.</param>
        /// <returns>Timeline 생성에 성공하면 true, 실패하면 false를 반환합니다.</returns>
        public static bool TryCreateTimelineFromData(CutsceneData cutsceneData, string timelineAssetPath, out TimelineAsset timelineAsset, out string error)
        {
            return CutsceneTimelineImportService.TryCreateTimelineFromData(cutsceneData, timelineAssetPath, out timelineAsset, out error);
        }

        /// <summary>
        /// <see cref="TimelineAsset"/>을 컷신 데이터로 변환하고 지정한 경로에 JSON으로 내보냅니다.
        /// </summary>
        /// <param name="timeline">내보낼 대상 Timeline 에셋입니다.</param>
        /// <param name="jsonPath">생성할 JSON 파일의 저장 경로입니다.</param>
        /// <param name="data">내보내기 과정에서 생성된 컷신 데이터입니다. 실패 시 null일 수 있습니다.</param>
        /// <param name="error">내보내기 실패 시 오류 메시지입니다. 성공 시 null 또는 빈 문자열일 수 있습니다.</param>
        /// <param name="cutsceneInfo">Localization 컬렉션 이름과 Key 생성에 사용할 컷신 테이블 정보입니다.</param>
        /// <returns>JSON 내보내기에 성공하면 true, 실패하면 false를 반환합니다.</returns>
        public static bool TryExportTimelineToJson(
            TimelineAsset timeline,
            string jsonPath,
            out CutsceneData data,
            out string error,
            StruckTableCutscene cutsceneInfo = null)
        {
            return CutsceneTimelineExportService.TryExportTimelineToJson(timeline, jsonPath, out data, out error, cutsceneInfo);
        }
    }
}