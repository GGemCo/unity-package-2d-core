using System;
using UnityEngine;

namespace GGemCo2DCore
{
    /// <summary>
    /// 캐릭터가 피격될 때 피격 VFX를 재생할 시점을 정의합니다.
    /// </summary>
    public enum IncomingHitVfxTriggerType
    {
        /// <summary>
        /// 실제 데미지가 확정된 시점에 피격 VFX를 재생합니다.
        /// </summary>
        OnDamageConfirmed = 0,

        /// <summary>
        /// 피격 애니메이션 이벤트(<c>GGemCoAniEventHit</c>) 시점에 피격 VFX를 재생합니다.
        /// </summary>
        OnAnimationEventHit = 1,

        /// <summary>
        /// 데미지 확정과 애니메이션 이벤트 두 경로 모두에서 피격 VFX를 재생합니다.
        /// </summary>
        Both = 2,
    }

    /// <summary>
    /// 캐릭터 피격 VFX의 재생 옵션을 정의합니다.
    /// </summary>
    /// <remarks>
    /// 피격 시스템 고유 정책은 이 구조가 담당하고, 실제 VFX 생성 정보는
    /// <see cref="StruckAnimationEventVfx"/>를 재사용합니다.
    /// 이전 버전의 단일 필드 저장 데이터를 잃지 않도록 레거시 필드를 숨김 상태로 유지합니다.
    /// </remarks>
    [Serializable]
    public struct IncomingHitVfxSettings
    {
        [Tooltip("피격 VFX를 사용할지 여부")]
        public bool enabled;

        [Tooltip("피격 VFX 재생 트리거 방식(데미지 확정/애니메이션 이벤트)을 선택합니다.")]
        public IncomingHitVfxTriggerType triggerType;

        [Tooltip("연속 피격 시 VFX 재생 최소 간격(초, 0 이하이면 제한 없음)")]
        [Min(0f)]
        public float minIntervalSeconds;

        /// <summary>
        /// 피격 VFX가 캐릭터를 따라가는 방식을 정의합니다.
        /// </summary>
        /// <remarks>
        /// <see cref="VfxConstants.FollowMode.None"/>이면 <see cref="StruckAnimationEventVfx.FlipPolicy"/>의
        /// <see cref="AnimationEventVfxFlipPolicy.EventCharacterFollow"/> 설정을 기존 호환 Follow 정책으로 사용합니다.
        /// </remarks>
        [Tooltip("피격 VFX Follow 모드입니다. None이면 VFX FlipPolicy의 Follow 설정을 기존 호환 정책으로 사용합니다.")]
        public VfxConstants.FollowMode followMode;

        /// <summary>
        /// 피격 VFX가 Follow 중 유지할 위치 기준 정책입니다.
        /// </summary>
        [Tooltip("피격 VFX Follow 위치 기준입니다. SpawnPosition이면 최초 스폰 위치의 상대 오프셋을 유지합니다.")]
        public VfxConstants.FollowAnchorMode followAnchorMode;

        [Tooltip("실제 재생할 VFX 정보입니다. AnimationEvent VFX와 같은 위치/Flip/Offset 정책을 사용합니다.")]
        public StruckAnimationEventVfx vfx;

        [SerializeField, HideInInspector] private int vfxUid;
        [SerializeField, HideInInspector] private bool followTarget;
        [SerializeField, HideInInspector] private Vector3 positionOffset;
        [SerializeField, HideInInspector] private ConfigCommon.PositionYType positionYType;
        [SerializeField, HideInInspector] private float scaleOverride;
        [SerializeField, HideInInspector] private float durationOverride;
        [SerializeField, HideInInspector] private bool hasSortingLayerOverride;
        [SerializeField, HideInInspector] private ConfigSortingLayer.Keys sortingLayerKey;
        [SerializeField, HideInInspector] private bool hasSortingOrderOverride;
        [SerializeField, HideInInspector] private int sortingOrder;

