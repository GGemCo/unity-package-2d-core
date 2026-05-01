using System.Collections.Generic;

namespace GGemCo2DCore
{
    /// <summary>
    /// 퀘스트 테이블 Structure
    /// </summary>
    public class StruckTableQuest : IUidName
    {
        public int Uid { get; set; }
        public string Name { get; set; }
        public QuestConstants.Type Type;
        public QuestConstants.TriggerType TriggerType;
        public string FileName;
        public int MapUid;
        public int NpcUid;
    }

    /// <summary>
    /// 퀘스트 테이블
    /// </summary>
    public class TableQuest : DefaultTable<StruckTableQuest>
    {
        public override string Key => ConfigAddressableTable.Quest;
        private static readonly Dictionary<int, Dictionary<int, List<int>>> QuestUidsByNpc = new Dictionary<int, Dictionary<int, List<int>>>();
        private static readonly Dictionary<int, List<int>> QuestUidsByEnterMap = new Dictionary<int, List<int>>();

        /// <summary>
        /// 퀘스트 테이블 재적재 전에 시작 조건별 인덱스를 초기화합니다.
        /// </summary>
        protected override void PreLoad()
        {
            QuestUidsByNpc.Clear();
            QuestUidsByEnterMap.Clear();
        }

        /// <summary>
        /// 테이블 행을 시작 조건별 조회 캐시에 등록합니다.
        /// </summary>
        /// <param name="data">적재가 완료된 퀘스트 테이블 행입니다.</param>
        protected override void OnLoadedData(StruckTableQuest data)
        {
            if (data == null || data.Uid <= 0) return;

            switch (data.TriggerType)
            {
                case QuestConstants.TriggerType.TalkToNpc:
                    AddTalkToNpcQuest(data.MapUid, data.NpcUid, data.Uid);
                    break;
                case QuestConstants.TriggerType.EnterMap:
                    AddEnterMapQuest(data.MapUid, data.Uid);
                    break;
            }
        }

        /// <summary>
        /// NPC 대화로 시작되는 퀘스트를 맵 UID와 NPC UID 기준으로 인덱싱합니다.
        /// </summary>
        /// <param name="mapUid">퀘스트를 시작할 맵 UID입니다.</param>
        /// <param name="npcUid">퀘스트를 시작할 NPC UID입니다.</param>
        /// <param name="questUid">등록할 퀘스트 UID입니다.</param>
        private static void AddTalkToNpcQuest(int mapUid, int npcUid, int questUid)
        {
            if (mapUid <= 0 || npcUid <= 0 || questUid <= 0) return;

            if (QuestUidsByNpc.ContainsKey(mapUid) != true)
            {
                Dictionary<int, List<int>> newData = new Dictionary<int, List<int>>();
                List<int> newData2 = new List<int> { questUid };
                newData.TryAdd(npcUid, newData2);
                QuestUidsByNpc.TryAdd(mapUid, newData);
            }
            else
            {
                Dictionary<int, List<int>> newData = QuestUidsByNpc[mapUid];
                if (newData.ContainsKey(npcUid) != true)
                {
                    List<int> newData2 = new List<int> { questUid };
                    newData.TryAdd(npcUid, newData2);
                }
                else
                {
                    List<int> newData2 = QuestUidsByNpc[mapUid][npcUid];
                    newData2.Add(questUid);
                }
            }
        }

        /// <summary>
        /// 맵 입장으로 시작되는 퀘스트를 맵 UID 기준으로 인덱싱합니다.
        /// </summary>
        /// <param name="mapUid">입장 조건으로 사용할 맵 UID입니다.</param>
        /// <param name="questUid">등록할 퀘스트 UID입니다.</param>
        private static void AddEnterMapQuest(int mapUid, int questUid)
        {
            if (mapUid <= 0 || questUid <= 0) return;

            if (!QuestUidsByEnterMap.TryGetValue(mapUid, out List<int> questUids))
            {
                questUids = new List<int>();
                QuestUidsByEnterMap.TryAdd(mapUid, questUids);
            }

            questUids.Add(questUid);
        }

        /// <summary>
        /// 퀘스트 테이블 행을 강타입 데이터로 변환합니다.
        /// TriggerType 컬럼이 없는 기존 테이블은 TalkToNpc로 보정합니다.
        /// </summary>
        /// <param name="data">헤더명과 값을 담은 테이블 행 사전입니다.</param>
        /// <returns>변환된 퀘스트 테이블 행입니다.</returns>
        protected override StruckTableQuest BuildRow(Dictionary<string, string> data)
        {
            return new StruckTableQuest
            {
                Uid = MathHelper.ParseInt(data["Uid"]),
                Type = EnumHelper.ConvertEnum<QuestConstants.Type>(data["Type"]),
                TriggerType = GetTriggerType(data),
                Name = data["Name"],
                FileName = data["FileName"],
                MapUid = MathHelper.ParseInt(data["MapUid"]),
                NpcUid = MathHelper.ParseInt(data["NpcUid"]),
            };
        }

        /// <summary>
        /// 테이블의 TriggerType 문자열을 퀘스트 시작 조건 enum으로 변환합니다.
        /// </summary>
        /// <param name="data">퀘스트 테이블 행 사전입니다.</param>
        /// <returns>파싱된 시작 조건입니다.</returns>
        private static QuestConstants.TriggerType GetTriggerType(Dictionary<string, string> data)
        {
            if (!data.ContainsKey("TriggerType"))
            {
                return QuestConstants.TriggerType.TalkToNpc;
            }

            string triggerType = data.GetValueOrDefault("TriggerType");
            return string.IsNullOrWhiteSpace(triggerType)
                ? QuestConstants.TriggerType.None
                : EnumHelper.ConvertEnum<QuestConstants.TriggerType>(triggerType);
        }

        /// <summary>
        /// 지정한 맵의 NPC와 대화하여 시작할 수 있는 퀘스트 UID 목록을 반환합니다.
        /// </summary>
        /// <param name="mapUid">NPC가 배치된 맵 UID입니다.</param>
        /// <param name="npcUid">대화 대상 NPC UID입니다.</param>
        /// <returns>시작 가능한 퀘스트 UID 목록입니다.</returns>
        public List<int> GetQuestsByNpcUid(int mapUid, int npcUid)
        {
            List<int> empty = new List<int>();
            if (QuestUidsByNpc.TryGetValue(mapUid, out var npcUids) != true || npcUids.ContainsKey(npcUid) != true)
                return empty;
            return QuestUidsByNpc[mapUid][npcUid];
        }

        /// <summary>
        /// 지정한 맵에 입장했을 때 자동 시작할 퀘스트 UID 목록을 반환합니다.
        /// </summary>
        /// <param name="mapUid">입장한 맵 UID입니다.</param>
        /// <returns>자동 시작할 퀘스트 UID 목록입니다.</returns>
        public List<int> GetQuestsByEnterMap(int mapUid)
        {
            return QuestUidsByEnterMap.TryGetValue(mapUid, out List<int> questUids)
                ? questUids
                : new List<int>();
        }
    }
}
