using System;
using System.Collections.Generic;
using UnityEngine;

namespace GGemCo2DCore
{
    public class VfxAnimationControllerSprite : Animation2dController, IVfxAnimationController
    {
        private VfxBehaviourBase _defaultEffect;
        private Renderer _effectRenderer;
        private float _durationStart;
        private float _durationPlay;
        private float _durationEnd;
        private float _durationTotal;
        private bool _isInitializedClip;

        protected override void Awake()
        {
            base.Awake();
            EnsureInitialized();
        }

        private void EnsureInitialized()
        {
            if (_isInitializedClip)
                return;

            _defaultEffect = GetComponent<VfxBehaviourBase>();
            _effectRenderer = GetComponent<Renderer>();
            if (_effectRenderer != null)
                _effectRenderer.sortingLayerName = ConfigSortingLayer.GetValue(ConfigSortingLayer.Keys.CharacterTop);

            // 이펙트는 상황에 따라 클립이 없는 경우가 있다.
            _durationStart = GetAnimationDuration(IVfxAnimationController.KeyClipNameStart, false, false);
            _durationPlay = GetAnimationDuration(IVfxAnimationController.KeyClipNamePlay, false, false);
            _durationEnd = GetAnimationDuration(IVfxAnimationController.KeyClipNameEnd, false, false);
            _durationTotal = _durationStart + _durationPlay + _durationEnd;
            _isInitializedClip = true;
        }

        public void SetEffectColor(string colorHex)
        {
            EnsureInitialized();
            SetColor(colorHex);
        }

        /// <summary>
        /// 애니메이션 클립이 플레이가 완료되면 호출되는 콜백 함수
        /// </summary>
        public void AnimationEventComplete(StruckAnimationEventComplete struckAnimationEventComplete)
        {
            EnsureInitialized();

            if (GetClipByName(IVfxAnimationController.KeyClipNameEnd) == null)
            {
                _defaultEffect?.DestroyForce();
                return;
            }

            if (Animator == null)
                return;

            AnimatorStateInfo state = Animator.GetCurrentAnimatorStateInfo(0);
            if (!state.IsName(IVfxAnimationController.KeyClipNameEnd))
                return;

            _defaultEffect?.OnEndAnimationComplete();
        }

        public void SetLoop(bool loop, int layerIndex = 0)
        {
            SetAnimationLoop(loop, layerIndex);
        }

        public float GetAnimationEventTime(string aniName, string eventName, List<string> exceptEventName = null)
        {
            return 0;
        }

