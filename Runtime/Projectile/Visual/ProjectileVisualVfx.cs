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
        private VfxBehaviourBase.DelegateEffectDestroy _handleVfxDestroy;
        private Action _endCompleteCallback;
        private bool _shouldFlip;
        private bool _isEndAnimationPlaying;
        private StruckTableProjectile _static;
        private MetadataProjectile _runtime;

        /// <summary>
        /// Projectile 생성 시 테이블/런타임 데이터에 맞는 VFX를 생성하고 Projectile 하위로 연결합니다.
        /// </summary>
        /// <param name="context">Projectile Visual 생성에 필요한 정적/런타임 컨텍스트입니다.</param>
        public void OnSpawn(in ProjectileVisualSpawnContext context)
        {
            ClearEndDestroyHandler();
            _vfx = null;
            _isEndAnimationPlaying = false;
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

            _vfx.transform.SetParent(transform, false);
            _vfx.transform.localPosition = Vector3.zero;

            float scale = 1f;
            if (_static != null && _static.VfxScale > 0)
                scale = _static.VfxScale;

            if (_runtime != null)
                scale *= Mathf.Max(0.01f, _runtime.ScaleMultiplier);

            if (scale > 0f)
                _vfx.SetScale(scale);
        }

        /// <summary>
        /// Projectile 이동 중 Visual 갱신을 처리합니다.
        /// </summary>
        /// <param name="context">현재 Projectile 위치/이동 정보를 담은 컨텍스트입니다.</param>
        public void OnUpdate(in ProjectileVisualUpdateContext context)
        {
            if (_vfx == null) return;
        }

        /// <summary>
        /// Projectile 충돌 시 Hit VFX만 생성합니다.
        /// - attached VFX의 수명은 충돌 시점이 아니라 부모 Projectile의 실제 종료 시점을 따릅니다.
        /// - 따라서 HitVfxUid가 없더라도 여기서 attached VFX 종료 애니메이션을 시작하지 않습니다.
        /// </summary>
        /// <param name="context">충돌 위치와 시전자 정보를 담은 컨텍스트입니다.</param>
        public void OnHit(in ProjectileVisualHitContext context)
        {
            if (SceneGame.Instance == null)
                return;

            if (_static == null || _static.HitVfxUid <= 0)
                return;

            SpawnHitVfx(context);
        }

        /// <summary>
        /// Projectile이 먼저 제거될 때 자식으로 붙어 있던 VFX를 분리하고 풀 반환 경로로 넘깁니다.
        /// </summary>
        /// <remarks>
        /// Projectile GameObject가 Destroy되면 자식 VFX도 함께 파괴될 수 있으므로,
        /// 풀에서 재사용해야 하는 VFX는 Projectile 제거 전에 부모를 끊어 독립적으로 정리합니다.
        /// </remarks>
        public void OnDespawn()
        {
            ReleaseVfxOnProjectileDespawn();
        }

        /// <summary>
        /// Projectile 도착 시 VFX 종료 애니메이션을 재생하고 완료 콜백을 연결합니다.
        /// </summary>
        /// <param name="onComplete">VFX 종료 후 Projectile을 제거하기 위한 콜백입니다.</param>
        /// <returns>종료 애니메이션 재생을 시작했으면 true를 반환합니다.</returns>
        public bool TryPlayEnd(Action onComplete)
        {
            if (_vfx == null)
                return false;

            var targetVfx = _vfx;
            ClearEndDestroyHandler(targetVfx);

            _endCompleteCallback = onComplete;
            _handleVfxDestroy = HandleVfxDestroy;

            bool started = targetVfx.TryPlayEndAnimation(_handleVfxDestroy);
            if (!started)
            {
                ClearEndDestroyHandler(targetVfx);
                return false;
            }

            if (_vfx == targetVfx)
                _isEndAnimationPlaying = true;

            return true;
        }

        /// <summary>
        /// Laser VFX인 경우 시작점과 끝점을 갱신합니다.
        /// </summary>
        /// <param name="start">레이저 시작 월드 좌표입니다.</param>
        /// <param name="end">레이저 끝 월드 좌표입니다.</param>
        public void SetEndpoints(Vector3 start, Vector3 end)
        {
            if (_vfx is VfxEffectLaser laser)
            {
                laser.SetEndpoints(start, end);
            }
        }

        /// <summary>
        /// 충돌 지점에 독립적인 Hit VFX를 생성합니다.
        /// - attached VFX와 분리된 1회성 연출이므로 부모 Projectile 수명과 무관하게 재생됩니다.
        /// </summary>
        /// <param name="context">충돌 위치와 시전자 정보를 담은 컨텍스트입니다.</param>
        private void SpawnHitVfx(in ProjectileVisualHitContext context)
        {
            var vfxManager = SceneGame.Instance != null ? SceneGame.Instance.VfxManager : null;
            if (vfxManager == null || _static == null || _static.HitVfxUid <= 0)
                return;

            var hit = vfxManager.CreateVfx(_static.HitVfxUid);
            if (hit == null)
                return;

            hit.SetCreateCharacter(context.FromCharacter);
            hit.transform.position = context.HitPosition;
        }

        /// <summary>
        /// Projectile 제거 시 VFX가 함께 Destroy되지 않도록 분리하고 필요한 경우 풀 반환을 요청합니다.
        /// </summary>
        private void ReleaseVfxOnProjectileDespawn()
        {
            if (_vfx == null)
                return;

            var targetVfx = _vfx;
            bool wasEndAnimationPlaying = _isEndAnimationPlaying;

            ClearEndDestroyHandler(targetVfx);
            _vfx = null;
            _isEndAnimationPlaying = false;

            if (targetVfx == null || targetVfx.gameObject == null)
                return;

            bool wasOwnedByProjectile = IsVfxOwnedByProjectile(targetVfx);
            DetachVfxIfOwned(targetVfx);

            if (wasEndAnimationPlaying)
                return;

            if (wasOwnedByProjectile || targetVfx.gameObject.activeSelf || targetVfx.gameObject.activeInHierarchy)
                targetVfx.DestroyForce();
        }

        /// <summary>
        /// VFX가 현재 Projectile 하위에 연결되어 있는지 확인합니다.
        /// </summary>
        /// <param name="targetVfx">소유 여부를 확인할 VFX입니다.</param>
        /// <returns>Projectile 하위에 있으면 true를 반환합니다.</returns>
        private bool IsVfxOwnedByProjectile(VfxBehaviourBase targetVfx)
        {
            if (targetVfx == null || targetVfx.transform == null || transform == null)
                return false;

            return targetVfx.transform.IsChildOf(transform);
        }

        /// <summary>
        /// Projectile 하위에 붙어 있는 VFX만 부모 관계를 끊어 독립적인 정리 루틴을 실행할 수 있게 합니다.
        /// </summary>
        /// <param name="targetVfx">분리할 VFX입니다.</param>
        private void DetachVfxIfOwned(VfxBehaviourBase targetVfx)
        {
            if (IsVfxOwnedByProjectile(targetVfx))
                targetVfx.transform.SetParent(null, true);
        }

        /// <summary>
        /// VFX 종료 이벤트가 호출되었을 때 Projectile 종료 콜백을 한 번만 실행합니다.
        /// </summary>
        private void HandleVfxDestroy()
        {
            Action callback = _endCompleteCallback;
            ClearEndDestroyHandler();
            _vfx = null;
            _isEndAnimationPlaying = false;

            callback?.Invoke();
        }

        /// <summary>
        /// 현재 연결된 VFX 종료 이벤트 구독을 해제합니다.
        /// </summary>
        /// <param name="eventSource">명시적으로 구독을 해제할 VFX입니다. 없으면 현재 VFX를 사용합니다.</param>
        private void ClearEndDestroyHandler(VfxBehaviourBase eventSource = null)
        {
            var source = eventSource != null ? eventSource : _vfx;
            if (source != null && _handleVfxDestroy != null)
                source.OnVfxDestroy -= _handleVfxDestroy;

            _handleVfxDestroy = null;
            _endCompleteCallback = null;
        }

        /// <summary>
        /// Projectile 데이터에서 사용할 VFX Uid를 결정합니다.
        /// </summary>
        /// <param name="staticData">Projectile 테이블 데이터입니다.</param>
        /// <param name="runtimeData">런타임에서 덮어쓴 Projectile 메타데이터입니다.</param>
        /// <returns>런타임 덮어쓰기 값이 있으면 해당 Uid, 아니면 테이블 VFX Uid를 반환합니다.</returns>
        private static int ResolveVfxUid(StruckTableProjectile staticData, MetadataProjectile runtimeData)
        {
            if (runtimeData != null && runtimeData.VisualVfxUidOverride > 0)
                return runtimeData.VisualVfxUidOverride;

            return staticData != null ? staticData.VfxUid : 0;
        }
    }
}
