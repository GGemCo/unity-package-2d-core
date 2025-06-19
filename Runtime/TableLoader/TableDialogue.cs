namespace GGemCo.Scripts
{
    /// <summary>
    /// 맵 테이블 Structure
    /// </summary>
    public class StruckTableDialogue : IUidName
    {
        public int Uid { get; set; }
        public string Name { get; set; }
        public string Memo;
        public string FileName;
    }
    /// <summary>
    /// 맵 테이블
    /// </summary>
    public class TableDialogue : DefaultTable
    {
        public StruckTableDialogue GetDataByUid(int uid)
        {
            if (uid <= 0)
            {
                GcLogger.LogError("uid is 0.");
                return null;
            }
            var data = GetData(uid);
            if (data == null) return null;
            return new StruckTableDialogue
            {
                Uid = int.Parse(data["Uid"]),
                Memo = data["Memo"],
                FileName = data["FileName"],
            };
        }
        public override bool TryGetDataByUid(int uid, out object info)
        {
            info = GetDataByUid(uid);
            return info != null && ((StruckTableDialogue)info).Uid > 0;
        }
    }
}