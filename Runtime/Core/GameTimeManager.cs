using System;
using UnityEngine;

namespace GGemCo2DCore
{
    /// <summary>
    /// 게임 시간 매니저
    /// - 현실 초를 '게임 초'로 확장/축소해서 관리
    /// - 고정 TicksPerSecond로 틱 이벤트 발행 (퍼포먼스/동기화 안정)
    /// - 일시정지, 배속 변경, 경과 게임 시간 조회 제공
    /// </summary>
    [DefaultExecutionOrder(-1000)]
    [DisallowMultipleComponent]
    public sealed class GameTimeManager : MonoBehaviour
    {
        [Header("시간 스케일")]
        [Tooltip("현실 1초가 몇 '게임 초'인지 (예: 60이면 현실 1초당 게임 60초가 흐름)")]
        [Min(0f)] public float gameSecondsPerRealSecond = 1f;

        [Header("틱 설정")]
        [Tooltip("초당 발행되는 게임 틱 수. 너무 높으면 오버헤드가 증가합니다.")]
        [Range(1, 60)] public int ticksPerSecond = 5;

        [Header("상태")]
        [Tooltip("시작 시 일시정지 여부")]
        public bool startPaused = false;

        /// <summary>현재 일시정지 여부</summary>
        public bool IsPaused { get; private set; }

        /// <summary>게임 틱(증가만) - 모든 시스템의 공통 기준 시계</summary>
        public long NowTick { get; private set; }

        /// <summary>총 경과 '게임 초'(정수)</summary>
        public int TotalGameSeconds { get; private set; }

        /// <summary>틱 이벤트 (NowTick 전달)</summary>
        public event Action<long> OnTick;

        /// <summary>정수 '게임 초'가 증가할 때마다 호출 (누적 초 전달)</summary>
        public event Action<int> OnGameSecond;

        private float _accumReal;       // 현실 시간 누적
        private float _accumGame;       // 게임 시간 누적(초)
        private float _tickIntervalGame; // 게임 시간 기준 한 틱 길이(초)

        private void Awake()
        {
            IsPaused = startPaused;
            RecalcTickInterval();
        }

        private void Update()
        {
            if (IsPaused || gameSecondsPerRealSecond <= 0f || ticksPerSecond <= 0)
                return;

            // 현실 시간 → 게임 시간 누적
            _accumReal += Time.unscaledDeltaTime;
            float deltaGame = _accumReal * gameSecondsPerRealSecond;
            _accumReal = 0f;

            _accumGame += deltaGame;

            // 틱 발행: 게임 시간 기준 고정 간격
            while (_accumGame >= _tickIntervalGame)
            {
                _accumGame -= _tickIntervalGame;
                NowTick++;
                OnTick?.Invoke(NowTick);
            }

            // 정수 게임 초 이벤트 (바닥 함수)
            int newTotal = Mathf.FloorToInt(TotalGameSeconds + deltaGame);
            if (newTotal > TotalGameSeconds)
            {
                TotalGameSeconds = newTotal;
                OnGameSecond?.Invoke(TotalGameSeconds);
            }
        }

        /// <summary>일시정지/재개</summary>
        public void SetPaused(bool paused) => IsPaused = paused;

        /// <summary>배속 변경(현실 1초 → 게임 n초)</summary>
        public void SetGameSecondsPerRealSecond(float value)
        {
            gameSecondsPerRealSecond = Mathf.Max(0f, value);
        }

        /// <summary>초당 틱 수 변경</summary>
        public void SetTicksPerSecond(int value)
        {
            ticksPerSecond = Mathf.Clamp(value, 1, 60);
            RecalcTickInterval();
        }

        /// <summary>게임 초 → 틱 단위 변환</summary>
        public long SecondsToTicks(float seconds)
        {
            if (ticksPerSecond <= 0) return 0;
            return Mathf.RoundToInt(seconds * ticksPerSecond);
        }

        private void RecalcTickInterval()
        {
            _tickIntervalGame = 1f / Mathf.Max(1, ticksPerSecond);
        }
    }
}
