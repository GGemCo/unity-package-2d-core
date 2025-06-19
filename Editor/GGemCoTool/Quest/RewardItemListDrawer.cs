using System.Collections.Generic;
using GGemCo.Scripts;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace GGemCo.Editor
{
    public class RewardItemListDrawer
    {
        private QuestReward reward;
        private ReorderableList list;
        private int selectedIndexItem = 0;
        
        public RewardItemListDrawer(QuestReward reward, MetadataQuestStepListDrawer metadataQuestStepListDrawer)
        {
            this.reward = reward;

            list = new ReorderableList(reward.items, typeof(RewardItem), true, true, true, true);
            list.drawHeaderCallback = rect => EditorGUI.LabelField(rect, "아이템 보상 목록");
            list.elementHeight = 24;
            list.drawElementCallback = (rect, index, isActive, isFocused) =>
            {
                var item = reward.items[index];
                float half = rect.width * 0.5f;

                if (item.itemUid > 0)
                {
                    selectedIndexItem = item.itemUid > 0
                        ? metadataQuestStepListDrawer.NameItem.FindIndex(x => x.Contains(item.itemUid.ToString()))
                        : 0;
                }
                selectedIndexItem = EditorGUI.Popup(new Rect(rect.x, rect.y + 2, half - 5, 18), "아이템",
                    selectedIndexItem, metadataQuestStepListDrawer.NameItem.ToArray());
                item.itemUid = metadataQuestStepListDrawer.StruckTableItems.GetValueOrDefault(selectedIndexItem)?.Uid ?? 0;
                
                item.amount = EditorGUI.IntField(new Rect(rect.x + half + 5, rect.y + 2, half - 5, 18), "수량", item.amount);
            };
        }

        public void DoLayout()
        {
            EditorGUILayout.LabelField("기본 보상");
            reward.experience = EditorGUILayout.IntField("경험치", reward.experience);
            reward.gold = EditorGUILayout.IntField("골드", reward.gold);
            reward.silver = EditorGUILayout.IntField("실버", reward.silver);

            GUILayout.Space(10);
            list.DoLayoutList();
        }
    }
}