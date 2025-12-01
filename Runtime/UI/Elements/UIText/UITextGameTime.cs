using System;
using TMPro;
using UnityEngine;

namespace GGemCo2DCore
{
    /// <summary>
    /// 게임 시간 관련 UI 텍스트(시:분, 날짜, 요일, 계절, 일차)를 단일 스크립트로 처리합니다.
    /// - 타입별로 필요한 이벤트만 구독합니다.
    /// - ServiceLocator(IGameTimeProvider)로 의존성을 분리하여 씬/싱글톤 초기화 순서 의존을 제거합니다.
    /// - 동일 문자열 스킵과 TMP.SetText 사용으로 GC/오버헤드를 최소화합니다.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class UITextGameTime : MonoBehaviour
    {
        /// <summary>
        /// 표시 타입(시:분/날짜/요일/계절/일차)을 정의합니다.
        /// 내부 enum으로 유지하여 인스펙터 노출은 필드(displayType)만 사용합니다.
        /// </summary>
        private enum DisplayType { Time, Date, Week, Climate, DayNumber }

        [Header("Display")]
        [SerializeField] private DisplayType displayType = DisplayType.Time;

        [Tooltip("Clock/Date 표시용 .NET 날짜 포맷 문자열")]
        [SerializeField] private string format = "HH:mm"; // Clock 기본값

        [Tooltip("요일/계절의 축약형 사용 여부 (예: Mon / Spr 등)")]
        [SerializeField] private bool useShort = false;

        // Localization 키 접두/템플릿(프로젝트 공통 상수에서 가져옴)
        private string _weekBaseKey;
        private string _climateBaseKey;
        private string _dayNumberKey;

        // 대상 TMP 텍스트 및 월→계절 매핑 설정(필요 시 사용)
        private TMP_Text _targetText;
        private GGemCoGameTimeSettings _timeSettings;

        // 동일 문자열이면 갱신 생략(알파/배치/할당 최소화)
        private const bool SkipIfSame = true;

        // DI로 주입받는 시간 공급자 및 내부 상태
        private IGameTimeProvider _time;
        private string _last;
        private bool _subscribed;

        /// <summary>
        /// 초기화 단계:
        /// - 로컬 컴포넌트 캐싱만 수행합니다(외부 서비스 접근 금지).
        /// - Localization 키 접두/템플릿을 프로젝트 상수에서 로드합니다.
        /// </summary>
        private void Awake()
        {
            if (_targetText == null) _targetText = GetComponent<TMP_Text>();
            if (_targetText == null) { enabled = false; return; }

            // 프로젝트 공통 Localization 키 상수에서 접두/템플릿 로드
            _weekBaseKey    = LocalizationConstants.Keys.Date.Week();
            _climateBaseKey = LocalizationConstants.Keys.Date.Climate();
            _dayNumberKey   = LocalizationConstants.Keys.Date.Day();
        }

        /// <summary>
        /// 활성화 시점:
        /// - ServiceLocator에서 IGameTimeProvider가 이미 등록되어 있으면 즉시 바인딩합니다.
        /// - 아직 없으면 OnServiceRegistered에 구독하여 지연 바인딩합니다.
        /// </summary>
        private void OnEnable()
        {
            if (ServiceLocator.TryResolve<IGameTimeProvider>(out var prov)) Bind(prov);
            else ServiceLocator.OnServiceRegistered += HandleServiceRegistered;
        }

        /// <summary>
        /// 비활성화 시점:
        /// - 모든 외부 이벤트 구독을 해제하고, 지연 바인딩 대기를 취소합니다.
        /// </summary>
        private void OnDisable()
        {
            Unbind();
            ServiceLocator.OnServiceRegistered -= HandleServiceRegistered;
        }

        /// <summary>
        /// 파괴 시점:
        /// - 비활성화와 동일하게 정리(중복 호출에도 안전).
        /// </summary>
        private void OnDestroy()
        {
            Unbind();
            ServiceLocator.OnServiceRegistered -= HandleServiceRegistered;
        }

        /// <summary>
        /// ServiceLocator에 새로운 서비스가 등록되었을 때 호출됩니다.
        /// - 등록 타입이 IGameTimeProvider일 때만 바인딩합니다.
        /// </summary>
        private void HandleServiceRegistered(Type t, object obj)
        {
            if (t == typeof(IGameTimeProvider) && obj is IGameTimeProvider p)
            {
                ServiceLocator.OnServiceRegistered -= HandleServiceRegistered;
                Bind(p);
            }
        }

        /// <summary>
        /// 시간 공급자 바인딩:
        /// - 타입별 필요한 이벤트를 구독하고, 초기 1회 렌더를 수행합니다.
        /// - 중복 바인딩을 방지합니다.
        /// </summary>
        /// <param name="prov">IGameTimeProvider 구현체</param>
        private void Bind(IGameTimeProvider prov)
        {
            if (prov == null || _subscribed) return;
            _time = prov;

            // 타입별로 필요한 이벤트만 구독(불필요한 업데이트 최소화)
            SubscribeByType(displayType, _time);
            _subscribed = true;

            // 즉시 1회 렌더(초기 화면 공백 방지)
            TryRenderNow();
        }

        /// <summary>
        /// 시간 공급자 연결 해제:
        /// - 타입별로 구독한 이벤트를 해제합니다.
        /// - 내부 참조를 정리합니다.
        /// </summary>
        private void Unbind()
        {
            if (_subscribed)
            {
                UnsubscribeByType(displayType, _time);
                _subscribed = false;
            }
            _time = null;
        }

        /// <summary>
        /// 표시 타입에 따라 적절한 이벤트를 구독합니다.
        /// - Time/Date: UI 주기 갱신(분/초 단위)
        /// - Week/DayNumber: 일 변경 시
        /// - Climate: 월 변경 시
        /// </summary>
        private void SubscribeByType(DisplayType type, IGameTimeProvider p)
        {
            switch (type)
            {
                case DisplayType.Time:
                case DisplayType.Date:
                    p.OnUiUpdateInterval += OnTick;  // 짧은 주기(분/초)로 갱신
                    break;
                case DisplayType.Week:
                case DisplayType.DayNumber:
                    p.OnDayChanged += OnTick;       // 일 경계에서만 갱신
                    break;
                case DisplayType.Climate:
                    p.OnMonthChanged += OnTick;     // 월 경계에서만 갱신
                    break;
            }
        }

        /// <summary>
        /// 표시 타입에 따라 구독한 이벤트를 해제합니다.
        /// - OnDisable/OnDestroy에서 호출됩니다.
        /// </summary>
        private void UnsubscribeByType(DisplayType type, IGameTimeProvider p)
        {
            if (p == null) return;
            switch (type)
            {
                case DisplayType.Time:
                case DisplayType.Date:
                    p.OnUiUpdateInterval -= OnTick;
                    break;
                case DisplayType.Week:
                case DisplayType.DayNumber:
                    p.OnDayChanged -= OnTick;
                    break;
                case DisplayType.Climate:
                    p.OnMonthChanged -= OnTick;
                    break;
            }
        }

        /// <summary>
        /// 시간 공급자로부터 전달받는 이벤트 콜백입니다.
        /// - 현재 시각(now)을 받아 렌더 시도 후, 텍스트를 갱신합니다.
        /// - 동일 문자열이면 갱신을 생략합니다.
        /// </summary>
        private void OnTick(DateTime now)
        {
            if (TryRender(now, out var s)) SetTextIfChanged(s);
        }

        /// <summary>
        /// 초기 바인딩 직후 1회 렌더를 수행합니다.
        /// - 공급자의 Now가 준비되지 않은 초기 프레임에서는 예외를 무시합니다.
        /// </summary>
        private void TryRenderNow()
        {
            try
            {
                if (_time != null && TryRender(_time.Now, out var s))
                    SetTextIfChanged(s);
            }
            catch
            {
                // 초기 프레임 타이밍 이슈 시 무시(다음 이벤트에서 정상 갱신)
            }
        }

        /// <summary>
        /// 텍스트가 이전과 다를 때에만 TMP.SetText를 호출합니다.
        /// - 불필요한 호출/할당/배치를 줄여 UI 성능을 개선합니다.
        /// </summary>
        private void SetTextIfChanged(string s)
        {
            if (SkipIfSame && _last == s) return; // 동일 문자열 스킵
            _targetText.SetText(s);
            _last = s;
        }

        /// <summary>
        /// 표시 타입(displayType)에 따라 문자열을 생성합니다.
        /// - Time/Date: .NET 포맷 문자열로 출력
        /// - Week/Climate: Localization 키 조립 후 로드
        /// - DayNumber: 로컬라이즈 템플릿("{0} 일차"/"Day {0}")에 값 바인딩
        /// </summary>
        /// <param name="now">현재 게임 시각</param>
        /// <param name="result">렌더 결과 문자열</param>
        /// <returns>성공 여부</returns>
        private bool TryRender(DateTime now, out string result)
        {
            switch (displayType)
            {
                case DisplayType.Time:
                    // 시간(시:분) - 기본 "HH:mm"
                    result = now.ToString(string.IsNullOrEmpty(format) ? "HH:mm" : format);
                    return true;

                case DisplayType.Date:
                    // 날짜(기본 "yyyy-MM-dd")
                    result = now.ToString(string.IsNullOrEmpty(format) ? "yyyy-MM-dd" : format);
                    return true;

                case DisplayType.Week:
                    // 요일 키: ui.date.week_monday / ui.date.week_monday_short
                    result = LocalizationFacade.Get(BuildWeekKey(_weekBaseKey, now.DayOfWeek, useShort));
                    return true;

                case DisplayType.Climate:
                    // 월→계절 매핑 후 키: ui.date.climate_spring / ui.date.climate_spring_short
                    var climate = ResolveClimate(now);
                    result = LocalizationFacade.Get(BuildClimateKey(_climateBaseKey, climate, useShort));
                    return true;

                case DisplayType.DayNumber:
                    // "{0} 일차" / "Day {0}" 템플릿에 경과일 삽입
                    var day = _time?.GetDayNumber() ?? 0;
                    var tpl = LocalizationFacade.Get(string.IsNullOrEmpty(_dayNumberKey)
                        ? "ui.date.daynumber"
                        : _dayNumberKey);
                    result = string.Format(tpl, day);
                    return true;
            }

            result = null;
            return false;
        }

        /// <summary>
        /// 요일 로컬라이즈 키를 생성합니다.
        /// - 예: baseKey=ui.date.week, Monday → "ui.date.week_monday" 또는 "_short"
        /// </summary>
        private static string BuildWeekKey(string baseKey, DayOfWeek d, bool shortForm)
        {
            var suffix = d.ToString().ToLower(); // monday, tuesday ...
            return shortForm ? $"{baseKey}_{suffix}_short" : $"{baseKey}_{suffix}";
        }

        /// <summary>
        /// 계절 로컬라이즈 키를 생성합니다.
        /// - 예: baseKey=ui.date.climate, Spring → "ui.date.climate_spring" 또는 "_short"
        /// </summary>
        private static string BuildClimateKey(string baseKey, ConfigCommon.ClimateId c, bool shortForm)
        {
            var suffix = c.ToString().ToLower(); // spring/summer/autumn/winter 등
            return shortForm ? $"{baseKey}_{suffix}_short" : $"{baseKey}_{suffix}";
        }

        /// <summary>
        /// 현재 월을 기반으로 계절을 해석합니다.
        /// - 설정(timeSettings)에 월→계절 테이블이 있으면 우선 사용합니다.
        /// - 없으면 안전 기본값(Spring)으로 대체합니다.
        /// </summary>
        private ConfigCommon.ClimateId ResolveClimate(DateTime now)
        {
            int m = Mathf.Clamp(now.Month, 1, 12);
            if (_timeSettings != null && _timeSettings.climateByMonth is { Length: >= 13 })
                return _timeSettings.climateByMonth[m];
            return ConfigCommon.ClimateId.Spring;
        }
    }

    /// <summary>
    /// Localization 접근을 캡슐화하는 퍼사드입니다.
    /// - 현재 커스텀 LocalizationManager를 위임 호출합니다.
    /// - 추후 Unity Localization Tables 사용 시 이 퍼사드만 교체하면 됩니다.
    /// </summary>
    internal static class LocalizationFacade
    {
        /// <summary>
        /// 지정된 키로 로컬라이즈된 문자열을 조회합니다.
        /// - 키 미존재 시 내부 매니저 정책(키 반환/빈 문자열/로그 경고)에 따릅니다.
        /// </summary>
        public static string Get(string key)
        {
            // 프로젝트 공통 LocalizationManager로 위임
            return LocalizationManager.Instance.GetCommonUIByKey(key);
        }
    }
}
