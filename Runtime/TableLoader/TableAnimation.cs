using System.Collections.Generic;
using UnityEngine;

namespace GGemCo2DCore
{
    /// <summary>
    /// 애니메이션 테이블 Structure
    /// </summary>
    public class StruckTableAnimation
    {
        public int Uid;
        public string Name;
        public CharacterConstants.Type Type;
        public ConfigCommon.AnimationController Controller;
        public string PrefabName;
        public float MoveStep;
        public float Width;
        public float Height;
        public float AttackRange;
        public Vector2 HitAreaSize;
        public CharacterConstants.FacingDirection8 DefaultFacingDirection8;
        public Vector2 ColliderOffset;
        public Vector2 ColliderSize;
    }
    /// <summary>
    /// 애니메이션 테이블
    /// </summary>
    public class TableAnimation : DefaultTable
    {
        private static readonly Dictionary<string, CharacterConstants.Type> MapType;
        static TableAnimation()
        {
            MapType = new Dictionary<string, CharacterConstants.Type>
            {
                { "Monster", CharacterConstants.Type.Monster },
                { "Npc", CharacterConstants.Type.Npc },
                { "Player", CharacterConstants.Type.Player },
            };
        }
        private CharacterConstants.Type ConvertType(string value) => MapType.GetValueOrDefault(value, CharacterConstants.Type.None);
        public string GetPrefabPath(int uid) => GetDataColumn(uid, "PrefabPath");
        
        public StruckTableAnimation GetDataByUid(int uid)
        {
            if (uid <= 0)
            {
                GcLogger.LogError("uid is 0.");
                return null;
            }
            var data = GetData(uid);
            if (data == null) return null;
            return new StruckTableAnimation
            {
                Uid = int.Parse(data["Uid"]),
                Name = data["Name"],
                Type = ConvertType(data["Type"]),
                Controller = ConvertAnimationController(data["Controller"]),
                PrefabName = data["PrefabName"],
                DefaultFacingDirection8 = ConvertFacing(data["DefaultFacing"]),
                Width = float.Parse(data["Width"]),
                Height = float.Parse(data["Height"]),
                MoveStep = float.Parse(data["MoveStep"]),
                AttackRange = float.Parse(data["AttackRange"]),
                HitAreaSize = ConvertVector2(data["HitAreaSize"]),
                ColliderOffset = ConvertVector2(data["ColliderOffset"]),
                ColliderSize = ConvertVector2(data["ColliderSize"]),
            };
        }
    }
}