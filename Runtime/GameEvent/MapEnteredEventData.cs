using UnityEngine;

namespace GGemCo2DCore
{
    /// <summary>
    /// 맵 입장이 완료되었을 때 전달되는 이벤트 데이터입니다.
    /// </summary>
    public readonly struct MapEnteredEventData
    {
        public readonly int MapUid;
        public readonly GameObject Map;
        public readonly double TimeRealtimeSinceStartup;

        /// <summary>
        /// 맵 입장 이벤트 데이터를 생성합니다.
        /// </summary>
        /// <param name="mapUid">입장이 완료된 맵 UID입니다.</param>
        /// <param name="map">로드된 맵 게임 오브젝트입니다.</param>
        public MapEnteredEventData(int mapUid, GameObject map)
        {
            MapUid = mapUid;
            Map = map;
            TimeRealtimeSinceStartup = Time.realtimeSinceStartupAsDouble;
        }
    }
}
