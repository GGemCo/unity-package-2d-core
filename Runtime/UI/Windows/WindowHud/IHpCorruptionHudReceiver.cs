namespace GGemCo2DCore
{
    /// <summary>
    /// HP HUD가 독 하트 오염 상태를 수신할 수 있을 때 구현하는 인터페이스입니다.
    /// </summary>
    public interface IHpCorruptionHudReceiver
    {
        void SetHpCorruption(HpCorruptionSnapshot snapshot);
    }
}
