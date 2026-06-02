using System.Collections.Generic;

namespace GGemCo2DCore
{
    /// <summary>
    /// 연출 테이블 Structure
    /// </summary>
    public class StruckTableCutscene
    {
        public int Uid;
        public bool PreLoad;
        public string Memo;
        public string FileName;
    }
    /// <summary>
    /// 연출 테이블
    /// </summary>
    public class TableCutscene : DefaultTable<StruckTableCutscene>
    {
        public override string Key => ConfigAddressableTable.Cutscene;
        protected override StruckTableCutscene BuildRow(Dictionary<string, string> data)
        {
            TableRowReader reader = ReadRow(data);
            return new StruckTableCutscene
            {
                Uid = reader.Int("Uid"),
                PreLoad = reader.BoolYN("PreLoad"),
                Memo = reader.String("Memo"),
                FileName = reader.String("FileName"),
            };
        }
    }
}