        /// <summary>
        /// 비활성 기본 설정을 생성합니다.
        /// </summary>
        /// <returns>피격 VFX가 꺼진 기본 설정을 반환합니다.</returns>
        public static IncomingHitVfxSettings CreateDisabled()
        {
            return new IncomingHitVfxSettings
            {
                enabled = false,
                triggerType = IncomingHitVfxTriggerType.OnDamageConfirmed,
                minIntervalSeconds = 0f,
                vfx = CreateDefaultVfxPayload(),
                followMode = VfxConstants.FollowMode.None,
                followAnchorMode = VfxConstants.FollowAnchorMode.FollowTargetOrigin,
            };
        }

        /// <summary>
        /// 기존 플레이어 피격 VFX 설정을 캐릭터 공통 설정으로 변환합니다.
        /// </summary>
        /// <param name="settings">플레이어 설정에 저장된 기존 피격 VFX 설정입니다.</param>
        /// <returns>캐릭터 공통 피격 VFX 설정입니다.</returns>
        /// <remarks>
        /// 기존 플레이어 ScriptableObject의 직렬화 타입을 유지하면서,
        /// 런타임 재생 로직만 몬스터와 공유하기 위해 사용합니다.
        /// </remarks>
        public static IncomingHitVfxSettings FromPlayerSettings(GGemCoPlayerSettings.IncomingHitVfxSettings settings)
        {
            return new IncomingHitVfxSettings
            {
                enabled = settings.enabled,
                triggerType = ConvertTriggerType(settings.triggerType),
                minIntervalSeconds = settings.minIntervalSeconds,
                vfx = CreateVfxPayload(
                    settings.vfxUid,
                    settings.followTarget,
                    settings.positionOffset,
                    settings.positionYType,
                    settings.scaleOverride,
                    settings.durationOverride,
                    settings.hasSortingLayerOverride,
                    settings.sortingLayerKey,
                    settings.hasSortingOrderOverride,
                    settings.sortingOrder),
                followMode = settings.GetRuntimeFollowMode(),
                followAnchorMode = settings.GetRuntimeFollowAnchorMode(),
            };
        }

        /// <summary>
        /// 실제 재생에 사용할 VFX payload를 반환합니다.
        /// </summary>
        /// <returns>현재 설정의 VFX payload입니다. 신규 필드가 비어 있고 레거시 값이 있으면 레거시 값으로 생성합니다.</returns>
        /// <remarks>
        /// 이전 버전에서 저장된 <c>vfxUid</c>, <c>positionOffset</c> 등의 값이 있는 에셋도
        /// 런타임에서 즉시 동작하도록 fallback 변환을 제공합니다.
        /// </remarks>
        public StruckAnimationEventVfx GetRuntimeVfx()
        {
            if (vfx != null && vfx.Uid > 0)
            {
                return vfx;
            }

            return vfxUid > 0 ? CreateVfxPayloadFromLegacyFields() : vfx;
        }

        /// <summary>
        /// 레거시 필드에 저장된 값을 신규 <see cref="vfx"/> 필드로 이전합니다.
        /// </summary>
        /// <returns>마이그레이션이 반영된 설정입니다.</returns>
        /// <remarks>
        /// Unity Inspector에서 기존 에셋을 열었을 때 신규 구조에 값이 표시되도록 사용합니다.
        /// 런타임에서는 <see cref="GetRuntimeVfx"/>가 별도로 fallback을 제공하므로 저장 전에도 안전합니다.
        /// </remarks>
        public IncomingHitVfxSettings MigrateLegacyVfxIfNeeded()
        {
            if ((vfx == null || vfx.Uid <= 0) && vfxUid > 0)
            {
                // 이전 버전의 평면 필드 데이터를 신규 공용 VFX payload로 복사합니다.
                vfx = CreateVfxPayloadFromLegacyFields();

                if (followTarget && followMode == VfxConstants.FollowMode.None)
                {
                    followMode = VfxConstants.FollowMode.PositionAndFlip;
                }
            }

            return this;
        }

