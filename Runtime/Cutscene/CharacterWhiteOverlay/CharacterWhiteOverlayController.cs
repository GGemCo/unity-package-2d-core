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
        private AddressableLoaderSettings _settings;

        /// <summary>
        /// White Overlay 컨트롤러를 생성합니다.
        /// </summary>
        /// <param name="manager">컷신 흐름을 관리하는 매니저입니다.</param>
        /// <param name="settings"></param>
        public CharacterWhiteOverlayController(CutsceneManager manager, AddressableLoaderSettings settings)
        {
            CutsceneManager = manager;
            _settings = settings;
        }

        /// <summary>
        /// 다음 프레임 대기 없이 즉시 준비를 지원합니다.
        /// </summary>
        public bool SupportsImmediateReady => true;


        /// <summary>
        /// Overlay 적용 전 사전 준비를 수행합니다.
        /// 현재는 별도 작업 없이 한 프레임을 양보합니다.
        /// </summary>
        /// <param name="evt">준비할 컷신 이벤트입니다.</param>
        /// <returns>비동기 준비 처리를 위한 열거자입니다.</returns>
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

        /// <summary>
        /// Overlay 효과를 시작하고 색상, 강도 범위, 지속 시간을 설정합니다.
        /// duration이 0 이하이면 즉시 목표 강도를 적용합니다.
        /// </summary>
        /// <param name="evt">실행할 컷신 이벤트입니다.</param>
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

            // Overlay 색상 및 타겟 렌더러 설정
            // 캐릭터 타입별 머티리얼 설정
            if (_data.characterType == CharacterConstants.Type.Player &&
                _settings?.playerSettings != null)
            {
                _controller.Configure(
                    _data.color,
                    _settings.playerSettings.spriteWhiteOverlayMaterial,
                    refreshTargets: true);
            }
            else if (_data.characterType == CharacterConstants.Type.Monster &&
                     _settings?.monsterSettings != null)
            {
                _controller.Configure(
                    _data.color,
                    _settings.monsterSettings.spriteWhiteOverlayMaterial,
                    refreshTargets: true);
            }
            else
            {
                _controller.Configure(_data.color, refreshTargets: true);
            }

            if (_duration <= 0f)
            {
                // 즉시 적용
                _controller.SetOverlay(_data.toStrength);
                return;
            }

            // 시작 강도 적용 후 보간 시작
            _controller.SetOverlay(_data.fromStrength);
        }

        /// <summary>
        /// Overlay 강도를 easing 기반으로 보간하여 적용합니다.
        /// 지정된 시간이 지나면 자동으로 종료합니다.
        /// </summary>
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

        /// <summary>
        /// Overlay 효과를 중지하고 설정에 따라 상태를 유지하거나 제거합니다.
        /// </summary>
        public void Stop()
        {
            _isPlaying = false;

            if (_controller == null || _data == null)
            {
                return;
            }

            if (_data.restoreOnStop)
            {
                // Overlay 제거 (원래 상태로 복원)
                _controller.ClearOverlay();
            }
            else
            {
                // 최종 강도 유지
                _controller.SetOverlay(_data.toStrength);
            }
        }

        /// <summary>
        /// 컷신 종료 시 Overlay 상태를 정리합니다.
        /// restoreOnStop 옵션이 활성화된 경우 Overlay를 제거합니다.
        /// </summary>
        public void End()
        {
            _isPlaying = false;

            if (_controller != null && _data is { restoreOnStop: true })
            {
                _controller.ClearOverlay();
            }
        }

        /// <summary>
        /// 대상 캐릭터를 탐색하고 SpriteWhiteOverlayController를 반환합니다.
        /// 필요 시 컴포넌트를 추가하고 캐릭터에 바인딩합니다.
        /// </summary>
        /// <param name="data">Overlay 적용 대상 정보입니다.</param>
        /// <returns>Overlay 제어용 컨트롤러입니다. 실패 시 null입니다.</returns>
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

            // Overlay 컨트롤러 보장
            characterBase.TryEnsureSpriteWhiteOverlayController();

            var controller = target.GetComponent<SpriteWhiteOverlayController>();

            if (controller == null)
            {
                controller = target.gameObject.AddComponent<SpriteWhiteOverlayController>();
            }

            // CharacterBase에 바인딩
            characterBase.BindSpriteWhiteOverlayController(_controller);

            return controller;
        }
    }
}