using UnityEngine;

namespace GGemCo2DCore
{
    /// <summary>
    /// 레이저의 조준/회전 정책 해석을 공용으로 제공하는 유틸리티입니다.
    /// - LaserBeam 런타임과 Skill/Editor 프리뷰가 같은 계산식을 재사용하도록 돕습니다.
    /// - RaycastDirectionMode, RaycastAngleDeg, VfxAngleSyncMode의 우선순위를 한 곳에서 관리합니다.
    /// </summary>
    public static class LaserAimPolicyUtility
    {
        /// <summary>
        /// 현재 레이저에 적용할 Raycast 방향 계산 모드를 해석합니다.
        /// 런타임 오버라이드가 있으면 우선 적용하고, 없으면 테이블 값을 사용합니다.
        /// </summary>
        /// <param name="info">레이저 테이블 정보입니다.</param>
        /// <param name="runtime">런타임 메타데이터입니다.</param>
        /// <returns>적용할 Raycast 방향 계산 모드입니다.</returns>
        public static LaserConstants.RaycastDirectionMode ResolveRaycastDirectionMode(StruckTableLaser info, MetadataLaser runtime)
        {
            if (runtime != null && runtime.UseRaycastDirectionModeOverride)
                return runtime.RaycastDirectionModeOverride;

            return info != null
                ? info.RaycastDirectionMode
                : LaserConstants.RaycastDirectionMode.TowardTarget;
        }

        /// <summary>
        /// 현재 레이저에 적용할 VFX 각도 동기화 모드를 해석합니다.
        /// 런타임 오버라이드가 있으면 우선 적용하고, 없으면 테이블 값을 사용합니다.
        /// </summary>
        /// <param name="info">레이저 테이블 정보입니다.</param>
        /// <param name="runtime">런타임 메타데이터입니다.</param>
        /// <returns>적용할 VFX 각도 동기화 모드입니다.</returns>
        public static LaserConstants.VfxAngleSyncMode ResolveVfxAngleSyncMode(StruckTableLaser info, MetadataLaser runtime)
        {
            if (runtime != null && runtime.UseVfxAngleSyncModeOverride)
                return runtime.VfxAngleSyncModeOverride;

            return info != null
                ? info.VfxAngleSyncMode
                : LaserConstants.VfxAngleSyncMode.FollowRaycast;
        }

        /// <summary>
        /// 현재 레이저에 적용할 Raycast 각도 값을 해석합니다.
        /// 런타임 오버라이드가 있으면 우선 적용하고, 없으면 테이블 값을 사용합니다.
        /// </summary>
        /// <param name="info">레이저 테이블 정보입니다.</param>
        /// <param name="runtime">런타임 메타데이터입니다.</param>
        /// <returns>적용할 Raycast 각도(도)입니다.</returns>
        public static float ResolveRaycastAngleDeg(StruckTableLaser info, MetadataLaser runtime)
        {
            if (runtime != null && runtime.UseRaycastAngleOverride)
                return runtime.RaycastAngleOverrideDeg;

            return info != null ? info.RaycastAngleDeg : 0f;
        }

        /// <summary>
        /// 시전자 기준 기본 바라보기 방향을 반환합니다.
        /// </summary>
        /// <param name="owner">방향 기준이 되는 시전자입니다.</param>
        /// <returns>좌우 반전에 따라 결정된 기본 방향입니다.</returns>
        public static Vector2 ResolveOwnerFacingDirection(CharacterBase owner)
        {
            if (owner != null && owner.IsFlipped())
                return Vector2.left;

            return Vector2.right;
        }

        /// <summary>
        /// 현재 레이저 정책에 맞춰 각도 기반 Raycast 방향을 계산합니다.
        /// </summary>
        /// <param name="info">레이저 테이블 정보입니다.</param>
        /// <param name="runtime">런타임 메타데이터입니다.</param>
        /// <param name="owner">시전자입니다.</param>
        /// <returns>각도 정책이 반영된 정규화 방향입니다.</returns>
        public static Vector2 ResolveDirectionByConfiguredAngle(StruckTableLaser info, MetadataLaser runtime, CharacterBase owner)
        {
            float angleDeg = ResolveRaycastAngleDeg(info, runtime);
            float baseAngle = 0f;

            if (owner != null && owner.IsFlipped())
            {
                baseAngle = 180f;
                angleDeg = -angleDeg;
            }

            float worldAngle = (baseAngle + angleDeg) * Mathf.Deg2Rad;
            Vector2 direction = new Vector2(Mathf.Cos(worldAngle), Mathf.Sin(worldAngle));
            if (direction.sqrMagnitude <= 1e-6f)
                return ResolveOwnerFacingDirection(owner);

            return direction.normalized;
        }

        /// <summary>
        /// 시작점과 목표점으로부터 방향 벡터를 계산합니다.
        /// 목표점이 시작점과 동일하면 시전자 기본 바라보기 방향으로 보정합니다.
        /// </summary>
        /// <param name="owner">시전자입니다.</param>
        /// <param name="start">시작점입니다.</param>
        /// <param name="targetPosition">목표점입니다.</param>
        /// <returns>정규화된 방향 벡터입니다.</returns>
        public static Vector2 ResolveDirection(CharacterBase owner, Vector2 start, Vector2 targetPosition)
        {
            Vector2 direction = targetPosition - start;
            if (direction.sqrMagnitude <= 1e-6f)
                return ResolveOwnerFacingDirection(owner);

            return direction.normalized;
        }

