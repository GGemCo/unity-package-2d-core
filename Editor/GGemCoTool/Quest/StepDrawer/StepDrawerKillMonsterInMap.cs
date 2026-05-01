using GGemCo2DCore;
using UnityEngine;

namespace GGemCo2DCoreEditor
{
    /// <summary>
    /// 맵 전체 몬스터 처치 목표의 에디터 입력 UI를 그립니다.
    /// </summary>
    public class StepDrawerKillMonsterInMap : IQuestStepDrawer
    {
        private int selectedIndexMap;

        /// <summary>
        /// 목표 맵 UID를 선택하는 드로어를 그립니다.
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
        /// 드로어가 차지할 높이를 반환합니다.
        /// </summary>
        /// <returns>한 줄 높이입니다.</returns>
        public float GetHeight() => 20;
    }
}
