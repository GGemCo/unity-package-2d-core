#if GGEMCO_USE_SPINE
using System.Collections.Generic;
using Spine;

namespace GGemCo2DCore
{
    public class EffectAnimationControllerSpine : Spine2dController, IEffectAnimationController
    {
        private DefaultEffect _defaultEffect;
        
        public void Initialize(DefaultEffect defaultEffect)
        {
            _defaultEffect =  defaultEffect;
        }
        public void PlayEffectAnimation(string animationName, bool loop = false, float timeScale = 1, List<StruckAddAnimation> addAnimations = null)
        {
            PlayAnimation(animationName, loop, timeScale, addAnimations);
        }

        public void SetEffectColor(string colorHex)
        {
            SetColor(colorHex);
        }

        protected override void OnAnimationComplete(TrackEntry entry)
        {
            if (entry.Animation.Name == IEffectAnimationController.KeyClipNameEnd)
            {
                _defaultEffect.Destroy();
            }
        }
    }
}
#endif