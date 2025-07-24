#if GGEMCO_USE_SPINE
using System;
using System.Collections.Generic;
using Spine;

namespace GGemCo2DCore
{
    public class EffectAnimationControllerSpine : Spine2dController, IEffectAnimationController
    {
        private DefaultEffect _defaultEffect;
        private float durationStart;
        private float durationPlay;
        private float durationEnd;
        private float durationTotal;
        
        protected override void Awake()
        {
            base.Awake();
            _defaultEffect = GetComponent<DefaultEffect>();
            if (_defaultEffect == null)
            {
                enabled = false;
                GcLogger.LogError("DefaultEffect not found");
                return;
            }
            durationStart = GetAnimationDuration(IEffectAnimationController.KeyClipNameStart, false);
            durationPlay = GetAnimationDuration(IEffectAnimationController.KeyClipNamePlay, false);
            durationEnd = GetAnimationDuration(IEffectAnimationController.KeyClipNameEnd, false);
            durationTotal = durationStart + durationPlay + durationEnd; 
        }

        public void SetEffectColor(string colorHex)
        {
            SetColor(colorHex);
        }
        /// <summary>
        /// 애니메이션 클립이 플레이가 완료되면 호출되는 콜백 함수
        /// </summary>
        /// <param name="entry"></param>
        protected override void OnAnimationComplete(TrackEntry entry)
        {
            if (FindAnimation(IEffectAnimationController.KeyClipNameEnd) == null)
            {
                _defaultEffect.DestroyForce();
                return;
            }

            if (entry.Animation.Name != IEffectAnimationController.KeyClipNameEnd) return;
            _defaultEffect.OnEndAnimationComplete();
        }
        public bool Play(float duration) 
        {
            var findAnimation = FindAnimation(IEffectAnimationController.KeyClipNameStart);
            if (findAnimation == null) return false;

            // 무제한 플레이
            if (duration < 0)
            {
                float timeScale = 1f;
                List<StruckAddAnimation> newAddAnimations = new List<StruckAddAnimation>
                {
                    new(IEffectAnimationController.KeyClipNamePlay, true, 0, timeScale),
                };
                PlayAnimation(IEffectAnimationController.KeyClipNameStart, false, timeScale, newAddAnimations);
            }
            // 한번만 재생
            else if (duration <= 0)
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

        public void PlayEnd()
        {
            PlayAnimation(IEffectAnimationController.KeyClipNameEnd);
        }
    }
}
#endif