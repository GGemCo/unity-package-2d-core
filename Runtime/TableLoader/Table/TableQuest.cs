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
        private static readonly Dictionary<int, Dictionary<int, List<int>>> QuestUids = new Dictionary<int, Dictionary<int, List<int>>>();

        protected override void PreLoad()
        {
            QuestUids.Clear();
        }
        protected override void OnLoadedData(StruckTableQuest data)
        {
            int mapUid = data.MapUid;
            int npcUid = data.NpcUid;
            int questUid = data.Uid;
            if (QuestUids.ContainsKey(mapUid) != true)
            {
                Dictionary<int, List<int>> newData = new Dictionary<int, List<int>>();
                List<int> newData2 = new List<int> { questUid };
                newData.TryAdd(npcUid, newData2);
                QuestUids.TryAdd(mapUid, newData);
            }
            else
            {
                Dictionary<int, List<int>> newData = QuestUids[mapUid];
                if (newData.ContainsKey(npcUid) != true)
                {
                    List<int> newData2 = new List<int> { questUid };
                    newData.TryAdd(npcUid, newData2);
                }
                else
                {
                    List<int> newData2 = QuestUids[mapUid][npcUid];
                    newData2.Add(questUid);
                }
            }
        }
        protected override StruckTableQuest BuildRow(Dictionary<string, string> data)
        {
            return new StruckTableQuest
            {
                Uid = MathHelper.ParseInt(data["Uid"]),
                Type = EnumHelper.ConvertEnum<QuestConstants.Type>(data["Type"]),
                Name = data["Name"],
                FileName = data["FileName"],
                MapUid = MathHelper.ParseInt(data["MapUid"]),
                NpcUid = MathHelper.ParseInt(data["NpcUid"]),
            };
        }
        public List<int> GetQuestsByNpcUnum(int mapUid, int npcUid)
        {
            List<int> empty = new List<int>();
            if (QuestUids.TryGetValue(mapUid, out var npcUids) != true || npcUids.ContainsKey(npcUid) != true)
                return empty;
            return QuestUids[mapUid][npcUid];
        }
    }
}