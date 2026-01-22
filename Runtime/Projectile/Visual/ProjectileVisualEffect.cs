using UnityEngine;

namespace GGemCo2DCore
{
    /// <summary>
    /// Effect 시스템(DefaultEffect) 기반 프로젝타일 표현.
    /// ProjectileBase는 EffectManager/DefaultEffect에 의존하지 않으며,
    /// 이 구현체에서만 의존한다.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ProjectileVisualEffect : MonoBehaviour, IProjectileVisual, IProjectileLaserVisual
    {
        private DefaultEffect _effect;
        private bool _shouldFlip;
        private StruckTableProjectile _static;
        private MetadataProjectile _runtime;

        public void OnSpawn(in ProjectileVisualSpawnContext context)
        {
            _static = context.StaticData;
            _runtime = context.RuntimeData;

            int effectUid = ResolveEffectUid(_static, _runtime);
            if (effectUid <= 0)
                return;

            var effectManager = SceneGame.Instance != null ? SceneGame.Instance.EffectManager : null;
            if (effectManager == null)
                return;

            _effect = effectManager.CreateEffect(effectUid);
            if (_effect == null)
                return;

            _effect.transform.SetParent(transform);
            _effect.transform.localPosition = Vector3.zero;

            // 충돌 후 End 애니를 돌릴 수 있으므로 무한(-1) 지속
            _effect.SetDuration(-1);

            float scale = 1f;
            if (_static != null && _static.EffectScale > 0)
                scale = _static.EffectScale;

            if (_runtime != null)
                scale *= Mathf.Max(0.01f, _runtime.ScaleMultiplier);

            if (scale > 0f)
                _effect.SetScale(scale);
        }

        public void OnUpdate(in ProjectileVisualUpdateContext context)
        {
            if (_effect == null) return;

            // 좌우 Flip: 이동 방향 기준
            if (context.Direction.x < -0.001f)
                _shouldFlip = true;
            else if (context.Direction.x > 0.001f)
                _shouldFlip = false;

            _effect.SetFlip(_shouldFlip);
        }

        public void OnHit(in ProjectileVisualHitContext context)
        {
            if (SceneGame.Instance == null) return;

            // HitEffectUid가 있으면 별도 생성, 없으면 End 애니 재생
            if (_static != null && _static.HitEffectUid > 0)
            {
                var hit = SceneGame.Instance.EffectManager.CreateEffect(_static.HitEffectUid);
                if (hit == null) return;

                hit.SetCreateCharacter(context.FromCharacter);
                hit.transform.position = context.HitPosition;
                hit.SetFlip(_shouldFlip);
            }
            else
            {
                _effect?.PlayEndAnimation();
            }
        }

        public void OnDespawn()
        {
            // Effect가 별도 파괴 정책을 가지고 있다면 여기서 정리할 수 있다.
            // 기본적으로 Projectile GameObject가 Destroy되면 자식도 함께 정리된다.
        }

        public void SetEndpoints(Vector3 start, Vector3 end)
        {
            if (_effect is EffectLaser laser)
            {
                laser.SetEndpoints(start, end);
            }
        }

        private static int ResolveEffectUid(StruckTableProjectile staticData, MetadataProjectile runtimeData)
        {
            if (runtimeData != null && runtimeData.VisualEffectUidOverride > 0)
                return runtimeData.VisualEffectUidOverride;

            return staticData != null ? staticData.EffectUid : 0;
        }
    }
}
