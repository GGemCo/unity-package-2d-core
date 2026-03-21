using System.Collections.Generic;

namespace GGemCo2DCore
{
    public class TableVfxParticle : DefaultTable<StruckTableVfx>
    {
        public override string Key => ConfigAddressableTable.VfxParticle;

        protected override StruckTableVfx BuildRow(Dictionary<string, string> data)
        {
            return new StruckTableVfx
            {
                Uid = MathHelper.ParseInt(data.GetValueOrDefault("Uid")),
                Name = data.GetValueOrDefault("Name"),
                AssetKind = VfxConstants.AssetKind.Particle,
                Category = EnumHelper.ConvertEnum<VfxConstants.Category>(data.GetValueOrDefault("Category")),
                Type = VfxConstants.Type.None,
                PrefabPath = data.GetValueOrDefault("PrefabPath"),
                AnimationController = ConfigCommon.AnimationController.Sprite,
                Width = MathHelper.ParseInt(data.GetValueOrDefault("Width")),
                Height = MathHelper.ParseInt(data.GetValueOrDefault("Height")),
                ColliderSize = ConvertVector2(data.GetValueOrDefault("ColliderSize")),
                NeedRotation = ConvertBoolean(data.GetValueOrDefault("NeedRotation")),
                Color = data.GetValueOrDefault("Color"),
                DefaultDirection = ConfigCommon.GetDirectionType(data.GetValueOrDefault("DefaultDirection", "Left")),
                PlaybackType = EnumHelper.ConvertEnum<VfxConstants.PlaybackType>(data.GetValueOrDefault("PlaybackType")),
                LifecycleType = EnumHelper.ConvertEnum<VfxConstants.LifecycleType>(data.GetValueOrDefault("LifecycleType")),
                AttachType = EnumHelper.ConvertEnum<VfxConstants.AttachType>(data.GetValueOrDefault("AttachType")),
                FollowMode = EnumHelper.ConvertEnum<VfxConstants.FollowMode>(data.GetValueOrDefault("FollowMode")),
                PoolPrewarmCount = MathHelper.ParseInt(data.GetValueOrDefault("PoolPrewarmCount")),
                PoolMaxSize = MathHelper.ParseInt(data.GetValueOrDefault("PoolMaxSize")),
                Loop = ConvertBoolean(data.GetValueOrDefault("Loop")),
                UseUnscaledTime = ConvertBoolean(data.GetValueOrDefault("UseUnscaledTime")),
            };
        }
    }
}
