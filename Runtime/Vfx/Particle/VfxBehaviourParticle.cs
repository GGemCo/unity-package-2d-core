using UnityEngine;

namespace GGemCo2DCore
{
    [DisallowMultipleComponent]
    public sealed class VfxBehaviourParticle : VfxBehaviourBase
    {
        private ParticleSystem[] _particleSystems;
        private bool[] _originalLoopStates;
        private bool _subscribed;
        private bool _forceOneShot;

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

        public override void SetForceOneShot(bool forceOneShot)
        {
            _forceOneShot = forceOneShot;
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
            if (_particleSystems == null || _particleSystems.Length == 0)
            {
                base.PlayEndAnimation();
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
