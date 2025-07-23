#if GGEMCO_USE_SPINE
using System;
using System.Collections.Generic;
using Spine;
using UnityEngine;
using Animation = Spine.Animation;

namespace GGemCo2DCore
{
    public class EffectAnimationControllerSpine : Spine2dController, IEffectAnimationController
    {
        private DefaultEffect _defaultEffect;
        
        protected override void Awake()
        {
            base.Awake();
            _defaultEffect = GetComponent<DefaultEffect>();
            if (_defaultEffect == null)
            {
                GcLogger.LogError("DefaultEffect is missing! This component will not function.");
                enabled = false; // 컴포넌트를 비활성화하여 다른 함수들이 실행되지 않도록 합니다.
                return;
            }
        }
        public void SetEffectColor(string colorHex)
        {
            SetColor(colorHex);
        }

        protected override void OnAnimationComplete(TrackEntry entry)
        {
            if (entry == null) return;
            if (entry.Animation.Name == IEffectAnimationController.KeyClipNameEnd)
            {
                _defaultEffect.OnEndAnimationComplete();
            }
        }
        public float GetAnimationEventTime(string aniName, string eventName, List<string> exceptEventName = null)
        {
            return GetEventTime(aniName, eventName, exceptEventName);
        }
        public bool PlayEffectAnimation(string animationName, bool loop = false, float timeScale = 1, List<StruckAddAnimation> addAnimations = null)
        {
            var findAnimation = FindAnimation(animationName);
            if (findAnimation == null) return false;
            PlayAnimation(animationName, loop, timeScale, addAnimations);
            return true;
        }
        public bool PlayEffectAnimation(string animationName, float duration)
        {
            var findAnimation = FindAnimation(animationName);
            if (findAnimation == null) return false;
            
            float eventTimeLoopStart = GetEventTime(animationName, "loop_start");
            float eventTimeLoopEnd = GetEventTime(animationName, "loop_end");
            if (eventTimeLoopStart > 0 && eventTimeLoopEnd > 0)
            {
                PlayAnimationWidthLoopEvent(animationName, duration);
            }
            else
            {
                List<StruckAddAnimation> addAnimations = new List<StruckAddAnimation>();
                if (duration <= 0)
                {
                    addAnimations.Add(new(IEffectAnimationController.KeyClipNamePlay));
                    addAnimations.Add(new (IEffectAnimationController.KeyClipNameEnd));
                }
                else
                {
                    addAnimations.Add(new(IEffectAnimationController.KeyClipNamePlay, true, 0, 1f));
                }
                PlayEffectAnimation(IEffectAnimationController.KeyClipNameStart, false, 1, addAnimations);
            }

            return true;
        }

        public bool PlayEndAnimation()
        {
            var findAnimation = FindAnimation(IEffectAnimationController.KeyClipNameEnd, false);
            if (findAnimation == null) return false;
            PlayAnimation(IEffectAnimationController.KeyClipNameEnd);
            return true;
        }
    }
}
#endif