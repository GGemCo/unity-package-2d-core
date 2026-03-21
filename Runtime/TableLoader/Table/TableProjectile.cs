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
        public int VfxUid;
        public float VfxScale;
        public int MoveSpeed;
        public int ArcHeightMin;
        public int ArcHeightMax;
        public Vector2 StartPosition;
        public Vector2 ColliderSize;
        /// <summary>
        /// Projectile 로컬 좌표 기준 Collider 중심 오프셋.
        /// - (0,0) 이면 기존(중심) 동작과 동일합니다.
        /// </summary>
        public Vector2 ColliderOffset;
        public int HitVfxUid;
        public ProjectileConstants.TargetType TargetType;
        public int TargetPositionRangeX;
        public int Count;
        public float SecDelayByOne;

        // ---- Boundary (Camera view) ----
        public ProjectileConstants.BoundaryMode BoundaryMode;
        public float BoundaryPadding;
        public int BounceMaxCount;
        public float BounceSpeedMultiplier;
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

            // ColliderOffset 컬럼이 없던 레거시 데이터와의 호환을 위해 기본값은 (0,0)으로 둔다.
            Vector2 colliderOffset = Vector2.zero;
            if (data.TryGetValue("ColliderOffset", out string colliderOffsetRaw) && !string.IsNullOrEmpty(colliderOffsetRaw))
                colliderOffset = ConvertVector2(colliderOffsetRaw);

            return new StruckTableProjectile
            {
                Uid = MathHelper.ParseInt(data["Uid"]),
                Type = type,
                Name = data["Name"],
                VfxUid = MathHelper.ParseInt(data["VfxUid"]),
                VfxScale = MathHelper.ParseFloat(data["VfxScale"]),
                MoveSpeed = MathHelper.ParseInt(data["MoveSpeed"]),
                ArcHeightMin = MathHelper.ParseInt(data["ArcHeightMin"]),
                ArcHeightMax = MathHelper.ParseInt(data["ArcHeightMax"]),
                StartPosition = ConvertVector2(data["StartPosition"]),
                ColliderSize = ConvertVector2(data["ColliderSize"]),
                ColliderOffset = colliderOffset,
                HitVfxUid = MathHelper.ParseInt(data["HitVfxUid"]),
                TargetType = EnumHelper.ConvertEnum<ProjectileConstants.TargetType>(data["TargetType"]),
                TargetPositionRangeX = MathHelper.ParseInt(data["TargetPositionRangeX"]),
                Count = MathHelper.ParseInt(data["Count"]),
                SecDelayByOne = MathHelper.ParseFloat(data["SecDelayByOne"]),
                BoundaryMode = EnumHelper.ConvertEnum<ProjectileConstants.BoundaryMode>(data["BoundaryMode"]),
                BoundaryPadding = MathHelper.ParseFloat(data["BoundaryPadding"]),
                BounceMaxCount = MathHelper.ParseInt(data["BounceMaxCount"]),
                BounceSpeedMultiplier = MathHelper.ParseFloat(data["BounceSpeedMultiplier"]),
            };
        }
    }
}