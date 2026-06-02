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
        public VfxConstants.FollowAnchorMode FollowAnchorMode;
        public int PoolPrewarmCount;
        public int PoolMaxSize;
        public bool UseUnscaledTime;
    }
    public class TableVfxEffect : DefaultTable<StruckTableVfxEffect>
    {
        public override string Key => ConfigAddressableTable.VfxEffect;

        protected override StruckTableVfxEffect BuildRow(Dictionary<string, string> data)
        {
            TableRowReader reader = ReadRow(data);
            return new StruckTableVfxEffect
            {
                Uid = reader.Int("Uid"),
                Name = reader.String("Name"),
                VfxUid = reader.Int("VfxUid", 0),
                Category = reader.Enum<VfxConstants.Category>("Category"),
                EffectType = reader.Enum<VfxConstants.EffectType>("EffectType"),
                PrefabPath = reader.String("PrefabPath"),
                AnimationController = reader.Enum<ConfigCommon.AnimationController>("AnimationController"),
                Width = reader.Int("Width"),
                Height = reader.Int("Height"),
                ColliderSize = reader.Vector2("ColliderSize"),
                NeedRotation = reader.BoolYN("NeedRotation"),
                Color = reader.String("Color"),
                DefaultDirection = ConfigCommon.GetDirectionType(reader.String("DefaultDirection", "Left")),
                // SortingLayer: None/빈 값이면 기존 런타임 기본 정렬 정책(기존 동작)을 사용합니다.
                SortingLayer = ParseSortingLayer(reader.String("SortingLayer", "None")),
                // SortingOrder: 0이면 기존 런타임 기본 정렬 정책(기존 동작)을 사용합니다.
                SortingOrder = reader.Int("SortingOrder", 0),
                LifecycleType = ParseLifecycleType(reader.String("LifecycleType")),
                AttachType = ParseAttachType(reader.String("AttachType")),
                FollowMode = ParseFollowMode(reader.String("FollowMode")),
                FollowAnchorMode = ParseFollowAnchorMode(reader.String("FollowAnchorMode")),
                PoolPrewarmCount = reader.Int("PoolPrewarmCount"),
                PoolMaxSize = reader.Int("PoolMaxSize"),
                UseUnscaledTime = reader.BoolYN("UseUnscaledTime"),
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
        /// Follow 위치 기준 정책을 테이블 문자열에서 변환합니다.
        /// </summary>
        /// <param name="value">vfx_effect.FollowAnchorMode 값입니다.</param>
        /// <returns>런타임 Follow 위치 기준 정책입니다.</returns>
        private static VfxConstants.FollowAnchorMode ParseFollowAnchorMode(string value)
        {
            return string.IsNullOrWhiteSpace(value)
                ? VfxConstants.FollowAnchorMode.FollowTargetOrigin
                : EnumHelper.ConvertEnum<VfxConstants.FollowAnchorMode>(value);
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
