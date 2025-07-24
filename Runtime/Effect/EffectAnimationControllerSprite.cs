using System;
using System.Collections.Generic;
using UnityEngine;

namespace GGemCo2DCore
{
    public class EffectAnimationControllerSprite : Animator2dController, IEffectAnimationController
    {
        private DefaultEffect _defaultEffect;
        private Renderer _effectRenderer;
        private float durationStart;
        private float durationPlay;
        private float durationEnd;
        private float durationTotal;
        
        protected override void Awake()
        {
            base.Awake();
            _defaultEffect = GetComponent<DefaultEffect>();
            _effectRenderer = GetComponent<Renderer>();
            _effectRenderer.sortingLayerName = ConfigSortingLayer.GetValue(ConfigSortingLayer.Keys.CharacterTop);
            
            durationStart = GetAnimationDuration(IEffectAnimationController.KeyClipNameStart);
            durationPlay = GetAnimationDuration(IEffectAnimationController.KeyClipNamePlay);
            durationEnd = GetAnimationDuration(IEffectAnimationController.KeyClipNameEnd);
            durationTotal = durationStart + durationPlay + durationEnd; 
        }

        public void SetEffectColor(string colorHex)
        {
            SetColor(colorHex);
        }

        /// <summary>
        /// 애니메이션 클립이 플레이가 완료되면 호출되는 콜백 함수
        /// </summary>
        public override void OnAnimationComplete()
        {
            if (!GetClipByName(IEffectAnimationController.KeyClipNameEnd))
            {
                _defaultEffect.DestroyForce();
                return;
            }
            AnimatorStateInfo state = Animator.GetCurrentAnimatorStateInfo(0);
            if (!state.IsName(IEffectAnimationController.KeyClipNameEnd)) return;
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
            var findAnimation = GetClipByName(IEffectAnimationController.KeyClipNameStart);
            if (findAnimation == null) return false;

            // 한번만 재생
            if (duration <= 0)
            {
                float timeScale = 1f;
                List<StruckAddAnimation> newAddAnimations = new List<StruckAddAnimation>
                {
                    new(IEffectAnimationController.KeyClipNamePlay, false, 0, timeScale),
                    new(IEffectAnimationController.KeyClipNameEnd, false, 0, timeScale)
                };
                PlayAnimation(IEffectAnimationController.KeyClipNameStart, false, timeScale, newAddAnimations);
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
                
                for(var i = 0; i< loopCntCeil; i++)
                {
                    StruckAddAnimation struckAddAnimation =
                        new StruckAddAnimation(IEffectAnimationController.KeyClipNamePlay, false, 0, newTimeScale);
                    newAddAnimations.Add(struckAddAnimation);
                }

                //endAni
                {
                    StruckAddAnimation struckAddAnimation =
                        new StruckAddAnimation(IEffectAnimationController.KeyClipNameEnd);
                    newAddAnimations.Add(struckAddAnimation);
                }

                //startAni
                PlayAnimation(IEffectAnimationController.KeyClipNameStart, false, 1, newAddAnimations);
            }
            // 전체 클립 timescale 빠르게 
            else
            {
                float timeScale = durationTotal / duration;
                List<StruckAddAnimation> newAddAnimations = new List<StruckAddAnimation>
                {
                    new(IEffectAnimationController.KeyClipNamePlay, false, 0, timeScale),
                    new(IEffectAnimationController.KeyClipNameEnd, false, 0, timeScale)
                };
                PlayAnimation(IEffectAnimationController.KeyClipNameStart, false, timeScale, newAddAnimations);
            }

            return true;
        }
    }
}
