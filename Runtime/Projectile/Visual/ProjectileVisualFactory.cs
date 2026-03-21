using UnityEngine;

namespace GGemCo2DCore
{
    /// <summary>
    /// 테이블/런타임 메타데이터를 기준으로 적절한 Visual 컴포넌트를 생성합니다.
    /// ProjectileBase는 이 팩토리만 통해 시각 표현을 연결합니다.
    /// </summary>
    public static class ProjectileVisualFactory
    {
        public static IProjectileVisual Attach(Transform projectileTransform, StruckTableProjectile staticData, MetadataProjectile runtimeData)
        {
            var resolved = ResolveVisualType(staticData, runtimeData);

            switch (resolved)
            {
                case ProjectileConstants.ProjectileVisualType.None:
                    return projectileTransform.gameObject.AddComponent<ProjectileVisualNone>();

                case ProjectileConstants.ProjectileVisualType.Sprite:
                    return projectileTransform.gameObject.AddComponent<ProjectileVisualSprite>();

                case ProjectileConstants.ProjectileVisualType.Animator:
                    return projectileTransform.gameObject.AddComponent<ProjectileVisualAnimator>();

                case ProjectileConstants.ProjectileVisualType.Vfx:
                    return projectileTransform.gameObject.AddComponent<ProjectileVisualVfx>();

                default:
                    // Safety fallback
                    return projectileTransform.gameObject.AddComponent<ProjectileVisualNone>();
            }
        }

        private static ProjectileConstants.ProjectileVisualType ResolveVisualType(StruckTableProjectile staticData, MetadataProjectile runtimeData)
        {
            if (runtimeData == null) return ProjectileConstants.ProjectileVisualType.None;

            if (runtimeData.VisualType != ProjectileConstants.ProjectileVisualType.Default)
                return runtimeData.VisualType;

            // Default: 테이블에 EffectUid가 있으면 Effect, 아니면 None.
            return staticData != null && staticData.VfxUid > 0
                ? ProjectileConstants.ProjectileVisualType.Vfx
                : ProjectileConstants.ProjectileVisualType.None;
        }
    }
}