using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using UnityEngine;

namespace GGemCo2DCore
{
    /// <summary>
    /// 퀘스트 매니저
    /// </summary>
    public class QuestManager
    {
        private SceneGame sceneGame;
        private TableQuest tableQuest;
        private UIWindowHudQuest uiWindowHudQuest;
        private UIWindowQuestReward uiWindowQuestReward;
        private UIWindowInventory uiWindowInventory;
        private QuestData questData;
        private PlayerData playerData;
        private InventoryData inventoryData;
        
        private readonly ObjectiveHandlerFactory handlerFactory = new ObjectiveHandlerFactory();

        // QuestUid → StepIndex → Handler
        private readonly Dictionary<int, Dictionary<int, IObjectiveHandler>> activeHandlers =
            new Dictionary<int, Dictionary<int, IObjectiveHandler>>();
        
        private readonly Dictionary<int, Quest> quests = new Dictionary<int, Quest>();
        
        public void Initialize(SceneGame scene)
        {
            quests.Clear();
            activeHandlers.Clear();
            sceneGame = scene;
            
            questData = sceneGame.saveDataManager.Quest;
            playerData = sceneGame.saveDataManager.Player;
            inventoryData = sceneGame.saveDataManager.Inventory;
            
            tableQuest = TableLoaderManager.Instance.TableQuest;
            
            uiWindowHudQuest =
                sceneGame.uIWindowManager?.GetUIWindowByUid<UIWindowHudQuest>(UIWindowManager.WindowUid.HudQuest);
            uiWindowQuestReward =
                sceneGame.uIWindowManager?.GetUIWindowByUid<UIWindowQuestReward>(UIWindowManager.WindowUid.QuestReward);
            uiWindowInventory =
                sceneGame.uIWindowManager?.GetUIWindowByUid<UIWindowInventory>(UIWindowManager.WindowUid.Inventory);
            
            LoadAllQuestJson();
            
            LoadQuestDatas();
        }
        /// <summary>
        /// 저장되어있는 퀘스트 불러오기
        /// </summary>
        private void LoadQuestDatas()
        {
            questData = sceneGame.saveDataManager.Quest;
            var datas = questData.GetQuestDatas();
            if (datas == null) return;
            foreach (var data in datas)
            {
                QuestSaveData questSaveData = data.Value;
                if (questSaveData == null) continue;
                if (questSaveData.Status != QuestConstants.Status.InProgress) continue;
                StartObjective(questSaveData.QuestUid, questSaveData.QuestStepIndex);
            }
        }
        /// <summary>
        /// 모든 json 파일 읽어두기
        /// </summary>
        private void LoadAllQuestJson()
        {
            var datas = tableQuest.GetDatas();
            foreach (var data in datas)
            {
                LoadQuestJson(data.Key);
            }
        }
        /// <summary>
        /// 퀘스트 시작 처리
        /// </summary>
        /// <param name="questUid"></param>
        /// <param name="npcUid"></param>
        /// <returns></returns>
        public bool StartQuest(int questUid, int npcUid)
        {
            if (questUid <= 0) return false;
            var info = tableQuest.GetDataByUid(questUid);
            if (info == null) return false;

            if (questData.IsStatusNone(questUid) != true)
            {
                sceneGame.systemMessageManager.ShowMessageWarning("진행중인 퀘스트 입니다.");
                return false;
            }

            Quest quest = LoadQuestJson(questUid);
            if (quest == null)
            {
                GcLogger.LogError("퀘스트 json 파일을 불러오지 못 했습니다. uid: " + questUid);
                return false;
            }
            
            // 첫 단계 시작
            int stepIndex = 0;
            StartObjective(quest.uid, stepIndex, npcUid);
            QuestStep questStep = GetQuestStep(quest.uid, stepIndex);
            // 첫 단계가 talk to npc 이면 바로 시작
            if (questStep.objectiveType == QuestConstants.ObjectiveType.TalkToNpc)
            {
                GameEventManager.DialogStart(npcUid);
            }
            return true;
        }
        /// <summary>
        /// 퀘스트 json 불러오기
        /// </summary>
        /// <param name="questUid"></param>
        /// <returns></returns>
        private Quest LoadQuestJson(int questUid)
        {
            if (questUid <= 0) return null;
            // 기존에 불러온 정보가 있으면
            Quest quest = quests.GetValueOrDefault(questUid);
            if (quest != null) return quest;
            
            var info = tableQuest.GetDataByUid(questUid);
            if (info == null) return null;
            string path = Path.Combine(QuestConstants.JsonFolderName, info.FileName);
            try
            {
                TextAsset textFile = Resources.Load<TextAsset>($"{path}");
                if (textFile != null)
                {
                    string content = textFile.text;
                    if (!string.IsNullOrEmpty(content))
                    {
                        quest = JsonConvert.DeserializeObject<Quest>(content);
                        if (quest != null)
                        {
                            quests.TryAdd(questUid, quest);
                            return quest;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                GcLogger.LogError($"퀘스트 json 파일을 불러오는중 오류가 발생했습니다. {path}: {ex.Message}");
            }
            return null;
        }
        /// <summary>
        /// quest 상태 변경
        /// </summary>
        /// <param name="questUid"></param>
        /// <param name="stepIndex"></param>
        /// <param name="status"></param>
        private void ChangeStatus(int questUid, int stepIndex, QuestConstants.Status status)
        {
            questData.SaveStatus(questUid, stepIndex, status);
        }
        /// <summary>
        /// UIWindowHudQuest 에 element 추가하기 
        /// </summary>
        /// <param name="questUid"></param>
        /// <param name="questStepIndex"></param>
        private void AddHudQuestElement(int questUid, int questStepIndex)
        {
            if (questUid <= 0) return;
            uiWindowHudQuest?.AddQuestElement(questUid, questStepIndex);
        }
        /// <summary>
        /// 다음 목표 시작
        /// 다음 목표가 없으면 end 처리 
        /// </summary>
        /// <param name="questUid"></param>
        public void NextStep(int questUid)
        {
            var quest = quests.GetValueOrDefault(questUid);
            if (quest == null)
            {
                GcLogger.LogError("quest 테이블에 없는 퀘스트 입니다. quest uid:"+questUid);
                return;
            }
            var stepDict = activeHandlers.GetValueOrDefault(questUid);
            if (stepDict == null)
            {
                GcLogger.LogError("진행중인 퀘스트가 아닙니다. quest uid:"+questUid);
                return;
            }
            
            // 현재 step 가져오기
            QuestSaveData questSaveData = questData.GetQuestData(questUid);
            // 현제 handler 지우기
            DisposeQuestStepHandlers(questUid, questSaveData.QuestStepIndex);
            
            int nextStepIndex = questSaveData.QuestStepIndex + 1;
            QuestStep questStep = GetQuestStep(questUid, nextStepIndex);
            // 다음 단계가 없으면 종료 처리
            if (questStep == null)
            {
                EndQuest(questUid);
            }
            else
            {
                // count 초기화 먼저 해주기
                questData.SaveCount(questUid, 0);
                StartObjective(questUid, nextStepIndex, questStep.targetUid);
            }
        }
        /// <summary>
        /// 퀘스트 완료 처리
        /// </summary>
        /// <param name="questUid"></param>
        private void EndQuest(int questUid)
        {
            if (questUid <= 0) return;
            QuestSaveData questSaveData = questData.GetQuestData(questUid);
            if (questSaveData == null) return;
            // 보상 주기
            GiveReward(questUid);
            // 인벤토리 공간 부족할때
            uiWindowQuestReward?.SetRewardInfoByQuestUid(questUid);
            
            // 저장하기
            questData.SaveStatus(questUid, questSaveData.QuestStepIndex, QuestConstants.Status.End);
            
            // UIWindowHudQuest 에 element 빼기
            uiWindowHudQuest?.RemoveQuestElement(questUid);
        }

        private void GiveReward(int questUid)
        {
            if (questUid <= 0) return;
            Quest quest = quests.GetValueOrDefault(questUid);
            if (quest == null)
            {
                GcLogger.LogError("quest json 정보가 없습니다. uid: "+questUid);
                return;
            }

            if (quest.reward == null)
            {
                GcLogger.LogError("quest 보상 정보가 없습니다. uid: "+questUid);
                return;
            }

            playerData?.AddExp(quest.reward.experience);
            playerData?.AddCurrency(CurrencyConstants.Type.Gold, quest.reward.gold);
            playerData?.AddCurrency(CurrencyConstants.Type.Silver, quest.reward.silver);
            if (quest.reward.items.Count <= 0) return;
            foreach (var rewardItem in quest.reward.items)
            {
                if (rewardItem == null) continue;
                ResultCommon result = inventoryData?.AddItem(rewardItem.itemUid, rewardItem.amount);
                uiWindowInventory?.SetIcons(result);
            }
        }

        /// <summary>
        /// 목표 시작
        /// </summary>
        /// <param name="questUid"></param>
        /// <param name="stepIndex"></param>
        /// <param name="npcUid"></param>
        private void StartObjective(int questUid, int stepIndex, int npcUid = 0)
        {
            QuestStep questStep = GetQuestStep(questUid, stepIndex);
            if (questStep == null)
            {
                GcLogger.LogError("퀘스트 json에 단계 정보가 없습니다. uid: "+questUid + ", stepIndex: "+stepIndex);
                return;
            }
            var handler = handlerFactory.CreateHandler(questStep.objectiveType);
            if (handler == null)
            {
                GcLogger.LogError("퀘스트 목표 정보가 없습니다. uid: "+questUid + ", stepIndex: "+stepIndex+", objecitve: "+questStep.objectiveType);
                return;
            }

            // 퀘스트 진행중으로, stepIndex 업데이트
            // 저장 먼저.
            ChangeStatus(questUid, stepIndex, QuestConstants.Status.InProgress);
            
            // 목표 시작
            if (npcUid <= 0 && questStep.targetUid > 0)
            {
                npcUid = questStep.targetUid;
            }
            handler.StartObjective(questUid, questStep, stepIndex, npcUid);

            if (!activeHandlers.ContainsKey(questUid))
                activeHandlers[questUid] = new Dictionary<int, IObjectiveHandler>();

            activeHandlers[questUid][stepIndex] = handler;
            
            // UIWindowHudQuest 에 element 추가
            AddHudQuestElement(questUid, stepIndex);
        }

        public void CheckStepComplete(int questUid, int stepIndex, QuestStep step)
        {
            if (!activeHandlers.TryGetValue(questUid, out var stepDict)) return;
            if (!stepDict.TryGetValue(stepIndex, out var handler)) return;
            if (handler.IsObjectiveComplete(step))
            {
                GcLogger.Log($"[QuestManager] 퀘스트 {questUid}, 스텝 {stepIndex} 완료!");
                // 다음 단계 or 완료 처리
            }
        }

        public void DisposeQuestHandlers(int questUid)
        {
            if (activeHandlers.TryGetValue(questUid, out var stepDict))
            {
                foreach (var handler in stepDict.Values)
                    handler.OnDispose();
            }

            activeHandlers.Remove(questUid);
        }

        private void DisposeQuestStepHandlers(int questUid, int stepIndex)
        {
            if (!activeHandlers.TryGetValue(questUid, out var stepDict)) return;
            if (!stepDict.TryGetValue(stepIndex, out var handler)) return;
            handler.OnDispose();
            activeHandlers[questUid].Remove(stepIndex);
        }

        public void OnDestroy()
        {
            DisposeAllHandlers();
        }
        private void DisposeAllHandlers()
        {
            foreach (var kvp in activeHandlers)
            {
                foreach (var handler in kvp.Value.Values)
                    handler.OnDispose();
            }

            activeHandlers.Clear();
        }

        public QuestStep GetQuestStep(int questUid, int stepIndex)
        {
            Quest quest = quests.GetValueOrDefault(questUid);
            if (quest == null) return null;
            if (stepIndex < 0 || stepIndex >= quest.steps.Count) return null;
            return quest.steps[stepIndex];
        }

        public Quest GetQuestInfo(int questUid)
        {
            return quests.GetValueOrDefault(questUid);
        }
    }
}