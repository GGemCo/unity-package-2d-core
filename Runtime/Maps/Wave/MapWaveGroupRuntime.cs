using System.Collections.Generic;

namespace GGemCo2DCore
{
    /// <summary>
    /// 웨이브 그룹 1회 실행 상태를 관리합니다.
    /// </summary>
    public sealed class MapWaveGroupRuntime
    {
        private readonly HashSet<int> _spawnedMonsterVids = new HashSet<int>();
        private readonly HashSet<int> _aliveMonsterVids = new HashSet<int>();

        /// <summary>
        /// 웨이브 그룹 런타임 인스턴스 ID입니다.
        /// </summary>
        public int InstanceId { get; }

        /// <summary>
        /// 이 그룹 인스턴스가 속한 시나리오입니다.
        /// </summary>
        public MapWaveScenarioRuntime Scenario { get; }

        /// <summary>
        /// 그룹 정의 데이터입니다.
        /// </summary>
        public MapWaveGroupData Data { get; }

        /// <summary>
        /// 같은 그룹 반복 실행 중 현재 회차입니다. 0부터 시작합니다.
        /// </summary>
        public int RepeatIndex { get; }

        /// <summary>
        /// 그룹의 모든 스폰 요청이 끝났는지 여부입니다.
        /// </summary>
        public bool IsSpawnCompleted { get; private set; }

        /// <summary>
        /// 다음 그룹 전환 요청이 이미 처리되었는지 여부입니다.
        /// </summary>
        public bool IsNextRequested { get; private set; }

        /// <summary>
        /// 현재 생존 중인 웨이브 몬스터 수입니다.
        /// </summary>
        public int AliveCount => _aliveMonsterVids.Count;

        /// <summary>
        /// 그룹이 스폰을 완료했고 생존 몬스터가 없는지 여부입니다.
        /// </summary>
        public bool IsCleared => IsSpawnCompleted && _aliveMonsterVids.Count == 0;

        /// <summary>
        /// 웨이브 그룹 런타임 상태를 생성합니다.
        /// </summary>
        /// <param name="instanceId">그룹 런타임 인스턴스 ID입니다.</param>
        /// <param name="scenario">소속 시나리오 런타임입니다.</param>
        /// <param name="data">그룹 정의 데이터입니다.</param>
        /// <param name="repeatIndex">반복 실행 회차입니다.</param>
        public MapWaveGroupRuntime(
            int instanceId,
            MapWaveScenarioRuntime scenario,
            MapWaveGroupData data,
            int repeatIndex)
        {
            InstanceId = instanceId;
            Scenario = scenario;
            Data = data;
            RepeatIndex = repeatIndex;
        }

        /// <summary>
        /// 그룹에서 생성한 몬스터 VID를 생존 목록에 등록합니다.
        /// </summary>
        /// <param name="monsterVid">생성된 몬스터 VID입니다.</param>
        public void RegisterMonster(int monsterVid)
        {
            if (monsterVid <= 0)
            {
                return;
            }

            _spawnedMonsterVids.Add(monsterVid);
            _aliveMonsterVids.Add(monsterVid);
        }

        /// <summary>
        /// 몬스터 사망을 반영하고 생존 목록에서 제거합니다.
        /// </summary>
        /// <param name="monsterVid">사망한 몬스터 VID입니다.</param>
        /// <returns>이 그룹에 속한 몬스터가 제거되었으면 <see langword="true"/>를 반환합니다.</returns>
        public bool MarkMonsterDead(int monsterVid)
        {
            return monsterVid > 0 && _aliveMonsterVids.Remove(monsterVid);
        }

        /// <summary>
        /// 이 그룹의 모든 스폰 요청이 끝났음을 표시합니다.
        /// </summary>
        public void MarkSpawnCompleted()
        {
            IsSpawnCompleted = true;
        }

        /// <summary>
        /// 다음 그룹 전환 요청이 시작되었음을 표시합니다.
        /// </summary>
        public void MarkNextRequested()
        {
            IsNextRequested = true;
        }

        /// <summary>
        /// 이 그룹에서 한 번이라도 생성한 모든 몬스터 VID 목록을 복사해 반환합니다.
        /// </summary>
        /// <returns>생성된 몬스터 VID 목록입니다.</returns>
        public List<int> GetSpawnedMonsterVids()
        {
            return new List<int>(_spawnedMonsterVids);
        }
    }
}
