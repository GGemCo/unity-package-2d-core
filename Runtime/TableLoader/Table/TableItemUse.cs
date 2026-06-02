using System;
using System.Collections.Generic;

namespace GGemCo2DCore
{
    /// <summary>
    /// item_use 테이블 Row
    /// - 하나의 Item(Uid)에 대한 "사용 그룹(UseGroup)" 정의
    /// - 실제 효과(Action)는 item_use_action 에서 UseGroupUid로 연결
    /// </summary>
    public sealed class StruckTableItemUse : IUidName
    {
        /// <summary>UseGroup 고유번호</summary>
        public int Uid { get; set; }
        /// <summary>표시용(디버그) 이름</summary>
        public string Name { get; set; }

        /// <summary>사용 대상 ItemUid(item.txt Uid)</summary>
        public int ItemUid;
        /// <summary>기본 소모 개수(일반적으로 1)</summary>
        public int ConsumeCount;
        /// <summary>쿨타임 오버라이드(0이면 item.txt CoolTime 사용)</summary>
        public float CooldownOverride;
        /// <summary>부분 성공 정책</summary>
        public ItemUseFailPolicy FailPolicy;
    }

    /// <summary>
    /// item_use 테이블
    /// </summary>
    public sealed class TableItemUse : DefaultTable<StruckTableItemUse>
    {
        public override string Key => ConfigAddressableTable.ItemUse;

        private readonly Dictionary<int, StruckTableItemUse> _byItemUid = new();

        protected override void PreLoad()
        {
            base.PreLoad();
            _byItemUid.Clear();
        }

        protected override StruckTableItemUse BuildRow(Dictionary<string, string> d)
        {
            TableRowReader reader = ReadRow(d);
            int uid = reader.Int("Uid");
            int itemUid = reader.Int("ItemUid");
            int consume = Math.Max(1, reader.Int("ConsumeCount"));
            float cd = reader.Float("CooldownOverride");

            var failPolicy = reader.Enum<ItemUseFailPolicy>("FailPolicy");

            // Name은 선택
            string name = reader.String("Name");
            if (string.IsNullOrWhiteSpace(name))
            {
                name = $"ItemUse_{uid}";
            }

            return new StruckTableItemUse
            {
                Uid = uid,
                Name = name,
                ItemUid = itemUid,
                ConsumeCount = consume,
                CooldownOverride = cd,
                FailPolicy = failPolicy,
            };
        }

        protected override void OnLoadedData(StruckTableItemUse row)
        {
            base.OnLoadedData(row);
            if (row == null || row.ItemUid <= 0) return;
            _byItemUid[row.ItemUid] = row;
        }

        public bool TryGetByItemUid(int itemUid, out StruckTableItemUse row)
            => _byItemUid.TryGetValue(itemUid, out row);
    }
}
