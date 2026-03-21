using UnityEngine;

namespace GGemCo2DCore
{
    public sealed class VfxRuntimeData
    {
        public int Uid;
        public string Name;
        public VfxConstants.AssetKind AssetKind;
        public VfxConstants.Category Category;
        public VfxConstants.EffectType EffectType;
        public string PrefabPath;
        public ConfigCommon.AnimationController AnimationController;
        public int Width;
        public int Height;
        public Vector2 ColliderSize;
        public bool NeedRotation;
        public string Color;
        public ConfigCommon.DirectionType DefaultDirection;
        public VfxConstants.PlaybackType PlaybackType;
        public VfxConstants.LifecycleType LifecycleType;
        public VfxConstants.AttachType AttachType;
        public VfxConstants.FollowMode FollowMode;
        public int PoolPrewarmCount;
        public int PoolMaxSize;
        public bool Loop;
        public bool UseUnscaledTime;
    }
}
