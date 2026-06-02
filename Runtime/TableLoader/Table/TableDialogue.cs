using System.Collections.Generic;

namespace GGemCo2DCore
{
    /// <summary>
    /// 대사 테이블 Structure
    /// </summary>
    public class StruckTableDialogue : IUidName
    {
        public int Uid { get; set; }
        public string Name { get; set; }
        public string Memo;
        public string FileName;
    }
    /// <summary>
    /// 대사 테이블
    /// </summary>
    public class TableDialogue : DefaultTable<StruckTableDialogue>
    {
        public override string Key => ConfigAddressableTable.Dialogue;
        protected override StruckTableDialogue BuildRow(Dictionary<string, string> data)
        {
            TableRowReader reader = ReadRow(data);
            return new StruckTableDialogue
            {
                Uid = reader.Int("Uid"),
                Memo = reader.String("Memo"),
                FileName = reader.String("FileName"),
            };
        }
    }
}