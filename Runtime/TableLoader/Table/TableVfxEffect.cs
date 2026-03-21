using System.Collections.Generic;

namespace GGemCo2DCore
{
    public class TableVfxEffect : DefaultTable<StruckTableVfx>
    {
        public override string Key => ConfigAddressableTable.VfxEffect;

        protected override StruckTableVfx BuildRow(Dictionary<string, string> data)
        {
            var animationController = ConvertAnimationController(data.GetValueOrDefault("AnimationController"));
            var typeRaw = data.GetValueOrDefault("Type");
            var playbackTypeRaw = data.GetValueOrDefault("PlaybackType");

            var type = string.IsNullOrWhiteSpace(typeRaw)
                ? VfxConstants.Type.Default
                : EnumHelper.ConvertEnum<VfxConstants.Type>(typeRaw);

            var playbackType = ResolvePlaybackType(playbackTypeRaw, type, animationController);

            return new StruckTableVfx
            {
                Uid = MathHelper.ParseInt(data.GetValueOrDefault("Uid")),
                Name = data.GetValueOrDefault("Name"),
                AssetKind = VfxConstants.AssetKind.Effect,
                Category = EnumHelper.ConvertEnum<VfxConstants.Category>(data.GetValueOrDefault("Category")),
                Type = type,
                PrefabPath = data.GetValueOrDefault("PrefabPath"),
                AnimationController = animationController,
                Width = MathHelper.ParseInt(data.GetValueOrDefault("Width")),
                Height = MathHelper.ParseInt(data.GetValueOrDefault("Height")),
                ColliderSize = ConvertVector2(data.GetValueOrDefault("ColliderSize")),
                NeedRotation = ConvertBoolean(data.GetValueOrDefault("NeedRotation")),
                Color = data.GetValueOrDefault("Color"),
                DefaultDirection = ConfigCommon.GetDirectionType(data.GetValueOrDefault("DefaultDirection", "Left")),
                PlaybackType = playbackType,
                LifecycleType = EnumHelper.ConvertEnum<VfxConstants.LifecycleType>(data.GetValueOrDefault("LifecycleType")),
                AttachType = EnumHelper.ConvertEnum<VfxConstants.AttachType>(data.GetValueOrDefault("AttachType")),
                FollowMode = EnumHelper.ConvertEnum<VfxConstants.FollowMode>(data.GetValueOrDefault("FollowMode")),
                PoolPrewarmCount = MathHelper.ParseInt(data.GetValueOrDefault("PoolPrewarmCount")),
                PoolMaxSize = MathHelper.ParseInt(data.GetValueOrDefault("PoolMaxSize")),
                Loop = ConvertBoolean(data.GetValueOrDefault("Loop")),
                UseUnscaledTime = ConvertBoolean(data.GetValueOrDefault("UseUnscaledTime")),
            };
        }

        private static VfxConstants.PlaybackType ResolvePlaybackType(
            string playbackTypeValue,
            VfxConstants.Type type,
            ConfigCommon.AnimationController animationController)
        {
            if (!string.IsNullOrWhiteSpace(playbackTypeValue))
                return EnumHelper.ConvertEnum<VfxConstants.PlaybackType>(playbackTypeValue);

            if (type == VfxConstants.Type.Laser)
                return VfxConstants.PlaybackType.Laser;

            return animationController == ConfigCommon.AnimationController.Spine
                ? VfxConstants.PlaybackType.SpineSequence
                : VfxConstants.PlaybackType.SpriteSequence;
        }
    }
}
