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
        public string PrefabPath;
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
                PrefabPath = data["PrefabPath"],
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