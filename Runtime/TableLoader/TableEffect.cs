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
    public class TableEffect : DefaultTable
    {
        private static readonly Dictionary<string, EffectConstants.Category> MapCategory;
        private static readonly Dictionary<string, EffectConstants.Type> MapType;
        static TableEffect()
        {
            MapCategory = new Dictionary<string, EffectConstants.Category>
            {
                { "Skill", EffectConstants.Category.Skill },
                { "Player", EffectConstants.Category.Player },
                { "Monster", EffectConstants.Category.Monster },
                { "Etc", EffectConstants.Category.Etc },
            };
            MapType = new Dictionary<string, EffectConstants.Type>
            {
                { "Default", EffectConstants.Type.Default },
                { "Laser", EffectConstants.Type.Laser },
            };
        }
        private EffectConstants.Category ConvertCategory(string grade) => MapCategory.GetValueOrDefault(grade, EffectConstants.Category.None);
        private EffectConstants.Type ConvertType(string grade) => MapType.GetValueOrDefault(grade, EffectConstants.Type.None);
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
                Category = ConvertCategory(data["Category"]),
                Type = ConvertType(data["Type"]),
                PrefabName = data["PrefabName"],
                AnimationController = ConvertAnimationController(data["AnimationController"]),
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