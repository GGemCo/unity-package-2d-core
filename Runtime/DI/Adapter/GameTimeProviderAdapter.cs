using System;

namespace GGemCo2DCore
{
    /// <summary>
    /// GameTimeManager를 IGameTimeProvider로 어댑팅
    /// - 내부 이벤트를 동일 시그니처로 포워딩
    /// </summary>
    public sealed class GameTimeProviderAdapter : IGameTimeProvider
    {
        private readonly GameTimeManager _mgr;

        public GameTimeProviderAdapter(GameTimeManager mgr)
        {
            _mgr = mgr ?? throw new ArgumentNullException(nameof(mgr));
            // GameTimeManager 쪽 이벤트에 연결
            GameTimeManager.OnUiUpdateInterval += HandleUi;
            _mgr.OnDayChanged += HandleDay;
            _mgr.OnMonthChanged += HandleMonth;
        }

        public DateTime Now => _mgr.Now;
        public event Action<DateTime> OnUiUpdateInterval;
        public event Action<DateTime> OnDayChanged;
        public event Action<DateTime> OnMonthChanged;

        public int GetDayNumber() => _mgr.GetDayNumber();

        private void HandleUi(DateTime now) => OnUiUpdateInterval?.Invoke(now);
        private void HandleDay(DateTime now) => OnDayChanged?.Invoke(now);
        private void HandleMonth(DateTime now) => OnMonthChanged?.Invoke(now);
    }
}