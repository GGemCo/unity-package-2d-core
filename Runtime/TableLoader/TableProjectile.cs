using System.Collections.Generic;
using UnityEngine;

namespace GGemCo2DCore
{
    /// <summary>
    /// 맵 테이블 Structure
    /// </summary>
    public class StruckTableProjectile
    {
        public int Uid;
        public ProjectileConstants.Type Type;
        public string Name;
        public int EffectUid;
        public float EffectScale;
        public int MoveSpeed;
        public int ArcHeightMin;
        public int ArcHeightMax;
        public Vector2 StartPosition;
        public Vector2 ColliderSize;
        public int HitEffectUid;
        public ProjectileConstants.TargetType TargetType;
        public int TargetPositionRangeX;
        public int Count;
        public float SecDelayByOne;
    }
    /// <summary>
    /// 맵 테이블
    /// </summary>
    public class TableProjectile : DefaultTable
    {
        private static readonly Dictionary<string, ProjectileConstants.Type> MapType;
        private static readonly Dictionary<string, ProjectileConstants.TargetType> MapTargetType;

        static TableProjectile()
        {
            MapType = new Dictionary<string, ProjectileConstants.Type>
            {
                { "Default", ProjectileConstants.Type.Default },
                { "Laser", ProjectileConstants.Type.Laser },
            };
            MapTargetType = new Dictionary<string, ProjectileConstants.TargetType>
            {
                { "Fixed", ProjectileConstants.TargetType.Fixed },
                { "Area", ProjectileConstants.TargetType.Area },
            };
        }
        private ProjectileConstants.Type ConvertType(string grade) => MapType.GetValueOrDefault(grade, ProjectileConstants.Type.None);
        private ProjectileConstants.TargetType ConvertTargetType(string grade) => MapTargetType.GetValueOrDefault(grade, ProjectileConstants.TargetType.None);

        public StruckTableProjectile GetDataByUid(int uid)
        {
            if (uid <= 0)
            {
                GcLogger.LogError("uid is 0.");
                return null;
            }
            var data = GetData(uid);
            if (data == null) return null;
            return new StruckTableProjectile
            {
                Uid = int.Parse(data["Uid"]),
                Type = ProjectileConstants.Type.Default,
                Name = data["Name"],
                EffectUid = int.Parse(data["EffectUid"]),
                EffectScale = float.Parse(data["EffectScale"]),
                MoveSpeed = int.Parse(data["MoveSpeed"]),
                ArcHeightMin = int.Parse(data["ArcHeightMin"]),
                ArcHeightMax = int.Parse(data["ArcHeightMax"]),
                StartPosition = ConvertVector2(data["StartPosition"]),
                ColliderSize = ConvertVector2(data["ColliderSize"]),
                HitEffectUid = int.Parse(data["HitEffectUid"]),
                TargetType = ConvertTargetType(data["TargetType"]),
                TargetPositionRangeX = int.Parse(data["TargetPositionRangeX"]),
                Count = int.Parse(data["Count"]),
                SecDelayByOne = float.Parse(data["SecDelayByOne"]),
            };
        }
    }
}