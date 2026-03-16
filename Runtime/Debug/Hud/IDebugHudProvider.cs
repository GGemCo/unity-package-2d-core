#if UNITY_EDITOR || DEVELOPMENT_BUILD
namespace GGemCo2DCore
{
    public enum DebugHudAnchor
    {
        TopLeft,
        TopRight,
        BottomLeft,
        BottomRight,
    }

    internal interface IDebugHudProvider
    {
        DebugHudAnchor Anchor { get; }
        bool IsEnabled(GGemCoSettings settings);
        void Initialize(GGemCoSettings settings);
        void Tick(float unscaledDeltaTime, GGemCoSettings settings);
        string GetText();
    }
}
#endif
