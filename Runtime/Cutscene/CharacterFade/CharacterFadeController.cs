using System.Collections;
using UnityEngine;

namespace GGemCo2DCore
{
    /// <summary>
    /// 컷신 타임라인에 따라 특정 캐릭터의 알파를 제어하여 Fade In/Out 연출을 수행하는 컨트롤러입니다.
    /// owner 기반 소유권을 사용해 동일 캐릭터에 대한 동시 제어 충돌을 방지합니다.
    /// </summary>
    public sealed class CharacterFadeController : CutsceneDefaultController, ICutsceneController
    {
        private CharacterFadeData _data;
        private CharacterBase _targetCharacter;
        private ICharacterAnimationController _animationController;

        private float _elapsed;
        private float _duration;
        private float _fromAlpha;
        private float _toAlpha;
        private bool _isPlaying;
        private bool _hasOwnership;

        private Color _capturedColor = Color.white;
        private bool _capturedActiveState = true;

        /// <summary>
        /// 캐릭터 페이드 컨트롤러를 생성합니다.
        /// </summary>
        /// <param name="manager">현재 컷신 흐름을 관리하는 매니저입니다.</param>
        public CharacterFadeController(CutsceneManager manager)
        {
            CutsceneManager = manager;
        }

        /// <summary>
        /// 다음 프레임 대기 없이 즉시 준비를 지원합니다.
        /// </summary>
        public bool SupportsImmediateReady => true;

        /// <summary>
        /// 다음 프레임 대기 없이 즉시 준비를 수행합니다.
        /// 현재 구현에서는 이벤트 타입 검증만 수행합니다.
        /// </summary>
        /// <param name="evt">준비할 컷신 이벤트입니다.</param>
        public void ReadyImmediate(CutsceneEvent evt)
        {
            if (evt.type != CutsceneEventType.CharacterFade)
            {
                return;
            }
        }

        /// <summary>
        /// 캐릭터 페이드 이벤트 준비를 수행합니다.
        /// </summary>
        /// <param name="evt">준비할 컷신 이벤트입니다.</param>
        /// <returns>코루틴 열거자입니다.</returns>
        public IEnumerator Ready(CutsceneEvent evt)
        {
            ReadyImmediate(evt);
            yield break;
        }

        /// <summary>
        /// 캐릭터 페이드 연출을 시작합니다.
        /// </summary>
        /// <param name="evt">실행할 컷신 이벤트입니다.</param>
        public void Trigger(CutsceneEvent evt)
        {
            if (evt.type != CutsceneEventType.CharacterFade)
            {
                return;
            }

            Stop();

            _data = evt.characterFade ?? new CharacterFadeData();

            _targetCharacter = ResolveTargetCharacter(_data);
            if (_targetCharacter == null)
            {
                GcLogger.LogError("CharacterFade target 캐릭터를 찾을 수 없습니다.");
                ClearRuntimeState();
                return;
            }

            _animationController = _targetCharacter.CharacterAnimationController;
            if (_animationController == null)
            {
                GcLogger.LogError(
                    "CharacterFade target에 ICharacterAnimationController가 없습니다. type: " +
                    _targetCharacter.type + "/ uid: " + _targetCharacter.uid);
                ClearRuntimeState();
                return;
            }

            if (!CutsceneCharacterFadeOwnershipService.TryAcquire(
                    _targetCharacter,
                    this,
                    out _capturedColor,
                    out _capturedActiveState))
            {
                GcLogger.Log(
                    "CharacterFade owner 획득에 실패했습니다. type: " +
                    _targetCharacter.type + "/ uid: " + _targetCharacter.uid);
                ClearRuntimeState();
                return;
            }

            _hasOwnership = true;
            _data.ResolveAlphaRange(out _fromAlpha, out _toAlpha);
            _duration = evt.duration > 0f ? evt.duration : 0f;
            _elapsed = 0f;
            _isPlaying = _duration > 0f;

            if (_data.fadeMode == CutsceneCharacterFadeMode.FadeIn && !_targetCharacter.gameObject.activeSelf)
            {
                _targetCharacter.gameObject.SetActive(true);
            }

            ApplyFadeAlpha(_fromAlpha);

            if (_duration <= 0f)
            {
                ApplyFadeAlpha(_toAlpha);
                FinalizeCompletedFade();
            }
        }

        /// <summary>
        /// 시간 경과에 따라 알파를 보간하여 적용합니다.
        /// </summary>
        public void Update()
        {
            if (!_isPlaying || _targetCharacter == null || _animationController == null || _data == null)
            {
                return;
            }

            if (!CutsceneCharacterFadeOwnershipService.IsOwnedBy(_targetCharacter, this))
            {
                ClearRuntimeState();
                return;
            }

            _elapsed += _data.useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;

            float t = Mathf.Clamp01(_elapsed / Mathf.Max(0.0001f, _duration));
            float eased = Mathf.Clamp01(Easing.Apply(t, _data.easing));
            float alpha = Mathf.Lerp(_fromAlpha, _toAlpha, eased);
            ApplyFadeAlpha(alpha);

            if (_elapsed >= _duration)
            {
                _isPlaying = false;
                FinalizeCompletedFade();
            }
        }

        /// <summary>
        /// 현재 페이드 진행을 중지하고 소유권 기준으로 상태를 정리합니다.
        /// </summary>
        public void Stop()
        {
            _isPlaying = false;

            if (!_hasOwnership)
            {
                ClearRuntimeState();
                return;
            }

            if (_targetCharacter == null)
            {
                CutsceneCharacterFadeOwnershipService.ReleaseAllByOwner(this);
                ClearRuntimeState();
                return;
            }

            bool isOwner = CutsceneCharacterFadeOwnershipService.IsOwnedBy(_targetCharacter, this);
            if (isOwner)
            {
                if (_data != null && _data.holdFinalState)
                {
                    ApplyFadeAlpha(_toAlpha);
                    ApplyPostFadeActiveState(_toAlpha);
                }
                else
                {
                    RestoreCapturedState();
                }
            }

            CutsceneCharacterFadeOwnershipService.Release(_targetCharacter, this);
            ClearRuntimeState();
        }

