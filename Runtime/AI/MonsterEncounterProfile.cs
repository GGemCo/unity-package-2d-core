using UnityEngine;

namespace GGemCo2DCore
{
    /// <summary>
    /// Encounter 그룹 활성화와 동료 지원 어그로 정책을 런타임용으로 정규화한 불변 프로필입니다.
    /// </summary>
    public readonly struct MonsterEncounterProfile
    {
        private const float DefaultEncounterThreat = 1f;
        private const int MaximumAssistCount = 32;

        /// <summary>Encounter 볼륨 또는 동료 지원으로 등록할 Threat입니다.</summary>
        public float EncounterThreat { get; }

        /// <summary>같은 Encounter 그룹에 지원 어그로를 전달할 최대 거리입니다. 0 이하면 거리 제한을 사용하지 않습니다.</summary>
        public float AssistRadius { get; }

        /// <summary>한 번의 지원 요청으로 활성화할 최대 동료 수입니다. 0 이하면 제한하지 않습니다.</summary>
        public int MaxAssistCount { get; }

        private MonsterEncounterProfile(float encounterThreat, float assistRadius, int maxAssistCount)
        {
            EncounterThreat = encounterThreat;
            AssistRadius = assistRadius;
            MaxAssistCount = maxAssistCount;
        }

        /// <summary>
        /// monster_combat_profile 테이블 데이터에서 Encounter 정책을 생성합니다.
        /// </summary>
        /// <param name="tableData">선택한 몬스터 전투 프로필 테이블 행입니다.</param>
        /// <returns>정규화된 Encounter 프로필입니다.</returns>
        public static MonsterEncounterProfile Create(StruckTableMonsterCombatProfile tableData)
        {
            float threat = tableData != null && tableData.EncounterThreat > 0f
                ? tableData.EncounterThreat
                : DefaultEncounterThreat;
            float assistRadius = tableData != null
                ? Mathf.Max(0f, tableData.EncounterAssistRadius)
                : 0f;
            int maxAssistCount = tableData != null && tableData.MaxEncounterAssistCount > 0
                ? Mathf.Clamp(tableData.MaxEncounterAssistCount, 1, MaximumAssistCount)
                : 0;

            return new MonsterEncounterProfile(threat, assistRadius, maxAssistCount);
        }
    }
}
