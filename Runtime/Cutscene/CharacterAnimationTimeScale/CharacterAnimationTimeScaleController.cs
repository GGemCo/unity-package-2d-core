using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GGemCo2DCore
{
    /// <summary>
    /// 특정 캐릭터의 애니메이션 playback time scale을 컷씬 타임라인으로 제어합니다.
    /// 0이면 현재 포즈를 유지한 채 정지한 것처럼 보이게 합니다.
    /// </summary>
    public sealed class CharacterAnimationTimeScaleController : CutsceneDefaultController, ICutsceneController
    {
        private CharacterAnimationTimeScaleData _data;
        private ICharacterAnimationController _animationController;
        private CharacterKey _currentKey;
        private bool _hasCurrentKey;
        private float _elapsed;
        private float _duration;
        private float _startScale;
        private float _targetScale;
        private bool _isPlaying;

        public CharacterAnimationTimeScaleController(CutsceneManager manager)
        {
            CutsceneManager = manager;
        }

        public IEnumerator Ready(CutsceneEvent evt)
        {
            if (evt.type != CutsceneEventType.CharacterAnimationTimeScale)
            {
                yield break;
            }

            yield return null;
        }

        public void Trigger(CutsceneEvent evt)
        {
            if (evt.type != CutsceneEventType.CharacterAnimationTimeScale)
            {
                return;
            }

            _data = evt.characterAnimationTimeScale ?? new CharacterAnimationTimeScaleData();
            _animationController = ResolveAnimationController(_data, out _currentKey);
            _hasCurrentKey = _animationController != null;
            if (_animationController == null)
            {
                return;
            }

            CaptureOriginalScaleIfNeeded(_currentKey, _animationController, _data);

            _elapsed = 0f;
            _duration = Mathf.Max(0f, evt.duration);
            _isPlaying = false;

            switch (_data.actionMode)
            {
                case CharacterAnimationTimeScaleActionMode.BlendAndHold:
                    _startScale = ResolveBlendStartScale(_currentKey, _animationController, _data);
                    _targetScale = Mathf.Max(0f, _data.toScale);

                    if (_duration <= 0f)
                    {
                        _animationController.SetPlaybackTimeScale(_targetScale);
                        return;
                    }

                    _animationController.SetPlaybackTimeScale(_startScale);
                    _isPlaying = true;
                    break;

                case CharacterAnimationTimeScaleActionMode.SetAndHold:
                    _animationController.SetPlaybackTimeScale(Mathf.Max(0f, _data.toScale));
                    break;

                case CharacterAnimationTimeScaleActionMode.Restore:
                    _startScale = _animationController.GetPlaybackTimeScale();
                    _targetScale = ResolveRestoreScale(_currentKey, _animationController, _data);

                    if (_duration <= 0f)
                    {
                        _animationController.SetPlaybackTimeScale(_targetScale);
                        return;
                    }

                    _isPlaying = true;
                    break;
            }
        }

        public void Update()
        {
            if (!_isPlaying || _animationController == null || _data == null)
            {
                return;
            }

            _elapsed += _data.useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
            float t = Mathf.Clamp01(_elapsed / Mathf.Max(0.0001f, _duration));
            float eased = Mathf.Clamp01(Easing.Apply(t, _data.easing));
            float value = Mathf.Lerp(_startScale, _targetScale, eased);
            _animationController.SetPlaybackTimeScale(value);

            if (_elapsed >= _duration)
            {
                Stop();
            }
        }

        public void Stop()
        {
            _isPlaying = false;
            if (_animationController == null)
            {
                return;
            }

            switch (_data?.actionMode)
            {
                case CharacterAnimationTimeScaleActionMode.BlendAndHold:
                case CharacterAnimationTimeScaleActionMode.Restore:
                    _animationController.SetPlaybackTimeScale(_targetScale);
                    break;
            }
        }

        public void End()
        {
            _isPlaying = false;
            if (_data == null || !_data.restoreOnCutsceneEnd)
            {
                return;
            }

            ForceRestoreOriginalState();
        }

        public void ForceRestoreOriginalState()
        {
            if (_hasCurrentKey && _animationController != null && CutsceneManager.TryGetCapturedCharacterAnimationTimeScale(_currentKey.CharacterType, _currentKey.CharacterUid, out float capturedScale))
            {
                _animationController.SetPlaybackTimeScale(capturedScale);
            }
        }

        private static float ResolveBlendStartScale(CharacterKey key, ICharacterAnimationController controller, CharacterAnimationTimeScaleData data)
        {
            if (data.captureOriginalOnTrigger)
            {
                return controller.GetPlaybackTimeScale();
            }

            return Mathf.Max(0f, data.fromScale);
        }

        private float ResolveRestoreScale(CharacterKey key, ICharacterAnimationController controller, CharacterAnimationTimeScaleData data)
        {
            if (data.useCapturedScaleForRestore && CutsceneManager.TryGetCapturedCharacterAnimationTimeScale(key.CharacterType, key.CharacterUid, out float capturedScale))
            {
                return capturedScale;
            }

            return Mathf.Max(0f, data.restoreScale);
        }

        private void CaptureOriginalScaleIfNeeded(CharacterKey key, ICharacterAnimationController controller, CharacterAnimationTimeScaleData data)
        {
            if (!data.captureOriginalOnTrigger)
            {
                return;
            }

            if (CutsceneManager.TryGetCapturedCharacterAnimationTimeScale(key.CharacterType, key.CharacterUid, out _))
            {
                return;
            }

            CutsceneManager.CaptureCharacterAnimationTimeScale(key.CharacterType, key.CharacterUid, controller.GetPlaybackTimeScale());
        }

        private ICharacterAnimationController ResolveAnimationController(CharacterAnimationTimeScaleData data, out CharacterKey key)
        {
            key = new CharacterKey(data.characterType, data.characterUid);
            return ResolveAnimationController(key);
        }

        private ICharacterAnimationController ResolveAnimationController(CharacterKey key)
        {
            Transform target = GetTargetTransform(key.CharacterType, key.CharacterUid);
            if (target == null)
            {
                target = CutsceneManager.GetCharacter(key.CharacterType, key.CharacterUid);
            }

            if (target == null)
            {
                GcLogger.LogError($"CharacterAnimationTimeScale target을 찾을 수 없습니다. type: {key.CharacterType}, uid: {key.CharacterUid}");
                return null;
            }

            CharacterBase characterBase = target.GetComponent<CharacterBase>();
            if (characterBase == null)
            {
                GcLogger.LogError($"CharacterAnimationTimeScale target에 CharacterBase가 없습니다. type: {key.CharacterType}, uid: {key.CharacterUid}");
                return null;
            }

            ICharacterAnimationController animationController = characterBase.CharacterAnimationController;
            if (animationController == null)
            {
                GcLogger.LogError($"CharacterAnimationTimeScale target에 ICharacterAnimationController가 없습니다. type: {key.CharacterType}, uid: {key.CharacterUid}");
                return null;
            }

            return animationController;
        }

        private readonly struct CharacterKey
        {
            public readonly CharacterConstants.Type CharacterType;
            public readonly int CharacterUid;

            public CharacterKey(CharacterConstants.Type characterType, int characterUid)
            {
                CharacterType = characterType;
                CharacterUid = characterUid;
            }
        }
    }
}
