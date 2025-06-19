using System.Collections.Generic;
using UnityEngine;

namespace GGemCo.Scripts
{
    public class NpcQuestData
    {
        public int QuestUid;
        public int QuestStepIndex;
        public int Count;
        public QuestConstants.Status Status;

        public NpcQuestData(int questUid, int questStepIndex, QuestConstants.Status status, int count = 0)
        {
            QuestUid = questUid;
            QuestStepIndex = questStepIndex;
            Count = count;
            Status = status;
        }
    }
    /// <summary>
    /// Npc 퀘스트 관리
    /// </summary>
    public class NpcQuestController : MonoBehaviour
    {
        private Npc npc;
        private int readyQuestCount;
        private int inProgressQuestCount;
        private QuestData questData;
        private GameObject iconQuestReady;
        private GameObject iconQuestInProgress;
        private readonly Vector3 diffPosition = new Vector3(0, 40f, 0);
        private GameObject containerNpcName;
        private List<int> questUidByReady;
        private List<QuestSaveData> questUidByInProgress;
        private void Awake()
        {
            questUidByReady = new List<int>();
            questUidByInProgress = new List<QuestSaveData>();
            npc = GetComponent<Npc>();
        }
        private void OnDestroy()
        {
            Destroy(iconQuestReady);
            Destroy(iconQuestInProgress);
        }

        private void Start()
        {
            // 순서 중요
            questData = SceneGame.Instance.saveDataManager.Quest;
            CreateIconMark();
            LoadQuest();
        }
        /// <summary>
        /// 느낌표, 물음표 아이콘 생성
        /// </summary>
        private void CreateIconMark()
        {
            if (containerNpcName == null)
            {
                containerNpcName = SceneGame.Instance.containerDropItemName;
            }
            GameObject prefabIconQuestReady =
                AddressablePrefabLoader.Instance.GetPreLoadGamePrefabByName(ConfigAddressables.KeyPrefabIconQuestReady);
            GameObject prefabIconQuestInProgress =
                AddressablePrefabLoader.Instance.GetPreLoadGamePrefabByName(ConfigAddressables.KeyPrefabIconQuestInProgress);
            if (prefabIconQuestReady != null)
            {
                iconQuestReady = Instantiate(prefabIconQuestReady, containerNpcName.transform);
                iconQuestReady.SetActive(false);
                Vector3 npcNameWorldPosition = npc.gameObject.transform.position +
                                               new Vector3(0, npc.GetHeightByScale(), 0) + diffPosition;
                iconQuestReady.transform.position = npcNameWorldPosition;
            }
            if (prefabIconQuestInProgress != null)
            {
                iconQuestInProgress = Instantiate(prefabIconQuestInProgress, containerNpcName.transform);
                iconQuestInProgress.SetActive(false);
                Vector3 npcNameWorldPosition = npc.gameObject.transform.position +
                                               new Vector3(0, npc.GetHeightByScale(), 0) + diffPosition;
                iconQuestInProgress.transform.position = npcNameWorldPosition;
            }
        }
        /// <summary>
        /// 컬링으로 인해 활성, 비활성 되기때문에 OnEnable 에서 처리한다
        /// </summary>
        private void OnEnable()
        {
            LoadQuest();
        }
        /// <summary>
        /// 받을 수 있는 퀘스트, 진행중인 퀘스트 찾기
        /// </summary>
        public void LoadQuest()
        {
            if (npc?.CharacterRegenData == null) return;
            if (questData == null) return;
            // 현재 맵에서 Npc 가 시작할 수 있는 퀘스트 찾기
            questUidByReady =
                TableLoaderManager.Instance.TableQuest.GetQuestsByNpcUnum(npc.CharacterRegenData.MapUid, npc.uid);
            // GcLogger.Log("npc.uid: "+npc.uid+" / questuid: "+string.Join(", ", questUids));
            
            // quest 테이블 내용으로는 시작 퀘스트를 찾는다.
            readyQuestCount = 0;
            foreach (int questUid in questUidByReady)
            {
                if (questData.IsStatusNone(questUid))
                {
                    readyQuestCount++;
                }
            }
            
            // QuestData 에 저장된 정보로 진행중인 퀘스트를 처리한다.
            questUidByInProgress = questData.GetInProgressQuest(npc.CharacterRegenData.MapUid, npc.uid);
            
            // 느낌표
            if (readyQuestCount > 0)
            {
                iconQuestReady.SetActive(true);
                iconQuestInProgress.SetActive(false);
            }
            // 물음표
            else if (questUidByInProgress.Count > 0) 
            {
                iconQuestReady.SetActive(false);
                iconQuestInProgress.SetActive(true);
            }
            else
            {
                iconQuestReady.SetActive(false);
                iconQuestInProgress.SetActive(false);
            }
        }
        /// <summary>
        /// 현재 Npc 가 진행할 수 있는 퀘스트 가져오기
        /// 받을 수 있거나 Talk To Npc 이거나
        /// </summary>
        /// <returns></returns>
        public List<NpcQuestData> GetQuestInfos()
        {
            List<NpcQuestData> questInfos = new List<NpcQuestData>();
            // 받을 수 있는 퀘스트
            foreach (var uid in questUidByReady)
            {
                if (questData.IsStatusNone(uid) != true) continue;
                questInfos.Add(new NpcQuestData(uid, 0, QuestConstants.Status.Ready));
            }
            // 진행중인 퀘스트 중 target Uid 가 현재 캐릭터와 같으면
            foreach (QuestSaveData questSaveData in questUidByInProgress)
            {
                QuestStep questStep = SceneGame.Instance.QuestManager.GetQuestStep(questSaveData.QuestUid, questSaveData.QuestStepIndex);
                if (questStep == null || questStep.objectiveType != QuestConstants.ObjectiveType.TalkToNpc ||
                    questStep.targetUid != npc.uid) continue;
                questInfos.Add(new NpcQuestData(questSaveData.QuestUid,questSaveData.QuestStepIndex, questSaveData.Status, questSaveData.Count));
            }

            return questInfos;
        }
    }
}