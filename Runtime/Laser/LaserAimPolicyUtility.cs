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
        /// 레이저 시작점과 타겟의 X 위치를 비교하여 좌우 수평 기준 방향을 계산하고 각도를 적용합니다.
        /// 좌우 어느 쪽을 향하더라도 양수 각도는 위쪽, 음수 각도는 아래쪽으로 대칭 적용됩니다.
        /// </summary>
        /// <param name="owner">타겟과 시작점의 X 위치가 같을 때 바라보기 방향을 제공할 시전자입니다.</param>
        /// <param name="start">레이저의 실제 시작점입니다.</param>
        /// <param name="targetPosition">레이저가 사용하는 타겟 위치입니다.</param>
        /// <param name="angleDeg">좌우 수평 방향을 기준으로 적용할 각도(도)입니다.</param>
        /// <returns>좌우 대칭 각도가 적용된 정규화 방향입니다.</returns>
        public static Vector2 ResolveHorizontalTargetDirection(
            CharacterBase owner,
            Vector2 start,
            Vector2 targetPosition,
            float angleDeg)
        {
            float horizontalDelta = targetPosition.x - start.x;
            float horizontalSign;
            if (Mathf.Abs(horizontalDelta) <= 1e-6f)
            {
                // 타겟과 시작점의 X 위치가 같으면 작은 좌표 오차로 방향이 흔들리지 않도록 시전자 방향을 사용합니다.
                horizontalSign = ResolveOwnerFacingDirection(owner).x;
            }
            else
            {
                horizontalSign = horizontalDelta > 0f ? 1f : -1f;
            }

            float angleRad = angleDeg * Mathf.Deg2Rad;
            float cos = Mathf.Cos(angleRad);
            float sin = Mathf.Sin(angleRad);
            Vector2 direction = new Vector2(cos * horizontalSign, sin);
            return direction.sqrMagnitude > 1e-6f
                ? direction.normalized
                : new Vector2(horizontalSign, 0f);
        }

        /// <summary>
        /// 현재 정책에 맞춰 Raycast 방향을 계산합니다.
        /// - ByAngle이면 각도 기반 방향을 사용합니다.
        /// - TowardTargetHorizontal이면 타겟의 좌우만 판정하고 수평 기준 각도를 대칭 적용합니다.
        /// - TowardTarget이면 타겟 캐릭터 또는 좌표 오버라이드 방향을 그대로 사용합니다.
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
            LaserConstants.RaycastDirectionMode directionMode = ResolveRaycastDirectionMode(info, runtime);
            if (directionMode == LaserConstants.RaycastDirectionMode.ByAngle)
            {
                return ResolveDirectionByConfiguredAngle(info, runtime, owner);
            }

            Vector2 resolvedTargetPosition;
            bool hasResolvedTarget;
            if (targetObject != null)
            {
                resolvedTargetPosition = targetObject.transform.position;
                hasResolvedTarget = true;
            }
            else
            {
                resolvedTargetPosition = targetPoint;
                hasResolvedTarget = hasTargetPoint;
            }

            if (directionMode == LaserConstants.RaycastDirectionMode.TowardTargetHorizontal)
            {
                if (hasResolvedTarget)
                {
                    float angleDeg = ResolveRaycastAngleDeg(info, runtime);
                    return ResolveHorizontalTargetDirection(owner, start, resolvedTargetPosition, angleDeg);
                }

                if (!allowFallbackToOwnerFacing)
                    return Vector2.zero;

                // 타겟이 없으면 시전자 방향과 동일한 X 위치의 가상 타겟을 사용하여 동일한 각도 규칙을 적용합니다.
                Vector2 fallbackDirection = ResolveOwnerFacingDirection(owner);
                Vector2 fallbackTargetPosition = start + fallbackDirection;
                float fallbackAngleDeg = ResolveRaycastAngleDeg(info, runtime);
                return ResolveHorizontalTargetDirection(owner, start, fallbackTargetPosition, fallbackAngleDeg);
            }

            if (hasResolvedTarget)
            {
                return ResolveDirection(owner, start, resolvedTargetPosition);
            }

            return allowFallbackToOwnerFacing ? ResolveOwnerFacingDirection(owner) : Vector2.zero;
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
