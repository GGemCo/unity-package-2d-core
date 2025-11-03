using System;
using UnityEngine;

namespace GGemCo2DCore
{
    /// <summary>
    /// GGemCo 게임 시간 설정
    /// </summary>
    [CreateAssetMenu(fileName = ConfigScriptableObject.GameTime.FileName, menuName = ConfigScriptableObject.GameTime.MenuName, order = ConfigScriptableObject.GameTime.Ordering)]
    public class GGemCoGameTimeSettings : ScriptableObject
    {
        [Header("인 게임 시간")] 
        // [Tooltip("시작 시 일시정지 여부")]
        // public bool startPaused = false;
        
        [Tooltip("현실 1초에 게임에서 흐르는 '게임 초'의 양. 예) 60 이면 현실 1초당 게임 60초(= 1분).")]
        [Min(0f)] public float gameSecondsPerRealSecond;

        [Tooltip("게임 시작 시점(달력 기준). 저장/불러오기 전에 초기값으로 사용. YYYY-MM-DD")]
        public string startGameDate;
        [Tooltip("잠자기 한 후 시작 시간")]
        public string timeByMorning;

        [Header("업데이트 옵션")]
        [Tooltip("true면 Time.unscaledDeltaTime 사용(글로벌 일시정지 등과 무관하게 흐름). false면 Time.deltaTime 사용.")]
        public bool useUnscaledTime;

        [Header("이벤트 티클(선택)")]
        [Tooltip("OnMinuteChanged 이벤트가 최소 몇 '게임 초' 누적마다 발생할지의 하한. 너무 낮으면 이벤트 과다 호출.")]
        [Range(0.1f, 60f)]
        public float minSecondsPerMinuteEvent;

        [Tooltip("OnHourChanged 이벤트 발생 하한(게임 초 누적 기준).")]
        [Range(1f, 3600f)]
        public float minSecondsPerHourEvent;

        [Tooltip("OnDayChanged 이벤트 발생 하한(게임 초 누적 기준).")]
        [Range(10f, 86400f)]
        public float minSecondsPerDayEvent;
        [Tooltip("날짜/시각 UI 텍스트를 갱신하는 주기(단위: '게임 분'). 0 또는 음수면 비활성화.")]
        [Range(0f, 1440f)]
        public float minMinutePerUIUpdateEvent;
        // 추가: 달(1~12) → 계절 매핑
        [Header("계절 by Month (1~12)")]
        [Tooltip("인덱스 0은 사용하지 않음 (1=1월 ... 12=12월)")]
        public ConfigCommon.ClimateId[] climateByMonth = new ConfigCommon.ClimateId[13]
        {
            ConfigCommon.ClimateId.Spring, // [0] dummy
            ConfigCommon.ClimateId.Winter, // 1월
            ConfigCommon.ClimateId.Winter, // 2월
            ConfigCommon.ClimateId.Spring, // 3월
            ConfigCommon.ClimateId.Spring, // 4월
            ConfigCommon.ClimateId.Spring, // 5월
            ConfigCommon.ClimateId.Summer, // 6월
            ConfigCommon.ClimateId.Summer, // 7월
            ConfigCommon.ClimateId.Summer, // 8월
            ConfigCommon.ClimateId.Autumn, // 9월
            ConfigCommon.ClimateId.Autumn, // 10월
            ConfigCommon.ClimateId.Autumn, // 11월
            ConfigCommon.ClimateId.Winter  // 12월
        };

        /// <summary>
        /// 기존 값이 비어있을 때만 기본값을 설정
        /// </summary>
        private void OnEnable()
        {
        }

        /// <summary>
        /// 처음 생성 시 한 번만 실행됨
        /// </summary>
        private void Reset()
        {
            // startPaused = false;
            gameSecondsPerRealSecond = 60f;
            startGameDate = "2000-01-01";
            useUnscaledTime = true;

            minSecondsPerMinuteEvent = 1f;
            minSecondsPerHourEvent = 10f;
            minSecondsPerDayEvent = 60f;
            minMinutePerUIUpdateEvent = 1f;
        }
    }
}
