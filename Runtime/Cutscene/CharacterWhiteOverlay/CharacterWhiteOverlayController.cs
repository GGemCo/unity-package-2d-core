using System.Collections;
using UnityEngine;

namespace GGemCo2DCore
{
    /// <summary>
    /// 특정 캐릭터의 Sprite White Overlay 강도를 컷신 타임라인으로 제어합니다.
    /// </summary>
    public sealed class CharacterWhiteOverlayController : CutsceneDefaultController, ICutsceneController
    {
        private CharacterWhiteOverlayData _data;
        private SpriteWhiteOverlayController _controller;
        private float _elapsed;
        private float _duration;
        private bool _isPlaying;

        public CharacterWhiteOverlayController(CutsceneManager manager)
        {
            CutsceneManager = manager;
        }

        public IEnumerator Ready(CutsceneEvent evt)
        {
            if (evt.type != CutsceneEventType.CharacterWhiteOverlay)
            {
                yield break;
            }

            yield return null;
        }

        public void Trigger(CutsceneEvent evt)
        {
            if (evt.type != CutsceneEventType.CharacterWhiteOverlay)
            {
                return;
            }

            _data = evt.characterWhiteOverlay ?? new CharacterWhiteOverlayData();
            _controller = ResolveOverlayController(_data);
            if (_controller == null)
            {
                return;
            }

            _duration = evt.duration > 0f ? evt.duration : 0f;
            _elapsed = 0f;
            _isPlaying = _duration > 0f;

            _controller.Configure(_data.color, refreshTargets: _data.refreshTargetsOnTrigger);

            if (_duration <= 0f)
            {
                _controller.SetOverlay(_data.toStrength);
                return;
            }

            _controller.SetOverlay(_data.fromStrength);
        }

        public void Update()
        {
            if (!_isPlaying || _controller == null || _data == null)
            {
                return;
            }

            _elapsed += _data.useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
            float t = Mathf.Clamp01(_elapsed / Mathf.Max(0.0001f, _duration));
            float eased = Mathf.Clamp01(Easing.Apply(t, _data.easing));
            float value = Mathf.Lerp(_data.fromStrength, _data.toStrength, eased);
            _controller.SetOverlay(value);

            if (_elapsed >= _duration)
            {
                Stop();
            }
        }

        public void Stop()
        {
            _isPlaying = false;
            if (_controller == null || _data == null)
            {
                return;
            }

            if (_data.restoreOnStop)
            {
                _controller.ClearOverlay();
            }
            else
            {
                _controller.SetOverlay(_data.toStrength);
            }
        }

        public void End()
        {
            _isPlaying = false;
            if (_controller != null && _data != null && _data.restoreOnStop)
            {
                _controller.ClearOverlay();
            }
        }

        private SpriteWhiteOverlayController ResolveOverlayController(CharacterWhiteOverlayData data)
        {
            var target = GetTargetTransform(data.characterType, data.characterUid);
            if (target == null)
            {
                target = CutsceneManager.GetCharacter(data.characterType, data.characterUid);
            }

            if (target == null)
            {
                GcLogger.LogError($"CharacterWhiteOverlay target을 찾을 수 없습니다. type: {data.characterType}, uid: {data.characterUid}");
                return null;
            }

            var characterBase = target.GetComponent<CharacterBase>();
            if (characterBase == null)
            {
                GcLogger.LogError($"CharacterWhiteOverlay target에 CharacterBase가 없습니다. type: {data.characterType}, uid: {data.characterUid}");
                return null;
            }

            characterBase.TryEnsureSpriteWhiteOverlayController();
            var controller = target.GetComponent<SpriteWhiteOverlayController>();
            if (controller == null)
            {
                controller = target.gameObject.AddComponent<SpriteWhiteOverlayController>();
            }

            var settings = AddressableLoaderSettings.Instance;
            if (data.characterType == CharacterConstants.Type.Player && settings != null && settings.playerSettings != null)
            {
                controller.Configure(data.color, settings.playerSettings.spriteWhiteOverlayMaterial, refreshTargets: true);
            }
            else if (data.characterType == CharacterConstants.Type.Monster && settings != null && settings.monsterSettings != null)
            {
                controller.Configure(data.color, settings.monsterSettings.spriteWhiteOverlayMaterial, refreshTargets: true);
            }
            else
            {
                controller.Configure(data.color, refreshTargets: true);
            }

            characterBase.BindSpriteWhiteOverlayController(controller);
            return controller;
        }
    }
}
