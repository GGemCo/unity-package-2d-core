using TMPro;
using UnityEngine;

namespace GGemCo2DCore
{
    public class UIElementHudQuest : MonoBehaviour
    {
        [Tooltip("퀘스트 제목")]
        public TextMeshProUGUI textQuestTitle;
        [Tooltip("퀘스트 목표")]
        public TextMeshProUGUI textQuestObjective;
        
        private int uid;
        private int stepIndex;
        private SceneGame sceneGame;
        private QuestManager questManager;
        
        private TableQuest tableQuest;
        private TableNpc tableNpc;
        private TableMonster tableMonster;
        private TableMap tableMap;
        private TableItem tableItem;

        private QuestData questData;
        public void InitializeInfo(int questUid, int questStepIndex)
        {
            uid = questUid;
            stepIndex = questStepIndex;
        }
        private void Start()
        {
            EnsureReferences();
            UpdateInfo();
        }

        /// <summary>
        /// HUD 갱신에 필요한 매니저와 테이블 참조를 준비합니다.
        /// </summary>
        private void EnsureReferences()
        {
            sceneGame ??= SceneGame.Instance;
            questManager ??= sceneGame?.QuestManager;
            tableQuest ??= TableLoaderManager.Instance?.TableQuest;
            tableNpc ??= TableLoaderManager.Instance?.TableNpc;
            tableMonster ??= TableLoaderManager.Instance?.TableMonster;
            tableMap ??= TableLoaderManager.Instance?.TableMap;
            tableItem ??= TableLoaderManager.Instance?.TableItem;
            questData ??= sceneGame?.saveDataManager?.Quest;
        }

        /// <summary>
        /// 현재 퀘스트 단계 정보를 기준으로 HUD 텍스트를 갱신합니다.
        /// </summary>
        public void UpdateInfo()
        {
            EnsureReferences();
            if (questManager == null || tableQuest == null || questData == null) return;
            if (uid <= 0) return;
            var info = tableQuest.GetDataByUid(uid);
            if (info == null) return;
            textQuestTitle.text = info.Name;
            
            // objective 별 처리
            QuestStep questStep = questManager.GetQuestStep(uid, stepIndex);
            if (questStep == null) return;
            switch (questStep.objectiveType)
            {
                case QuestConstants.ObjectiveType.None:
                    break;
                case QuestConstants.ObjectiveType.TalkToNpc:
                    var infoNpc = tableNpc.GetDataByUid(questStep.targetUid);
                    textQuestObjective.text = $"Talk to {infoNpc.Name}";//$"{infoNpc.Name}와 대화하기";
                    break;
                case QuestConstants.ObjectiveType.CollectItem:
                case QuestConstants.ObjectiveType.KillMonster:
                case QuestConstants.ObjectiveType.KillMonsterInMap:
                    int count = questData.GetCount(uid);
                    SetCount(count);
                    break;
                case QuestConstants.ObjectiveType.EnterMap:
                    SetEnterMapObjective(questStep);
                    break;
                case QuestConstants.ObjectiveType.ReachPosition:
                case QuestConstants.ObjectiveType.PlayCutscene:
                default:
                    break;
            }
        }

        /// <summary>
        /// EnterMap 목표의 맵 입장 안내 문구를 설정합니다.
        /// </summary>
        /// <param name="questStep">현재 표시할 퀘스트 단계 정보입니다.</param>
        private void SetEnterMapObjective(QuestStep questStep)
        {
            if (questStep == null || tableMap == null) return;

            var infoMap = tableMap.GetDataByUid(questStep.mapUid);
            if (infoMap == null) return;

            textQuestObjective.text = $"Enter {infoMap.Name}";
        }

        /// <summary>
        /// 목표 타입에 맞춰 진행 수량을 표시합니다.
        /// </summary>
        /// <param name="count">현재까지 진행한 수량입니다.</param>
        public void SetCount(int count)
        {
            EnsureReferences();
            if (questManager == null) return;
            QuestStep questStep = questManager.GetQuestStep(uid, stepIndex);
            if (questStep == null) return;
            switch (questStep.objectiveType)
            {
                case QuestConstants.ObjectiveType.KillMonster:
                    var infoMonster = tableMonster.GetDataByUid(questStep.targetUid);
                    if (infoMonster == null) return;
                    textQuestObjective.text = $"Hunt {infoMonster.Name} ({count}/{questStep.count})";//$"({count}/{questStep.count}) {infoMonster.Name} 사냥하기";
                    break;
                case QuestConstants.ObjectiveType.KillMonsterInMap:
                    var infoMap = tableMap.GetDataByUid(questStep.mapUid);
                    if (infoMap == null) return;
                    textQuestObjective.text = $"Hunt all monsters in {infoMap.Name} ({count}/{questStep.count})";
                    break;
                case QuestConstants.ObjectiveType.CollectItem:
                    var infoItem = tableItem.GetDataByUid(questStep.targetUid);
                    if (infoItem == null) return;
                    textQuestObjective.text = $"Collect {ItemDisplayNameUtility.GetDisplayName(infoItem)} ({count}/{questStep.count})";//$"({count}/{questStep.count}) {infoItem.Name} 수집하기";
                    break;
                default:
                    break;
            }
        }
    }
}
