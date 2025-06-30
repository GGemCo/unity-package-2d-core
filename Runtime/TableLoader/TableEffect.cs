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
        public EffectConstants.Type Type;
        public string PrefabName;
        public int Width;
        public int Height;
        public Vector2 ColliderSize;
        public bool NeedRotation;
        public string Color;
    }
    /// <summary>
    /// 이펙트 테이블
    /// </summary>
    public class TableEffect : DefaultTable
    {
        private static readonly Dictionary<string, EffectConstants.Type> MapType;
        static TableEffect()
        {
            MapType = new Dictionary<string, EffectConstants.Type>
            {
                { "Skill", EffectConstants.Type.Skill },
            };
        }
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
                Type = ConvertType(data["Type"]),
                PrefabName = data["PrefabName"],
                Width = int.Parse(data["Width"]),
                Height = int.Parse(data["Height"]),
                ColliderSize = ConvertVector2(data["ColliderSize"]),
                NeedRotation = ConvertBoolean(data["NeedRotation"]),
                Color = data["Color"],
            };
        }
    }
}