        /// <summary>
        /// duration 정책에 맞춰 start/play/end 클립 재생 순서를 구성하고 시작 애니메이션을 재생합니다.
        /// </summary>
        /// <param name="duration">재생 시간입니다. 0 이하는 1회 재생, 음수는 무제한 재생입니다.</param>
        /// <param name="timeScale">기본 재생 속도입니다.</param>
        /// <param name="forceReset">true면 같은 Animator 상태라도 첫 프레임부터 다시 재생합니다.</param>
        /// <returns>재생 가능한 클립이 있으면 true를 반환합니다.</returns>
        public bool Play(float duration, float timeScale = 1f, bool forceReset = false)
        {
            EnsureInitialized();

            var startClip = GetClipByName(IVfxAnimationController.KeyClipNameStart);
            var playClip = GetClipByName(IVfxAnimationController.KeyClipNamePlay);
            var endClip = GetClipByName(IVfxAnimationController.KeyClipNameEnd);

            if (startClip == null && playClip == null && endClip == null)
                return false;

            // 무기한 유지 VFX에 종료 클립만 있으면 생성 시점에는 정적 렌더러를 유지합니다.
            // end 클립은 Projectile 도착·충돌처럼 실제 종료 요청이 들어왔을 때 PlayEnd()에서만 재생합니다.
            if (duration < 0f && startClip == null && playClip == null && endClip != null)
                return true;

            var startAnimationName = startClip != null
                ? IVfxAnimationController.KeyClipNameStart
                : playClip != null
                    ? IVfxAnimationController.KeyClipNamePlay
                    : IVfxAnimationController.KeyClipNameEnd;

            var addAnimations = new List<StruckAddAnimation>();
            float startTimeScale = timeScale;

            // 무제한 플레이
            if (duration < 0f)
            {
                if (playClip != null)
                {
                    if (startClip != null)
                        addAnimations.Add(new StruckAddAnimation(IVfxAnimationController.KeyClipNamePlay, true, 0f, 1f));
                    else
                        startAnimationName = IVfxAnimationController.KeyClipNamePlay;
                }
            }
            // 한번만 재생
            else if (duration <= 0f)
            {
                if (startClip != null)
                {
                    if (playClip != null)
                        addAnimations.Add(new StruckAddAnimation(IVfxAnimationController.KeyClipNamePlay, false, 0f, 1f));
                    if (endClip != null)
                        addAnimations.Add(new StruckAddAnimation(IVfxAnimationController.KeyClipNameEnd, false, 0f, 1f));
                }
                else if (startAnimationName == IVfxAnimationController.KeyClipNamePlay && endClip != null)
                {
                    addAnimations.Add(new StruckAddAnimation(IVfxAnimationController.KeyClipNameEnd, false, 0f, 1f));
                }
            }
            // play 클립 loop 하기
            else if (_durationTotal < duration && playClip != null && _durationPlay > 0f)
            {
                var realLoopDuration = Mathf.Max(0f, duration - _durationStart - _durationEnd);
                var loopRatio = realLoopDuration / _durationPlay;
                var loopCntCeil = Math.Max(1, (int)Math.Ceiling(loopRatio));
                var newTimeScale = loopRatio > 0f ? loopCntCeil / (float)loopRatio : 1f;

                for (var i = 0; i < loopCntCeil; i++)
                    addAnimations.Add(new StruckAddAnimation(IVfxAnimationController.KeyClipNamePlay, false, 0f, newTimeScale));

                if (endClip != null)
                    addAnimations.Add(new StruckAddAnimation(IVfxAnimationController.KeyClipNameEnd));
            }
            // 전체 클립 timescale 빠르게 또는 loop 클립이 없는 fallback
            else
            {
                var totalPlayableDuration = 0f;
                if (startClip != null)
                    totalPlayableDuration += _durationStart;
                if (playClip != null)
                    totalPlayableDuration += _durationPlay;
                if (endClip != null)
                    totalPlayableDuration += _durationEnd;

                if (duration > 0f && totalPlayableDuration > 0f)
                    startTimeScale = totalPlayableDuration / duration;

                if (startClip != null)
                {
                    if (playClip != null)
                        addAnimations.Add(new StruckAddAnimation(IVfxAnimationController.KeyClipNamePlay, false, 0f, startTimeScale));
                    if (endClip != null)
                        addAnimations.Add(new StruckAddAnimation(IVfxAnimationController.KeyClipNameEnd, false, 0f, startTimeScale));
                }
                else if (startAnimationName == IVfxAnimationController.KeyClipNamePlay && endClip != null)
                {
                    addAnimations.Add(new StruckAddAnimation(IVfxAnimationController.KeyClipNameEnd, false, 0f, startTimeScale));
                }
            }

            PlayAnimation(startAnimationName, false, startTimeScale, addAnimations, forceReset: forceReset);
            return true;
        }

        public bool HasEndAnimation()
        {
            EnsureInitialized();
            return GetClipByName(IVfxAnimationController.KeyClipNameEnd) != null;
        }

        /// <summary>
        /// 종료 클립을 첫 프레임부터 재생합니다.
        /// </summary>
        public void PlayEnd()
        {
            EnsureInitialized();

            if (GetClipByName(IVfxAnimationController.KeyClipNameEnd) != null)
            {
                PlayAnimation(IVfxAnimationController.KeyClipNameEnd, forceReset: true);
                return;
            }

            _defaultEffect?.OnEndAnimationComplete();
        }
    }
}
