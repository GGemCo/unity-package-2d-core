
namespace GGemCo2DCore
{
    /// <summary>
    /// 어펙트 테이블 Structure
    /// </summary>
    public class StruckTableNpcGatheringCount
    {
        public int Uid;
        public string Memo;
        public int DropItemUid;
        public int DropItemValue;
    }
    /// <summary>
    /// 어펙트 테이블
    /// </summary>
    public class TableNpcGatheringCount : DefaultTable
    {
        public StruckTableNpcGatheringCount GetDataByUid(int uid)
        {
            if (uid <= 0)
            {
                GcLogger.LogError("uid is 0.");
                return null;
            }
            var data = GetData(uid);
            if (data == null) return null;
            return new StruckTableNpcGatheringCount
            {
                Uid = int.Parse(data["Uid"]),
                Memo = data["Memo"],
                DropItemUid = int.Parse(data["DropItemUid"]),
                DropItemValue = int.Parse(data["DropItemValue"]),
            };
        }
    }
}