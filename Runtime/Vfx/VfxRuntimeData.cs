using UnityEngine;

namespace GGemCo2DCore
{
    public sealed class VfxSpawnPolicy
    {
        public VfxConstants.LifecycleType LifecycleType = VfxConstants.LifecycleType.AutoRelease;
        public VfxConstants.AttachType AttachType = VfxConstants.AttachType.World;
        public VfxConstants.FollowMode FollowMode = VfxConstants.FollowMode.None;

        public VfxSpawnPolicy Clone()
        {
            return new VfxSpawnPolicy
            {
                LifecycleType = LifecycleType,
                AttachType = AttachType,
                FollowMode = FollowMode,
            };
        }
    }

    public abstract class VfxRuntimeData
    {
        public int Uid;
        public string Name;
        public string PrefabPath;
        public int PoolPrewarmCount;
        public int PoolMaxSize;
        public bool UseUnscaledTime;
        public VfxSpawnPolicy DefaultSpawnPolicy = new VfxSpawnPolicy();

        public abstract VfxConstants.AssetKind AssetKind { get; }
        public virtual VfxConstants.PlaybackType PlaybackType => VfxConstants.PlaybackType.Auto;
        public virtual VfxConstants.Category Category => VfxConstants.Category.None;
        public virtual VfxConstants.EffectType EffectType => VfxConstants.EffectType.None;
        public virtual ConfigCommon.AnimationController AnimationController => ConfigCommon.AnimationController.Sprite;
        public virtual int Width => 0;
        public virtual int Height => 0;
        public virtual Vector2 ColliderSize => Vector2.zero;
        public virtual bool NeedRotation => false;
        public virtual string Color => string.Empty;
        public virtual ConfigCommon.DirectionType DefaultDirection => ConfigCommon.DirectionType.Right;
        public virtual bool Loop => false;
    }

    public sealed class VfxEffectRuntimeData : VfxRuntimeData
    {
        public VfxConstants.Category EffectCategory;
        public VfxConstants.EffectType EffectKind;
        public string EffectPrefabPath;
        public ConfigCommon.AnimationController EffectAnimationController;
        public int EffectWidth;
        public int EffectHeight;
        public Vector2 EffectColliderSize;
        public bool EffectNeedRotation;
        public string EffectColor;
        public ConfigCommon.DirectionType EffectDefaultDirection;

        public override VfxConstants.AssetKind AssetKind => VfxConstants.AssetKind.Effect;
        public override VfxConstants.Category Category => EffectCategory;
        public override VfxConstants.EffectType EffectType => EffectKind;
        public override ConfigCommon.AnimationController AnimationController => EffectAnimationController;
        public override int Width => EffectWidth;
        public override int Height => EffectHeight;
        public override Vector2 ColliderSize => EffectColliderSize;
        public override bool NeedRotation => EffectNeedRotation;
        public override string Color => EffectColor;
        public override ConfigCommon.DirectionType DefaultDirection => EffectDefaultDirection;
        public override VfxConstants.PlaybackType PlaybackType
        {
            get
            {
                if (EffectKind == VfxConstants.EffectType.Laser)
                    return VfxConstants.PlaybackType.Laser;

                return EffectAnimationController == ConfigCommon.AnimationController.Spine
                    ? VfxConstants.PlaybackType.SpineSequence
                    : VfxConstants.PlaybackType.SpriteSequence;
            }
        }
    }

    public sealed class VfxParticleRuntimeData : VfxRuntimeData
    {
        public bool ParticleLoop;

        public override VfxConstants.AssetKind AssetKind => VfxConstants.AssetKind.Particle;
        public override VfxConstants.PlaybackType PlaybackType => VfxConstants.PlaybackType.ParticleSystem;
        public override bool Loop => ParticleLoop;
    }
}
