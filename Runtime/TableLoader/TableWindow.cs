
namespace GGemCo.Scripts
{
    /// <summary>
    /// Window 테이블 Structure
    /// </summary>
    public class StruckTableWindow
    {
        public int Uid;
        public string Name;
        public bool DefaultActive;
        public int Ordering;
        public bool IsInteraction;
        public int[] OpenWindowUid;
        public int[] CloseWindowUid;
    }

    /// <summary>
    /// Window 테이블
    /// </summary>
    public class TableWindow : DefaultTable
    {
        public StruckTableWindow GetDataByUid(int uid)
        {
            if (uid <= 0)
            {
                GcLogger.LogError("uid is 0.");
                return null;
            }

            var data = GetData(uid);
            if (data == null) return null;
            return new StruckTableWindow
            {
                Uid = int.Parse(data["Uid"]),
                Name = data["Name"],
                DefaultActive = ConvertBoolean(data["DefaultActive"]),
                Ordering = int.Parse(data["Ordering"]),
                IsInteraction = ConvertBoolean(data["IsInteraction"]),
                OpenWindowUid = ConvertIntArray(data["OpenWindowUid"]),
                CloseWindowUid = ConvertIntArray(data["CloseWindowUid"]),
            };
        }
    }
}