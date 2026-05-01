using GGemCo2DCore;
using UnityEngine;

namespace GGemCo2DCoreEditor
{
    /// <summary>
    /// EnterMap 퀘스트 목표의 맵 선택 UI를 그립니다.
    /// </summary>
    public class StepDrawerEnterMap : IQuestStepDrawer
    {
        private int selectedIndexMap;

        /// <summary>
        /// 목표 맵 UID를 선택하는 팝업 필드를 그립니다.
        /// </summary>
        /// <param name="y">현재 그리기 Y 좌표입니다.</param>
        /// <param name="rect">리스트 요소 영역입니다.</param>
        /// <param name="step">수정할 퀘스트 단계 데이터입니다.</param>
        /// <param name="metadataQuestStepListDrawer">테이블 선택지 메타데이터입니다.</param>
        public void Draw(ref float y, Rect rect, QuestStep step, MetadataQuestStepListDrawer metadataQuestStepListDrawer)
        {
            EditorPopupUtils.DrawUidPopup(
                "맵",
                ref selectedIndexMap,
                metadataQuestStepListDrawer.NameMap,
                metadataQuestStepListDrawer.StruckTableMaps,
                ref step.mapUid,
                rect,
                ref y
            );
        }

        /// <summary>
        /// EnterMap 목표 UI가 차지할 높이를 반환합니다.
        /// </summary>
        /// <returns>한 줄 높이입니다.</returns>
        public float GetHeight() => 20;
    }
}
