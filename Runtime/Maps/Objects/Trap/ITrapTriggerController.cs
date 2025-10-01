namespace GGemCo2DCore
{
    /// <summary>
    /// 트랩이 외부 제어(Start/End/Toggle)를 지원하려면 구현하는 인터페이스.
    /// </summary>
    public interface ITrapTriggerController
    {
        bool IsActive { get; }
        void RequestStart(UnityEngine.Collider2D triggerSource);
        void RequestEnd();
    }
}