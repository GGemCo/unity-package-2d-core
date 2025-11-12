using System.Collections.Generic;
using UnityEngine;

namespace GGemCo2DCore
{
    /// <summary>
    /// 이펙트 테이블 Structure
    /// </summary>
    public class StruckTableEffect
    {
        public int Uid;
        public string Name;
        public EffectConstants.Category Category;
        public EffectConstants.Type Type;
        public string PrefabName;
        public ConfigCommon.AnimationController AnimationController;
        public int Width;
        public int Height;
        public Vector2 ColliderSize;
        public bool NeedRotation;
        public string Color;
        public ConfigCommon.DirectionType DefaultDirection;
    }
    /// <summary>
    /// 이펙트 테이블
    /// </summary>
    public class TableEffect : DefaultTable<StruckTableEffect>
    {
        public override string Key => ConfigAddressableTable.Effect;
        protected override StruckTableEffect BuildRow(Dictionary<string, string> data)
        {
            return new StruckTableEffect
            {
                Uid = MathHelper.ParseInt(data["Uid"]),
                Name = data["Name"],
                Category = EnumHelper.ConvertEnum<EffectConstants.Category>(data["Category"]),
                Type = EffectConstants.Type.Default,
                PrefabName = data["PrefabName"],
                AnimationController = ConvertAnimationController(data["AnimationController"]),
                Width = MathHelper.ParseInt(data["Width"]),
                Height = MathHelper.ParseInt(data["Height"]),
                ColliderSize = ConvertVector2(data["ColliderSize"]),
                NeedRotation = ConvertBoolean(data["NeedRotation"]),
                Color = data["Color"],
                DefaultDirection = ConfigCommon.GetDirectionType(data["DefaultDirection"]),
            };
        }
    }
}