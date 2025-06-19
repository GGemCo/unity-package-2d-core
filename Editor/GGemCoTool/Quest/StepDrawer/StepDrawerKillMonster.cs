using GGemCo.Scripts;
using UnityEditor;
using UnityEngine;

namespace GGemCo.Editor
{
    public class StepDrawerKillMonster : IQuestStepDrawer
    {
        private int selectedIndexMap = 0;
        private int selectedIndexMonster = 0;
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
            
            EditorPopupUtils.DrawUidPopup(
                "몬스터",
                ref selectedIndexMonster,
                metadataQuestStepListDrawer.NameMonster,
                metadataQuestStepListDrawer.StruckTableMonsters,
                ref step.targetUid,
                rect,
                ref y
            );
            
            step.count = EditorGUI.IntField(new Rect(rect.x, y, rect.width, 18), "타겟 Count", step.count);
            y += 20;
        }
        public float GetHeight() => 3 * 20; // 2줄
    }
}