using System.Collections.Generic;

namespace GGemCo.Scripts
{
    public interface IEffectAnimationController
    {
        void PlayEffectAnimation(string animationName, bool loop = false, float timeScale = 1.0f,
            List<StruckAddAnimation> addAnimations = null);

        void SetEffectColor(string colorHex);
    }
}