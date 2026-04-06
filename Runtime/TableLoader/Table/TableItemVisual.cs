using System;
using System.Collections.Generic;

namespace GGemCo2DCore
{
    public sealed class StruckTableItemVisual : IUidName
    {
        public int Uid { get; set; }
        public string Name { get; set; }
        public int ItemUid;
        public ItemConstants.DropVisualType VisualType;
        public int VfxUid;
        public float Scale;
        public float OffsetY;
    }

    public sealed class TableItemVisual : DefaultTable<StruckTableItemVisual>
    {
        public override string Key => ConfigAddressableTable.ItemVisual;

        private readonly Dictionary<int, StruckTableItemVisual> _byItemUid = new();
        
        protected override void PreLoad()
        {
            base.PreLoad();
            _byItemUid.Clear();
        }
        
        protected override StruckTableItemVisual BuildRow(Dictionary<string, string> data)
        {
            int uid = MathHelper.ParseInt(data.GetValueOrDefault("Uid"));
            return new StruckTableItemVisual
            {
                Uid = uid,
                Name = data.GetValueOrDefault("Name"),
                ItemUid = MathHelper.ParseInt(data.GetValueOrDefault("ItemUid")),
                VisualType = EnumHelper.ConvertEnum<ItemConstants.DropVisualType>(data.GetValueOrDefault("VisualType")),
                VfxUid = MathHelper.ParseInt(data.GetValueOrDefault("VfxUid")),
                Scale = MathHelper.ParseFloat(data.GetValueOrDefault("Scale")),
                OffsetY = MathHelper.ParseFloat(data.GetValueOrDefault("OffsetY")),
            };
        }
        
        protected override void OnLoadedData(StruckTableItemVisual row)
        {
            base.OnLoadedData(row);
            if (row == null || row.ItemUid <= 0) return;
            _byItemUid[row.ItemUid] = row;
        }
        
        public StruckTableItemVisual TryGetByItemUid(int itemUid)
            => _byItemUid.GetValueOrDefault(itemUid);
    }
}
