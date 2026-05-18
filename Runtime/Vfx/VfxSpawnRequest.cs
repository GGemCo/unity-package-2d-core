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
        public bool ForceOneShot;
        public float ScaleOverride;
        public string ColorOverride;
        public bool ForceUiCanvasParent;
        public ConfigSortingLayer.Keys? SortingLayerOverride;
        public int? SortingOrderOverride;
        /// <summary>
        /// 스폰 기준 위치에 더해질 월드 오프셋입니다.
        /// Follow 대상이 있을 때는 매 프레임 동일한 오프셋 규칙이 유지됩니다.
        /// </summary>
        public Vector3 PositionOffset;
        public float PositionY;
        public ConfigCommon.PositionYType PositionYType;
        public VfxConstants.LifecycleType? LifecycleTypeOverride;
        public VfxConstants.AttachType? AttachTypeOverride;
        public VfxConstants.FollowMode? FollowModeOverride;

        /// <summary>
        /// true이면 생성된 VFX가 지정된 방향을 기준으로 좌우 반전과 회전을 계산합니다.
        /// </summary>
        public bool UseDirection;

        /// <summary>
        /// VFX가 바라봐야 하는 월드 기준 2D 방향입니다.
        /// </summary>
        public Vector2 Direction;

        /// <summary>
        /// 수직 방향처럼 Direction.x가 0에 가까울 때 좌우 기준으로 사용할 보조 방향입니다.
        /// </summary>
        public Vector2 SourceDirection;

        /// <summary>
        /// true이면 vfx_effect 테이블의 DefaultDirection 기반 좌우 반전을 건너뜁니다.
        /// </summary>
        public bool DisableDefaultDirectionFlip;

        /// <summary>
        /// true이면 vfx_effect 테이블의 NeedRotation 기반 각도 보정을 건너뜁니다.
        /// </summary>
        public bool DisableDirectionRotation;

        /// <summary>
        /// true이면 VFX 테이블의 EffectType과 무관하게 VfxEffectLaser 컴포넌트를 사용합니다.
        /// </summary>
        public bool ForceLaserEffectBehaviour;

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
