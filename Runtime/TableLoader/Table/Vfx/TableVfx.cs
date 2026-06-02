using System.Collections.Generic;

namespace GGemCo2DCore
{
    /// <summary>
    /// 외부 시스템이 참조하는 대표 VFX 테이블 행입니다.
    /// 실제 프리팹 리소스는 vfx_effect / vfx_particle 또는 vfx_variant 후보를 통해 해석됩니다.
    /// </summary>
    public class StruckTableVfx : IUidName
    {
        public int Uid { get; set; }
        public string Name { get; set; }
        public VfxConstants.Category Category;
        public VfxConstants.AssetKind AssetKind;
        public VfxConstants.ResolveMode ResolveMode;
        public VfxConstants.SelectionMode SelectionMode;
        public int NoRepeatRecentCount;
        public int FallbackResourceUid;
        public bool Enabled;
    }

    public class TableVfx : DefaultTable<StruckTableVfx>
    {
        public override string Key => ConfigAddressableTable.Vfx;

        /// <summary>
        /// vfx 테이블 한 줄을 대표 VFX 행으로 변환합니다.
        /// </summary>
        /// <param name="data">헤더명과 값의 사전입니다.</param>
        /// <returns>대표 VFX 행입니다.</returns>
        protected override StruckTableVfx BuildRow(Dictionary<string, string> data)
        {
            TableRowReader reader = ReadRow(data);
            return new StruckTableVfx
            {
                Uid = reader.Int("Uid", 0),
                Name = reader.String("Name", string.Empty),
                Category = reader.Enum<VfxConstants.Category>("Category"),
                AssetKind = reader.Enum<VfxConstants.AssetKind>("AssetKind"),
                ResolveMode = reader.Enum<VfxConstants.ResolveMode>("ResolveMode", VfxConstants.ResolveMode.Direct),
                SelectionMode = reader.Enum<VfxConstants.SelectionMode>("SelectionMode", VfxConstants.SelectionMode.RandomEqual),
                NoRepeatRecentCount = reader.Int("NoRepeatRecentCount", 0),
                FallbackResourceUid = reader.Int("FallbackResourceUid", 0),
                Enabled = reader.BoolYN("Enabled", true),
            };
        }
    }
}
