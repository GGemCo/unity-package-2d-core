using System.Collections.Generic;

namespace GGemCo2DCore
{
    /// <summary>
    /// 아이템 등급(희귀도) 정의.
    /// </summary>
    public enum ItemRarity
    {
        Normal = 0,
        Magic = 1,
        Rare = 2,
        Unique = 3,
    }

    /// <summary>
    /// 드랍 시 랜덤 옵션 Roll 규칙 테이블 구조.
    /// </summary>
    public sealed class StruckTableItemRollRule
    {
        public int Uid;
        public ItemRarity Rarity;
        public int MinAffixCount;
        public int MaxAffixCount;
        public int MaxPrefix;
        public int MaxSuffix;
        public bool AllowDuplicateGroup;
    }

    /// <summary>
    /// 드랍 시 랜덤 옵션 Roll 규칙 테이블.
    /// </summary>
    public sealed class TableItemRollRule : DefaultTable<StruckTableItemRollRule>
    {
        public override string Key => ConfigAddressableTable.ItemRollRule;

        private readonly Dictionary<ItemRarity, StruckTableItemRollRule> _byRarity = new();

        protected override void PreLoad() => _byRarity.Clear();

        protected override void OnLoadedData(StruckTableItemRollRule data)
        {
            if (data == null) return;
            _byRarity[data.Rarity] = data;
        }

        public StruckTableItemRollRule GetByRarity(ItemRarity rarity)
        {
            return _byRarity.GetValueOrDefault(rarity);
        }

        protected override StruckTableItemRollRule BuildRow(Dictionary<string, string> data)
        {
            bool allowDup = false;
            var token = data.GetValueOrDefault("AllowDuplicateGroup");
            if (!string.IsNullOrWhiteSpace(token))
            {
                // true/false 또는 0/1 모두 허용
                if (!bool.TryParse(token, out allowDup))
                    allowDup = MathHelper.ParseInt(token) != 0;
            }

            return new StruckTableItemRollRule
            {
                Uid = MathHelper.ParseInt(data["Uid"]),
                Rarity = EnumHelper.ConvertEnum<ItemRarity>(data.GetValueOrDefault("Rarity")),
                MinAffixCount = MathHelper.ParseInt(data.GetValueOrDefault("MinAffixCount")),
                MaxAffixCount = MathHelper.ParseInt(data.GetValueOrDefault("MaxAffixCount")),
                MaxPrefix = MathHelper.ParseInt(data.GetValueOrDefault("MaxPrefix")),
                MaxSuffix = MathHelper.ParseInt(data.GetValueOrDefault("MaxSuffix")),
                AllowDuplicateGroup = allowDup,
            };
        }
    }
}
