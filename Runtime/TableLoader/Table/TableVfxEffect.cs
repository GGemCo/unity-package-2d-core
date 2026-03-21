using System.Collections.Generic;

namespace GGemCo2DCore
{
    public class TableVfxEffect : DefaultTable<StruckTableVfx>
    {
        public override string Key => ConfigAddressableTable.VfxEffect;

        protected override StruckTableVfx BuildRow(Dictionary<string, string> data)
            => TableVfxRowBuilder.BuildEffectRow(data);
    }
}
