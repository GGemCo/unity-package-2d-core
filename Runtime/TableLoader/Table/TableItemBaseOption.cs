using System.Collections.Generic;

namespace GGemCo2DCore
{
    /// <summary>
    /// 아이템 고정 옵션(Definition) 테이블 구조.
    /// </summary>
    public sealed class StruckTableItemBaseOption
    {
        public int Uid;
        public int ItemUid;
        public ItemOptionKind Kind;
        public string TargetId;
        public ConfigCommon.SuffixType Op;
        public float Value;
        public int Chance;
        public float Duration;
    }

    /// <summary>
    /// 아이템 고정 옵션 테이블.
    /// - 아이템 정의(ItemUid) 기준으로 여러 옵션을 1:N으로 조회합니다.
    /// </summary>
    public sealed class TableItemBaseOption : DefaultTable<StruckTableItemBaseOption>
    {
        public override string Key => ConfigAddressableTable.ItemBaseOption;

        private readonly Dictionary<int, List<StruckTableItemBaseOption>> _byItemUid = new();

        protected override void PreLoad()
        {
            _byItemUid.Clear();
        }

        protected override void OnLoadedData(StruckTableItemBaseOption data)
        {
            if (data == null || data.ItemUid <= 0) return;
            if (!_byItemUid.TryGetValue(data.ItemUid, out var list))
                _byItemUid[data.ItemUid] = list = new List<StruckTableItemBaseOption>(4);
            list.Add(data);
        }

        public IReadOnlyList<StruckTableItemBaseOption> GetByItemUid(int itemUid)
        {
            if (itemUid <= 0) return System.Array.Empty<StruckTableItemBaseOption>();
            return _byItemUid.TryGetValue(itemUid, out var list) ? list : System.Array.Empty<StruckTableItemBaseOption>();
        }

        protected override StruckTableItemBaseOption BuildRow(Dictionary<string, string> data)
        {
            TableRowReader reader = ReadRow(data);
            return new StruckTableItemBaseOption
            {
                Uid = reader.Int("Uid"),
                ItemUid = reader.Int("ItemUid"),
                Kind = reader.Enum<ItemOptionKind>("Kind"),
                TargetId = reader.String("TargetId"),
                Op = reader.Enum<ConfigCommon.SuffixType>("Op"),
                Value = reader.Float("Value"),
                Chance = reader.Int("Chance"),
                Duration = reader.Float("Duration"),
            };
        }
    }
}
