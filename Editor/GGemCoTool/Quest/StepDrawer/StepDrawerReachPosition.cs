using GGemCo.Scripts;
using UnityEditor;
using UnityEngine;

namespace GGemCo.Editor
{
    public class StepDrawerReachPosition : IQuestStepDrawer
    {
        public void Draw(ref float y, Rect rect, QuestStep step, MetadataQuestStepListDrawer metadataQuestStepListDrawer)
        {
            step.mapUid = EditorGUI.IntField(new Rect(rect.x, y, rect.width, 18), "맵 UID", step.mapUid);
            y += 20;
            Vector2 pos = EditorGUI.Vector2Field(new Rect(rect.x, y, rect.width, 18), "좌표", step.position.ToVector2());
            step.position = new Vec2(pos);
            y += 20;
        }
        public float GetHeight() => 2 * 20; // 2줄
    }
}