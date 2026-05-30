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
            return new StruckTableVfx
            {
                Uid = MathHelper.ParseInt(GetValue(data, "Uid", "0")),
                Name = GetValue(data, "Name", string.Empty),
                Category = EnumHelper.ConvertEnum<VfxConstants.Category>(GetValue(data, "Category", "None")),
                AssetKind = EnumHelper.ConvertEnum<VfxConstants.AssetKind>(GetValue(data, "AssetKind", "None")),
                ResolveMode = EnumHelper.ConvertEnum<VfxConstants.ResolveMode>(GetValue(data, "ResolveMode", "Direct")),
                SelectionMode = EnumHelper.ConvertEnum<VfxConstants.SelectionMode>(GetValue(data, "SelectionMode", "RandomEqual")),
                NoRepeatRecentCount = MathHelper.ParseInt(GetValue(data, "NoRepeatRecentCount", "0")),
                FallbackResourceUid = MathHelper.ParseInt(GetValue(data, "FallbackResourceUid", "0")),
                Enabled = ConvertBoolean(GetValue(data, "Enabled", "Y")),
            };
        }

        /// <summary>
        /// 헤더가 없을 수 있는 마이그레이션 중간 테이블에서 값을 안전하게 읽습니다.
        /// </summary>
        /// <param name="data">헤더명과 값의 사전입니다.</param>
        /// <param name="key">조회할 헤더명입니다.</param>
        /// <param name="defaultValue">헤더가 없거나 값이 비어 있을 때 사용할 기본값입니다.</param>
        /// <returns>조회된 값 또는 기본값입니다.</returns>
        private static string GetValue(Dictionary<string, string> data, string key, string defaultValue)
        {
            if (data == null || string.IsNullOrWhiteSpace(key))
                return defaultValue;

            return data.TryGetValue(key, out string value) && !string.IsNullOrWhiteSpace(value)
                ? value
                : defaultValue;
        }
    }
}
