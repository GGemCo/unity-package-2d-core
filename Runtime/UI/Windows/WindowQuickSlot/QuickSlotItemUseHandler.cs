namespace GGemCo2DCore
{
    /// <summary>
    /// 소비 아이템 퀵슬롯 실행 핸들러입니다.
    /// 실제 사용은 인벤토리 원본 아이템을 찾아 서비스로 위임합니다.
    /// </summary>
    public class QuickSlotItemUseHandler : IQuickSlotUseHandler
    {
        public bool CanUse(UIWindowQuickSlot window, SaveDataIcon entry, out string failMessageKey)
        {
            failMessageKey = null;
            return window != null && window.CanUseQuickSlotItem(entry, out failMessageKey);
        }

        public bool Use(UIWindowQuickSlot window, SaveDataIcon entry, out string failMessageKey)
        {
            failMessageKey = null;
            return window != null && window.TryUseQuickSlotItem(entry, out failMessageKey);
        }
    }
}
