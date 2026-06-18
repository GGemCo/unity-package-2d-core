using System.Collections.Generic;

namespace GGemCo2DCore
{
    /// <summary>
    /// 하나의 맵 웨이브 진행 규칙을 정의합니다.
    /// </summary>
    [System.Serializable]
    public sealed class MapWaveScenarioData
    {
        /// <summary>
        /// 웨이브 시나리오 UID입니다.
        /// </summary>
        public int ScenarioUid;

        /// <summary>
        /// 이 웨이브 시나리오가 적용되는 맵 UID입니다.
        /// </summary>
        public int MapUid;

        /// <summary>
        /// 기획 확인용 메모입니다.
        /// </summary>
        public string Memo;

        /// <summary>
        /// 맵 로드 후 자동으로 웨이브를 시작할지 여부입니다.
        /// </summary>
        public bool AutoStart;

        /// <summary>
        /// 자동 시작 시 웨이브 시작 전 대기 시간입니다.
        /// </summary>
        public float StartDelaySeconds;

        /// <summary>
        /// 웨이브 시작 기준점으로 사용할 스폰 포인트 ID입니다.
        /// </summary>
        public int StartPointId;

        /// <summary>
        /// 웨이브 진행 중 기존 개별 몬스터 리젠을 억제할지 여부입니다.
        /// </summary>
        public bool SuppressNormalMonsterRespawnWhileRunning;

        /// <summary>
        /// 이 시나리오에서 참조할 스폰 위치 목록입니다.
        /// </summary>
        public List<MapWaveSpawnPointData> SpawnPoints = new List<MapWaveSpawnPointData>();

        /// <summary>
        /// 순서대로 실행할 웨이브 그룹 목록입니다.
        /// </summary>
        public List<MapWaveGroupData> Groups = new List<MapWaveGroupData>();
    }
}
