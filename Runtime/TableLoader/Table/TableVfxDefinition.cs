using System.Collections.Generic;
using UnityEngine;

namespace GGemCo2DCore
{
    public class StruckTableVfx
    {
        public int Uid;
        public string Name;
        public VfxConstants.AssetKind AssetKind;
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

    internal static class TableVfxRowBuilder
    {
        public static string Get(Dictionary<string, string> data, string key, string fallback = "")
            => data.TryGetValue(key, out var value) ? value : fallback;

        public static bool Has(Dictionary<string, string> data, string key)
            => data.ContainsKey(key) && !string.IsNullOrWhiteSpace(data[key]);

        public static StruckTableVfx BuildEffectRow(Dictionary<string, string> data)
        {
            var animationController = ConvertAnimationController(Get(data, "AnimationController", nameof(ConfigCommon.AnimationController.Sprite)));
            var type = Has(data, "Type")
                ? EnumHelper.ConvertEnum<VfxConstants.Type>(Get(data, "Type"))
                : VfxConstants.Type.Default;

            return BuildCommonRow(
                data,
                assetKind: VfxConstants.AssetKind.Effect,
                type: type,
                animationController: animationController,
                playbackType: ResolveEffectPlaybackType(Get(data, "PlaybackType"), type, animationController));
        }

        public static StruckTableVfx BuildParticleRow(Dictionary<string, string> data)
        {
            return BuildCommonRow(
                data,
                assetKind: VfxConstants.AssetKind.Particle,
                type: VfxConstants.Type.None,
                animationController: ConfigCommon.AnimationController.Sprite,
                playbackType: ResolveParticlePlaybackType(Get(data, "PlaybackType")));
        }

        private static StruckTableVfx BuildCommonRow(
            Dictionary<string, string> data,
            VfxConstants.AssetKind assetKind,
            VfxConstants.Type type,
            ConfigCommon.AnimationController animationController,
            VfxConstants.PlaybackType playbackType)
        {
            return new StruckTableVfx
            {
                Uid = MathHelper.ParseInt(Get(data, "Uid")),
                Name = Get(data, "Name"),
                AssetKind = assetKind,
                Category = EnumHelper.ConvertEnum<VfxConstants.Category>(Get(data, "Category")),
                Type = type,
                PrefabPath = Get(data, "PrefabPath"),
                AnimationController = animationController,
                Width = MathHelper.ParseInt(Get(data, "Width")),
                Height = MathHelper.ParseInt(Get(data, "Height")),
                ColliderSize = ConvertVector2(Get(data, "ColliderSize")),
                NeedRotation = ConvertBoolean(Get(data, "NeedRotation")),
                Color = Get(data, "Color"),
                DefaultDirection = ConfigCommon.GetDirectionType(Get(data, "DefaultDirection", "Left")),
                PlaybackType = playbackType,
                LifecycleType = Has(data, "LifecycleType")
                    ? EnumHelper.ConvertEnum<VfxConstants.LifecycleType>(Get(data, "LifecycleType"))
                    : VfxConstants.LifecycleType.AutoRelease,
                AttachType = Has(data, "AttachType")
                    ? EnumHelper.ConvertEnum<VfxConstants.AttachType>(Get(data, "AttachType"))
                    : VfxConstants.AttachType.World,
                FollowMode = Has(data, "FollowMode")
                    ? EnumHelper.ConvertEnum<VfxConstants.FollowMode>(Get(data, "FollowMode"))
                    : VfxConstants.FollowMode.None,
                PoolPrewarmCount = MathHelper.ParseInt(Get(data, "PoolPrewarmCount")),
                PoolMaxSize = MathHelper.ParseInt(Get(data, "PoolMaxSize")),
                Loop = ConvertBoolean(Get(data, "Loop")),
                UseUnscaledTime = ConvertBoolean(Get(data, "UseUnscaledTime")),
            };
        }

        private static VfxConstants.PlaybackType ResolveEffectPlaybackType(
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

        private static VfxConstants.PlaybackType ResolveParticlePlaybackType(string playbackTypeValue)
        {
            if (!string.IsNullOrWhiteSpace(playbackTypeValue))
                return EnumHelper.ConvertEnum<VfxConstants.PlaybackType>(playbackTypeValue);

            return VfxConstants.PlaybackType.ParticleSystem;
        }

        private static ConfigCommon.AnimationController ConvertAnimationController(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return ConfigCommon.AnimationController.Sprite;

            return EnumHelper.ConvertEnum<ConfigCommon.AnimationController>(value);
        }

        private static Vector2 ConvertVector2(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return Vector2.zero;

            var parts = value.Split(',');
            var x = MathHelper.ParseFloat(parts.Length > 0 ? parts[0] : "0");
            var y = MathHelper.ParseFloat(parts.Length > 1 ? parts[1] : "0");
            return new Vector2(x, y);
        }

        private static bool ConvertBoolean(string value) => value == "Y";
    }
}
