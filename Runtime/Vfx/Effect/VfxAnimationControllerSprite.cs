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
        private bool _isInitialized;

        protected override void Awake()
        {
            base.Awake();
            EnsureInitialized();
        }

        protected void EnsureInitialized()
        {
            if (_isInitialized)
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
            _isInitialized = true;
        }

        public void SetEffectColor(string colorHex)
        {
            EnsureInitialized();
            SetColor(colorHex);
        }

        /// <summary>
        /// 애니메이션 클립이 플레이가 완료되면 호출되는 콜백 함수
        /// </summary>
        public override void GGemCoAniEventComplete()
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

        public bool Play(float duration, float timeScale = 1f)
        {
            EnsureInitialized();

            var startClip = GetClipByName(IVfxAnimationController.KeyClipNameStart);
            var playClip = GetClipByName(IVfxAnimationController.KeyClipNamePlay);
            var endClip = GetClipByName(IVfxAnimationController.KeyClipNameEnd);

            if (startClip == null && playClip == null && endClip == null)
                return false;

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

            PlayAnimation(startAnimationName, false, startTimeScale, addAnimations);
            return true;
        }

        public bool HasEndAnimation()
        {
            EnsureInitialized();
            return GetClipByName(IVfxAnimationController.KeyClipNameEnd) != null;
        }

        public void PlayEnd()
        {
            EnsureInitialized();

            if (GetClipByName(IVfxAnimationController.KeyClipNameEnd) != null)
            {
                PlayAnimation(IVfxAnimationController.KeyClipNameEnd);
                return;
            }

            _defaultEffect?.OnEndAnimationComplete();
        }
    }
}
