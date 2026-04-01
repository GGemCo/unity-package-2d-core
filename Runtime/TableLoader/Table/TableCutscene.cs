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
            return new StruckTableCutscene
            {
                Uid = MathHelper.ParseInt(data["Uid"]),
                PreLoad = ConvertBoolean(data["PreLoad"]),
                Memo = data["Memo"],
                FileName = data["FileName"],
            };
        }
    }
}