        /// <summary>
        /// 현재 설정과 VFX payload를 기준으로 실제 Follow 모드를 반환합니다.
        /// </summary>
        /// <param name="payload">검사할 VFX payload입니다.</param>
        /// <returns>런타임 VFX 생성 요청에 적용할 Follow 모드입니다.</returns>
        /// <remarks>
        /// 신규 <see cref="followMode"/> 값이 지정되어 있으면 우선 사용합니다.
        /// 값이 <see cref="VfxConstants.FollowMode.None"/>이면 기존 에셋 호환을 위해
        /// <see cref="AnimationEventVfxFlipPolicy.EventCharacterFollow"/>를 <see cref="VfxConstants.FollowMode.PositionAndFlip"/>으로 해석합니다.
        /// </remarks>
        public VfxConstants.FollowMode GetRuntimeFollowMode(StruckAnimationEventVfx payload)
        {
            if (followMode != VfxConstants.FollowMode.None)
            {
                return followMode;
            }

            return IsFollowVfx(payload)
                ? VfxConstants.FollowMode.PositionAndFlip
                : VfxConstants.FollowMode.None;
        }

        /// <summary>
        /// 현재 설정과 VFX payload가 지속형 Follow 정책인지 확인합니다.
        /// </summary>
        /// <param name="payload">검사할 VFX payload입니다.</param>
        /// <returns>캐릭터를 따라가는 VFX이면 <see langword="true"/>입니다.</returns>
        public bool IsRuntimeFollowVfx(StruckAnimationEventVfx payload)
        {
            return GetRuntimeFollowMode(payload) != VfxConstants.FollowMode.None;
        }

        /// <summary>
        /// 현재 설정과 VFX payload를 기준으로 실제 Follow 위치 기준 정책을 반환합니다.
        /// </summary>
        /// <param name="payload">검사할 VFX payload입니다.</param>
        /// <returns>런타임 VFX 생성 요청에 적용할 Follow 위치 기준 정책입니다.</returns>
        public VfxConstants.FollowAnchorMode GetRuntimeFollowAnchorMode(StruckAnimationEventVfx payload)
        {
            if (followAnchorMode != VfxConstants.FollowAnchorMode.FollowTargetOrigin)
            {
                return followAnchorMode;
            }

            return payload != null
                ? payload.FollowAnchorMode
                : VfxConstants.FollowAnchorMode.FollowTargetOrigin;
        }

        /// <summary>
        /// 현재 VFX payload가 지속형 Follow 정책인지 확인합니다.
        /// </summary>
        /// <param name="payload">검사할 VFX payload입니다.</param>
        /// <returns>캐릭터를 따라가는 VFX이면 <see langword="true"/>입니다.</returns>
        public static bool IsFollowVfx(StruckAnimationEventVfx payload)
        {
            return payload != null && payload.FlipPolicy == AnimationEventVfxFlipPolicy.EventCharacterFollow;
        }

        /// <summary>
        /// 기존 플레이어 전용 트리거 타입을 캐릭터 공통 트리거 타입으로 변환합니다.
        /// </summary>
        /// <param name="triggerType">플레이어 설정에 저장된 기존 트리거 타입입니다.</param>
        /// <returns>캐릭터 공통 트리거 타입입니다.</returns>
        public static IncomingHitVfxTriggerType ConvertTriggerType(GGemCoPlayerSettings.IncomingHitVfxTriggerType triggerType)
        {
            switch (triggerType)
            {
                case GGemCoPlayerSettings.IncomingHitVfxTriggerType.OnAnimationEventPlayerHit:
                    return IncomingHitVfxTriggerType.OnAnimationEventHit;
                case GGemCoPlayerSettings.IncomingHitVfxTriggerType.Both:
                    return IncomingHitVfxTriggerType.Both;
                case GGemCoPlayerSettings.IncomingHitVfxTriggerType.OnDamageConfirmed:
                default:
                    return IncomingHitVfxTriggerType.OnDamageConfirmed;
            }
        }

