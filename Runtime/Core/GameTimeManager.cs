using System;
using UnityEngine;

namespace GGemCo2DCore
{
    /// <summary>
    /// 시뮬레이션 게임 시간 매니저
    /// - 현실 시간 흐름을 받아 게임 시간(DateTime)으로 변환/진행
    /// - 현실 1초 → 게임에서 몇 초가 흐를지 배율을 public으로 노출
    /// - 일시정지/배속 변경/저장·불러오기/분·시·일 경계 이벤트 제공
    /// 
    /// 설계 메모(유지보수/퍼포먼스):
    /// - Update 1회당 누적만 처리해 GC 최소화(박스화/임시할당 없음)
    /// - double 누적로스 방지: 게임 초 누적은 double, DateTime 변환은 필요 시에만
    /// - Time.deltaTime vs unscaledDeltaTime 선택 지원
    /// - Time.timeScale은 건드리지 않음(전역 타임스케일과 독립 운영)
    /// </summary>
    [DisallowMultipleComponent]
    public class GameTimeManager : MonoBehaviour, ISimulationTimeProvider
    {
        private float _gameSecondsPerRealSecond = 60f;
        private DateTime _timeByMorning;
        private bool _isPaused;

        // 내부 누적
        private double _accumGameSeconds;        // 시작 기준 게임 '초' 누적치
        private DateTime _startDateTime;         // 기준 시각
        private DateTime _currentDateTimeCache;  // 마지막 계산된 캐시(필요 시에만 갱신)
        private bool _dirtyDateTime = true;      // 캐시 무효 플래그

        // 이벤트용 경계 누적(너무 잦은 호출 방지)
        private double _sinceLastMinuteEvent;
        private double _sinceLastHourEvent;
        private double _sinceLastDayEvent;
        // UI 주기 이벤트 누적(설정의 minMinutePerUIUpdateEvent를 초로 환산)
        private double _sinceLastUiUpdate;

        private GGemCoGameTimeSettings _settings;
        private GameTimeData _gameTimeData;
        // GameTimeManager 내부에 상수 추가
        private const double SecondsPerDay = 86400.0;
        
        // 경계 교차 탐지는 간소화를 위해 캐시만 사용(정확 경계 필요 시 이전값 저장해 비교하는 방식으로 확장)
        private int _lastMinute = -1, _lastHour = -1, _lastDay = -1; // 월/년은 일 변화에서 암시적 처리

        // 추가: 월 경계 추적
        private int _lastMonth = -1;

        #region 이벤트
        /// <summary>프레임에서 진행된 '게임 초'를 알림(deltaSeconds, now).</summary>
        public event Action<double, DateTime> OnTimeAdvanced;

        /// <summary>분 경계(또는 minSecondsPerMinuteEvent 누적 이상) 통지.</summary>
        public event Action<DateTime> OnMinuteChanged;

        /// <summary>시 경계(또는 minSecondsPerHourEvent 누적 이상) 통지.</summary>
        public event Action<DateTime> OnHourChanged;

        /// <summary>일 경계(또는 minSecondsPerDayEvent 누적 이상) 통지.</summary>
        public event Action<DateTime> OnDayChanged;
        /// <summary>월 경계(또는 minSecondsPerDayEvent 누적 이상) 통지.</summary>
        public event Action<DateTime> OnMonthChanged;

        /// <summary>일시정지 토글 통지.</summary>
        public event Action<bool> OnPauseChanged;

        /// <summary>배속 변경 통지.</summary>
        public event Action<float> OnSpeedChanged;
        /// <summary>UI 텍스트 갱신용 주기 이벤트(설정의 '게임 분' 주기 기반).</summary>
        public static event Action<DateTime> OnUiUpdateInterval;
        #endregion

        #region 수명주기
        private void Awake()
        {
            // _isPaused = _settings.startPaused;
            // 정지 시작. 타일맵이 로드가 완료되면 시작. BootstrapperMap 에서 처리
            _isPaused = true;
            _settings = AddressableLoaderSettings.Instance.gameTimeSettings;
            if (_settings)
            {
                _gameSecondsPerRealSecond = _settings.gameSecondsPerRealSecond;
                if (!string.IsNullOrEmpty(_settings.timeByMorning))
                {
                    _timeByMorning = DateTime.Parse(_settings.timeByMorning);
                }
            }
            InitializeFromSettings();
        }

        private void Start()
        {
            if (!SceneGame.Instance)
            {
                GcLogger.LogError($"SceneGame.Instance가 없습니다.");
                return;
            }
            _gameTimeData = SceneGame.Instance.saveDataManager.GameTime;
            if (_gameTimeData != null)
            {
                AdvanceSeconds(_gameTimeData.CurrentGameTime);
            }
        }

