using UnityEngine;

namespace GGemCo2DCore
{
    [DisallowMultipleComponent]
    public sealed class ParticleSystemVfxBehaviour : VfxBehaviourBase
    {
        private ParticleSystem[] _particleSystems;
        private bool _subscribed;

        protected override void Awake()
        {
            base.Awake();
            CacheParticleSystems();
        }

        public override void Initialize(VfxRuntimeData runtimeData, VfxSpawnPolicy spawnPolicy, System.Action<int, GameObject> releaseAction = null)
        {
            base.Initialize(runtimeData, spawnPolicy, releaseAction);
            CacheParticleSystems();
            ConfigureStopAction();
        }

        private void CacheParticleSystems()
        {
            _particleSystems = GetComponentsInChildren<ParticleSystem>(true);
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
            ApplyCommonVisuals();
            if (_particleSystems == null || _particleSystems.Length == 0)
            {
                OnEndAnimationComplete();
                return;
            }

            ConfigureStopAction();
            StartLifecycleTimerIfNeeded();

            for (int i = 0; i < _particleSystems.Length; i++)
            {
                if (_particleSystems[i] == null)
                    continue;
                _particleSystems[i].Clear(true);
                _particleSystems[i].Play(true);
            }
        }

        public override void PlayEndAnimation()
        {
            if (_particleSystems == null || _particleSystems.Length == 0)
            {
                OnEndAnimationComplete();
                return;
            }

            for (int i = 0; i < _particleSystems.Length; i++)
            {
                if (_particleSystems[i] == null)
                    continue;
                _particleSystems[i].Stop(true, ParticleSystemStopBehavior.StopEmitting);
            }
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
