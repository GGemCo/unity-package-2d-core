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
        
        private int _uid;
        private int _stepIndex;
        private SceneGame _sceneGame;
        private QuestManager _questManager;
        
        private TableQuest _tableQuest;
        private TableNpc _tableNpc;
        private TableMonster _tableMonster;
        private TableMap _tableMap;
        private TableItem _tableItem;
        private TableCutscene _tableCutscene;

        private QuestData _questData;
        public void InitializeInfo(int questUid, int questStepIndex)
        {
            _uid = questUid;
            _stepIndex = questStepIndex;
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
            _sceneGame ??= SceneGame.Instance;
            _questManager ??= _sceneGame?.QuestManager;
            _tableQuest ??= TableLoaderManager.Instance?.TableQuest;
            _tableNpc ??= TableLoaderManager.Instance?.TableNpc;
            _tableMonster ??= TableLoaderManager.Instance?.TableMonster;
            _tableMap ??= TableLoaderManager.Instance?.TableMap;
            _tableItem ??= TableLoaderManager.Instance?.TableItem;
            _tableCutscene ??= TableLoaderManager.Instance?.TableCutscene;
            _questData ??= _sceneGame?.saveDataManager?.Quest;
        }

        /// <summary>
        /// 현재 퀘스트 단계 정보를 기준으로 HUD 텍스트를 갱신합니다.
        /// </summary>
        public void UpdateInfo()
        {
            EnsureReferences();
            if (_questManager == null || _tableQuest == null || _questData == null) return;
            if (_uid <= 0) return;
            var info = _tableQuest.GetDataByUid(_uid);
            if (info == null) return;
            textQuestTitle.text = info.Name;
            
            // objective 별 처리
            QuestStep questStep = _questManager.GetQuestStep(_uid, _stepIndex);
            if (questStep == null) return;
            switch (questStep.objectiveType)
            {
                case QuestConstants.ObjectiveType.None:
                    break;
                case QuestConstants.ObjectiveType.TalkToNpc:
                    var infoNpc = _tableNpc.GetDataByUid(questStep.targetUid);
                    textQuestObjective.text = $"Talk to {infoNpc.Name}";//$"{infoNpc.Name}와 대화하기";
                    break;
                case QuestConstants.ObjectiveType.CollectItem:
                case QuestConstants.ObjectiveType.KillMonster:
                case QuestConstants.ObjectiveType.KillMonsterInMap:
                    int count = _questData.GetCount(_uid);
                    SetCount(count);
                    break;
                case QuestConstants.ObjectiveType.EnterMap:
                    SetEnterMapObjective(questStep);
                    break;
                case QuestConstants.ObjectiveType.ReachPosition:
                    break;
                case QuestConstants.ObjectiveType.PlayCutscene:
                    SetPlayCutsceneObjective(questStep);
                    break;
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
            if (questStep == null || _tableMap == null) return;

            var infoMap = _tableMap.GetDataByUid(questStep.mapUid);
            if (infoMap == null) return;

            textQuestObjective.text = $"Enter {infoMap.Name}";
        }

        /// <summary>
        /// PlayCutscene 목표의 HUD 문구를 설정합니다.
        /// </summary>
        /// <param name="questStep">현재 표시할 퀘스트 단계 정보입니다.</param>
        private void SetPlayCutsceneObjective(QuestStep questStep)
        {
            if (questStep == null) return;

            int cutsceneUid = questStep.GetPlayCutsceneUid();
            if (cutsceneUid <= 0)
            {
                textQuestObjective.text = "Play cutscene";
                return;
            }

            StruckTableCutscene cutsceneInfo = _tableCutscene?.GetDataByUid(cutsceneUid);
            if (cutsceneInfo == null)
            {
                textQuestObjective.text = $"Play cutscene ({cutsceneUid})";
                return;
            }

            string displayName = string.IsNullOrWhiteSpace(cutsceneInfo.Memo)
                ? cutsceneInfo.FileName
                : cutsceneInfo.Memo;
            textQuestObjective.text = $"Play cutscene: {displayName}";
        }

        /// <summary>
        /// 목표 타입에 맞춰 진행 수량을 표시합니다.
        /// </summary>
        /// <param name="count">현재까지 진행한 수량입니다.</param>
        public void SetCount(int count)
        {
            EnsureReferences();
            if (_questManager == null) return;
            QuestStep questStep = _questManager.GetQuestStep(_uid, _stepIndex);
            if (questStep == null) return;
            switch (questStep.objectiveType)
            {
                case QuestConstants.ObjectiveType.KillMonster:
                    var infoMonster = _tableMonster.GetDataByUid(questStep.targetUid);
                    if (infoMonster == null) return;
                    textQuestObjective.text = $"Hunt {infoMonster.Name} ({count}/{questStep.count})";//$"({count}/{questStep.count}) {infoMonster.Name} 사냥하기";
                    break;
                case QuestConstants.ObjectiveType.KillMonsterInMap:
                    var infoMap = _tableMap.GetDataByUid(questStep.mapUid);
                    if (infoMap == null) return;
                    textQuestObjective.text = $"Hunt all monsters in {infoMap.Name} ({count}/{questStep.count})";
                    break;
                case QuestConstants.ObjectiveType.CollectItem:
                    var infoItem = _tableItem.GetDataByUid(questStep.targetUid);
                    if (infoItem == null) return;
                    textQuestObjective.text = $"Collect {ItemDisplayNameUtility.GetDisplayName(infoItem)} ({count}/{questStep.count})";//$"({count}/{questStep.count}) {infoItem.Name} 수집하기";
                    break;
                default:
                    break;
            }
        }
    }
}
