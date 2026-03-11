namespace GGemCo2DCore
{
    /// <summary>
    /// UI 효과 재생 시 구분에 사용하는 이벤트 타입입니다.
    /// </summary>
    public enum UIEffectEventType
    {
        None = 0,
        WindowOpen,
        WindowClose,
        ResourceIncrease,
        ResourceDecrease,
        ResourceMaxChanged,
        CooldownCompleted
    }
}
