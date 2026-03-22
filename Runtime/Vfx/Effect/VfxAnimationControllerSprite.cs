using System;
using System.Collections.Generic;
using UnityEngine;

namespace GGemCo2DCore
{
    public class VfxAnimationControllerSprite : Animation2dController, IVfxAnimationController
    {
        private VfxBehaviourBase _defaultEffect;
        private Renderer _effectRenderer;
        private float durationStart;
        private float durationPlay;
        private float durationEnd;
        private float durationTotal;
        
        protected override void Awake()
        {
            base.Awake();
            _defaultEffect = GetComponent<VfxBehaviourBase>();
            _effectRenderer = GetComponent<Renderer>();
            if (_effectRenderer)
                _effectRenderer.sortingLayerName = ConfigSortingLayer.GetValue(ConfigSortingLayer.Keys.CharacterTop);
            
            // 이펙트는 상황에 따라 클립이 없는 경우가 있다.
            durationStart = GetAnimationDuration(IVfxAnimationController.KeyClipNameStart, false, false);
            durationPlay = GetAnimationDuration(IVfxAnimationController.KeyClipNamePlay, false, false);
            durationEnd = GetAnimationDuration(IVfxAnimationController.KeyClipNameEnd, false, false);
            durationTotal = durationStart + durationPlay + durationEnd; 
        }

        public void SetEffectColor(string colorHex)
        {
            SetColor(colorHex);
        }

        /// <summary>
        /// 애니메이션 클립이 플레이가 완료되면 호출되는 콜백 함수
        /// </summary>
        public override void GGemCoAniEventComplete()
        {
            if (!GetClipByName(IVfxAnimationController.KeyClipNameEnd))
            {
                _defaultEffect.DestroyForce();
                return;
            }
            AnimatorStateInfo state = Animator.GetCurrentAnimatorStateInfo(0);
            if (!state.IsName(IVfxAnimationController.KeyClipNameEnd)) return;
            _defaultEffect.OnEndAnimationComplete();
        }
        public void SetLoop(bool loop, int layerIndex = 0)
        {
            SetAnimationLoop(loop, layerIndex);
        }
        public float GetAnimationEventTime(string aniName, string eventName, List<string> exceptEventName = null)
        {
            return 0;
        }
		public bool Play(float duration) 
		{
            var findAnimation = GetClipByName(IVfxAnimationController.KeyClipNameStart);
            if (findAnimation == null) return false;

            // 무제한 플레이
            if (duration < 0)
            {
                float timeScale = 1f;
                List<StruckAddAnimation> newAddAnimations = new List<StruckAddAnimation>();
                if (durationPlay > 0)
                {
                    newAddAnimations.Add(new(IVfxAnimationController.KeyClipNamePlay, true, 0, timeScale));
                }
                PlayAnimation(IVfxAnimationController.KeyClipNameStart, false, timeScale, newAddAnimations);
            }
            // 한번만 재생
            else if (duration <= 0)
            {
                float timeScale = 1f;
                List<StruckAddAnimation> newAddAnimations = new List<StruckAddAnimation>();
                if (durationPlay > 0)
                {
                    newAddAnimations.Add(new(IVfxAnimationController.KeyClipNamePlay, false, 0, timeScale));
                }
                if (durationEnd > 0)
                {
                    newAddAnimations.Add(new(IVfxAnimationController.KeyClipNameEnd, false, 0, timeScale));
                }
                PlayAnimation(IVfxAnimationController.KeyClipNameStart, false, timeScale, newAddAnimations);
            }
            // play 클립 loop 하기
            else if (durationTotal < duration)
            {
                //loopAni
                var realLoopDuration = duration - durationStart - durationEnd;
                var loopCnt = realLoopDuration/durationPlay;
                var loopCntCeil = Math.Ceiling(realLoopDuration/durationPlay);
                float newTimeScale = (float)loopCntCeil/loopCnt;
                List<StruckAddAnimation> newAddAnimations = new List<StruckAddAnimation>();

                if (durationPlay > 0)
                {
                    for (var i = 0; i < loopCntCeil; i++)
                    {
                        StruckAddAnimation struckAddAnimation =
                            new StruckAddAnimation(IVfxAnimationController.KeyClipNamePlay, false, 0, newTimeScale);
                        newAddAnimations.Add(struckAddAnimation);
                    }
                }

                //endAni
                if (durationEnd > 0)
                {
                    StruckAddAnimation struckAddAnimation =
                        new StruckAddAnimation(IVfxAnimationController.KeyClipNameEnd);
                    newAddAnimations.Add(struckAddAnimation);
                }

                //startAni
                PlayAnimation(IVfxAnimationController.KeyClipNameStart, false, 1, newAddAnimations);
            }
            // 전체 클립 timescale 빠르게 
            else
            {
                float timeScale = durationTotal / duration;
                List<StruckAddAnimation> newAddAnimations = new List<StruckAddAnimation>();
                if (durationPlay > 0)
                {
                    newAddAnimations.Add(new(IVfxAnimationController.KeyClipNamePlay, false, 0, timeScale));
                }
                if (durationEnd > 0)
                {
                    newAddAnimations.Add(new(IVfxAnimationController.KeyClipNameEnd, false, 0, timeScale));
                }
                PlayAnimation(IVfxAnimationController.KeyClipNameStart, false, timeScale, newAddAnimations);
            }

            return true;
        }
        public bool HasEndAnimation()
        {
            return GetClipByName(IVfxAnimationController.KeyClipNameEnd) != null;
        }

        public void PlayEnd()
        {
            PlayAnimation(IVfxAnimationController.KeyClipNameEnd);
        }
    }
}
