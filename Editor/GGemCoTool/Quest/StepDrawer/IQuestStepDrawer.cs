using GGemCo2DCore;
using UnityEngine;

namespace GGemCo.Editor
{
    public interface IQuestStepDrawer
    {
        void Draw(ref float y, Rect rect, QuestStep step, MetadataQuestStepListDrawer metadataQuestStepListDrawer);
        float GetHeight();
    }
}