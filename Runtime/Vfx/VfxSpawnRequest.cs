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
        /// <summary>
        /// true이면 Owner/OwnerGameObject에서 해석한 캐릭터를 AttachType.Owner 정책에는 사용하지 않습니다.
        /// AnimationEvent VFX처럼 생성 위치와 Flip 기준으로만 캐릭터가 필요한 경우 사용합니다.
        /// </summary>
        public bool IgnoreOwnerAttachPolicy;
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

        /// <summary>
        /// true이면 생성 캐릭터를 기록하되 생성 시점의 좌우 Flip 적용은 건너뜁니다.
        /// </summary>
        public bool DisableOwnerFlipOnSpawn;

        /// <summary>
        /// AnimationEvent VFX JSON 데이터를 VFX 생성 요청으로 변환합니다.
        /// </summary>
        /// <param name="data">AnimationEvent에서 전달된 VFX 데이터입니다.</param>
        /// <param name="fromObject">AnimationEvent를 발생시킨 오브젝트입니다.</param>
        /// <returns>VFX 생성 요청입니다.</returns>
        public static VfxSpawnRequest FromAnimationEvent(StruckAnimationEventVfx data, GameObject fromObject = null)
        {
            CharacterBase owner = ResolveAnimationEventCharacter(fromObject);
            Vector3? worldPosition = fromObject != null ? fromObject.transform.position : (Vector3?)null;
            Vector3 offset = BuildAnimationEventOffset(data, owner);

            var request = new VfxSpawnRequest
            {
                VfxUid = data?.Uid ?? 0,
                Owner = owner,
                OwnerGameObject = fromObject,
                IgnoreOwnerAttachPolicy = true,
                WorldPosition = worldPosition,
                DurationOverride = data?.Duration ?? 0f,
                ScaleOverride = data?.Scale ?? 0f,
                ColorOverride = data != null ? data.Color : string.Empty,
                PositionOffset = offset,
                PositionYType = ResolveAnimationEventPositionYType(data, owner),
            };

            ApplyAnimationEventFlipPolicy(ref request, data, owner);
            return request;
        }

        /// <summary>
        /// AnimationEvent를 발생시킨 오브젝트에서 캐릭터 기준을 찾습니다.
        /// </summary>
        /// <param name="fromObject">AnimationEvent를 발생시킨 오브젝트입니다.</param>
        /// <returns>찾은 캐릭터입니다. 없으면 null을 반환합니다.</returns>
        private static CharacterBase ResolveAnimationEventCharacter(GameObject fromObject)
        {
            if (fromObject == null)
                return null;

            CharacterBase owner = fromObject.GetComponent<CharacterBase>();
            return owner != null ? owner : fromObject.GetComponentInParent<CharacterBase>();
        }

        /// <summary>
        /// AnimationEvent VFX의 월드 오프셋을 계산합니다.
        /// </summary>
        /// <param name="data">AnimationEvent에서 전달된 VFX 데이터입니다.</param>
        /// <param name="owner">이벤트 발생 캐릭터입니다.</param>
        /// <returns>캐릭터 Flip 정책이 반영된 월드 오프셋입니다.</returns>
        private static Vector3 BuildAnimationEventOffset(StruckAnimationEventVfx data, CharacterBase owner)
        {
            if (data == null)
                return Vector3.zero;

            Vector3 offset = new Vector3(data.OffsetX, data.OffsetY, data.OffsetZ);
            if (ShouldMirrorAnimationEventOffsetX(data, owner))
                offset.x = -offset.x;

            return offset;
        }

        /// <summary>
        /// AnimationEvent VFX의 X축 오프셋을 캐릭터 Flip 상태로 반전해야 하는지 확인합니다.
        /// </summary>
        /// <param name="data">AnimationEvent에서 전달된 VFX 데이터입니다.</param>
        /// <param name="owner">이벤트 발생 캐릭터입니다.</param>
        /// <returns>반전이 필요하면 true입니다.</returns>
        private static bool ShouldMirrorAnimationEventOffsetX(StruckAnimationEventVfx data, CharacterBase owner)
        {
            return data != null
                   && data.MirrorOffsetXByCharacterFlip
                   && owner != null
                   && owner.IsFlipped();
        }

        /// <summary>
        /// AnimationEvent VFX의 위치 기준을 기존 VFX Y 위치 정책으로 변환합니다.
        /// </summary>
        /// <param name="data">AnimationEvent에서 전달된 VFX 데이터입니다.</param>
        /// <param name="owner">이벤트 발생 캐릭터입니다.</param>
        /// <returns>VFX 생성 요청에 적용할 Y 위치 정책입니다.</returns>
        private static ConfigCommon.PositionYType ResolveAnimationEventPositionYType(StruckAnimationEventVfx data, CharacterBase owner)
        {
            if (data == null || owner == null)
                return ConfigCommon.PositionYType.None;

            return data.PositionBasis == AnimationEventVfxPositionBasis.EventCharacterHead
                ? ConfigCommon.PositionYType.CharacterHeight
                : ConfigCommon.PositionYType.None;
        }

        /// <summary>
        /// AnimationEvent VFX의 캐릭터 Flip 반영 정책을 생성 요청에 적용합니다.
        /// </summary>
        /// <param name="request">수정할 VFX 생성 요청입니다.</param>
        /// <param name="data">AnimationEvent에서 전달된 VFX 데이터입니다.</param>
        /// <param name="owner">이벤트 발생 캐릭터입니다.</param>
        private static void ApplyAnimationEventFlipPolicy(
            ref VfxSpawnRequest request,
            StruckAnimationEventVfx data,
            CharacterBase owner)
        {
            AnimationEventVfxFlipPolicy flipPolicy = data?.FlipPolicy ?? AnimationEventVfxFlipPolicy.EventCharacterOnSpawn;

            switch (flipPolicy)
            {
                case AnimationEventVfxFlipPolicy.None:
                    request.DisableOwnerFlipOnSpawn = true;
                    break;
                case AnimationEventVfxFlipPolicy.EventCharacterFollow:
                    request.DisableOwnerFlipOnSpawn = false;
                    if (owner != null)
                    {
                        request.FollowTarget = owner;
                        request.FollowModeOverride = VfxConstants.FollowMode.PositionAndFlip;
                    }
                    break;
                case AnimationEventVfxFlipPolicy.EventCharacterOnSpawn:
                default:
                    request.DisableOwnerFlipOnSpawn = false;
                    break;
            }
        }
    }
}
