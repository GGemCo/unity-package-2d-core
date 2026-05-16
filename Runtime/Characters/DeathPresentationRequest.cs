namespace GGemCo2DCore
{
    /// <summary>
    /// 캐릭터 사망 시 기본 사망 애니메이션 대신 재생할 연출 요청 데이터입니다.
    /// </summary>
    /// <remarks>
    /// Core는 Affect, Skill 같은 상위 패키지를 직접 참조하지 않아야 하므로,
    /// 사망 원인별 세부 데이터는 이 범용 요청으로 변환된 뒤 전달됩니다.
    /// </remarks>
    public sealed class DeathPresentationRequest
    {
        /// <summary>
        /// 사망 시 재생할 캐릭터 애니메이션 이름입니다.
        /// 비어 있으면 기본 사망 애니메이션으로 폴백할 수 있습니다.
        /// </summary>
        public string AnimationName;

        /// <summary>
        /// 사망 위치에 재생할 VFX UID입니다.
        /// 0 이하이면 VFX를 재생하지 않습니다.
        /// </summary>
        public int VfxUid;

        /// <summary>
        /// 사망 VFX의 스케일 오버라이드 값입니다.
        /// 0 이하이면 VFX 테이블의 기본 스케일 정책을 사용합니다.
        /// </summary>
        public float VfxScale;

        /// <summary>
        /// 사망 VFX의 Y축 오프셋입니다.
        /// </summary>
        public float VfxOffsetY;

        /// <summary>
        /// 사망 VFX 위치 계산 시 캐릭터 높이를 반영할지 여부입니다.
        /// </summary>
        public ConfigCommon.PositionYType VfxPositionYType;

        /// <summary>
        /// 사망 VFX가 사망 캐릭터를 따라가야 하는지 여부입니다.
        /// </summary>
        public bool FollowVfxTarget;

        /// <summary>
        /// 사망 VFX 정렬 레이어를 덮어쓸지 여부입니다.
        /// </summary>
        public bool HasVfxSortingLayerOverride;

        /// <summary>
        /// 사망 VFX에 적용할 정렬 레이어 키입니다.
        /// </summary>
        public ConfigSortingLayer.Keys VfxSortingLayerKey;

        /// <summary>
        /// 사망 VFX 지속 시간 오버라이드입니다.
        /// 0 이하이면 VFX 테이블 기본 지속 시간을 사용합니다.
        /// </summary>
        public float VfxDurationOverride;

        /// <summary>
        /// 사망 시 재생할 컷씬 UID입니다.
        /// 현재 Core 기본 구현에서는 보관만 하며, 프로젝트별 컷씬 브리지에서 확장할 수 있습니다.
        /// </summary>
        public int CutsceneUid;

        /// <summary>
        /// 전용 애니메이션이 없을 때 기본 사망 애니메이션 폴백을 막을지 여부입니다.
        /// </summary>
        public bool SuppressDefaultDeathAnimation;

        /// <summary>
        /// 사망 애니메이션 종료 후 마지막 프레임에 고정할지 여부입니다.
        /// </summary>
        public bool FreezeLastFrame;

        /// <summary>
        /// 여러 사망 연출 후보가 있을 때 우선순위를 결정하는 값입니다.
        /// 값이 높을수록 우선합니다.
        /// </summary>
        public int Priority;

        /// <summary>
        /// 실행 가능한 사망 연출 데이터가 하나라도 있는지 확인합니다.
        /// </summary>
        public bool IsConfigured =>
            !string.IsNullOrWhiteSpace(AnimationName) ||
            VfxUid > 0 ||
            CutsceneUid > 0 ||
            SuppressDefaultDeathAnimation ||
            FreezeLastFrame;

        /// <summary>
        /// 동일 데이터를 가진 새 요청 인스턴스를 생성합니다.
        /// </summary>
        /// <returns>복제된 사망 연출 요청입니다.</returns>
        public DeathPresentationRequest Clone()
        {
            return new DeathPresentationRequest
            {
                AnimationName = AnimationName,
                VfxUid = VfxUid,
                VfxScale = VfxScale,
                VfxOffsetY = VfxOffsetY,
                VfxPositionYType = VfxPositionYType,
                FollowVfxTarget = FollowVfxTarget,
                HasVfxSortingLayerOverride = HasVfxSortingLayerOverride,
                VfxSortingLayerKey = VfxSortingLayerKey,
                VfxDurationOverride = VfxDurationOverride,
                CutsceneUid = CutsceneUid,
                SuppressDefaultDeathAnimation = SuppressDefaultDeathAnimation,
                FreezeLastFrame = FreezeLastFrame,
                Priority = Priority,
            };
        }
    }
}
