namespace GGemCo2DCore
{
    /// <summary>
    /// HP HUD가 속성 임계 반응으로 표시 중인 HP 상태를 수신할 수 있을 때 구현하는 인터페이스입니다.
    /// </summary>
    public interface IElementTriggeredHpHudReceiver
    {
        void SetTriggeredHpStates(ElementTriggeredHpCollectionSnapshot snapshot);
    }

    /// <summary>
    /// HP HUD가 독 하트 오염 상태를 수신할 수 있을 때 구현하는 레거시 인터페이스입니다.
    /// </summary>
    public interface IHpCorruptionHudReceiver
    {
        void SetHpCorruption(HpCorruptionSnapshot snapshot);
    }
}
