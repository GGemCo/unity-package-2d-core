namespace GGemCo2DCore
{
    public static class ProjectileConstants
    {
        /// <summary>
        /// 프로젝타일 시각 표현 방식.
        /// - Default: 테이블 기본값을 따른다(VfxUid 있으면 Vfx, 없으면 None).
        /// - None: 시각 표현 없음(로직만 수행).
        /// - Vfx: Vfx 시스템(DefaultVfx) 기반 표현.
        /// - Sprite: 단일 SpriteRenderer 기반 표현.
        /// - Animator: Animator + SpriteRenderer 기반 스프라이트 애니메이션 표현.
        /// </summary>
        public enum ProjectileVisualType
        {
            Default = 0,
            None = 1,
            Vfx = 2,
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
            Laser,
            Linear,
            Arc,
            Path,
            LinearThenSegments,
        }

        /// <summary>
        /// 프로젝타일이 데미지를 적용하는 방식입니다.
        /// - OnHit: HitArea와 충돌할 때 데미지를 적용합니다. 완전히 이탈한 뒤 다시 진입하면 다시 데미지를 줄 수 있습니다.
        /// - PeriodicOverlap: 이동 중 일정 주기로 현재 콜라이더와 겹친 대상에게 데미지를 줍니다.
        /// - None: 데미지를 주지 않고 이동/연출만 수행합니다.
        /// </summary>
        public enum DamageApplyMode
        {
            OnHit = 0,
            PeriodicOverlap = 1,
            None = 2,
        }

        /// <summary>
        /// 프로젝타일이 타겟 또는 지형과 충돌했을 때의 생존 정책입니다.
        /// - DestroyOnTargetHit: 충돌 즉시 종료합니다.
        /// - KeepUntilRouteEnd: 충돌 여부와 상관없이 마지막 경로 지점까지 유지합니다.
        /// </summary>
        public enum HitLifetimeMode
        {
            DestroyOnTargetHit = 0,
            KeepUntilRouteEnd = 1,
        }

        /// <summary>
        /// Path 타입 프로젝타일의 경로 좌표 기준입니다.
        /// - StartRelative: 발사 시작 위치를 기준으로 PathPoints를 해석합니다.
        /// - TargetRelative: 발사 목표 위치를 기준으로 PathPoints를 해석합니다.
        /// - World: PathPoints를 월드 좌표로 그대로 사용합니다.
        /// </summary>
        public enum PathCoordinateMode
        {
            StartRelative = 0,
            TargetRelative = 1,
            World = 2,
        }

        /// <summary>
        /// 타겟 직선 이동 이후에 실행되는 세그먼트 방향의 해석 기준입니다.
        /// - World: 입력한 방향 벡터를 월드 좌표 방향으로 그대로 사용합니다.
        /// - InitialDirectionRelative: 입력한 방향을 최초 타겟 방향 기준의 로컬 방향으로 해석합니다.
        /// </summary>
        public enum SegmentDirectionMode
        {
            World = 0,
            InitialDirectionRelative = 1,
        }

        /// <summary>
        /// InitialDirectionRelative 모드에서 사용할 상대 축 해석 방식입니다.
        /// - Full2D: 최초 타겟 방향 전체를 기준으로 로컬 축을 회전시켜 해석합니다.
        /// - HorizontalMirror: 최초 타겟 방향의 좌/우만 반영하고, Y축은 월드 기준 그대로 유지합니다.
        /// </summary>
        public enum SegmentRelativeAxesMode
        {
            Full2D = 0,
            HorizontalMirror = 1,
        }

        public enum BoundaryMode
        {
            Destroy = 0,
            Bounce = 1,
        }

    }
}
