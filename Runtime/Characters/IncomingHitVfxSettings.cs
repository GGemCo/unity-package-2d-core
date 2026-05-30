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
    /// 플레이어 전용 설정과 몬스터 전용 설정이 동일한 실행 로직을 사용할 수 있도록
    /// 캐릭터 공통 설정 타입으로 분리합니다.
    /// </remarks>
    [Serializable]
    public struct IncomingHitVfxSettings
    {
        [Tooltip("피격 VFX를 사용할지 여부")]
        public bool enabled;

        [Tooltip("재생할 vfx_effect 테이블 Uid")]
        public int vfxUid;

        [Tooltip("VFX를 피격 캐릭터를 따라가며 재생할지 여부")]
        public bool followTarget;

        [Tooltip("피격 VFX의 추가 위치 오프셋(World 기준)")]
        public Vector3 positionOffset;

        [Tooltip("Y 위치 계산 시 캐릭터 높이 자동 반영 여부")]
        public ConfigCommon.PositionYType positionYType;

        [Tooltip("VFX 크기 오버라이드 값 (0 이하이면 테이블 기본값 사용)")]
        public float scaleOverride;

        [Tooltip("VFX 지속 시간 오버라이드 값(초, 0 이하이면 테이블 기본값 사용)")]
        public float durationOverride;

        [Tooltip("Sorting Layer 오버라이드 사용 여부")]
        public bool hasSortingLayerOverride;

        [Tooltip("오버라이드할 Sorting Layer 키")]
        public ConfigSortingLayer.Keys sortingLayerKey;

        [Tooltip("Sorting Order 오버라이드 사용 여부")]
        public bool hasSortingOrderOverride;

        [Tooltip("오버라이드할 Sorting Order 값")]
        public int sortingOrder;

        [Tooltip("연속 피격 시 VFX 재생 최소 간격(초, 0 이하이면 제한 없음)")]
        [Min(0f)]
        public float minIntervalSeconds;

        [Tooltip("피격 VFX 재생 트리거 방식(데미지 확정/애니메이션 이벤트)을 선택합니다.")]
        public IncomingHitVfxTriggerType triggerType;

        /// <summary>
        /// 비활성 기본 설정을 생성합니다.
        /// </summary>
        /// <returns>피격 VFX가 꺼진 기본 설정을 반환합니다.</returns>
        public static IncomingHitVfxSettings CreateDisabled()
        {
            return new IncomingHitVfxSettings
            {
                enabled = false,
                vfxUid = 0,
                followTarget = false,
                positionOffset = Vector3.zero,
                positionYType = ConfigCommon.PositionYType.None,
                scaleOverride = 0f,
                durationOverride = 0f,
                hasSortingLayerOverride = false,
                sortingLayerKey = ConfigSortingLayer.Keys.CharacterTop,
                hasSortingOrderOverride = false,
                sortingOrder = 0,
                minIntervalSeconds = 0f,
                triggerType = IncomingHitVfxTriggerType.OnDamageConfirmed,
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
                vfxUid = settings.vfxUid,
                followTarget = settings.followTarget,
                positionOffset = settings.positionOffset,
                positionYType = settings.positionYType,
                scaleOverride = settings.scaleOverride,
                durationOverride = settings.durationOverride,
                hasSortingLayerOverride = settings.hasSortingLayerOverride,
                sortingLayerKey = settings.sortingLayerKey,
                hasSortingOrderOverride = settings.hasSortingOrderOverride,
                sortingOrder = settings.sortingOrder,
                minIntervalSeconds = settings.minIntervalSeconds,
                triggerType = ConvertTriggerType(settings.triggerType),
            };
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
    }
}
