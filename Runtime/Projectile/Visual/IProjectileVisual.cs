using UnityEngine;

namespace GGemCo2DCore
{
    /// <summary>
    /// 프로젝타일 시각 표현 인터페이스.
    /// 로직(ProjectileBase)은 이 인터페이스만 알고, Effect/Sprite/Animator 구현체를 몰라야 한다.
    /// </summary>
    public interface IProjectileVisual
    {
        /// <summary>
        /// 발사체가 생성/초기화된 직후 호출된다.
        /// </summary>
        void OnSpawn(in ProjectileVisualSpawnContext context);

        /// <summary>
        /// 진행 중(이동/회전/플립 등) 매 프레임 필요한 갱신이 있으면 호출된다.
        /// </summary>
        void OnUpdate(in ProjectileVisualUpdateContext context);

        /// <summary>
        /// 타겟/지면 등에 히트했을 때 호출된다.
        /// </summary>
        void OnHit(in ProjectileVisualHitContext context);

        /// <summary>
        /// 발사체가 소멸되기 직전에 호출된다.
        /// </summary>
        void OnDespawn();
    }

    /// <summary>
    /// 레이저 계열(히트스캔)에서 시작/끝 좌표를 시각 표현에 전달하기 위한 인터페이스.
    /// </summary>
    public interface IProjectileLaserVisual
    {
        void SetEndpoints(Vector3 start, Vector3 end);
    }

    public readonly struct ProjectileVisualSpawnContext
    {
        public readonly Transform ProjectileTransform;
        public readonly StruckTableProjectile StaticData;
        public readonly MetadataProjectile RuntimeData;

        public ProjectileVisualSpawnContext(
            Transform projectileTransform,
            StruckTableProjectile staticData,
            MetadataProjectile runtimeData)
        {
            ProjectileTransform = projectileTransform;
            StaticData = staticData;
            RuntimeData = runtimeData;
        }
    }

    public readonly struct ProjectileVisualUpdateContext
    {
        public readonly Vector2 StartPoint;
        public readonly Vector2 TargetPoint;
        public readonly Vector2 CurrentPosition;
        public readonly Vector2 Delta;
        public readonly Vector2 Direction;

        public ProjectileVisualUpdateContext(
            Vector2 startPoint,
            Vector2 targetPoint,
            Vector2 currentPosition,
            Vector2 delta,
            Vector2 direction)
        {
            StartPoint = startPoint;
            TargetPoint = targetPoint;
            CurrentPosition = currentPosition;
            Delta = delta;
            Direction = direction;
        }
    }

    public readonly struct ProjectileVisualHitContext
    {
        public readonly Vector3 HitPosition;
        public readonly CharacterBase FromCharacter;
        public readonly Collider2D HitCollider;

        public ProjectileVisualHitContext(Vector3 hitPosition, CharacterBase fromCharacter, Collider2D hitCollider)
        {
            HitPosition = hitPosition;
            FromCharacter = fromCharacter;
            HitCollider = hitCollider;
        }
    }
}
