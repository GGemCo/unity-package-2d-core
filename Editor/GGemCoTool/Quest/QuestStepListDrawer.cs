using System.Collections.Generic;
using GGemCo.Scripts;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace GGemCo.Editor
{
    public class QuestStepListDrawer
    {
        public ReorderableList List { get; private set; }

        public QuestStepListDrawer(List<QuestStep> steps, MetadataQuestStepListDrawer metadataQuestStepListDrawer)
        {
            List = new ReorderableList(steps, typeof(QuestStep), true, true, true, true);

            List.drawHeaderCallback = rect => EditorGUI.LabelField(rect, "퀘스트 단계");

            List.elementHeightCallback = index =>
            {
                var step = steps[index];
                var drawer = QuestStepDrawerFactory.GetDrawer(step.objectiveType);
                return GetBaseHeight() + (drawer?.GetHeight() ?? 20) + 10; // 기본 + 타입별
            };

            List.drawElementCallback = (rect, index, isActive, isFocused) =>
            {
                var step = steps[index];
                float y = rect.y + 2;

                step.objectiveType = (QuestConstants.ObjectiveType)EditorGUI.EnumPopup(
                    new Rect(rect.x, y, rect.width, 18), "목표 타입", step.objectiveType);
                y += 20;

                var drawer = QuestStepDrawerFactory.GetDrawer(step.objectiveType);
                drawer?.Draw(ref y, rect, step, metadataQuestStepListDrawer);
            };
        }

        private float GetBaseHeight() => 20 + 4; // step, objectiveType, padding
    }
}