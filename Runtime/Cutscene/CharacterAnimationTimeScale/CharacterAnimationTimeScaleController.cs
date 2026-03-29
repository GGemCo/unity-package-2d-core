using System.Collections;
using UnityEngine;

namespace GGemCo2DCore
{
    /// <summary>
    /// 컷신 타임라인에 따라 특정 캐릭터의 애니메이션 playback time scale을 제어하는 컨트롤러입니다.
    /// Blend, 즉시 설정(Set), 원상 복구(Restore) 모드를 지원하며 필요 시 원래 값을 캡처하여 복원할 수 있습니다.
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

        /// <summary>
        /// 애니메이션 타임스케일 컨트롤러를 생성합니다.
        /// </summary>
        /// <param name="manager">컷신 흐름을 관리하는 매니저입니다.</param>
        public CharacterAnimationTimeScaleController(CutsceneManager manager)
        {
            CutsceneManager = manager;
        }

        /// <summary>
        /// 이벤트 실행 전 사전 준비를 수행합니다.
        /// 현재는 별도 작업 없이 한 프레임을 양보합니다.
        /// </summary>
        /// <param name="evt">준비할 컷신 이벤트입니다.</param>
        /// <returns>비동기 준비 처리를 위한 열거자입니다.</returns>
        public IEnumerator Ready(CutsceneEvent evt)
        {
            if (evt.type != CutsceneEventType.CharacterAnimationTimeScale)
            {
                yield break;
            }

            yield return null;
        }

        /// <summary>
        /// 애니메이션 타임스케일 제어를 시작합니다.
        /// ActionMode에 따라 Blend, Set, Restore 동작을 수행합니다.
        /// </summary>
        /// <param name="evt">실행할 컷신 이벤트입니다.</param>
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

            // 필요 시 현재 scale을 캡처 (복원용)
            CaptureOriginalScaleIfNeeded(_currentKey, _animationController, _data);

            _elapsed = 0f;
            _duration = Mathf.Max(0f, evt.duration);
            _isPlaying = false;

            switch (_data.actionMode)
            {
                // 현재 값 또는 지정된 시작값에서 목표 scale까지 보간 후 유지
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

                // 즉시 scale을 설정하고 유지
                case CharacterAnimationTimeScaleActionMode.SetAndHold:
                    _animationController.SetPlaybackTimeScale(Mathf.Max(0f, _data.toScale));
                    break;

                // 현재 또는 캡처된 scale로 복원 (보간 가능)
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

        /// <summary>
        /// 타임스케일 보간을 진행하고 완료 시 자동으로 종료합니다.
        /// </summary>
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

        /// <summary>
        /// 진행 중인 보간을 중지하고 최종 scale을 적용합니다.
        /// </summary>
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

        /// <summary>
        /// 컷신 종료 시 설정에 따라 원래 타임스케일로 복원합니다.
        /// </summary>
        public void End()
        {
            _isPlaying = false;

            if (_data == null || !_data.restoreOnCutsceneEnd)
            {
                return;
            }

            ForceRestoreOriginalState();
        }

        /// <summary>
        /// 캡처된 원래 타임스케일 값으로 강제 복원합니다.
        /// </summary>
        public void ForceRestoreOriginalState()
        {
            if (_hasCurrentKey &&
                _animationController != null &&
                CutsceneManager.TryGetCapturedCharacterAnimationTimeScale(
                    _currentKey.CharacterType,
                    _currentKey.CharacterUid,
                    out float capturedScale))
            {
                _animationController.SetPlaybackTimeScale(capturedScale);
            }
        }

        /// <summary>
        /// Blend 시작값을 결정합니다.
        /// 캡처 모드이면 현재 값, 아니면 데이터에서 지정된 값 사용.
        /// </summary>
        private static float ResolveBlendStartScale(
            CharacterKey key,
            ICharacterAnimationController controller,
            CharacterAnimationTimeScaleData data)
        {
            if (data.captureOriginalOnTrigger)
            {
                return controller.GetPlaybackTimeScale();
            }

            return Mathf.Max(0f, data.fromScale);
        }

        /// <summary>
        /// Restore 대상 scale을 결정합니다.
        /// 캡처된 값 사용 여부에 따라 분기합니다.
        /// </summary>
        private float ResolveRestoreScale(
            CharacterKey key,
            ICharacterAnimationController controller,
            CharacterAnimationTimeScaleData data)
        {
            if (data.useCapturedScaleForRestore &&
                CutsceneManager.TryGetCapturedCharacterAnimationTimeScale(
                    key.CharacterType,
                    key.CharacterUid,
                    out float capturedScale))
            {
                return capturedScale;
            }

            return Mathf.Max(0f, data.restoreScale);
        }

        /// <summary>
        /// 필요 시 현재 타임스케일 값을 캡처하여 컷신 종료 시 복원할 수 있도록 저장합니다.
        /// </summary>
        private void CaptureOriginalScaleIfNeeded(
            CharacterKey key,
            ICharacterAnimationController controller,
            CharacterAnimationTimeScaleData data)
        {
            if (!data.captureOriginalOnTrigger)
            {
                return;
            }

            if (CutsceneManager.TryGetCapturedCharacterAnimationTimeScale(
                key.CharacterType,
                key.CharacterUid,
                out _))
            {
                return;
            }

            CutsceneManager.CaptureCharacterAnimationTimeScale(
                key.CharacterType,
                key.CharacterUid,
                controller.GetPlaybackTimeScale());
        }

        /// <summary>
        /// 데이터 기준으로 애니메이션 컨트롤러를 찾고 캐릭터 키를 반환합니다.
        /// </summary>
        private ICharacterAnimationController ResolveAnimationController(
            CharacterAnimationTimeScaleData data,
            out CharacterKey key)
        {
            key = new CharacterKey(data.characterType, data.characterUid);
            return ResolveAnimationController(key);
        }

        /// <summary>
        /// 캐릭터를 탐색하여 애니메이션 컨트롤러를 반환합니다.
        /// 존재하지 않으면 로그를 출력합니다.
        /// </summary>
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

        /// <summary>
        /// 캐릭터를 식별하기 위한 키 구조체입니다.
        /// </summary>
        private readonly struct CharacterKey
        {
            /// <summary>캐릭터 타입</summary>
            public readonly CharacterConstants.Type CharacterType;

            /// <summary>캐릭터 고유 ID</summary>
            public readonly int CharacterUid;

            /// <summary>
            /// 캐릭터 키를 생성합니다.
            /// </summary>
            /// <param name="characterType">캐릭터 타입입니다.</param>
            /// <param name="characterUid">캐릭터 고유 ID입니다.</param>
            public CharacterKey(CharacterConstants.Type characterType, int characterUid)
            {
                CharacterType = characterType;
                CharacterUid = characterUid;
            }
        }
    }
}