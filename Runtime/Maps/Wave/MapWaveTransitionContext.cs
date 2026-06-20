using UnityEngine;

namespace GGemCo2DCore
{
    /// <summary>
    /// 웨이브 그룹 전환이 요청된 시점의 범용 컨텍스트입니다.
    /// 상위 게임 계층은 이 데이터를 사용해 이동 유도, UI 안내 등 프로젝트 전용 연출을 결정할 수 있습니다.
    /// </summary>
    public sealed class MapWaveTransitionContext
    {
        /// <summary>웨이브 시나리오 UID입니다.</summary>
        public int ScenarioUid { get; }

        /// <summary>전환 기준이 된 이전 그룹 UID입니다.</summary>
        public int PreviousGroupUid { get; }

        /// <summary>다음에 실행될 그룹 UID입니다. 다음 그룹이 없으면 0입니다.</summary>
        public int NextGroupUid { get; }

        /// <summary>이전 그룹의 다음 전환 정책입니다.</summary>
        public WaveNextPolicy NextPolicy { get; }

        /// <summary>다음 그룹 전환을 요청한 원인입니다.</summary>
        public WaveNextTriggerReason TriggerReason { get; }

        /// <summary>전환 조건 충족 후 다음 그룹 시작 전까지의 지연 시간입니다.</summary>
        public float NextDelaySeconds { get; }

        /// <summary>다음 그룹의 이동 유도 기준 위치를 계산했는지 여부입니다.</summary>
        public bool HasNavigationPosition { get; }

        /// <summary>다음 그룹의 이동 유도 기준 위치입니다.</summary>
        public Vector3 NavigationPosition { get; }

        /// <summary>
        /// 웨이브 그룹 전환 컨텍스트를 생성합니다.
        /// </summary>
        /// <param name="scenarioUid">웨이브 시나리오 UID입니다.</param>
        /// <param name="previousGroupUid">이전 그룹 UID입니다.</param>
        /// <param name="nextGroupUid">다음 그룹 UID입니다.</param>
        /// <param name="nextPolicy">이전 그룹의 다음 전환 정책입니다.</param>
        /// <param name="triggerReason">전환 요청 원인입니다.</param>
        /// <param name="nextDelaySeconds">다음 그룹 시작 전 지연 시간입니다.</param>
        /// <param name="hasNavigationPosition">이동 유도 기준 위치 보유 여부입니다.</param>
        /// <param name="navigationPosition">이동 유도 기준 위치입니다.</param>
        public MapWaveTransitionContext(
            int scenarioUid,
            int previousGroupUid,
            int nextGroupUid,
            WaveNextPolicy nextPolicy,
            WaveNextTriggerReason triggerReason,
            float nextDelaySeconds,
            bool hasNavigationPosition,
            Vector3 navigationPosition)
        {
            ScenarioUid = scenarioUid;
            PreviousGroupUid = previousGroupUid;
            NextGroupUid = nextGroupUid;
            NextPolicy = nextPolicy;
            TriggerReason = triggerReason;
            NextDelaySeconds = Mathf.Max(0f, nextDelaySeconds);
            HasNavigationPosition = hasNavigationPosition;
            NavigationPosition = navigationPosition;
        }
    }
}
