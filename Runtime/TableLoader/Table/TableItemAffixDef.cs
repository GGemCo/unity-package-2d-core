using System.Collections.Generic;

namespace GGemCo2DCore
{
    public enum ItemAffixType
    {
        Prefix = 0,
        Suffix = 1,
    }

    /// <summary>
    /// 랜덤 옵션(Affix) 정의 테이블 구조.
    /// </summary>
    public sealed class StruckTableItemAffixDef
    {
        public int AffixUid;
        public ItemAffixType AffixType;
        public ItemOptionKind Kind;
        public string TargetId;
        public ConfigCommon.SuffixType Op;
        public float MinValue;
        public float MaxValue;
        public int MinLevel;
        public int GroupId;
        public int Weight;
    }

    /// <summary>
    /// 랜덤 옵션(Affix) 정의 테이블.
    /// </summary>
    public sealed class TableItemAffixDef : DefaultTable<StruckTableItemAffixDef>
    {
        public override string Key => ConfigAddressableTable.ItemAffixDef;

        private readonly Dictionary<int, StruckTableItemAffixDef> _byUid = new();

        protected override void PreLoad() => _byUid.Clear();

        protected override void OnLoadedData(StruckTableItemAffixDef data)
        {
            if (data == null || data.AffixUid <= 0) return;
            _byUid[data.AffixUid] = data;
        }

        public StruckTableItemAffixDef GetByUid(int affixUid)
        {
            if (affixUid <= 0) return null;
            return _byUid.GetValueOrDefault(affixUid);
        }

        protected override StruckTableItemAffixDef BuildRow(Dictionary<string, string> data)
        {
            var groupToken = data.GetValueOrDefault("GroupId");
            var groupId = MathHelper.ParseInt(groupToken);
            if (groupId == 0 && !string.IsNullOrEmpty(groupToken))
            {
                // 테이블에서 문자열 토큰을 사용한 경우(예: DEF_FLAT), 안정적인 해시로 ID를 생성한다.
                groupId = StableHash32(groupToken);
                if (groupId == 0) groupId = 1;
            }
            return new StruckTableItemAffixDef
            {
                AffixUid = MathHelper.ParseInt(data.GetValueOrDefault("AffixUid")),
                AffixType = EnumHelper.ConvertEnum<ItemAffixType>(data.GetValueOrDefault("AffixType")),
                Kind = EnumHelper.ConvertEnum<ItemOptionKind>(data.GetValueOrDefault("Kind")),
                TargetId = data.GetValueOrDefault("TargetId"),
                Op = EnumHelper.ConvertEnum<ConfigCommon.SuffixType>(data.GetValueOrDefault("Op")),
                MinValue = MathHelper.ParseFloat(data.GetValueOrDefault("MinValue")),
                MaxValue = MathHelper.ParseFloat(data.GetValueOrDefault("MaxValue")),
                MinLevel = MathHelper.ParseInt(data.GetValueOrDefault("MinLevel")),
                GroupId = groupId,
                Weight = MathHelper.ParseInt(data.GetValueOrDefault("Weight")),
            };
        }
        
    }
}
