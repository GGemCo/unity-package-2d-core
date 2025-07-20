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
        public string PrefabName;
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
    public class TableEffect : DefaultTable
    {
        private static readonly Dictionary<string, EffectConstants.Category> MapCategory;
        static TableEffect()
        {
            MapCategory = new Dictionary<string, EffectConstants.Category>
            {
                { "Skill", EffectConstants.Category.Skill },
                { "Player", EffectConstants.Category.Player },
                { "Monster", EffectConstants.Category.Monster },
            };
        }
        private EffectConstants.Category ConvertType(string grade) => MapCategory.GetValueOrDefault(grade, EffectConstants.Category.None);
        public StruckTableEffect GetDataByUid(int uid)
        {
            if (uid <= 0)
            {
                GcLogger.LogError("uid is 0.");
                return null;
            }
            var data = GetData(uid);
            if (data == null) return null;
            return new StruckTableEffect
            {
                Uid = int.Parse(data["Uid"]),
                Name = data["Name"],
                Category = ConvertType(data["Category"]),
                PrefabName = data["PrefabName"],
                Width = int.Parse(data["Width"]),
                Height = int.Parse(data["Height"]),
                ColliderSize = ConvertVector2(data["ColliderSize"]),
                NeedRotation = ConvertBoolean(data["NeedRotation"]),
                Color = data["Color"],
                DefaultDirection = ConfigCommon.GetDirectionType(data["DefaultDirection"]),
            };
        }
    }
}