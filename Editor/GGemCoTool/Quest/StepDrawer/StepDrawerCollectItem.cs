using GGemCo.Scripts;
using UnityEditor;
using UnityEngine;

namespace GGemCo.Editor
{
    public class StepDrawerCollectItem : IQuestStepDrawer
    {
        private int selectedIndexItem = 0;
        public void Draw(ref float y, Rect rect, QuestStep step, MetadataQuestStepListDrawer metadataQuestStepListDrawer)
        {
            EditorPopupUtils.DrawUidPopup(
                "아이템",
                ref selectedIndexItem,
                metadataQuestStepListDrawer.NameItem,
                metadataQuestStepListDrawer.StruckTableItems,
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