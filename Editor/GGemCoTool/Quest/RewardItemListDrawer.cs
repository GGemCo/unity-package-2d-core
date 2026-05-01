using System.Collections.Generic;
using GGemCo2DCore;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace GGemCo2DCoreEditor
{
    public class RewardItemListDrawer
    {
        private readonly QuestReward reward;
        private readonly MetadataQuestStepListDrawer metadataQuestStepListDrawer;
        private readonly ReorderableList list;
        private readonly ReorderableList mapNodeIdList;
        private int selectedIndexItem = 0;
        private int selectedIndexClearMap = 0;
        
        /// <summary>
        /// 퀘스트 보상 입력 UI를 초기화합니다.
        /// </summary>
        /// <param name="reward">수정할 퀘스트 보상 데이터입니다.</param>
        /// <param name="metadataQuestStepListDrawer">테이블 선택지 메타데이터입니다.</param>
        public RewardItemListDrawer(QuestReward reward, MetadataQuestStepListDrawer metadataQuestStepListDrawer)
        {
            this.reward = reward;
            this.metadataQuestStepListDrawer = metadataQuestStepListDrawer;
            this.reward.items ??= new List<RewardItem>();
            this.reward.mapProgress ??= new QuestRewardMapProgress();
            this.reward.mapProgress.activateWorldMapNodeIds ??= new List<string>();

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

            mapNodeIdList = new ReorderableList(
                this.reward.mapProgress.activateWorldMapNodeIds,
                typeof(string),
                true,
                true,
                true,
                true);
            mapNodeIdList.drawHeaderCallback = rect => EditorGUI.LabelField(rect, "활성화할 월드맵 노드 ID");
            mapNodeIdList.elementHeight = 22;
            mapNodeIdList.drawElementCallback = (rect, index, isActive, isFocused) =>
            {
                this.reward.mapProgress.activateWorldMapNodeIds[index] = EditorGUI.TextField(
                    new Rect(rect.x, rect.y + 2, rect.width, 18),
                    this.reward.mapProgress.activateWorldMapNodeIds[index]);
            };
        }

        /// <summary>
        /// 기본 보상, 아이템 보상, 맵 진행 보상 UI를 순서대로 그립니다.
        /// </summary>
        public void DoLayout()
        {
            EditorGUILayout.LabelField("기본 보상");
            reward.experience = EditorGUILayout.IntField("경험치", reward.experience);
            reward.gold = EditorGUILayout.IntField("골드", reward.gold);
            reward.silver = EditorGUILayout.IntField("실버", reward.silver);

            GUILayout.Space(10);
            list.DoLayoutList();

            GUILayout.Space(10);
            DrawMapProgressReward();
        }

        /// <summary>
        /// 맵 클리어 및 월드맵 노드 활성화 보상 UI를 그립니다.
        /// </summary>
        private void DrawMapProgressReward()
        {
            EditorGUILayout.LabelField("맵 진행 보상");
            DrawClearMapPopup();
            mapNodeIdList.DoLayoutList();
        }

        /// <summary>
        /// 클리어 처리할 맵을 선택하는 팝업 UI를 그립니다.
        /// </summary>
        private void DrawClearMapPopup()
        {
            List<string> mapOptions = new List<string> { "없음" };
            mapOptions.AddRange(metadataQuestStepListDrawer.NameMap);

            if (reward.mapProgress.clearMapUid > 0)
            {
                int mapIndex = metadataQuestStepListDrawer.NameMap.FindIndex(
                    x => x.Contains(reward.mapProgress.clearMapUid.ToString()));
                selectedIndexClearMap = mapIndex >= 0 ? mapIndex + 1 : 0;
            }
            else
            {
                selectedIndexClearMap = 0;
            }

            selectedIndexClearMap = EditorGUILayout.Popup("클리어 맵", selectedIndexClearMap, mapOptions.ToArray());
            if (selectedIndexClearMap <= 0)
            {
                reward.mapProgress.clearMapUid = 0;
                return;
            }

            reward.mapProgress.clearMapUid =
                metadataQuestStepListDrawer.StruckTableMaps.GetValueOrDefault(selectedIndexClearMap - 1)?.Uid ?? 0;
        }
    }
}
