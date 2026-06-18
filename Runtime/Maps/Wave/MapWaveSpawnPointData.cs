namespace GGemCo2DCore
{
    /// <summary>
    /// 웨이브 몬스터가 생성될 기준 위치를 정의합니다.
    /// </summary>
    [System.Serializable]
    public sealed class MapWaveSpawnPointData
    {
        /// <summary>
        /// 그룹/몬스터 설정에서 참조할 스폰 포인트 ID입니다.
        /// </summary>
        public int PointId;

        /// <summary>
        /// 맵 기준 X 좌표입니다.
        /// </summary>
        public float x;

        /// <summary>
        /// 맵 기준 Y 좌표입니다.
        /// </summary>
        public float y;

        /// <summary>
        /// 맵 기준 Z 좌표입니다.
        /// </summary>
        public float z;

        /// <summary>
        /// 생성 위치에 적용할 무작위 반경입니다. 0이면 정확한 좌표에 생성합니다.
        /// </summary>
        public float RandomRadius;

        /// <summary>
        /// 스폰 포인트 기본 맵 표시 정책입니다.
        /// </summary>
        public MapCharacterVisibilityPolicy MapVisibilityPolicy = MapCharacterVisibilityPolicy.DefaultCulling;
    }
}
