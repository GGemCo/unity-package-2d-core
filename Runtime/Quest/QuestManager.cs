using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Newtonsoft.Json;
using UnityEngine;

namespace GGemCo2DCore
{
    /// <summary>
    /// 퀘스트 매니저
    /// </summary>
    public class QuestManager
    {
        private SceneGame _sceneGame;
        private TableQuest _tableQuest;
        private UIWindowHudQuest _uiWindowHudQuest;
        private UIWindowQuestReward _uiWindowQuestReward;
        private UIWindowInventory _uiWindowInventory;
        private QuestData _questData;
        private PlayerData _playerData;
        private InventoryData _inventoryData;
        private bool _isQuestJsonLoaded;
        private bool _isRegisteredMapEntered;
        private int _pendingMapEnteredUid;
        
        private readonly ObjectiveHandlerFactory _handlerFactory = new ObjectiveHandlerFactory();

        // QuestUid → StepIndex → Handler
        private readonly Dictionary<int, Dictionary<int, IObjectiveHandler>> _activeHandlers =
            new Dictionary<int, Dictionary<int, IObjectiveHandler>>();
        
        private readonly Dictionary<int, Quest> _quests = new Dictionary<int, Quest>();
        
        public void Initialize(SceneGame scene)
        {
            _quests.Clear();
            _activeHandlers.Clear();
            _isQuestJsonLoaded = false;
            _pendingMapEnteredUid = 0;
            _sceneGame = scene;
            _tableQuest = TableLoaderManager.Instance.TableQuest;
        }

        /// <summary>
        /// 게임 씬 시작 후 저장 데이터와 UI 참조를 연결하고 퀘스트 이벤트를 구독합니다.
        /// </summary>
        public void OnStartBySceneGame()
        {
            _questData = _sceneGame.saveDataManager.Quest;
            _playerData = _sceneGame.saveDataManager.Player;
            _inventoryData = _sceneGame.saveDataManager.Inventory;
            _uiWindowHudQuest =
                _sceneGame.uIWindowManager?.GetUIWindowByUid<UIWindowHudQuest>(UIWindowConstants.WindowUid.HudQuest);
            _uiWindowQuestReward =
                _sceneGame.uIWindowManager?.GetUIWindowByUid<UIWindowQuestReward>(UIWindowConstants.WindowUid.QuestReward);
            _uiWindowInventory =
                _sceneGame.uIWindowManager?.GetUIWindowByUid<UIWindowInventory>(UIWindowConstants.WindowUid.Inventory);
            RegisterMapEnteredEvent();
            _ = LoadAllQuestJson();
        }