        private void Update()
        {
            if (_isPaused || _gameSecondsPerRealSecond <= 0f)
                return;

            float dt = (_settings != null && _settings.useUnscaledTime) ? Time.unscaledDeltaTime : Time.deltaTime;
            double deltaGameSec = dt * _gameSecondsPerRealSecond;

            if (deltaGameSec <= 0d) return;

            _accumGameSeconds += deltaGameSec;
            _dirtyDateTime = true;

            // 이벤트 누적
            _sinceLastMinuteEvent += deltaGameSec;
            _sinceLastHourEvent   += deltaGameSec;
            _sinceLastDayEvent    += deltaGameSec;
            _sinceLastUiUpdate     += deltaGameSec;

            // 콜백(프레임 단위)
            OnTimeAdvanced?.Invoke(deltaGameSec, Now);

            // 경계/스로틀 이벤트
            TryFireThresholdEvents();
        }
        #endregion

        #region 초기화 & 설정
        /// <summary>설정 자산으로 초기화(없으면 합리적 기본값 사용).</summary>
        private void InitializeFromSettings()
        {
            if (_settings != null)
            {
                _gameSecondsPerRealSecond = Mathf.Max(0f, _settings.gameSecondsPerRealSecond);
                var dt = new DateTime(1, 1, 1, 0, 0, 0, DateTimeKind.Unspecified);
                if (!string.IsNullOrEmpty(_settings.startGameDate))
                {
                    dt = DateTime.Parse(_settings.startGameDate);
                }
                _startDateTime = SanitizeStartDate(dt);
            }
            else
            {
                GcLogger.LogError($"{nameof(GGemCoSettings)} 스크립터블 오브젝트가 없습니다.");
                _gameSecondsPerRealSecond = Mathf.Max(0f, _gameSecondsPerRealSecond <= 0f ? 60f : _gameSecondsPerRealSecond);
                _startDateTime = new DateTime(1,1,1,0,0,0, DateTimeKind.Unspecified);
            }

            _accumGameSeconds = 0d;
            _sinceLastMinuteEvent = _sinceLastHourEvent = _sinceLastDayEvent = 0d;
            _sinceLastUiUpdate = 0d;
            
            // 경계 캐시 초기화
            _lastMinute = _lastHour = _lastDay = _lastMonth = -1;
            _dirtyDateTime = true;
        }

        private static DateTime SanitizeStartDate(DateTime dt)
        {
            // DateTimeKind는 게임 내부 로직에서 크게 의미 없으므로 Unspecified 권장
            if (dt.Kind != DateTimeKind.Unspecified)
                dt = DateTime.SpecifyKind(dt, DateTimeKind.Unspecified);
            return dt;
        }
        #endregion

        #region 공개 API
        /// <summary>현재 게임 시각(DateTime). 내부 캐시 사용.</summary>
        public DateTime Now
        {
            get
            {
                if (_dirtyDateTime)
                {
                    _currentDateTimeCache = _startDateTime.AddSeconds(_accumGameSeconds);
                    _dirtyDateTime = false;
                }
                return _currentDateTimeCache;
            }
        }

        public double NowSeconds() => _accumGameSeconds;

        /// <summary>외부에서 게임 시간을 특정 시각으로 맞춥니다.</summary>
        public void SetNow(DateTime newNow)
        {
            newNow = SanitizeStartDate(newNow);
            _startDateTime = newNow;
            _accumGameSeconds = 0d;
            _sinceLastMinuteEvent = _sinceLastHourEvent = _sinceLastDayEvent = 0d;
            // 경계 캐시 초기화
            _lastMinute = _lastHour = _lastDay = _lastMonth = -1;
            _sinceLastUiUpdate = 0d;
            _dirtyDateTime = true;
        }

        /// <summary>게임 시간을 '게임 초' 단위로 즉시 진행(프레임과 무관).</summary>
        public void AdvanceSeconds(double gameSeconds)
        {
            if (gameSeconds <= 0d) return;
            _accumGameSeconds += gameSeconds;
            _dirtyDateTime = true;

            _sinceLastMinuteEvent += gameSeconds;
            _sinceLastHourEvent   += gameSeconds;
            _sinceLastDayEvent    += gameSeconds;
            _sinceLastUiUpdate    += gameSeconds;

            OnTimeAdvanced?.Invoke(gameSeconds, Now);
            TryFireThresholdEvents();
        }

        /// <summary>일시정지/해제.</summary>
        public void SetPause(bool pause)
        {
            if (_isPaused == pause) return;
            _isPaused = pause;
            OnPauseChanged?.Invoke(_isPaused);
        }

        /// <summary>배속 변경: 현실 1초당 게임 초.</summary>
        public void SetSpeed(float newGameSecondsPerRealSecond)
        {
            newGameSecondsPerRealSecond = Mathf.Max(0f, newGameSecondsPerRealSecond);
            if (Mathf.Approximately(_gameSecondsPerRealSecond, newGameSecondsPerRealSecond)) return;
            _gameSecondsPerRealSecond = newGameSecondsPerRealSecond;
            OnSpeedChanged?.Invoke(_gameSecondsPerRealSecond);
        }

        /// <summary>저장용 ISO 문자열(현 시각 기준) 반환.</summary>
        public string GetSaveStringIso() => Now.ToString("yyyy-MM-ddTHH:mm:ss");