        /// <summary>
        /// 컷신 종료 시 진행 중인 페이드 연출을 안전하게 정리합니다.
        /// </summary>
        public void End()
        {
            Stop();
        }

        /// <summary>
        /// 페이드 연출이 정상 완료되었을 때 상태를 정리합니다.
        /// </summary>
        private void FinalizeCompletedFade()
        {
            if (!_hasOwnership || _targetCharacter == null)
            {
                ClearRuntimeState();
                return;
            }

            bool isOwner = CutsceneCharacterFadeOwnershipService.IsOwnedBy(_targetCharacter, this);
            if (!isOwner)
            {
                ClearRuntimeState();
                return;
            }

            if (_data != null && _data.holdFinalState)
            {
                ApplyPostFadeActiveState(_toAlpha);
            }
            else
            {
                RestoreCapturedState();
            }

            CutsceneCharacterFadeOwnershipService.Release(_targetCharacter, this);
            ClearRuntimeState();
        }

        /// <summary>
        /// 페이드 알파를 대상 캐릭터에 적용합니다.
        /// </summary>
        /// <param name="alpha">적용할 알파값입니다.</param>
        private void ApplyFadeAlpha(float alpha)
        {
            if (_animationController == null || _data == null)
            {
                return;
            }

            Color color = _data.preserveCurrentRgb ? _capturedColor : _data.tintColor;
            color.a = Mathf.Clamp01(alpha);
            _animationController.SetCharacterColor(color);
        }

        /// <summary>
        /// Fade 완료 후 활성화 상태를 적용합니다.
        /// </summary>
        /// <param name="finalAlpha">최종 알파값입니다.</param>
        private void ApplyPostFadeActiveState(float finalAlpha)
        {
            if (_targetCharacter == null || _data == null)
            {
                return;
            }

            if (_data.fadeMode == CutsceneCharacterFadeMode.FadeIn)
            {
                if (!_targetCharacter.gameObject.activeSelf)
                {
                    _targetCharacter.gameObject.SetActive(true);
                }

                return;
            }

            if (_data.deactivateOnFadeOutComplete && finalAlpha <= 0.001f)
            {
                _targetCharacter.gameObject.SetActive(false);
            }
        }

        /// <summary>
        /// 소유권 획득 시점에 캡처한 색상/활성 상태로 복원합니다.
        /// </summary>
        private void RestoreCapturedState()
        {
            if (_animationController != null)
            {
                _animationController.SetCharacterColor(_capturedColor);
            }

            if (_targetCharacter != null)
            {
                _targetCharacter.gameObject.SetActive(_capturedActiveState);
            }
        }

        /// <summary>
        /// 캐릭터 페이드 대상 캐릭터를 해석합니다.
        /// Fixed 모드는 type/uid를, RuntimeOverride 모드는 CutsceneManager 런타임 키를 사용합니다.
        /// </summary>
        /// <param name="data">캐릭터 페이드 데이터입니다.</param>
        /// <returns>해석된 대상 캐릭터입니다. 찾지 못하면 <see langword="null"/>을 반환합니다.</returns>
        private CharacterBase ResolveTargetCharacter(CharacterFadeData data)
        {
            if (data == null)
            {
                return null;
            }

            var reference = data.target;
            if (reference != null && reference.sourceMode == CutsceneCharacterTargetSourceMode.RuntimeOverride)
            {
                if (reference.runtimeTargetKey != CutsceneKeyCharacterTarget.None &&
                    CutsceneManager.TryGetCharacterTargetOverride(reference.runtimeTargetKey, out var runtimeTarget) &&
                    runtimeTarget != null)
                {
                    return runtimeTarget;
                }

                if (reference.runtimeTargetKey != CutsceneKeyCharacterTarget.None)
                {
                    GcLogger.Log($"CharacterFade runtime override not found. key={reference.runtimeTargetKey}");
                    return null;
                }
            }

            CharacterConstants.Type resolvedType = CharacterConstants.Type.None;
            int resolvedUid = 0;

            if (reference != null)
            {
                resolvedType = reference.characterType;
                resolvedUid = reference.characterUid;
            }

            if (resolvedType == CharacterConstants.Type.None && data.characterType != CharacterConstants.Type.None)
            {
                resolvedType = data.characterType;
            }

            if (resolvedUid == 0 && data.characterUid != 0)
            {
                resolvedUid = data.characterUid;
            }

            if (resolvedType == CharacterConstants.Type.None)
            {
                return null;
            }

            var target = GetTargetTransform(resolvedType, resolvedUid);
            if (target == null)
            {
                target = CutsceneManager.GetCharacter(resolvedType, resolvedUid);
            }

            return target != null ? target.GetComponent<CharacterBase>() : null;
        }

        /// <summary>
        /// 컨트롤러의 런타임 상태를 초기화합니다.
        /// </summary>
        private void ClearRuntimeState()
        {
            _data = null;
            _targetCharacter = null;
            _animationController = null;
            _elapsed = 0f;
            _duration = 0f;
            _fromAlpha = 0f;
            _toAlpha = 0f;
            _isPlaying = false;
            _hasOwnership = false;
            _capturedColor = Color.white;
            _capturedActiveState = true;
        }
    }
}
