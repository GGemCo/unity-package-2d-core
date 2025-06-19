using TMPro;
using UnityEngine;

namespace GGemCo.Scripts
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
        private TableItem tableItem;

        private QuestData questData;
        public void InitializeInfo(int questUid, int questStepIndex)
        {
            uid = questUid;
            stepIndex = questStepIndex;
        }
        private void Start()
        {
            sceneGame = SceneGame.Instance;
            questManager = sceneGame.QuestManager;
            tableQuest = TableLoaderManager.Instance.TableQuest;
            tableNpc = TableLoaderManager.Instance.TableNpc;
            tableMonster = TableLoaderManager.Instance.TableMonster;
            tableItem = TableLoaderManager.Instance.TableItem;
            questData = sceneGame.saveDataManager.Quest;
            UpdateInfo();
        }

        public void UpdateInfo()
        {
            if (uid <= 0) return;
            var info = tableQuest.GetDataByUid(uid);
            if (info == null) return;
            textQuestTitle.text = info.Name;
            
            // objective 별 처리
            QuestStep questStep = questManager.GetQuestStep(uid, stepIndex);
            switch (questStep.objectiveType)
            {
                case QuestConstants.ObjectiveType.None:
                    break;
                case QuestConstants.ObjectiveType.TalkToNpc:
                    var infoNpc = tableNpc.GetDataByUid(questStep.targetUid);
                    textQuestObjective.text = $"{infoNpc.Name}와 대화하기";
                    break;
                case QuestConstants.ObjectiveType.CollectItem:
                case QuestConstants.ObjectiveType.KillMonster:
                    int count = questData.GetCount(uid);
                    SetCount(count);
                    break;
                case QuestConstants.ObjectiveType.ReachMap:
                case QuestConstants.ObjectiveType.ReachPosition:
                case QuestConstants.ObjectiveType.PlayCutscene:
                default:
                    break;
            }
        }

        public void SetCount(int count)
        {
            QuestStep questStep = questManager.GetQuestStep(uid, stepIndex);
            switch (questStep.objectiveType)
            {
                case QuestConstants.ObjectiveType.KillMonster:
                    var infoMonster = tableMonster.GetDataByUid(questStep.targetUid);
                    if (infoMonster == null) return;
                    textQuestObjective.text = $"({count}/{questStep.count}) {infoMonster.Name} 사냥하기";
                    break;
                case QuestConstants.ObjectiveType.CollectItem:
                    var infoItem = tableItem.GetDataByUid(questStep.targetUid);
                    if (infoItem == null) return;
                    textQuestObjective.text = $"({count}/{questStep.count}) {infoItem.Name} 수집하기";
                    break;
                default:
                    break;
            }
        }
    }
}