        /// <summary>저장 문자열(ISO)로부터 시간 복구.</summary>
        public bool TryLoadFromIso(string iso)
        {
            if (DateTime.TryParse(iso, out var parsed))
            {
                SetNow(parsed);
                return true;
            }
            return false;
        }
        // "정확도용" 실수 반환: 시작 이후 경과 '게임 일' (소수 포함)
        public double GetElapsedDaysExact()
        {
            return _accumGameSeconds / SecondsPerDay;
        }

        // "표시용" 정수 반환: 게임 시작 후 몇 '일째'인지 (Day 1부터 시작)
        public int GetDayNumber()
        {
            // 0초~<86400초: Day 1, 86400초~<172800초: Day 2 ...
            return (int)(_accumGameSeconds / SecondsPerDay) + 1;
        }
        /// <summary>
        /// 다음 날 아침 시작 시간으로 변경하기. 예) 잠자기
        /// </summary>
        public void SetNextDay()
        {
            // 설정이 없다면 기본 06:00:00 사용(안전장치)
            var morningTod = (_timeByMorning != default)
                ? _timeByMorning.TimeOfDay
                : new TimeSpan(6, 0, 0);

            // 다음날 날짜 + 아침 시각
            DateTime target = Now.Date.AddDays(1).Add(morningTod);

            double deltaSeconds = (target - Now).TotalSeconds;
            if (deltaSeconds > 0d)
            {
                AdvanceSeconds(deltaSeconds); // 이벤트/경계 처리 일괄 적용
            }
        }

        public string GetNowDateString()
        {
            return Now.ToString("yyyy-MM-dd");
        }
        #endregion

        #region 내부: 이벤트 스로틀/경계
        private void TryFireThresholdEvents()
        {
            // 설정 없을 수도 있어 null 체크
            float minMin = _settings ? _settings.minSecondsPerMinuteEvent : 1f;
            float minHr  = _settings ? _settings.minSecondsPerHourEvent  : 10f;
            float minDay = _settings ? _settings.minSecondsPerDayEvent   : 60f;

            // 분/시/일 경과 임계 + 실제 캘린더 경계 교차 시 두 조건 중 하나라도 충족하면 호출
            if (_sinceLastMinuteEvent >= minMin || HasCrossedMinute(Now))
            {
                _sinceLastMinuteEvent = 0d;
                OnMinuteChanged?.Invoke(Now);
            }

            if (_sinceLastHourEvent >= minHr || HasCrossedHour(Now))
            {
                _sinceLastHourEvent = 0d;
                OnHourChanged?.Invoke(Now);
            }

            if (_sinceLastDayEvent >= minDay || HasCrossedDay(Now))
            {
                _sinceLastDayEvent = 0d;
                OnDayChanged?.Invoke(Now);
            }
            
            // 월 경계 이벤트 (스로틀 없이 경계 교차만 감지)
            if (HasCrossedMonth(Now))
            {
                OnMonthChanged?.Invoke(Now);
            }
            
            // --- UI 업데이트 주기 이벤트 (게임 '분' → '초'로 환산) ---
            if (_settings != null && _settings.minMinutePerUIUpdateEvent > 0f && OnUiUpdateInterval != null)
            {
                double uiThresholdSec = _settings.minMinutePerUIUpdateEvent * 60.0;
                if (_sinceLastUiUpdate >= uiThresholdSec)
                {
                    _sinceLastUiUpdate = 0d;
                    OnUiUpdateInterval.Invoke(Now);
                }
            }
        }

        private bool HasCrossedMinute(DateTime now)
        {
            if (_lastMinute != now.Minute)
            {
                _lastMinute = now.Minute;
                return true;
            }
            return false;
        }
        private bool HasCrossedHour(DateTime now)
        {
            if (_lastHour != now.Hour)
            {
                _lastHour = now.Hour;
                return true;
            }
            return false;
        }
        private bool HasCrossedDay(DateTime now)
        {
            if (_lastDay != now.Day)
            {
                _lastDay = now.Day;
                return true;
            }
            return false;
        }
        private bool HasCrossedMonth(DateTime now)
        {
            if (_lastMonth != now.Month)
            {
                _lastMonth = now.Month;
                return true;
            }
            return false;
        }
        #endregion
    }

    /// <summary>주입/테스트를 위한 시간 공급 인터페이스.</summary>
    public interface ISimulationTimeProvider
    {
        DateTime Now { get; }
        void SetNow(DateTime dt);
        void AdvanceSeconds(double gameSeconds);
        void SetPause(bool pause);
        void SetSpeed(float gameSecondsPerRealSecond);
        string GetSaveStringIso();
        bool TryLoadFromIso(string iso);
        event Action<double, DateTime> OnTimeAdvanced;
        event Action<DateTime> OnMinuteChanged;
        event Action<DateTime> OnHourChanged;
        event Action<DateTime> OnDayChanged;
        event Action<DateTime> OnMonthChanged;
        event Action<bool> OnPauseChanged;
        event Action<float> OnSpeedChanged;
        double GetElapsedDaysExact();
        int GetDayNumber();
    }
}
