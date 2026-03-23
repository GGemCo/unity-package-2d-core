using System;
using UnityEngine;

namespace GGemCo2DCore
{
    /// <summary>
    /// Vfx 시스템(DefaultVfx) 기반 프로젝타일 표현.
    /// ProjectileBase는 VfxManager/DefaultVfx에 의존하지 않으며,
    /// 이 구현체에서만 의존한다.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ProjectileVisualVfx : MonoBehaviour, IProjectileVisual, IProjectileLaserVisual
    {
        private VfxBehaviourBase _vfx;
        private bool _shouldFlip;
        private StruckTableProjectile _static;
        private MetadataProjectile _runtime;

        public void OnSpawn(in ProjectileVisualSpawnContext context)
        {
            _static = context.StaticData;
            _runtime = context.RuntimeData;

            int vfxUid = ResolveVfxUid(_static, _runtime);
            if (vfxUid <= 0)
                return;

            var vfxManager = SceneGame.Instance != null ? SceneGame.Instance.VfxManager : null;
            if (vfxManager == null)
                return;

            // 충돌 후 End 애니를 돌릴 수 있으므로 무한(-1) 지속
            _vfx = vfxManager.CreateVfx(vfxUid, -1f);
            if (_vfx == null)
                return;

            _vfx.transform.SetParent(transform);
            _vfx.transform.localPosition = Vector3.zero;

            float scale = 1f;
            if (_static != null && _static.VfxScale > 0)
                scale = _static.VfxScale;

            if (_runtime != null)
                scale *= Mathf.Max(0.01f, _runtime.ScaleMultiplier);

            if (scale > 0f)
                _vfx.SetScale(scale);
        }

        public void OnUpdate(in ProjectileVisualUpdateContext context)
        {
            if (_vfx == null) return;
        }

        public void OnHit(in ProjectileVisualHitContext context)
        {
            if (SceneGame.Instance == null) return;

            // HitVfxUid가 있으면 별도 생성, 없으면 End 애니 재생
            if (_static != null && _static.HitVfxUid > 0)
            {
                var hit = SceneGame.Instance.VfxManager.CreateVfx(_static.HitVfxUid);
                if (hit == null) return;

                hit.SetCreateCharacter(context.FromCharacter);
                hit.transform.position = context.HitPosition;
            }
            else
            {
                _vfx?.PlayEndAnimation();
            }
        }

        public void OnDespawn()
        {
            // Vfx가 별도 파괴 정책을 가지고 있다면 여기서 정리할 수 있다.
            // 기본적으로 Projectile GameObject가 Destroy되면 자식도 함께 정리된다.
        }

        public bool TryPlayEnd(Action onComplete)
        {
            if (_vfx == null)
                return false;

            void HandleVfxDestroy()
            {
                if (_vfx != null)
                    _vfx.OnVfxDestroy -= HandleVfxDestroy;

                onComplete?.Invoke();
            }

            bool started = _vfx.TryPlayEndAnimation(HandleVfxDestroy);
            if (!started && _vfx != null)
                _vfx.OnVfxDestroy -= HandleVfxDestroy;

            return started;
        }

        public void SetEndpoints(Vector3 start, Vector3 end)
        {
            if (_vfx is VfxEffectLaser laser)
            {
                laser.SetEndpoints(start, end);
            }
        }

        private static int ResolveVfxUid(StruckTableProjectile staticData, MetadataProjectile runtimeData)
        {
            if (runtimeData != null && runtimeData.VisualVfxUidOverride > 0)
                return runtimeData.VisualVfxUidOverride;

            return staticData != null ? staticData.VfxUid : 0;
        }
    }
}
