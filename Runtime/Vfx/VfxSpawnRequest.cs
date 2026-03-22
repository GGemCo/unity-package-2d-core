using UnityEngine;

namespace GGemCo2DCore
{
    public struct VfxSpawnRequest
    {
        public int VfxUid;
        public CharacterBase Owner;
        public CharacterBase Target;
        public CharacterBase FollowTarget;
        public GameObject OwnerGameObject;
        public Vector3? WorldPosition;
        public Transform Parent;
        public float DurationOverride;
        public float ScaleOverride;
        public string ColorOverride;
        public bool ForceUiCanvasParent;
        public ConfigSortingLayer.Keys? SortingLayerOverride;
        public int? SortingOrderOverride;
        public float PositionY;
        public ConfigCommon.PositionYType PositionYType;
        public VfxConstants.LifecycleType? LifecycleTypeOverride;
        public VfxConstants.AttachType? AttachTypeOverride;
        public VfxConstants.FollowMode? FollowModeOverride;

        public static VfxSpawnRequest FromAnimationEvent(StruckAnimationEventVfx data, GameObject fromObject = null)
        {
            var owner = fromObject != null ? fromObject.GetComponent<CharacterBase>() : null;
            return new VfxSpawnRequest
            {
                VfxUid = data?.Uid ?? 0,
                Owner = owner,
                OwnerGameObject = fromObject,
                DurationOverride = data?.Duration ?? 0f,
                ScaleOverride = data?.Scale ?? 0f,
                ColorOverride = data != null ? data.Color : string.Empty,
            };
        }
    }
}