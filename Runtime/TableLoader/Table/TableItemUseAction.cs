using System;
using System.Collections.Generic;
using System.Linq;

namespace GGemCo2DCore
{
    /// <summary>
    /// item_use_action 테이블 Row
    /// - 1행 = 1개 효과(Action)
    /// - UseGroupUid를 통해 item_use(Uid)와 연결
    /// </summary>
    public sealed class StruckTableItemUseAction : IUidName
    {
        public int Uid { get; set; }
        public string Name { get; set; }

        public int UseGroupUid;
        public int Order;
        public ItemUseActionType ActionType;

        // 범용 파라미터(필요한 Action만 사용)
        public int ParamIntA;
        public int ParamIntB;
        public float ParamFloatA;
        public float ParamFloatB;
        public string ParamStringA;
        public string ParamStringB;
    }

    /// <summary>
    /// item_use_action 테이블
    /// </summary>
    public sealed class TableItemUseAction : DefaultTable<StruckTableItemUseAction>
    {
        public override string Key => ConfigAddressableTable.ItemUseAction;

        private readonly Dictionary<int, List<StruckTableItemUseAction>> _byUseGroupUid = new();

        protected override void PreLoad()
        {
            base.PreLoad();
            _byUseGroupUid.Clear();
        }

        protected override StruckTableItemUseAction BuildRow(Dictionary<string, string> d)
        {
            int uid = MathHelper.ParseInt(d.GetValueOrDefault("Uid"));
            int useGroupUid = MathHelper.ParseInt(d.GetValueOrDefault("UseGroupUid"));
            int order = MathHelper.ParseInt(d.GetValueOrDefault("Order"));
            var type = EnumHelper.ConvertEnum<ItemUseActionType>(d.GetValueOrDefault("ActionType"));

            string name = d.GetValueOrDefault("Name");
            if (string.IsNullOrWhiteSpace(name))
            {
                name = $"ItemUseAction_{uid}";
            }

            return new StruckTableItemUseAction
            {
                Uid = uid,
                Name = name,
                UseGroupUid = useGroupUid,
                Order = order,
                ActionType = type,
                ParamIntA = MathHelper.ParseInt(d.GetValueOrDefault("ParamIntA")),
                ParamIntB = MathHelper.ParseInt(d.GetValueOrDefault("ParamIntB")),
                ParamFloatA = MathHelper.ParseFloat(d.GetValueOrDefault("ParamFloatA")),
                ParamFloatB = MathHelper.ParseFloat(d.GetValueOrDefault("ParamFloatB")),
                ParamStringA = d.GetValueOrDefault("ParamStringA"),
                ParamStringB = d.GetValueOrDefault("ParamStringB"),
            };
        }

        protected override void OnLoadedData(StruckTableItemUseAction row)
        {
            base.OnLoadedData(row);
            if (row == null || row.UseGroupUid <= 0) return;
            if (!_byUseGroupUid.TryGetValue(row.UseGroupUid, out var list))
            {
                list = new List<StruckTableItemUseAction>();
                _byUseGroupUid[row.UseGroupUid] = list;
            }
            list.Add(row);
        }

        /// <summary>
        /// UseGroupUid에 해당하는 Action 목록을 Order 기준으로 정렬하여 반환합니다.
        /// </summary>
        public IReadOnlyList<StruckTableItemUseAction> GetActions(int useGroupUid)
        {
            if (!_byUseGroupUid.TryGetValue(useGroupUid, out var list) || list == null || list.Count == 0)
            {
                return Array.Empty<StruckTableItemUseAction>();
            }
            // 로드 후 정렬 비용을 줄이기 위해 호출 시점에만 정렬(리스트 복사)
            return list.OrderBy(x => x.Order).ToList();
        }
    }
}
