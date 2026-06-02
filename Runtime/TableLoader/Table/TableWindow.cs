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
            TableRowReader reader = ReadRow(data);
            return new StruckTableWindow
            {
                Uid = reader.Int("Uid"),
                Name = reader.String("Name"),
                UseInGame = reader.BoolYN("UseInGame"),
                DefaultActive = reader.BoolYN("DefaultActive"),
                Ordering = reader.Int("Ordering"),
                IsInteraction = reader.BoolYN("IsInteraction"),
                OpenWindowUid = reader.IntArray("OpenWindowUid"),
                CloseWindowUid = reader.IntArray("CloseWindowUid"),
                PrefabName = reader.String("PrefabName"),
            };
        }
    }
}