using System.Collections.Generic;

namespace GGemCo2DCore
{
    public class StruckTableVfxParticle
    {
        public int Uid;
        public string Name;
        public string PrefabPath;
        public int PoolPrewarmCount;
        public int PoolMaxSize;
        public bool Loop;
        public bool UseUnscaledTime;
    }
    public class TableVfxParticle : DefaultTable<StruckTableVfxParticle>
    {
        public override string Key => ConfigAddressableTable.VfxParticle;

        protected override StruckTableVfxParticle BuildRow(Dictionary<string, string> data)
        {
            return new StruckTableVfxParticle
            {
                Uid = MathHelper.ParseInt(data.GetValueOrDefault("Uid")),
                Name = data.GetValueOrDefault("Name"),
                PrefabPath = data.GetValueOrDefault("PrefabPath"),
                PoolPrewarmCount = MathHelper.ParseInt(data.GetValueOrDefault("PoolPrewarmCount")),
                PoolMaxSize = MathHelper.ParseInt(data.GetValueOrDefault("PoolMaxSize")),
                Loop = ConvertBoolean(data.GetValueOrDefault("Loop")),
                UseUnscaledTime = ConvertBoolean(data.GetValueOrDefault("UseUnscaledTime")),
            };
        }
    }
}
