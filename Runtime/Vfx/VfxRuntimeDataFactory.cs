namespace GGemCo2DCore
{
    public static class VfxRuntimeDataFactory
    {
        public static VfxRuntimeData Create(StruckTableVfxEffect row)
        {
            if (row == null)
                return null;

            return new VfxRuntimeData
            {
                Uid = row.Uid,
                Name = row.Name,
                AssetKind = VfxConstants.AssetKind.Effect,
                Category = row.Category,
                EffectType = row.EffectType,
                PrefabPath = row.PrefabPath,
                AnimationController = row.AnimationController,
                Width = row.Width,
                Height = row.Height,
                ColliderSize = row.ColliderSize,
                NeedRotation = row.NeedRotation,
                Color = row.Color,
                DefaultDirection = row.DefaultDirection,
                PoolPrewarmCount = row.PoolPrewarmCount,
                PoolMaxSize = row.PoolMaxSize,
                UseUnscaledTime = row.UseUnscaledTime,
            };
        }

        public static VfxRuntimeData Create(StruckTableVfxParticle row)
        {
            if (row == null)
                return null;

            return new VfxRuntimeData
            {
                Uid = row.Uid,
                Name = row.Name,
                AssetKind = VfxConstants.AssetKind.Particle,
                EffectType = VfxConstants.EffectType.None,
                PrefabPath = row.PrefabPath,
                AnimationController = ConfigCommon.AnimationController.Sprite,
                PoolPrewarmCount = row.PoolPrewarmCount,
                PoolMaxSize = row.PoolMaxSize,
                Loop = row.Loop,
                UseUnscaledTime = row.UseUnscaledTime,
            };
        }
    }
}
