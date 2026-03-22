using System.Collections.Generic;

namespace GGemCo2DCore
{
    public class StruckTableVfxParticle
    {
        public int Uid;
        public string Name;
        public string PrefabPath;
        public VfxConstants.LifecycleType LifecycleType;
        public VfxConstants.AttachType AttachType;
        public VfxConstants.FollowMode FollowMode;
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
                LifecycleType = ParseLifecycleType(data.GetValueOrDefault("LifecycleType")),
                AttachType = ParseAttachType(data.GetValueOrDefault("AttachType")),
                FollowMode = ParseFollowMode(data.GetValueOrDefault("FollowMode")),
                PoolPrewarmCount = MathHelper.ParseInt(data.GetValueOrDefault("PoolPrewarmCount")),
                PoolMaxSize = MathHelper.ParseInt(data.GetValueOrDefault("PoolMaxSize")),
                Loop = ConvertBoolean(data.GetValueOrDefault("Loop")),
                UseUnscaledTime = ConvertBoolean(data.GetValueOrDefault("UseUnscaledTime")),
            };
        }

        private static VfxConstants.LifecycleType ParseLifecycleType(string value)
        {
            return string.IsNullOrWhiteSpace(value)
                ? VfxConstants.LifecycleType.AutoRelease
                : EnumHelper.ConvertEnum<VfxConstants.LifecycleType>(value);
        }

        private static VfxConstants.AttachType ParseAttachType(string value)
        {
            return string.IsNullOrWhiteSpace(value)
                ? VfxConstants.AttachType.World
                : EnumHelper.ConvertEnum<VfxConstants.AttachType>(value);
        }

        private static VfxConstants.FollowMode ParseFollowMode(string value)
        {
            return string.IsNullOrWhiteSpace(value)
                ? VfxConstants.FollowMode.None
                : EnumHelper.ConvertEnum<VfxConstants.FollowMode>(value);
        }
    }
}
