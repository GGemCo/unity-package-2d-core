
namespace GGemCo.Scripts
{
    /// <summary>
    /// 맵 테이블 Structure
    /// </summary>
    public class StruckTableCutscene
    {
        public int Uid;
        public string Memo;
        public string FileName;
    }
    /// <summary>
    /// 맵 테이블
    /// </summary>
    public class TableCutscene : DefaultTable
    {
        public StruckTableCutscene GetDataByUid(int uid)
        {
            if (uid <= 0)
            {
                GcLogger.LogError("uid is 0.");
                return null;
            }
            var data = GetData(uid);
            if (data == null) return null;
            return new StruckTableCutscene
            {
                Uid = int.Parse(data["Uid"]),
                Memo = data["Memo"],
                FileName = data["FileName"],
            };
        }
    }
}