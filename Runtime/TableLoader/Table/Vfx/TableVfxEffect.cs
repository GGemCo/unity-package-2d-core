using System.Collections.Generic;
using UnityEngine;

namespace GGemCo2DCore
{
    public class StruckTableVfxEffect
    {
        public int Uid;
        public string Name;
        public int VfxUid;
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
        public string SortingLayer;
        public int SortingOrder;
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
                VfxUid = MathHelper.ParseInt(data.GetValueOrDefault("VfxUid", "0")),
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
                // SortingLayer: None/빈 값이면 기존 런타임 기본 정렬 정책(기존 동작)을 사용합니다.
                SortingLayer = ParseSortingLayer(data.GetValueOrDefault("SortingLayer", "None")),
                // SortingOrder: 0이면 기존 런타임 기본 정렬 정책(기존 동작)을 사용합니다.
                SortingOrder = MathHelper.ParseInt(data.GetValueOrDefault("SortingOrder", "0")),
                LifecycleType = ParseLifecycleType(data.GetValueOrDefault("LifecycleType")),
                AttachType = ParseAttachType(data.GetValueOrDefault("AttachType")),
                FollowMode = ParseFollowMode(data.GetValueOrDefault("FollowMode")),
                PoolPrewarmCount = MathHelper.ParseInt(data.GetValueOrDefault("PoolPrewarmCount")),
                PoolMaxSize = MathHelper.ParseInt(data.GetValueOrDefault("PoolMaxSize")),
                UseUnscaledTime = ConvertBoolean(data.GetValueOrDefault("UseUnscaledTime")),
            };
        }


        /// <summary>
        /// 대표 VFX UID에 직접 연결된 첫 번째 Effect 리소스를 반환합니다.
        /// </summary>
        /// <param name="vfxUid">대표 VFX UID입니다.</param>
        /// <returns>연결된 Effect 행입니다. 없으면 null입니다.</returns>
        public StruckTableVfxEffect GetFirstByVfxUid(int vfxUid)
        {
            foreach (KeyValuePair<int, StruckTableVfxEffect> pair in GetDatas())
            {
                if (pair.Value == null)
                    continue;

                if (pair.Value.VfxUid == vfxUid)
                    return pair.Value;
            }

            return null;
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

        /// <summary>
        /// vfx_effect.SortingLayer 원본 문자열을 런타임 기본값 규칙에 맞춰 정규화합니다.
        /// </summary>
        /// <param name="value">테이블에서 읽은 SortingLayer 문자열입니다.</param>
        /// <returns>빈 값이면 "None", 아니면 Trim된 원본 값을 반환합니다.</returns>
        private static string ParseSortingLayer(string value)
        {
            return string.IsNullOrWhiteSpace(value)
                ? "None"
                : value.Trim();
        }
    }
}