        /// <summary>
        /// 맵 입장 이벤트를 중복 없이 구독합니다.
        /// </summary>
        private void RegisterMapEnteredEvent()
        {
            if (_isRegisteredMapEntered) return;
            GameEventManager.MapEnteredEvent += OnMapEntered;
            _isRegisteredMapEntered = true;
        }
        /// <summary>
        /// 저장되어있는 퀘스트 불러오기
        /// </summary>
        private void LoadQuestDatas()
        {
            _questData = _sceneGame.saveDataManager.Quest;
            var datas = _questData.GetQuestDatas();
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
        private async Task LoadAllQuestJson()
        {
            var datas = _tableQuest.GetDatas();
            foreach (var data in datas)
            {
                await LoadQuestJson(data.Key);
            }
            
            LoadQuestDatas();
            _isQuestJsonLoaded = true;
            await TryStartPendingEnterMapQuests();
        }

        /// <summary>
        /// 맵 입장 이벤트를 받아 EnterMap 트리거 퀘스트를 시작합니다.
        /// 퀘스트 JSON 적재가 끝나기 전이면 실제 맵 입장 이벤트로 들어온 맵만 보류합니다.
        /// </summary>
        /// <param name="eventData">입장 완료된 맵 정보입니다.</param>
        private async void OnMapEntered(MapEnteredEventData eventData)
        {
            if (eventData.MapUid <= 0) return;
            if (!_isQuestJsonLoaded)
            {
                _pendingMapEnteredUid = eventData.MapUid;
                return;
            }

            await TryStartQuestsByEnterMap(eventData.MapUid);
        }

        /// <summary>
        /// 퀘스트 JSON 로드 전에 수신한 맵 입장 이벤트가 있으면 해당 맵의 EnterMap 퀘스트를 시작합니다.
        /// LoadAllQuestJson 자체에서는 임의의 현재 맵을 사용하지 않고, OnMapLoadComplete에서 발행된 이벤트만 처리합니다.
        /// </summary>
        private async Task TryStartPendingEnterMapQuests()
        {
            if (_pendingMapEnteredUid <= 0) return;

            int mapUid = _pendingMapEnteredUid;
            _pendingMapEnteredUid = 0;
            await TryStartQuestsByEnterMap(mapUid);
        }

        /// <summary>
        /// 지정한 맵에 입장했을 때 자동 시작 가능한 퀘스트를 찾아 시작합니다.
        /// </summary>
        /// <param name="mapUid">입장한 맵 UID입니다.</param>
        private async Task TryStartQuestsByEnterMap(int mapUid)
        {
            if (mapUid <= 0 || _tableQuest == null || _questData == null) return;

            List<int> questUids = _tableQuest.GetQuestsByEnterMap(mapUid);
            foreach (int questUid in questUids)
            {
                if (!_questData.IsStatusNone(questUid)) continue;
                await StartQuest(questUid, 0, false);
            }
        }
        /// <summary>
        /// 퀘스트 시작 처리
        /// </summary>
        /// <param name="questUid"></param>
        /// <param name="npcUid">퀘스트를 시작한 NPC UID입니다. 맵 입장 시작 퀘스트는 0을 사용합니다.</param>
        /// <param name="showAlreadyStartedWarning">이미 진행 중일 때 시스템 경고 메시지를 표시할지 여부입니다.</param>
        /// <returns></returns>
        public async Task<bool> StartQuest(int questUid, int npcUid, bool showAlreadyStartedWarning = true)
        {
            if (questUid <= 0) return false;
            var info = _tableQuest.GetDataByUid(questUid);
            if (info == null) return false;

            if (_questData.IsStatusNone(questUid) != true)
            {
                if (showAlreadyStartedWarning)
                {
                    _sceneGame.systemMessageManager.ShowMessageWarning("Quest_InProgress");//"진행중인 퀘스트 입니다."
                }
                return false;
            }

            Quest quest = await LoadQuestJson(questUid);
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
            if (questStep != null && questStep.objectiveType == QuestConstants.ObjectiveType.TalkToNpc)
            {
                int dialogNpcUid = npcUid > 0 ? npcUid : questStep.targetUid;
                var data = new DialogEventData(
                    npcUid: dialogNpcUid
                );
                GameEventManager.DialogStart(data);
            }
            return true;
        }
        /// <summary>
        /// 퀘스트 json 불러오기
        /// </summary>
        /// <param name="questUid"></param>
        /// <returns></returns>
        private async Task<Quest> LoadQuestJson(int questUid)
        {
            if (questUid <= 0) return null;
            // 기존에 불러온 정보가 있으면
            Quest quest = _quests.GetValueOrDefault(questUid);
            if (quest != null) return quest;
            
            var info = _tableQuest.GetDataByUid(questUid);
            if (info == null) return null;
            string key = $"{ConfigAddressableKey.Quest}_{info.Uid}";
            try
            {
                TextAsset textFile = await AddressableLoaderController.LoadByKeyAsync<TextAsset>(key);
                
                if (textFile != null)
                {
                    string content = textFile.text;
                    if (!string.IsNullOrEmpty(content))
                    {
                        quest = JsonConvert.DeserializeObject<Quest>(content);
                        if (quest != null)
                        {
                            _quests.TryAdd(questUid, quest);
                            return quest;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                GcLogger.LogError($"퀘스트 json 파일을 불러오는중 오류가 발생했습니다. {key}: {ex.Message}");
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
            _questData.SaveStatus(questUid, stepIndex, status);
        }
        /// <summary>
        /// UIWindowHudQuest 에 element 추가하기 
        /// </summary>
        /// <param name="questUid"></param>
        /// <param name="questStepIndex"></param>
        private void AddHudQuestElement(int questUid, int questStepIndex)
        {
            if (questUid <= 0) return;
            _uiWindowHudQuest?.AddQuestElement(questUid, questStepIndex);
        }
        /// <summary>
        /// 다음 목표 시작
        /// 다음 목표가 없으면 end 처리 
        /// </summary>
        /// <param name="questUid"></param>
        public void NextStep(int questUid)
        {
            var quest = _quests.GetValueOrDefault(questUid);
            if (quest == null)
            {
                GcLogger.LogError("quest 테이블에 없는 퀘스트 입니다. quest uid:"+questUid);
                return;
            }
            var stepDict = _activeHandlers.GetValueOrDefault(questUid);
            if (stepDict == null)
            {
                GcLogger.LogError("진행중인 퀘스트가 아닙니다. quest uid:"+questUid);
                return;
            }
            
            // 현재 step 가져오기
            QuestSaveData questSaveData = _questData.GetQuestData(questUid);
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
                _questData.SaveCount(questUid, 0);
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
            QuestSaveData questSaveData = _questData.GetQuestData(questUid);
            if (questSaveData == null) return;
            // 보상 주기
            GiveReward(questUid);
            // 인벤토리 공간 부족할때
            _uiWindowQuestReward?.SetRewardInfoByQuestUid(questUid);
            
            // 저장하기
            _questData.SaveStatus(questUid, questSaveData.QuestStepIndex, QuestConstants.Status.End);
            
            // UIWindowHudQuest 에 element 빼기
            _uiWindowHudQuest?.RemoveQuestElement(questUid);
        }

        /// <summary>
        /// 퀘스트 완료 보상을 플레이어 데이터와 맵 진행 데이터에 적용합니다.
        /// </summary>
        /// <param name="questUid">보상을 지급할 퀘스트 UID입니다.</param>
        private void GiveReward(int questUid)
        {
            if (questUid <= 0) return;
            Quest quest = _quests.GetValueOrDefault(questUid);
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

            _playerData?.AddExp(quest.reward.experience);
            _playerData?.AddCurrency(CurrencyConstants.Type.Gold, quest.reward.gold);
            _playerData?.AddCurrency(CurrencyConstants.Type.Silver, quest.reward.silver);
            GiveItemReward(quest.reward.items);
            GiveMapProgressReward(quest.reward.mapProgress);
            GiveLicenseReward(quest.reward.licenses);
        }

        /// <summary>
        /// 퀘스트 아이템 보상을 인벤토리에 추가하고 인벤토리 UI를 갱신합니다.
        /// </summary>
        /// <param name="items">지급할 아이템 보상 목록입니다.</param>
        private void GiveItemReward(List<RewardItem> items)
        {
            if (items == null || items.Count <= 0) return;

            foreach (var rewardItem in items)
            {
                if (rewardItem == null) continue;
                ResultCommon result = _inventoryData?.AddItem(rewardItem.itemUid, rewardItem.amount);
                _uiWindowInventory?.SetIcons(result);
            }
        }

        /// <summary>
        /// 퀘스트 완료 보상으로 맵 클리어와 월드맵 노드 활성화를 적용합니다.
        /// </summary>
        /// <param name="mapProgress">맵 진행 보상 정보입니다.</param>
        private void GiveMapProgressReward(QuestRewardMapProgress mapProgress)
        {
            if (mapProgress == null) return;

            bool hasClearMapReward = mapProgress.clearMapUid > 0;
            bool hasWorldMapNodes = mapProgress.activateWorldMapNodeIds != null &&
                                    mapProgress.activateWorldMapNodeIds.Count > 0;
            if (!hasClearMapReward && !hasWorldMapNodes) return;

            _sceneGame.saveDataManager.MapProgressController.ClearMap(
                mapProgress.clearMapUid,
                mapProgress.activateWorldMapNodeIds);
        }

        /// <summary>
        /// 퀘스트 완료 보상으로 라이센스 값을 저장합니다.
        /// </summary>
        /// <param name="licenses">저장할 라이센스 보상 목록입니다.</param>
        private void GiveLicenseReward(List<QuestRewardLicense> licenses)
        {
            if (licenses == null || licenses.Count <= 0) return;

            LicenseManager licenseManager = _sceneGame?.saveDataManager?.LicenseManager;
            if (licenseManager == null) return;

            foreach (QuestRewardLicense license in licenses)
            {
                if (license == null || license.licenseUid <= 0) continue;

                string value = string.IsNullOrWhiteSpace(license.value)
                    ? LicenseConstants.TrueValue
                    : license.value;
                licenseManager.SetByUid(license.licenseUid, value);
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
            var handler = _handlerFactory.CreateHandler(questStep.objectiveType);
            if (handler == null)
            {
                GcLogger.LogError("퀘스트 목표 정보가 없습니다. uid: "+questUid + ", stepIndex: "+stepIndex+", objecitve: "+questStep.objectiveType);
                return;
            }

            // 퀘스트 진행중으로, stepIndex 업데이트
            // 저장 먼저.
            ChangeStatus(questUid, stepIndex, QuestConstants.Status.InProgress);
            
            if (!_activeHandlers.ContainsKey(questUid))
                _activeHandlers[questUid] = new Dictionary<int, IObjectiveHandler>();

            _activeHandlers[questUid][stepIndex] = handler;
            
            // UIWindowHudQuest 에 element 추가
            AddHudQuestElement(questUid, stepIndex);

            // 목표 시작
            if (npcUid <= 0 && questStep.targetUid > 0)
            {
                npcUid = questStep.targetUid;
            }
            handler.StartObjective(questUid, questStep, stepIndex, npcUid);
        }

        public void CheckStepComplete(int questUid, int stepIndex, QuestStep step)
        {
            if (!_activeHandlers.TryGetValue(questUid, out var stepDict)) return;
            if (!stepDict.TryGetValue(stepIndex, out var handler)) return;
            if (handler.IsObjectiveComplete(step))
            {
                GcLogger.Log($"[QuestManager] 퀘스트 {questUid}, 스텝 {stepIndex} 완료!");
                // 다음 단계 or 완료 처리
            }
        }

        /// <summary>
        /// 지정한 맵에서 특정 목표 타입의 퀘스트가 진행 중인지 확인합니다.
        /// </summary>
        /// <param name="mapUid">확인할 맵 UID입니다.</param>
        /// <param name="objectiveType">확인할 목표 타입입니다.</param>
        /// <returns>해당 맵에서 목표 타입이 진행 중이면 true입니다.</returns>
        public bool HasActiveObjective(int mapUid, QuestConstants.ObjectiveType objectiveType)
        {
            if (mapUid <= 0) return false;

            foreach (var questHandlers in _activeHandlers)
            {
                int questUid = questHandlers.Key;
                foreach (int stepIndex in questHandlers.Value.Keys)
                {
                    QuestStep questStep = GetQuestStep(questUid, stepIndex);
                    if (questStep == null) continue;
                    if (questStep.objectiveType != objectiveType) continue;
                    if (questStep.mapUid != mapUid) continue;
                    return true;
                }
            }

            return false;
        }

        public void DisposeQuestHandlers(int questUid)
        {
            if (_activeHandlers.TryGetValue(questUid, out var stepDict))
            {
                foreach (var handler in stepDict.Values)
                    handler.OnDispose();
            }

            _activeHandlers.Remove(questUid);
        }

        private void DisposeQuestStepHandlers(int questUid, int stepIndex)
        {
            if (!_activeHandlers.TryGetValue(questUid, out var stepDict)) return;
            if (!stepDict.TryGetValue(stepIndex, out var handler)) return;
            handler.OnDispose();
            _activeHandlers[questUid].Remove(stepIndex);
        }

        public void OnDestroy()
        {
            if (_isRegisteredMapEntered)
            {
                GameEventManager.MapEnteredEvent -= OnMapEntered;
                _isRegisteredMapEntered = false;
            }

            DisposeAllHandlers();
        }
        private void DisposeAllHandlers()
        {
            foreach (var kvp in _activeHandlers)
            {
                foreach (var handler in kvp.Value.Values)
                    handler.OnDispose();
            }

            _activeHandlers.Clear();
        }

        public QuestStep GetQuestStep(int questUid, int stepIndex)
        {
            Quest quest = _quests.GetValueOrDefault(questUid);
            if (quest == null) return null;
            if (stepIndex < 0 || stepIndex >= quest.steps.Count) return null;
            return quest.steps[stepIndex];
        }

        public Quest GetQuestInfo(int questUid)
        {
            return _quests.GetValueOrDefault(questUid);
        }
    }
}
