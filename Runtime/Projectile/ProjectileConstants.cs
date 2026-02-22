namespace GGemCo2DCore
{
    public static class ProjectileConstants
    {
        /// <summary>
        /// 프로젝타일 시각 표현 방식.
        /// - Default: 테이블 기본값을 따른다(EffectUid 있으면 Effect, 없으면 None).
        /// - None: 시각 표현 없음(로직만 수행).
        /// - Effect: Effect 시스템(DefaultEffect) 기반 표현.
        /// - Sprite: 단일 SpriteRenderer 기반 표현.
        /// - Animator: Animator + SpriteRenderer 기반 스프라이트 애니메이션 표현.
        /// </summary>
        public enum ProjectileVisualType
        {
            Default = 0,
            None = 1,
            Effect = 2,
            Sprite = 3,
            Animator = 4,
        }
        public enum TargetType
        {
            None,
            Fixed,
            Area,
            Position
        }

        public enum Type
        {
            None,
            Default,
            Laser
        }

        public enum BoundaryMode
        {
            Destroy = 0,
            Bounce = 1,
        }

    }
}