        /// <summary>
        /// 기준 방향에 각도 오프셋을 적용한 정규화 방향을 반환합니다.
        /// 양수 각도는 반시계 방향, 음수 각도는 시계 방향으로 회전합니다.
        /// </summary>
        /// <param name="direction">회전할 기준 방향입니다.</param>
        /// <param name="angleOffsetDeg">기준 방향에 더할 각도(도)입니다.</param>
        /// <returns>각도 오프셋이 적용된 정규화 방향입니다.</returns>
        public static Vector2 ApplyDirectionAngleOffset(Vector2 direction, float angleOffsetDeg)
        {
            if (direction.sqrMagnitude <= 1e-6f)
                return Vector2.zero;

            Vector2 normalizedDirection = direction.normalized;
            if (Mathf.Abs(angleOffsetDeg) <= 1e-6f)
                return normalizedDirection;

            float angleRad = angleOffsetDeg * Mathf.Deg2Rad;
            float cos = Mathf.Cos(angleRad);
            float sin = Mathf.Sin(angleRad);
            return new Vector2(
                normalizedDirection.x * cos - normalizedDirection.y * sin,
                normalizedDirection.x * sin + normalizedDirection.y * cos).normalized;
        }

        /// <summary>
        /// 현재 정책에 맞춰 Raycast 방향을 계산합니다.
        /// - ByAngle이면 각도 기반 방향을 사용합니다.
        /// - TowardTarget이면 타겟 캐릭터 또는 좌표 오버라이드 방향에 런타임 각도 오프셋을 적용합니다.
        /// - 방향을 해석할 수 없으면 필요 시 시전자 기본 바라보기 방향으로 보정합니다.
        /// </summary>
        /// <param name="info">레이저 테이블 정보입니다.</param>
        /// <param name="runtime">런타임 메타데이터입니다.</param>
        /// <param name="owner">시전자입니다.</param>
        /// <param name="targetObject">고정 타겟 캐릭터입니다.</param>
        /// <param name="hasTargetPoint">좌표 타겟 사용 여부입니다.</param>
        /// <param name="targetPoint">좌표 타겟 값입니다.</param>
        /// <param name="start">시작점입니다.</param>
        /// <param name="allowFallbackToOwnerFacing">방향 해석 실패 시 시전자 기본 바라보기 방향으로 보정할지 여부입니다.</param>
        /// <returns>정규화된 Raycast 방향입니다.</returns>
        public static Vector2 ResolveRaycastDirection(
            StruckTableLaser info,
            MetadataLaser runtime,
            CharacterBase owner,
            CharacterBase targetObject,
            bool hasTargetPoint,
            Vector2 targetPoint,
            Vector2 start,
            bool allowFallbackToOwnerFacing = true)
        {
            if (ResolveRaycastDirectionMode(info, runtime) == LaserConstants.RaycastDirectionMode.ByAngle)
                return ResolveDirectionByConfiguredAngle(info, runtime, owner);

            Vector2 direction;
            if (targetObject != null)
            {
                direction = ResolveDirection(owner, start, targetObject.transform.position);
            }
            else if (hasTargetPoint)
            {
                direction = ResolveDirection(owner, start, targetPoint);
            }
            else
            {
                direction = allowFallbackToOwnerFacing ? ResolveOwnerFacingDirection(owner) : Vector2.zero;
            }

            float angleOffsetDeg = runtime != null ? runtime.TargetDirectionAngleOffsetDeg : 0f;
            return ApplyDirectionAngleOffset(direction, angleOffsetDeg);
        }

        /// <summary>
        /// Skill/Editor 프리뷰에서 사용할 시각 방향을 계산합니다.
        /// - FollowRaycast와 LockAtLaunch는 프리뷰 시점의 Raycast 방향을 그대로 사용합니다.
        /// - None은 실제 런타임처럼 회전을 강제하지 않으므로, 프리뷰에서는 기준 로컬 축(+X)을 가이드로 사용합니다.
        /// </summary>
        /// <param name="info">레이저 테이블 정보입니다.</param>
        /// <param name="runtime">런타임 메타데이터입니다.</param>
        /// <param name="raycastDirection">프리뷰 시점의 Raycast 방향입니다.</param>
        /// <returns>프리뷰에 표시할 시각 방향입니다.</returns>
        public static Vector2 ResolvePreviewVisualDirection(StruckTableLaser info, MetadataLaser runtime, Vector2 raycastDirection)
        {
            switch (ResolveVfxAngleSyncMode(info, runtime))
            {
                case LaserConstants.VfxAngleSyncMode.None:
                    return Vector2.right;

                case LaserConstants.VfxAngleSyncMode.FollowRaycast:
                case LaserConstants.VfxAngleSyncMode.LockAtLaunch:
                default:
                    return raycastDirection.sqrMagnitude > 1e-6f ? raycastDirection.normalized : Vector2.right;
            }
        }
    }
}
