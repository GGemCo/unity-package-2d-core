using System.Collections.Generic;

namespace GGemCo2DCore
{
    /// <summary>
    /// Window 테이블 Structure
    /// </summary>
    public class StruckTableWindow
    {
        public int Uid;
        public string Name;
        public bool UseInGame;
        public bool DefaultActive;
        public int Ordering;
        public bool IsInteraction;
        public int[] OpenWindowUid;
        public int[] CloseWindowUid;
        public string PrefabName;
    }

    /// <summary>
    /// Window 테이블
    /// </summary>
    public class TableWindow : DefaultTable<StruckTableWindow>
    {
        public override string Key => ConfigAddressableTable.Window;
        protected override StruckTableWindow BuildRow(Dictionary<string, string> data)
        {
            return new StruckTableWindow
            {
                Uid = MathHelper.ParseInt(data["Uid"]),
                Name = data["Name"],
                UseInGame = ConvertBoolean(data["UseInGame"]),
                DefaultActive = ConvertBoolean(data["DefaultActive"]),
                Ordering = MathHelper.ParseInt(data["Ordering"]),
                IsInteraction = ConvertBoolean(data["IsInteraction"]),
                OpenWindowUid = ConvertIntArray(data["OpenWindowUid"]),
                CloseWindowUid = ConvertIntArray(data["CloseWindowUid"]),
                PrefabName = data["PrefabName"],
            };
        }
    }
}