        /// <summary>
        /// 기본 VFX payload를 생성합니다.
        /// </summary>
        /// <returns>기본값으로 초기화된 VFX payload입니다.</returns>
        private static StruckAnimationEventVfx CreateDefaultVfxPayload()
        {
            return new StruckAnimationEventVfx
            {
                Uid = 0,
                Scale = 1f,
                Duration = 0f,
                Color = "FFFFFF",
                PositionBasis = AnimationEventVfxPositionBasis.EventObject,
                OffsetX = 0f,
                OffsetY = 0f,
                OffsetZ = 0f,
                MirrorOffsetXByCharacterFlip = true,
                FlipPolicy = AnimationEventVfxFlipPolicy.EventCharacterOnSpawn,
                FollowAnchorMode = VfxConstants.FollowAnchorMode.FollowTargetOrigin,
                HasSortingLayerOverride = false,
                SortingLayerKey = ConfigSortingLayer.Keys.CharacterTop,
                HasSortingOrderOverride = false,
                SortingOrder = 0,
            };
        }

        /// <summary>
        /// 기존 피격 VFX 평면 필드 값을 공용 AnimationEvent VFX payload로 변환합니다.
        /// </summary>
        /// <param name="uid">재생할 VFX Uid입니다.</param>
        /// <param name="follow">캐릭터를 따라갈지 여부입니다.</param>
        /// <param name="offset">월드 오프셋입니다.</param>
        /// <param name="yType">기존 Y 위치 정책입니다.</param>
        /// <param name="scale">스케일 오버라이드입니다.</param>
        /// <param name="duration">지속 시간 오버라이드입니다.</param>
        /// <param name="useSortingLayerOverride">Sorting Layer 오버라이드 사용 여부입니다.</param>
        /// <param name="sortingLayer">Sorting Layer 키입니다.</param>
        /// <param name="useSortingOrderOverride">Sorting Order 오버라이드 사용 여부입니다.</param>
        /// <param name="order">Sorting Order 값입니다.</param>
        /// <returns>변환된 VFX payload입니다.</returns>
        private static StruckAnimationEventVfx CreateVfxPayload(
            int uid,
            bool follow,
            Vector3 offset,
            ConfigCommon.PositionYType yType,
            float scale,
            float duration,
            bool useSortingLayerOverride,
            ConfigSortingLayer.Keys sortingLayer,
            bool useSortingOrderOverride,
            int order)
        {
            StruckAnimationEventVfx payload = CreateDefaultVfxPayload();
            payload.Uid = uid;
            payload.Scale = scale > 0f ? scale : 0f;
            payload.Duration = Mathf.Max(0f, duration);
            payload.PositionBasis = yType == ConfigCommon.PositionYType.CharacterHeight
                ? AnimationEventVfxPositionBasis.EventCharacterHead
                : AnimationEventVfxPositionBasis.EventObject;
            payload.OffsetX = offset.x;
            payload.OffsetY = offset.y;
            payload.OffsetZ = offset.z;
            payload.FlipPolicy = follow
                ? AnimationEventVfxFlipPolicy.EventCharacterFollow
                : AnimationEventVfxFlipPolicy.EventCharacterOnSpawn;
            payload.HasSortingLayerOverride = useSortingLayerOverride;
            payload.SortingLayerKey = sortingLayer;
            payload.HasSortingOrderOverride = useSortingOrderOverride;
            payload.SortingOrder = order;
            return payload;
        }

        /// <summary>
        /// 숨김 레거시 필드 값을 공용 VFX payload로 변환합니다.
        /// </summary>
        /// <returns>레거시 필드 기반 VFX payload입니다.</returns>
        private StruckAnimationEventVfx CreateVfxPayloadFromLegacyFields()
        {
            return CreateVfxPayload(
                vfxUid,
                followTarget,
                positionOffset,
                positionYType,
                scaleOverride,
                durationOverride,
                hasSortingLayerOverride,
                sortingLayerKey,
                hasSortingOrderOverride,
                sortingOrder);
        }
    }
}
