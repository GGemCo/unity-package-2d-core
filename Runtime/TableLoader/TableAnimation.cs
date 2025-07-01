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
        public string PrefabName;
        public float MoveStep;
        public float Width;
        public float Height;
        public int AttackRange;
        public Vector2 HitAreaSize;
        public CharacterConstants.CharacterFacing DefaultFacing;
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
        private CharacterConstants.Type ConvertType(string grade) => MapType.GetValueOrDefault(grade, CharacterConstants.Type.None);
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
                PrefabName = data["PrefabName"],
                MoveStep = float.Parse(data["MoveStep"]),
                AttackRange = int.Parse(data["AttackRange"]),
                Width = float.Parse(data["Width"]),
                Height = float.Parse(data["Height"]),
                HitAreaSize = ConvertVector2(data["HitAreaSize"]),
                DefaultFacing = ConvertFacing(data["DefaultFacing"]),
            };
        }
    }
}