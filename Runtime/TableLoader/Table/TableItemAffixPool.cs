using System.Collections.Generic;

namespace GGemCo2DCore
{
    /// <summary>
    /// 아이템 분류별 Affix 후보군 테이블 구조.
    /// </summary>
    public sealed class StruckTableItemAffixPool
    {
        public int Uid;
        public ItemConstants.Category Category;
        public ItemConstants.SubCategory SubCategory;
        public int ItemUid;
        public int AffixUid;
        public int WeightOverride;
    }

    /// <summary>
    /// 아이템 분류별 Affix 후보군 테이블.
    /// </summary>
    public sealed class TableItemAffixPool : DefaultTable<StruckTableItemAffixPool>
    {
        public override string Key => ConfigAddressableTable.ItemAffixPool;

        private readonly List<StruckTableItemAffixPool> _rows = new();

        protected override void PreLoad() => _rows.Clear();

        protected override void OnLoadedData(StruckTableItemAffixPool data)
        {
            if (data == null || data.AffixUid <= 0) return;
            _rows.Add(data);
        }

        /// <summary>
        /// 아이템 정보 기반으로 후보군을 반환한다.
        /// - ItemUid 매칭이 가장 우선
        /// - 그 다음 Category/SubCategory 매칭
        /// </summary>
        public List<StruckTableItemAffixPool> GetCandidates(StruckTableItem item)
        {
            var result = new List<StruckTableItemAffixPool>(32);
            if (item == null) return result;

            // 1) ItemUid 전용
            for (int i = 0; i < _rows.Count; i++)
            {
                var r = _rows[i];
                if (r.ItemUid > 0 && r.ItemUid == item.Uid)
                    result.Add(r);
            }
            if (result.Count > 0) return result;

            // 2) Category/SubCategory
            for (int i = 0; i < _rows.Count; i++)
            {
                var r = _rows[i];
                if (r.ItemUid > 0) continue;

                if (r.Category != ItemConstants.Category.None && r.Category != item.Category)
                    continue;
                if (r.SubCategory != ItemConstants.SubCategory.None && r.SubCategory != item.SubCategory)
                    continue;

                result.Add(r);
            }

            return result;
        }

        protected override StruckTableItemAffixPool BuildRow(Dictionary<string, string> data)
        {
            TableRowReader reader = ReadRow(data);
            return new StruckTableItemAffixPool
            {
                Uid = reader.Int("Uid"),
                Category = reader.Enum<ItemConstants.Category>("Category"),
                SubCategory = reader.Enum<ItemConstants.SubCategory>("SubCategory"),
                ItemUid = reader.Int("ItemUid"),
                AffixUid = reader.Int("AffixUid"),
                WeightOverride = reader.Int("WeightOverride"),
            };
        }
    }
}
