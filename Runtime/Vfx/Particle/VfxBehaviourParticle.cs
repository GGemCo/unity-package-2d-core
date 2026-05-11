using UnityEngine;

namespace GGemCo2DCore
{
    [DisallowMultipleComponent]
    public sealed class VfxBehaviourParticle : VfxBehaviourBase
    {
        private ParticleSystem[] _particleSystems;
        private bool[] _originalLoopStates;
        private Renderer[] _renderers;
        private int[] _defaultSortingLayerIds;
        private int[] _defaultSortingOrders;
        private bool _subscribed;
        private bool _forceOneShot;

        protected override bool UseTimelineFadeOutAlpha => false;

        protected override void Awake()
        {
            base.Awake();
            CacheParticleSystems();
        }

        public override void Initialize(VfxRuntimeData runtimeData, VfxSpawnPolicy spawnPolicy, System.Action<int, GameObject> releaseAction = null)
        {
            base.Initialize(runtimeData, spawnPolicy, releaseAction);
            CacheParticleSystems();
            CacheRenderers();
            RestoreDefaultSorting();
            ConfigureStopAction();
        }

        private void CacheParticleSystems()
        {
            _particleSystems = GetComponentsInChildren<ParticleSystem>(true);
            if (_particleSystems == null)
            {
                _originalLoopStates = null;
                return;
            }

            if (_originalLoopStates == null || _originalLoopStates.Length != _particleSystems.Length)
            {
                _originalLoopStates = new bool[_particleSystems.Length];
                for (int i = 0; i < _particleSystems.Length; i++)
                {
                    var ps = _particleSystems[i];
                    _originalLoopStates[i] = ps != null && ps.main.loop;
                }
            }
        }

        /// <summary>
        /// 파티클 VFX 하위의 렌더러 목록과 최초 정렬값을 캐시합니다.
        /// </summary>
        private void CacheRenderers()
        {
            _renderers = GetComponentsInChildren<Renderer>(true);
            if (_renderers == null || _renderers.Length == 0)
            {
                _defaultSortingLayerIds = null;
                _defaultSortingOrders = null;
                return;
            }

            if (_defaultSortingLayerIds != null &&
                _defaultSortingOrders != null &&
                _defaultSortingLayerIds.Length == _renderers.Length &&
                _defaultSortingOrders.Length == _renderers.Length)
            {
                return;
            }

            _defaultSortingLayerIds = new int[_renderers.Length];
            _defaultSortingOrders = new int[_renderers.Length];

            for (int i = 0; i < _renderers.Length; i++)
            {
                var renderer = _renderers[i];
                if (renderer == null)
                    continue;

                _defaultSortingLayerIds[i] = renderer.sortingLayerID;
                _defaultSortingOrders[i] = renderer.sortingOrder;
            }
        }

        /// <summary>
        /// 풀에서 재사용되는 파티클 VFX의 정렬값을 프리팹 기준값으로 되돌립니다.
        /// </summary>
        private void RestoreDefaultSorting()
        {
            if (_renderers == null || _defaultSortingLayerIds == null || _defaultSortingOrders == null)
                return;

            for (int i = 0; i < _renderers.Length; i++)
            {
                var renderer = _renderers[i];
                if (renderer == null)
                    continue;

                if (i < _defaultSortingLayerIds.Length)
                    renderer.sortingLayerID = _defaultSortingLayerIds[i];

                if (i < _defaultSortingOrders.Length)
                    renderer.sortingOrder = _defaultSortingOrders[i];
            }
        }

        public override void SetForceOneShot(bool forceOneShot)
        {
            _forceOneShot = forceOneShot;
        }

        /// <summary>
        /// 하위 ParticleSystemRenderer와 일반 Renderer에 동일한 Sorting Layer를 적용합니다.
        /// </summary>
        /// <param name="sortingLayer">적용할 Sorting Layer 키입니다.</param>
        public override void SetSortingLayer(ConfigSortingLayer.Keys sortingLayer)
        {
            CacheRenderers();
            if (_renderers == null)
                return;

            string sortingLayerName = ConfigSortingLayer.GetValue(sortingLayer);
            for (int i = 0; i < _renderers.Length; i++)
            {
                if (_renderers[i] == null)
                    continue;

                _renderers[i].sortingLayerName = sortingLayerName;
            }
        }

        /// <summary>
        /// 하위 ParticleSystemRenderer와 일반 Renderer에 동일한 Sorting Order를 적용합니다.
        /// </summary>
        /// <param name="sortingOrder">적용할 Sorting Order 값입니다.</param>
        public override void SetSortingOrder(int sortingOrder)
        {
            CacheRenderers();
            if (_renderers == null)
                return;

            for (int i = 0; i < _renderers.Length; i++)
            {
                if (_renderers[i] == null)
                    continue;

                _renderers[i].sortingOrder = sortingOrder;
            }
        }

        private void ApplyLoopConfiguration()
        {
            if (_particleSystems == null || _originalLoopStates == null)
                return;

            for (int i = 0; i < _particleSystems.Length; i++)
            {
                var ps = _particleSystems[i];
                if (ps == null)
                    continue;

                var main = ps.main;
                bool originalLoop = i < _originalLoopStates.Length && _originalLoopStates[i];
                main.loop = _forceOneShot ? false : originalLoop;
            }
        }

        private void ConfigureStopAction()
        {
            if (_particleSystems == null)
                return;

            for (int i = 0; i < _particleSystems.Length; i++)
            {
                var ps = _particleSystems[i];
                if (ps == null)
                    continue;

                var main = ps.main;
                main.stopAction = ParticleSystemStopAction.Callback;
                main.useUnscaledTime = UseUnscaledTime();
            }
            _subscribed = true;
        }

        protected override void PlayOnSpawn()
        {
            base.PlayOnSpawn();

            if (_particleSystems == null || _particleSystems.Length == 0)
            {
                OnEndAnimationComplete();
                return;
            }

            ConfigureStopAction();
            ApplyLoopConfiguration();

            for (int i = 0; i < _particleSystems.Length; i++)
            {
                if (_particleSystems[i] == null)
                    continue;
                _particleSystems[i].Clear(true);
                _particleSystems[i].Play(true);
            }
        }

        public override bool TryPlayEndAnimation(DelegateEffectDestroy onEffectDestroy = null)
        {
            if (_particleSystems == null || _particleSystems.Length == 0)
                return false;

            if (onEffectDestroy != null)
                OnVfxDestroy += onEffectDestroy;

            PlayEndAnimation();
            return true;
        }

        public override void PlayEndAnimation()
        {
            BeginReleaseOnAnimationComplete();

            if (_particleSystems == null || _particleSystems.Length == 0)
            {
                BeginReleaseSequence();
                return;
            }

            for (int i = 0; i < _particleSystems.Length; i++)
            {
                if (_particleSystems[i] == null)
                    continue;
                _particleSystems[i].Stop(true, ParticleSystemStopBehavior.StopEmitting);
            }
        }

        protected override void OnTimelineDurationElapsed()
        {
            PlayEndAnimation();
        }

        private void OnParticleSystemStopped()
        {
            if (!_subscribed || _particleSystems == null || _particleSystems.Length == 0)
                return;

            for (int i = 0; i < _particleSystems.Length; i++)
            {
                var ps = _particleSystems[i];
                if (ps != null && ps.IsAlive(true))
                    return;
            }

            OnEndAnimationComplete();
        }
    }
}
