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

        public int ItemUseUid;
        public int Order;
        public ItemUseActionType ActionType;

        /// <summary>
        /// 임시 HP 액션의 적용 정책입니다.
        /// 다른 액션 타입에서는 기본값 <see cref="ItemTempHpApplyPolicy.Add"/>를 사용합니다.
        /// </summary>
        public ItemTempHpApplyPolicy ActionPolicy;

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
            TableRowReader reader = ReadRow(d);
            int uid = reader.Int("Uid");
            int itemUseUid = reader.Int("ItemUseUid");
            int order = reader.Int("Order");
            var type = reader.Enum<ItemUseActionType>("ActionType");

            string name = reader.String("Name");
            if (string.IsNullOrWhiteSpace(name))
            {
                name = $"ItemUseAction_{uid}";
            }

            return new StruckTableItemUseAction
            {
                Uid = uid,
                Name = name,
                ItemUseUid = itemUseUid,
                Order = order,
                ActionType = type,
                // 범용 문자열 파라미터를 로드 시점에 강타입 정책으로 변환합니다.
                // 기존 테이블의 빈 값은 누적 증가(Add)로 처리하여 하위 호환성을 유지합니다.
                ActionPolicy = type == ItemUseActionType.AddMaxHpTemp
                    ? reader.Enum("ParamStringB", ItemTempHpApplyPolicy.Add)
                    : ItemTempHpApplyPolicy.Add,
                ParamIntA = reader.Int("ParamIntA"),
                ParamIntB = reader.Int("ParamIntB"),
                ParamFloatA = reader.Float("ParamFloatA"),
                ParamFloatB = reader.Float("ParamFloatB"),
                ParamStringA = reader.String("ParamStringA"),
                ParamStringB = reader.String("ParamStringB"),
            };
        }

        protected override void OnLoadedData(StruckTableItemUseAction row)
        {
            base.OnLoadedData(row);
            if (row == null || row.ItemUseUid <= 0) return;
            if (!_byUseGroupUid.TryGetValue(row.ItemUseUid, out var list))
            {
                list = new List<StruckTableItemUseAction>();
                _byUseGroupUid[row.ItemUseUid] = list;
            }
            list.Add(row);
        }

        /// <summary>
        /// UseGroupUid에 해당하는 Action 목록을 Order 기준으로 정렬하여 반환합니다.
        /// </summary>
        public List<StruckTableItemUseAction> GetActions(int useGroupUid)
        {
            if (!_byUseGroupUid.TryGetValue(useGroupUid, out var list) || list == null || list.Count == 0)
            {
                return null;
            }
            // 로드 후 정렬 비용을 줄이기 위해 호출 시점에만 정렬(리스트 복사)
            return list.OrderBy(x => x.Order).ToList();
        }
    }
}
