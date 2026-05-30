namespace GGemCo2DCore
{
    /// <summary>
    /// VfxResolver가 선택한 최종 VFX 리소스와 요청별 보정값을 담는 읽기 전용 결과 구조체입니다.
    /// </summary>
    public readonly struct ResolvedVfx
    {
        public readonly int RequestedVfxUid;
        public readonly int ResourceUid;
        public readonly VfxConstants.AssetKind AssetKind;
        public readonly VfxRuntimeData RuntimeData;
        public readonly float ScaleOverride;
        public readonly float DurationOverride;
        public readonly string ColorOverride;
        public readonly bool ShouldPlay;
        public readonly StruckTableVfx Vfx;
        public readonly StruckTableVfxVariant Variant;

        /// <summary>
        /// 최종 생성할 VFX 정보를 생성합니다.
        /// </summary>
        /// <param name="requestedVfxUid">외부에서 요청한 대표 VFX UID입니다.</param>
        /// <param name="resourceUid">실제 리소스 테이블 UID입니다.</param>
        /// <param name="assetKind">실제 리소스 타입입니다.</param>
        /// <param name="runtimeData">실제 VFX 런타임 데이터입니다.</param>
        /// <param name="vfx">대표 VFX 행입니다. 레거시 직접 재생이면 null일 수 있습니다.</param>
        /// <param name="variant">선택된 variant 행입니다. Direct 재생이면 null입니다.</param>
        /// <param name="scaleOverride">variant 후보의 스케일 보정값입니다.</param>
        /// <param name="durationOverride">variant 후보의 지속 시간 보정값입니다.</param>
        /// <param name="colorOverride">variant 후보의 색상 보정값입니다.</param>
        /// <param name="shouldPlay">무출력 후보를 선택했을 때 false입니다.</param>
        public ResolvedVfx(
            int requestedVfxUid,
            int resourceUid,
            VfxConstants.AssetKind assetKind,
            VfxRuntimeData runtimeData,
            StruckTableVfx vfx,
            StruckTableVfxVariant variant,
            float scaleOverride = 0f,
            float durationOverride = 0f,
            string colorOverride = null,
            bool shouldPlay = true)
        {
            RequestedVfxUid = requestedVfxUid;
            ResourceUid = resourceUid;
            AssetKind = assetKind;
            RuntimeData = runtimeData;
            Vfx = vfx;
            Variant = variant;
            ScaleOverride = scaleOverride;
            DurationOverride = durationOverride;
            ColorOverride = colorOverride ?? string.Empty;
            ShouldPlay = shouldPlay;
        }

        /// <summary>
        /// 무출력 후보 선택 결과를 생성합니다.
        /// </summary>
        /// <param name="requestedVfxUid">외부에서 요청한 대표 VFX UID입니다.</param>
        /// <param name="vfx">대표 VFX 행입니다.</param>
        /// <param name="variant">무출력으로 선택된 variant 행입니다.</param>
        /// <returns>생성하지 않는 결과입니다.</returns>
        public static ResolvedVfx Silent(int requestedVfxUid, StruckTableVfx vfx, StruckTableVfxVariant variant)
        {
            return new ResolvedVfx(requestedVfxUid, 0, VfxConstants.AssetKind.None, null, vfx, variant, shouldPlay: false);
        }
    }
}
