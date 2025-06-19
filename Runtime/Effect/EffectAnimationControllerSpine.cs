
using System.Collections.Generic;

namespace GGemCo.Scripts
{
#if GGEMCO_USE_SPINE
    public class EffectAnimationControllerSpine : Spine2dController, IEffectAnimationController
    {
        public void PlayEffectAnimation(string animationName, bool loop = false, float timeScale = 1, List<StruckAddAnimation> addAnimations = null)
        {
            PlayAnimation(animationName, loop, timeScale, addAnimations);
        }

        public void SetEffectColor(string colorHex)
        {
            SetColor(colorHex);
        }
    }
#endif
}