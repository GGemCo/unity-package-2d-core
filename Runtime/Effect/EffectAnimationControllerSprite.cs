
using System.Collections.Generic;

namespace GGemCo2DCore
{
    public class EffectAnimationControllerSprite : DefaultEffect, IEffectAnimationController
    {
        private DefaultEffect _defaultEffect;
        
        public void Initialize(DefaultEffect defaultEffect)
        {
            _defaultEffect =  defaultEffect;
        }
        public void PlayEffectAnimation(string animationName, bool loop = false, float timeScale = 1, List<StruckAddAnimation> addAnimations = null)
        {
        }

        public void SetEffectColor(string colorHex)
        {
        }
    }
}