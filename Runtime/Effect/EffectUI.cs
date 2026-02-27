using System.Collections.Generic;
using UnityEngine;

namespace GGemCo2DCore
{
    /// <summary>
    /// 디폴트 이펙트
    /// </summary>
    public class EffectUI : Animation2dController
    {
        [Tooltip("재생 속도")]
        public float timeScale;
        
        private float _durationStart;
        private float _durationPlay;
        private float _durationEnd;
        private float _durationTotal;
        protected override void Awake()
        {
            base.Awake();
            
            _durationStart = GetAnimationDuration(IEffectAnimationController.KeyClipNameStart, false);
            _durationPlay = GetAnimationDuration(IEffectAnimationController.KeyClipNamePlay, false);
            _durationEnd = GetAnimationDuration(IEffectAnimationController.KeyClipNameEnd, false);
            _durationTotal = _durationStart + _durationPlay + _durationEnd; 
        }

        private void Start()
        {
            List<StruckAddAnimation> newAddAnimations = new List<StruckAddAnimation>();
            if (_durationPlay > 0)
            {
                newAddAnimations.Add(new(IEffectAnimationController.KeyClipNamePlay, true, 0, timeScale));
            }
            PlayAnimation(IEffectAnimationController.KeyClipNameStart, false, timeScale, newAddAnimations);
        }

    }
}