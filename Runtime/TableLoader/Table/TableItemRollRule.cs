using System.Collections.Generic;

namespace GGemCo2DCore
{

    /// <summary>
    /// 드랍 시 랜덤 옵션 Roll 규칙 테이블 구조.
    /// </summary>
    public sealed class StruckTableItemRollRule
    {
        public int Uid;
        public ItemConstants.Class Rarity;
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

        private readonly Dictionary<ItemConstants.Class, StruckTableItemRollRule> _byRarity = new();

        protected override void PreLoad() => _byRarity.Clear();

        protected override void OnLoadedData(StruckTableItemRollRule data)
        {
            if (data == null) return;
            _byRarity[data.Rarity] = data;
        }

        public StruckTableItemRollRule GetByRarity(ItemConstants.Class rarity)
        {
            return _byRarity.GetValueOrDefault(rarity);
        }

        protected override StruckTableItemRollRule BuildRow(Dictionary<string, string> data)
        {
            TableRowReader reader = ReadRow(data);
            bool allowDup = false;
            var token = reader.String("AllowDuplicateGroup");
            if (!string.IsNullOrWhiteSpace(token))
            {
                // true/false 또는 0/1 모두 허용
                if (!bool.TryParse(token, out allowDup))
                    allowDup = MathHelper.ParseInt(token) != 0;
            }

            return new StruckTableItemRollRule
            {
                Uid = reader.Int("Uid"),
                Rarity = reader.Enum<ItemConstants.Class>("Rarity"),
                MinAffixCount = reader.Int("MinAffixCount"),
                MaxAffixCount = reader.Int("MaxAffixCount"),
                MaxPrefix = reader.Int("MaxPrefix"),
                MaxSuffix = reader.Int("MaxSuffix"),
                AllowDuplicateGroup = allowDup,
            };
        }
    }
}
