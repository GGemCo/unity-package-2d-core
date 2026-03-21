using System.Collections.Generic;

namespace GGemCo2DCore
{
    public class TableVfxParticle : DefaultTable<StruckTableVfx>
    {
        public override string Key => ConfigAddressableTable.VfxParticle;

        protected override StruckTableVfx BuildRow(Dictionary<string, string> data)
            => TableVfxRowBuilder.BuildParticleRow(data);
    }
}
