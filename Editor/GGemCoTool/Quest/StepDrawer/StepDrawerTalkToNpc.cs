using GGemCo.Scripts;
using UnityEngine;

namespace GGemCo.Editor
{
    public class StepDrawerTalkToNpc : IQuestStepDrawer
    {
        private int selectedIndexNpc = 0;
        private int selectedIndexMap = 0;
        private int selectedIndexDialogue = 0;
        
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
                "NPC",
                ref selectedIndexNpc,
                metadataQuestStepListDrawer.NameNpc,
                metadataQuestStepListDrawer.StruckTableNpcs,
                ref step.targetUid,
                rect,
                ref y
            );
            
            EditorPopupUtils.DrawUidPopup(
                "대화",
                ref selectedIndexDialogue,
                metadataQuestStepListDrawer.NameDialogue,
                metadataQuestStepListDrawer.StruckTableDialogues,
                ref step.dialogueUid,
                rect,
                ref y
            );
        }
        public float GetHeight() => 3 * 20;
    }
}