namespace GGemCo2DCore
{
    /// <summary>
    /// 액티브 스킬 퀵슬롯 실행 핸들러입니다.
    /// </summary>
    public class QuickSlotSkillUseHandler : IQuickSlotUseHandler
    {
        public bool CanUse(UIWindowQuickSlot window, SaveDataIcon entry, out string failMessageKey)
        {
            failMessageKey = null;
            return window != null && entry != null && entry.Uid > 0;
        }

        public bool Use(UIWindowQuickSlot window, SaveDataIcon entry, out string failMessageKey)
        {
            failMessageKey = null;
            return window != null && window.TryUseQuickSlotSkill(entry, out failMessageKey);
        }
    }
}
