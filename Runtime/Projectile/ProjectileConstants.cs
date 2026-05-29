using UnityEngine;

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

        /// <summary>
        /// 프로젝타일이 타겟에게 적중했을 때 Hit VFX를 출력할 월드 좌표 계산 정책입니다.
        /// </summary>
        public enum HitVfxPositionPolicy
        {
            /// <summary>
            /// 실제 충돌 처리에서 전달된 좌표를 기준으로 Hit VFX를 출력합니다.
            /// 기존 발사체 동작을 유지하기 위해 0번 기본값으로 사용합니다.
            /// </summary>
            CollisionPoint = 0,

            /// <summary>
            /// 타겟 캐릭터 중심 좌표에 Hit VFX 오프셋을 더한 위치에 출력합니다.
            /// </summary>
            TargetOffset = 1,

            /// <summary>
            /// 발사체 현재 좌표에 Hit VFX 오프셋을 더한 위치에 출력합니다.
            /// </summary>
            ProjectilePosition = 2,

            /// <summary>
            /// 타겟 HitArea 내부의 정규화 좌표에 Hit VFX 오프셋을 더한 위치에 출력합니다.
            /// </summary>
            TargetHitAreaNormalized = 3,
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
        /// 프로젝타일이 경로의 종착 지점에 도달했을 때 처리 정책입니다.
        /// - DestroyOnArrived: 도착 즉시 End 연출 후 발사체를 제거합니다.
        /// - ContinueAfterArrived: 도착 제거를 무시하고 마지막 진행 방향으로 계속 이동합니다.
        /// </summary>
        public enum ArrivalPolicy
        {
            DestroyOnArrived = 0,
            ContinueAfterArrived = 1,
        }


        /// <summary>
        /// 프로젝타일이 캐릭터가 아닌 환경 Collider와 충돌했을 때의 처리 정책입니다.
        /// - Ignore: 환경 충돌을 별도로 처리하지 않습니다.
        /// - PlayHitVisualOnly: Hit VFX만 출력하고 발사체 이동/수명은 유지합니다.
        /// - PlayHitVisualAndFollowHitLifetime: Hit VFX를 출력하고 HitLifetimeMode가 DestroyOnTargetHit이면 제거합니다.
        /// - PlayHitVisualAndDestroy: HitLifetimeMode와 관계없이 Hit VFX 출력 후 즉시 제거합니다.
        /// </summary>
        public enum EnvironmentHitPolicy
        {
            Ignore = 0,
            PlayHitVisualOnly = 1,
            PlayHitVisualAndFollowHitLifetime = 2,
            PlayHitVisualAndDestroy = 3,
        }

        /// <summary>
        /// 기본 환경 히트 대상으로 사용할 TileMap Ground/Wall 레이어 마스크를 반환합니다.
        /// - LayerMask.GetMask는 레이어 이름 목록을 비트마스크로 변환하므로 레이어 번호를 직접 더하지 않습니다.
        /// - 프로젝트 설정에 해당 레이어 이름이 없으면 Unity가 0 비트를 반환하므로 안전하게 무시됩니다.
        /// </summary>
        /// <returns>GGemCo_TileMapGround, GGemCo_TileMapWall 레이어를 포함하는 비트마스크입니다.</returns>
        public static int GetDefaultEnvironmentHitLayerMask()
        {
            int mask = 0;

            string groundLayerName = ConfigLayer.GetValue(ConfigLayer.Keys.TileMapGround);
            if (!string.IsNullOrEmpty(groundLayerName))
                mask |= LayerMask.GetMask(groundLayerName);

            string wallLayerName = ConfigLayer.GetValue(ConfigLayer.Keys.TileMapWall);
            if (!string.IsNullOrEmpty(wallLayerName))
                mask |= LayerMask.GetMask(wallLayerName);

            return mask;
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
