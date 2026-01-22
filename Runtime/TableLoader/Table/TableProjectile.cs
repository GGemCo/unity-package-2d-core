using System.Collections.Generic;
using UnityEngine;

namespace GGemCo2DCore
{
    /// <summary>
    /// Projectile 테이블 Row 구조.
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
    /// Projectile 테이블.
    /// </summary>
    public class TableProjectile : DefaultTable<StruckTableProjectile>
    {
        public override string Key => ConfigAddressableTable.Projectile;

        protected override StruckTableProjectile BuildRow(Dictionary<string, string> data)
        {
            // Type 컬럼이 없던 레거시 데이터와의 호환을 위해 기본값은 Default로 둔다.
            ProjectileConstants.Type type = ProjectileConstants.Type.Default;
            if (data.TryGetValue("Type", out string typeRaw) && !string.IsNullOrEmpty(typeRaw))
                type = EnumHelper.ConvertEnum<ProjectileConstants.Type>(typeRaw);

            return new StruckTableProjectile
            {
                Uid = MathHelper.ParseInt(data["Uid"]),
                Type = type,
                Name = data["Name"],
                EffectUid = MathHelper.ParseInt(data["EffectUid"]),
                EffectScale = MathHelper.ParseFloat(data["EffectScale"]),
                MoveSpeed = MathHelper.ParseInt(data["MoveSpeed"]),
                ArcHeightMin = MathHelper.ParseInt(data["ArcHeightMin"]),
                ArcHeightMax = MathHelper.ParseInt(data["ArcHeightMax"]),
                StartPosition = ConvertVector2(data["StartPosition"]),
                ColliderSize = ConvertVector2(data["ColliderSize"]),
                HitEffectUid = MathHelper.ParseInt(data["HitEffectUid"]),
                TargetType = EnumHelper.ConvertEnum<ProjectileConstants.TargetType>(data["TargetType"]),
                TargetPositionRangeX = MathHelper.ParseInt(data["TargetPositionRangeX"]),
                Count = MathHelper.ParseInt(data["Count"]),
                SecDelayByOne = MathHelper.ParseFloat(data["SecDelayByOne"]),
            };
        }
    }
}
