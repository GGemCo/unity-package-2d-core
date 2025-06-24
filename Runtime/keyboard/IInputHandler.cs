namespace GGemCo2DCore
{
    public interface IInputHandler
    {
        int Priority { get; }
        bool HandleInput(); // 처리 여부 반환
    }
}