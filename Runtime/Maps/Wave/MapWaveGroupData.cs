using System.Collections.Generic;

namespace GGemCo2DCore
{
    /// <summary>
    /// 웨이브 시나리오 안에서 한 번에 스폰될 몬스터 묶음을 정의합니다.
    /// </summary>
    [System.Serializable]
    public sealed class MapWaveGroupData
    {
        /// <summary>
        /// 웨이브 그룹 UID입니다.
        /// </summary>
        public int GroupUid;

        /// <summary>
        /// 자동 진행 시 사용할 실행 순서입니다.
        /// </summary>
        public int Order;

        /// <summary>
        /// 같은 그룹을 반복 스폰할 횟수입니다. 기본값은 1회입니다.
        /// </summary>
        public int RepeatCount = 1;

        /// <summary>
        /// 다음 그룹으로 넘어갈 조건입니다.
        /// </summary>
        public WaveNextPolicy NextPolicy = WaveNextPolicy.WhenAllDead;

        /// <summary>
        /// 시간 기반 전환 정책에서 사용할 대기 시간입니다.
        /// </summary>
        public float NextAfterSeconds;

        /// <summary>
        /// 전환 조건을 만족한 뒤 다음 그룹 스폰 전 추가로 대기할 시간입니다.
        /// </summary>
        public float NextDelaySeconds;

        /// <summary>
        /// 명시적으로 이어갈 다음 그룹 UID입니다. 0이면 Order 기준 다음 그룹을 사용합니다.
        /// </summary>
        public int NextGroupUid;

        /// <summary>
        /// 이 그룹이 시작될 방향을 상위 게임 계층에 안내할 스폰 포인트 ID입니다.
        /// 0이면 그룹 몬스터의 스폰 위치 평균을 이동 유도 기준으로 사용합니다.
        /// </summary>
        public int NavigationPointId;

        /// <summary>
        /// 이 그룹에서 생성할 몬스터 목록입니다.
        /// </summary>
        public List<MapWaveMonsterSpawnData> Monsters = new List<MapWaveMonsterSpawnData>();
    }
}
