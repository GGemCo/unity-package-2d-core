using System.Collections.Generic;

namespace GGemCo2DCore
{
    public interface IEffectAnimationController
    {
        // 이펙트 시작 애니 클립 이름
        public const string KeyClipNameStart = "start";
        // 루프 되는 클립 이름
        public const string KeyClipNamePlay = "play";
        // 없어지는 애니 클립 이름
        public const string KeyClipNameEnd = "end";

        void PlayEffectAnimation(string animationName, bool loop = false, float timeScale = 1.0f,
            List<StruckAddAnimation> addAnimations = null);

        void SetEffectColor(string colorHex);
    }
}