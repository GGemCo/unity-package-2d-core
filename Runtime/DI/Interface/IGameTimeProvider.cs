using System;

namespace GGemCo2DCore
{
    /// <summary>
    /// UI/도메인에서 사용하는 '게임 시간' 인터페이스
    /// - 구현체: GameTimeProviderAdapter (GameTimeManager 래핑)
    /// </summary>
    public interface IGameTimeProvider
    {
        DateTime Now { get; }

        // UI 주기적 갱신(분/초 단위 등)
        event Action<DateTime> OnUiUpdateInterval;
        // 날짜/월 등 경계 이벤트
        event Action<DateTime> OnDayChanged;
        event Action<DateTime> OnMonthChanged;

        int GetDayNumber();
    }
}