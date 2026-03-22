
namespace GGemCo2DCore
{
    public interface IVfxAnimationController
    {
        // 이펙트 시작 애니 클립 이름
        public const string KeyClipNameStart = "start";
        // 루프 되는 클립 이름
        public const string KeyClipNamePlay = "play";
        // 없어지는 애니 클립 이름
        public const string KeyClipNameEnd = "end";
        void SetEffectColor(string colorHex);
        bool HasEndAnimation();
        
        bool Play(float duration, float timeScale = 1f);
        void PlayEnd();
    }
}