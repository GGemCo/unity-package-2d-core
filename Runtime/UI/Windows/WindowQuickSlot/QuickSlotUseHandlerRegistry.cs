using System.Collections.Generic;

namespace GGemCo2DCore
{
    /// <summary>
    /// 퀵슬롯 엔트리 타입별 실행 핸들러입니다.
    /// Core 는 핸들러 인터페이스만 알고, 실제 실행 방식은 아이콘 타입별 구현에 위임합니다.
    /// </summary>
    public interface IQuickSlotUseHandler
    {
        bool CanUse(UIWindowQuickSlot window, SaveDataIcon entry, out string failMessageKey);
        bool Use(UIWindowQuickSlot window, SaveDataIcon entry, out string failMessageKey);
    }

    /// <summary>
    /// 아이콘 타입별 퀵슬롯 실행 핸들러를 등록/조회합니다.
    /// 나중에 상위 패키지에서 같은 타입을 재등록해 동작을 덮어쓸 수 있습니다.
    /// </summary>
    public static class QuickSlotUseHandlerRegistry
    {
        private static readonly Dictionary<IconConstants.Type, IQuickSlotUseHandler> Handlers = new();

        public static void Register(IconConstants.Type iconType, IQuickSlotUseHandler handler)
        {
            if (handler == null)
                return;

            Handlers[iconType] = handler;
        }

        public static bool TryGet(IconConstants.Type iconType, out IQuickSlotUseHandler handler)
        {
            return Handlers.TryGetValue(iconType, out handler);
        }
    }
}
