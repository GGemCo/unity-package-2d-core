using UnityEngine;

namespace GGemCo2DCore
{
    /// <summary>
    /// 레이저 시작점 정책을 해석하는 공용 유틸리티입니다.
    /// - Skill 미리보기와 런타임이 동일한 규칙을 사용하도록 시작점 계산을 중앙화합니다.
    /// - laser 테이블 기본 오프셋과 런타임 오버라이드 정책을 함께 반영합니다.
    /// </summary>
    public static class LaserStartPointResolver
    {
        /// <summary>
        /// 현재 컨텍스트에서 사용할 레이저 시작점을 계산합니다.
        /// </summary>
        /// <param name="info">laser 테이블 정보입니다.</param>
        /// <param name="metadata">런타임 오버라이드 메타데이터입니다.</param>
        /// <param name="fallbackTransformPosition">시전자가 없을 때 사용할 기본 위치입니다.</param>
        /// <returns>정책을 반영한 시작점 월드 좌표입니다.</returns>
        public static Vector2 ResolveCurrentStartPoint(
            StruckTableLaser info,
            MetadataLaser metadata,
            Vector2 fallbackTransformPosition)
        {
            Vector2 ownerPosition = fallbackTransformPosition;
            if (metadata != null && metadata.Owner != null)
                ownerPosition = metadata.Owner.transform.position;

            Vector2 tableOffset = info != null ? info.StartPosition : Vector2.zero;
            return ResolveStartPoint(ownerPosition, tableOffset, metadata);
        }

        /// <summary>
        /// 시전자 위치와 테이블 오프셋을 기준으로 최종 시작점을 계산합니다.
        /// </summary>
        /// <param name="ownerPosition">시전자 기준 월드 위치입니다.</param>
        /// <param name="tableOffset">laser 테이블의 StartPosition 오프셋입니다.</param>
        /// <param name="metadata">런타임 오버라이드 메타데이터입니다.</param>
        /// <returns>정책을 반영한 시작점 월드 좌표입니다.</returns>
        public static Vector2 ResolveStartPoint(
            Vector2 ownerPosition,
            Vector2 tableOffset,
            MetadataLaser metadata)
        {
            if (metadata == null)
                return ownerPosition + tableOffset;

            switch (metadata.StartPositionOverrideMode)
            {
                case LaserConstants.StartPositionOverrideMode.ReplaceTableOffset:
                    return ownerPosition + ResolveCasterFlipStartOffset(metadata.StartPositionOverride, metadata);

                case LaserConstants.StartPositionOverrideMode.AddToTableOffset:
                    return ownerPosition + ResolveCasterFlipStartOffset(tableOffset + metadata.StartPositionOverride, metadata);

                case LaserConstants.StartPositionOverrideMode.WorldPosition:
                    return metadata.StartPositionOverride;

                default:
                    return ownerPosition + ResolveCasterFlipStartOffset(tableOffset, metadata);
            }
        }

        /// <summary>
        /// 시전자 좌우 반전 상태에 따라 시작점 오프셋의 X 값을 반전합니다.
        /// </summary>
        /// <param name="offset">시전자 기준으로 계산된 시작점 오프셋입니다.</param>
        /// <param name="metadata">런타임 오버라이드 메타데이터입니다.</param>
        /// <returns>Caster flip 정책이 반영된 시작점 오프셋입니다.</returns>
        private static Vector2 ResolveCasterFlipStartOffset(Vector2 offset, MetadataLaser metadata)
        {
            if (metadata == null || metadata.UseCasterFlipStartOffsetX != true)
                return offset;

            if (metadata.Owner == null || metadata.Owner.IsFlipped() != true)
                return offset;

            return new Vector2(-offset.x, offset.y);
        }
    }
}
