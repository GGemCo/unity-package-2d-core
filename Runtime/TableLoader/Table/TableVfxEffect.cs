using System.Collections.Generic;
using UnityEngine;

namespace GGemCo2DCore
{
    public class StruckTableVfxEffect
    {
        public int Uid;
        public string Name;
        public VfxConstants.Category Category;
        public VfxConstants.EffectType EffectType;
        public string PrefabPath;
        public ConfigCommon.AnimationController AnimationController;
        public int Width;
        public int Height;
        public Vector2 ColliderSize;
        public bool NeedRotation;
        public string Color;
        public ConfigCommon.DirectionType DefaultDirection;
        public VfxConstants.LifecycleType LifecycleType;
        public VfxConstants.AttachType AttachType;
        public VfxConstants.FollowMode FollowMode;
        public int PoolPrewarmCount;
        public int PoolMaxSize;
        public bool UseUnscaledTime;
    }
    public class TableVfxEffect : DefaultTable<StruckTableVfxEffect>
    {
        public override string Key => ConfigAddressableTable.VfxEffect;

        protected override StruckTableVfxEffect BuildRow(Dictionary<string, string> data)
        {
            return new StruckTableVfxEffect
            {
                Uid = MathHelper.ParseInt(data.GetValueOrDefault("Uid")),
                Name = data.GetValueOrDefault("Name"),
                Category = EnumHelper.ConvertEnum<VfxConstants.Category>(data.GetValueOrDefault("Category")),
                EffectType = EnumHelper.ConvertEnum<VfxConstants.EffectType>(data.GetValueOrDefault("EffectType")),
                PrefabPath = data.GetValueOrDefault("PrefabPath"),
                AnimationController = EnumHelper.ConvertEnum<ConfigCommon.AnimationController>(data.GetValueOrDefault("AnimationController")),
                Width = MathHelper.ParseInt(data.GetValueOrDefault("Width")),
                Height = MathHelper.ParseInt(data.GetValueOrDefault("Height")),
                ColliderSize = ConvertVector2(data.GetValueOrDefault("ColliderSize")),
                NeedRotation = ConvertBoolean(data.GetValueOrDefault("NeedRotation")),
                Color = data.GetValueOrDefault("Color"),
                DefaultDirection = ConfigCommon.GetDirectionType(data.GetValueOrDefault("DefaultDirection", "Left")),
                LifecycleType = ParseLifecycleType(data.GetValueOrDefault("LifecycleType")),
                AttachType = ParseAttachType(data.GetValueOrDefault("AttachType")),
                FollowMode = ParseFollowMode(data.GetValueOrDefault("FollowMode")),
                PoolPrewarmCount = MathHelper.ParseInt(data.GetValueOrDefault("PoolPrewarmCount")),
                PoolMaxSize = MathHelper.ParseInt(data.GetValueOrDefault("PoolMaxSize")),
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
