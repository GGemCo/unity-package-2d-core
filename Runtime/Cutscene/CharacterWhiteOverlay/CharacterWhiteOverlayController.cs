using System.Collections;
using UnityEngine;

namespace GGemCo2DCore
{
    /// <summary>
    /// 컷신 타임라인에 따라 특정 캐릭터의 Sprite White Overlay 강도를 제어하는 컨트롤러입니다.
    /// 지정된 색상과 강도를 적용하며, 보간(easing) 및 종료 시 복원 옵션을 지원합니다.
    /// </summary>
    public sealed class CharacterWhiteOverlayController : CutsceneDefaultController, ICutsceneController
    {
        private CharacterWhiteOverlayData _data;
        private SpriteWhiteOverlayController _controller;
        private float _elapsed;
        private float _duration;
        private bool _isPlaying;
        private readonly AddressableLoaderSettings _settings;
        private CharacterConstants.Type _resolvedCharacterType;
        private int _resolvedCharacterUid;

        /// <summary>
        /// White Overlay 컨트롤러를 생성합니다.
        /// </summary>
        /// <param name="manager">컷신 흐름을 관리하는 매니저입니다.</param>
        /// <param name="settings">캐릭터 타입별 Overlay 머티리얼 설정입니다.</param>
        public CharacterWhiteOverlayController(CutsceneManager manager, AddressableLoaderSettings settings)
        {
            CutsceneManager = manager;
            _settings = settings;
        }

        /// <summary>
        /// 다음 프레임 대기 없이 즉시 준비를 지원합니다.
        /// </summary>
        public bool SupportsImmediateReady => true;

        public void ReadyImmediate(CutsceneEvent evt)
        {
            if (evt.type != CutsceneEventType.CharacterWhiteOverlay)
            {
                return;
            }
        }

        public IEnumerator Ready(CutsceneEvent evt)
        {
            ReadyImmediate(evt);
            yield break;
        }

        public void Trigger(CutsceneEvent evt)
        {
            if (evt.type != CutsceneEventType.CharacterWhiteOverlay)
            {
                return;
            }

            _data = evt.characterWhiteOverlay ?? new CharacterWhiteOverlayData();

            _controller = ResolveOverlayController(_data, out _resolvedCharacterType, out _resolvedCharacterUid);
            if (_controller == null)
            {
                return;
            }

            _duration = evt.duration > 0f ? evt.duration : 0f;
            _elapsed = 0f;
            _isPlaying = _duration > 0f;

            if (_resolvedCharacterType == CharacterConstants.Type.Player && _settings?.playerSettings != null)
            {
                _controller.Configure(_data.color, _settings.playerSettings.spriteWhiteOverlayMaterial, _data.refreshTargetsOnTrigger);
            }
            else if (_resolvedCharacterType == CharacterConstants.Type.Monster && _settings?.monsterSettings != null)
            {
                _controller.Configure(_data.color, _settings.monsterSettings.spriteWhiteOverlayMaterial, _data.refreshTargetsOnTrigger);
            }
            else
            {
                _controller.Configure(_data.color, refreshTargets: _data.refreshTargetsOnTrigger);
            }

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

            if (_controller != null && _data is { restoreOnStop: true })
            {
                _controller.ClearOverlay();
            }
        }

        private SpriteWhiteOverlayController ResolveOverlayController(CharacterWhiteOverlayData data, out CharacterConstants.Type resolvedCharacterType, out int resolvedCharacterUid)
        {
            resolvedCharacterType = CharacterConstants.Type.Player;
            resolvedCharacterUid = 0;

            var target = ResolveTargetTransform(data, out resolvedCharacterType, out resolvedCharacterUid);
            if (target == null)
            {
                GcLogger.LogError($"CharacterWhiteOverlay target을 찾을 수 없습니다. type: {resolvedCharacterType}, uid: {resolvedCharacterUid}");
                return null;
            }

            var characterBase = target.GetComponent<CharacterBase>();
            if (characterBase == null)
            {
                GcLogger.LogError($"CharacterWhiteOverlay target에 CharacterBase가 없습니다. type: {resolvedCharacterType}, uid: {resolvedCharacterUid}");
                return null;
            }

            characterBase.TryEnsureSpriteWhiteOverlayController();

            var controller = target.GetComponent<SpriteWhiteOverlayController>();
            if (controller == null)
            {
                controller = target.gameObject.AddComponent<SpriteWhiteOverlayController>();
            }

            characterBase.BindSpriteWhiteOverlayController(controller);
            return controller;
        }

        private Transform ResolveTargetTransform(CharacterWhiteOverlayData data, out CharacterConstants.Type resolvedCharacterType, out int resolvedCharacterUid)
        {
            resolvedCharacterType = CharacterConstants.Type.Player;
            resolvedCharacterUid = 0;

            if (data == null)
            {
                return null;
            }

            var reference = data.target;
            if (reference != null && reference.sourceMode == CutsceneCharacterTargetSourceMode.RuntimeOverride)
            {
                if (reference.runtimeTargetKey != CutsceneKeyCharacterTarget.None &&
                    CutsceneManager.TryGetCharacterTargetOverride(reference.runtimeTargetKey, out var runtimeCharacter) &&
                    runtimeCharacter != null)
                {
                    resolvedCharacterType = runtimeCharacter.type;
                    resolvedCharacterUid = runtimeCharacter.uid;
                    return runtimeCharacter.transform;
                }

                if (reference.runtimeTargetKey != CutsceneKeyCharacterTarget.None)
                {
                    GcLogger.Log($"CharacterWhiteOverlay runtime override not found. key={reference.runtimeTargetKey}");
                    return null;
                }
            }

            if (reference != null)
            {
                resolvedCharacterType = reference.characterType;
                resolvedCharacterUid = reference.characterUid;
            }

            if (resolvedCharacterUid == 0 && data.characterUid != 0)
            {
                resolvedCharacterType = data.characterType;
                resolvedCharacterUid = data.characterUid;
            }

            var target = GetTargetTransform(resolvedCharacterType, resolvedCharacterUid);
            if (target == null)
            {
                target = CutsceneManager.GetCharacter(resolvedCharacterType, resolvedCharacterUid);
            }

            return target;
        }
    }
}
