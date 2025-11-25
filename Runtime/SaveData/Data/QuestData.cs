using System.Collections.Generic;

namespace GGemCo2DCore
{
    public class QuestSaveData
    {
        public int QuestUid;
        public int QuestStepIndex;
        public int Count;
        public QuestConstants.Status Status;

        public QuestSaveData(int questUid, int questStepIndex, QuestConstants.Status status, int count = 0)
        {
            QuestUid = questUid;
            QuestStepIndex = questStepIndex;
            Count = count;
            Status = status;
        }
    }
    /// <summary>
    /// 세이브 데이터 - 퀘스트 정보
    /// </summary>
    public class QuestData : DefaultData, ISaveData
    {
        // public 으로 해야 json 으로 저장된다. 
        public Dictionary<int, QuestSaveData> QuestDatas = new Dictionary<int, QuestSaveData>();
        private UIWindowHudQuest uiWindowHudQuest;

        public void Initialize(TableLoaderManager loader, SaveDataContainer saveDataContainer = null)
        {
            QuestDatas.Clear();
            if (saveDataContainer?.QuestData != null)
            {
                QuestDatas = new Dictionary<int, QuestSaveData>(saveDataContainer.QuestData.QuestDatas);
            }
            uiWindowHudQuest =
                SceneGame.Instance.uIWindowManager?.GetUIWindowByUid<UIWindowHudQuest>(
                    UIWindowConstants.WindowUid.HudQuest);
        }

        private QuestConstants.Status GetStatus(int questUid)
        {
            var data = QuestDatas.GetValueOrDefault(questUid);
            return data?.Status ?? QuestConstants.Status.None;
        }
        public bool IsStatusNone(int questUid)
        {
            return GetStatus(questUid) == QuestConstants.Status.None;
        }
        public bool IsStatusInProgress(int questUid)
        {
            return GetStatus(questUid) == QuestConstants.Status.InProgress;
        }
        public bool IsStatusComplete(int questUid)
        {
            return GetStatus(questUid) == QuestConstants.Status.Complete;
        }
        public bool IsStatusEnd(int questUid)
        {
            return GetStatus(questUid) == QuestConstants.Status.End;
        }

        public void SaveStatus(int questUid, int questStepIndex, QuestConstants.Status status)
        {
            if (QuestDatas.ContainsKey(questUid))
            {
                QuestDatas[questUid].QuestStepIndex = questStepIndex;
                QuestDatas[questUid].Status = status;
            }
            else
            {
                QuestDatas.TryAdd(questUid, new QuestSaveData(questUid, questStepIndex, status));
            }
            SaveDatas();
        }

        public void SaveCount(int questUid, int count)
        {
            if (!QuestDatas.TryGetValue(questUid, out var data)) return;
            data.Count = count;
            uiWindowHudQuest?.SetCount(questUid, count);
            SaveDatas();
        }

        protected override int GetMaxSlotCount()
        {
            return 0;
        }

        public Dictionary<int, QuestSaveData> GetQuestDatas()
        {
            return QuestDatas;
        }
        /// <summary>
        /// 진행중인 퀘스트 찾기
        /// </summary>
        /// <param name="mapUid"></param>
        /// <param name="npcUid"></param>
        /// <returns></returns>
        public List<QuestSaveData> GetInProgressQuest(int mapUid, int npcUid)
        {
            List<QuestSaveData> questUids = new List<QuestSaveData>();
            foreach (var data in QuestDatas)
            {
                int uid = data.Value.QuestUid;
                int stepIndex = data.Value.QuestStepIndex;
                QuestConstants.Status status = data.Value.Status;
                if (data.Value.QuestUid <= 0) continue;
                QuestStep questStep = SceneGame.Instance.QuestManager.GetQuestStep(uid, stepIndex);
                if (questStep == null || questStep.targetUid <= 0) continue;
                if (questStep.mapUid != mapUid) continue;
                if (questStep.targetUid != npcUid) continue;
                if (status != QuestConstants.Status.InProgress) continue;
                questUids.Add(new QuestSaveData(data.Value.QuestUid, data.Value.QuestStepIndex, data.Value.Status));
            }

            return questUids;
        }

        public int GetCount(int questUid)
        {
            var data = QuestDatas.GetValueOrDefault(questUid);
            return data?.Count ?? 0;
        }

        public QuestSaveData GetQuestData(int questUid)
        {
            return QuestDatas.GetValueOrDefault(questUid);
        }
    }
}