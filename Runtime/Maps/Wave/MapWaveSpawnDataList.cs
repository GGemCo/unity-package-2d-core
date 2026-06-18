using System.Collections.Generic;

namespace GGemCo2DCore
{
    /// <summary>
    /// 맵 웨이브 스폰 JSON의 루트 데이터입니다.
    /// </summary>
    [System.Serializable]
    public sealed class MapWaveSpawnDataList
    {
        /// <summary>
        /// 맵에서 사용할 웨이브 시나리오 목록입니다.
        /// </summary>
        public List<MapWaveScenarioData> WaveScenarios = new List<MapWaveScenarioData>();
    }
}
