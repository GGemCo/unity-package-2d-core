namespace GGemCo2DCore
{
    public static class VfxRuntimeDataFactory
    {
        public static VfxRuntimeData Create(StruckTableVfxEffect row)
        {
            if (row == null)
                return null;

            return new VfxEffectRuntimeData
            {
                Uid = row.Uid,
                Name = row.Name,
                PrefabPath = row.PrefabPath,
                EffectCategory = row.Category,
                EffectKind = row.EffectType,
                EffectAnimationController = row.AnimationController,
                EffectWidth = row.Width,
                EffectHeight = row.Height,
                EffectColliderSize = row.ColliderSize,
                EffectNeedRotation = row.NeedRotation,
                EffectColor = row.Color,
                EffectDefaultDirection = row.DefaultDirection,
                PoolPrewarmCount = row.PoolPrewarmCount,
                PoolMaxSize = row.PoolMaxSize,
                UseUnscaledTime = row.UseUnscaledTime,
                DefaultSpawnPolicy = new VfxSpawnPolicy
                {
                    LifecycleType = row.LifecycleType,
                    AttachType = row.AttachType,
                    FollowMode = row.FollowMode,
                },
            };
        }

        public static VfxRuntimeData Create(StruckTableVfxParticle row)
        {
            if (row == null)
                return null;

            return new VfxParticleRuntimeData
            {
                Uid = row.Uid,
                Name = row.Name,
                PrefabPath = row.PrefabPath,
                PoolPrewarmCount = row.PoolPrewarmCount,
                PoolMaxSize = row.PoolMaxSize,
                ParticleLoop = row.Loop,
                UseUnscaledTime = row.UseUnscaledTime,
                DefaultSpawnPolicy = new VfxSpawnPolicy
                {
                    LifecycleType = row.LifecycleType,
                    AttachType = row.AttachType,
                    FollowMode = row.FollowMode,
                },
            };
        }
    }
}
