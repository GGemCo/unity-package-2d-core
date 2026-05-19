using GGemCo2DCore;
using UnityEditor;
using UnityEngine;

namespace GGemCo2DCoreEditor
{
    /// <summary>
    /// <see cref="QuestConstants.ObjectiveType.PlayCutscene"/> 단계 입력 UI를 그립니다.
    /// </summary>
    public sealed class StepDrawerPlayCutscene : IQuestStepDrawer
    {
        private int _selectedIndexCutscene;

        /// <summary>
        /// 컷신 UID 선택 팝업을 그리고 선택 결과를 단계 데이터에 반영합니다.
        /// </summary>
        /// <param name="y">현재 그리기 Y 좌표입니다.</param>
        /// <param name="rect">리스트 요소의 표시 영역입니다.</param>
        /// <param name="step">수정할 퀘스트 단계 데이터입니다.</param>
        /// <param name="metadataQuestStepListDrawer">컷신 목록 메타데이터입니다.</param>
        public void Draw(ref float y, Rect rect, QuestStep step, MetadataQuestStepListDrawer metadataQuestStepListDrawer)
        {
            if (step == null)
            {
                return;
            }

            if (metadataQuestStepListDrawer?.NameCutscene == null ||
                metadataQuestStepListDrawer.NameCutscene.Count <= 0 ||
                metadataQuestStepListDrawer.StruckTableCutscenes == null)
            {
                EditorGUI.HelpBox(new Rect(rect.x, y, rect.width, 18), "컷신 테이블 데이터가 없습니다.", MessageType.Warning);
                y += 20;
                return;
            }

            // 구버전 데이터(targetUid 저장)도 새 필드(cutsceneUid)로 자연스럽게 승격합니다.
            if (step.cutsceneUid <= 0 && step.targetUid > 0)
            {
                step.cutsceneUid = step.targetUid;
            }

            if (step.cutsceneUid > 0)
            {
                int selectedUid = step.cutsceneUid;
                _selectedIndexCutscene = metadataQuestStepListDrawer.NameCutscene.FindIndex(x => x.Contains(selectedUid.ToString()));
                if (_selectedIndexCutscene < 0)
                {
                    _selectedIndexCutscene = 0;
                }
            }

            _selectedIndexCutscene = EditorGUI.Popup(
                new Rect(rect.x, y, rect.width, 18),
                "컷신",
                _selectedIndexCutscene,
                metadataQuestStepListDrawer.NameCutscene.ToArray());

            if (metadataQuestStepListDrawer.StruckTableCutscenes.TryGetValue(_selectedIndexCutscene, out StruckTableCutscene selectedCutscene) &&
                selectedCutscene != null)
            {
                step.cutsceneUid = selectedCutscene.Uid;
            }
            else
            {
                step.cutsceneUid = 0;
            }

            y += 20;
        }

        /// <summary>
        /// 드로어가 사용하는 고정 높이를 반환합니다.
        /// </summary>
        /// <returns>한 줄 높이(20)를 반환합니다.</returns>
        public float GetHeight() => 20;
    }
}
