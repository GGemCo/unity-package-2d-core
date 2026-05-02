using System.Collections.Generic;
using GGemCo2DCore;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace GGemCo2DCoreEditor
{
    public class RewardItemListDrawer
    {
        private readonly QuestReward _reward;
        private readonly MetadataQuestStepListDrawer _metadataQuestStepListDrawer;
        private readonly ReorderableList _list;
        private readonly ReorderableList _visibleMapNodeIdList;
        private readonly ReorderableList _mapNodeIdList;
        private readonly ReorderableList _licenseList;
        private int _selectedIndexItem = 0;
        private int _selectedIndexClearMap = 0;
        
        /// <summary>
        /// 퀘스트 보상 입력 UI를 초기화합니다.
        /// </summary>
        /// <param name="reward">수정할 퀘스트 보상 데이터입니다.</param>
        /// <param name="metadataQuestStepListDrawer">테이블 선택지 메타데이터입니다.</param>
        public RewardItemListDrawer(QuestReward reward, MetadataQuestStepListDrawer metadataQuestStepListDrawer)
        {
            _reward = reward;
            _metadataQuestStepListDrawer = metadataQuestStepListDrawer;
            _reward.items ??= new List<RewardItem>();
            _reward.mapProgress ??= new QuestRewardMapProgress();
            _reward.mapProgress.visibleWorldMapNodeIds ??= new List<string>();
            _reward.mapProgress.activateWorldMapNodeIds ??= new List<string>();
            _reward.licenses ??= new List<QuestRewardLicense>();

            _list = new ReorderableList(reward.items, typeof(RewardItem), true, true, true, true);
            _list.drawHeaderCallback = rect => EditorGUI.LabelField(rect, "아이템 보상 목록");
            _list.elementHeight = 24;
            _list.drawElementCallback = (rect, index, isActive, isFocused) =>
            {
                var item = reward.items[index];
                float half = rect.width * 0.5f;

                if (item.itemUid > 0)
                {
                    _selectedIndexItem = item.itemUid > 0
                        ? metadataQuestStepListDrawer.NameItem.FindIndex(x => x.Contains(item.itemUid.ToString()))
                        : 0;
                }
                _selectedIndexItem = EditorGUI.Popup(new Rect(rect.x, rect.y + 2, half - 5, 18), "아이템",
                    _selectedIndexItem, metadataQuestStepListDrawer.NameItem.ToArray());
                item.itemUid = metadataQuestStepListDrawer.StruckTableItems.GetValueOrDefault(_selectedIndexItem)?.Uid ?? 0;
                
                item.amount = EditorGUI.IntField(new Rect(rect.x + half + 5, rect.y + 2, half - 5, 18), "수량", item.amount);
            };

            _visibleMapNodeIdList = new ReorderableList(
                _reward.mapProgress.visibleWorldMapNodeIds,
                typeof(string),
                true,
                true,
                true,
                true)
            {
                drawHeaderCallback = rect =>
                    EditorGUI.LabelField(rect, "표시할 월드맵 노드 ID (비활성 유지)"),
                elementHeight = 22,
                drawElementCallback = (rect, index, isActive, isFocused) =>
                {
                    DrawNodeIdElement(_reward.mapProgress.visibleWorldMapNodeIds, rect, index);
                }
            };

            _mapNodeIdList = new ReorderableList(
                _reward.mapProgress.activateWorldMapNodeIds,
                typeof(string),
                true,
                true,
                true,
                true)
            {
                drawHeaderCallback = rect =>
                    EditorGUI.LabelField(rect, "활성화할 월드맵 노드 ID (비활성 해제)"),
                elementHeight = 22,
                drawElementCallback = (rect, index, isActive, isFocused) =>
                {
                    DrawNodeIdElement(_reward.mapProgress.activateWorldMapNodeIds, rect, index);
                }
            };

            _licenseList = new ReorderableList(
                _reward.licenses,
                typeof(QuestRewardLicense),
                true,
                true,
                true,
                true)
            {
                drawHeaderCallback = rect => EditorGUI.LabelField(rect, "라이센스 보상 목록"),
                elementHeight = 24,
                drawElementCallback = DrawLicenseElement
            };
        }

        /// <summary>
        /// 기본 보상, 아이템 보상, 맵 진행 보상 UI를 순서대로 그립니다.
        /// </summary>
        public void DoLayout()
        {
            EditorGUILayout.LabelField("기본 보상");
            _reward.experience = EditorGUILayout.IntField("경험치", _reward.experience);
            _reward.gold = EditorGUILayout.IntField("골드", _reward.gold);
            _reward.silver = EditorGUILayout.IntField("실버", _reward.silver);

            GUILayout.Space(10);
            _list.DoLayoutList();

            GUILayout.Space(10);
            DrawMapProgressReward();

            GUILayout.Space(10);
            DrawLicenseReward();
        }

        /// <summary>
        /// 맵 클리어, 월드맵 노드 표시, 월드맵 노드 활성화 보상 UI를 그립니다.
        /// </summary>
        private void DrawMapProgressReward()
        {
            EditorGUILayout.LabelField("맵 진행 보상");
            DrawClearMapPopup();
            _visibleMapNodeIdList.DoLayoutList();
            _mapNodeIdList.DoLayoutList();
        }

        /// <summary>
        /// 월드맵 노드 ID 목록의 한 줄 입력 UI를 그립니다.
        /// 빈 값은 저장 시 그대로 유지하며, 런타임 저장 처리에서 공백 ID를 무시합니다.
        /// </summary>
        /// <param name="nodeIds">수정할 월드맵 노드 ID 목록입니다.</param>
        /// <param name="rect">그릴 영역입니다.</param>
        /// <param name="index">수정할 목록 인덱스입니다.</param>
        private static void DrawNodeIdElement(List<string> nodeIds, Rect rect, int index)
        {
            if (nodeIds == null || index < 0 || index >= nodeIds.Count)
            {
                return;
            }

            nodeIds[index] = EditorGUI.TextField(
                new Rect(rect.x, rect.y + 2, rect.width, 18),
                nodeIds[index]);
        }

        /// <summary>
        /// 클리어 처리할 맵을 선택하는 팝업 UI를 그립니다.
        /// </summary>
        private void DrawClearMapPopup()
        {
            List<string> mapOptions = new List<string> { "없음" };
            mapOptions.AddRange(_metadataQuestStepListDrawer.NameMap);

            if (_reward.mapProgress.clearMapUid > 0)
            {
                int mapIndex = _metadataQuestStepListDrawer.NameMap.FindIndex(
                    x => x.Contains(_reward.mapProgress.clearMapUid.ToString()));
                _selectedIndexClearMap = mapIndex >= 0 ? mapIndex + 1 : 0;
            }
            else
            {
                _selectedIndexClearMap = 0;
            }

            _selectedIndexClearMap = EditorGUILayout.Popup("클리어 맵", _selectedIndexClearMap, mapOptions.ToArray());
            if (_selectedIndexClearMap <= 0)
            {
                _reward.mapProgress.clearMapUid = 0;
                return;
            }

            _reward.mapProgress.clearMapUid =
                _metadataQuestStepListDrawer.StruckTableMaps.GetValueOrDefault(_selectedIndexClearMap - 1)?.Uid ?? 0;
        }

        /// <summary>
        /// 라이센스 보상 목록 UI를 그립니다.
        /// </summary>
        private void DrawLicenseReward()
        {
            _licenseList.DoLayoutList();
        }

        /// <summary>
        /// 라이센스 보상 한 줄의 선택 UI와 저장값 입력 UI를 그립니다.
        /// </summary>
        /// <param name="rect">그릴 영역입니다.</param>
        /// <param name="index">그릴 라이센스 보상 인덱스입니다.</param>
        /// <param name="isActive">현재 줄이 선택되어 있는지 여부입니다.</param>
        /// <param name="isFocused">현재 줄에 포커스가 있는지 여부입니다.</param>
        private void DrawLicenseElement(Rect rect, int index, bool isActive, bool isFocused)
        {
            QuestRewardLicense license = _reward.licenses[index];
            if (license == null)
            {
                license = new QuestRewardLicense();
                _reward.licenses[index] = license;
            }
            float half = rect.width * 0.5f;

            List<string> options = new List<string> { "없음" };
            if (_metadataQuestStepListDrawer.NameLicense != null)
            {
                options.AddRange(_metadataQuestStepListDrawer.NameLicense);
            }

            int selectedIndex = 0;
            if (license.licenseUid > 0)
            {
                int foundIndex = _metadataQuestStepListDrawer.NameLicense != null
                    ? _metadataQuestStepListDrawer.NameLicense.FindIndex(
                        x => x.StartsWith($"{license.licenseUid} - "))
                    : -1;
                selectedIndex = foundIndex >= 0 ? foundIndex + 1 : 0;
            }

            selectedIndex = EditorGUI.Popup(
                new Rect(rect.x, rect.y + 2, half - 5, 18),
                "라이센스",
                selectedIndex,
                options.ToArray());

            license.licenseUid = selectedIndex <= 0
                ? 0
                : _metadataQuestStepListDrawer.StruckTableLicenses?.GetValueOrDefault(selectedIndex - 1)?.Uid ?? 0;

            license.value = EditorGUI.TextField(
                new Rect(rect.x + half + 5, rect.y + 2, half - 5, 18),
                "값",
                string.IsNullOrEmpty(license.value) ? LicenseConstants.TrueValue : license.value);
        }
    }
}
