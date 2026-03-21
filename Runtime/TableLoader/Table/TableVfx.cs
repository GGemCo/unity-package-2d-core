using System.Collections.Generic;
using UnityEngine;

namespace GGemCo2DCore
{
    public class StruckTableVfx
    {
        public int Uid;
        public string Name;
        public VfxConstants.Category Category;
        public VfxConstants.Type Type;
        public string PrefabPath;
        public ConfigCommon.AnimationController AnimationController;
        public int Width;
        public int Height;
        public Vector2 ColliderSize;
        public bool NeedRotation;
        public string Color;
        public ConfigCommon.DirectionType DefaultDirection;
        public VfxConstants.PlaybackType PlaybackType;
        public VfxConstants.LifecycleType LifecycleType;
        public VfxConstants.AttachType AttachType;
        public VfxConstants.FollowMode FollowMode;
        public int PoolPrewarmCount;
        public int PoolMaxSize;
        public bool Loop;
        public bool UseUnscaledTime;
    }

    public class TableVfx : DefaultTable<StruckTableVfx>
    {
        public override string Key => ConfigAddressableTable.Vfx;

        protected override StruckTableVfx BuildRow(Dictionary<string, string> data)
        {
            string Get(string key, string fallback = "")
                => data.TryGetValue(key, out var value) ? value : fallback;

            bool Has(string key) => data.ContainsKey(key) && !string.IsNullOrWhiteSpace(data[key]);

            var animationController = ConvertAnimationController(Get("AnimationController", nameof(ConfigCommon.AnimationController.Sprite)));
            var type = Has("Type")
                ? EnumHelper.ConvertEnum<VfxConstants.Type>(Get("Type"))
                : VfxConstants.Type.Default;

            var playbackType = ResolvePlaybackType(Get("PlaybackType"), type, animationController);

            return new StruckTableVfx
            {
                Uid = MathHelper.ParseInt(Get("Uid")),
                Name = Get("Name"),
                Category = EnumHelper.ConvertEnum<VfxConstants.Category>(Get("Category")),
                Type = type,
                PrefabPath = Get("PrefabPath"),
                AnimationController = animationController,
                Width = MathHelper.ParseInt(Get("Width")),
                Height = MathHelper.ParseInt(Get("Height")),
                ColliderSize = ConvertVector2(Get("ColliderSize")),
                NeedRotation = ConvertBoolean(Get("NeedRotation")),
                Color = Get("Color"),
                DefaultDirection = ConfigCommon.GetDirectionType(Get("DefaultDirection", "Left")),
                PlaybackType = playbackType,
                LifecycleType = Has("LifecycleType")
                    ? EnumHelper.ConvertEnum<VfxConstants.LifecycleType>(Get("LifecycleType"))
                    : VfxConstants.LifecycleType.AutoRelease,
                AttachType = Has("AttachType")
                    ? EnumHelper.ConvertEnum<VfxConstants.AttachType>(Get("AttachType"))
                    : VfxConstants.AttachType.World,
                FollowMode = Has("FollowMode")
                    ? EnumHelper.ConvertEnum<VfxConstants.FollowMode>(Get("FollowMode"))
                    : VfxConstants.FollowMode.None,
                PoolPrewarmCount = MathHelper.ParseInt(Get("PoolPrewarmCount")),
                PoolMaxSize = MathHelper.ParseInt(Get("PoolMaxSize")),
                Loop = ConvertBoolean(Get("Loop")),
                UseUnscaledTime = ConvertBoolean(Get("UseUnscaledTime")),
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
