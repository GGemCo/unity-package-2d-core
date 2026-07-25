namespace GGemCo2DCore
{
    /// <summary>
    /// 맵 진행 상태에서 발생할 수 있는 표준 변경 종류입니다.
    /// </summary>
    public enum MapProgressChangeType
    {
        None = 0,
        MapCleared = 1,
    }

    /// <summary>
    /// 맵 클리어처럼 저장 가능한 맵 진행 상태가 실제로 변경되었음을 전달합니다.
    /// </summary>
    public readonly struct MapProgressChangedEventData
    {
        /// <summary>
        /// 발생한 맵 진행 상태 변경 종류입니다.
        /// </summary>
        public readonly MapProgressChangeType ChangeType;

        /// <summary>
        /// 변경된 TableMap UID입니다.
        /// </summary>
        public readonly int MapUid;

        /// <summary>
        /// 맵 진행 상태 변경 이벤트 데이터를 생성합니다.
        /// </summary>
        /// <param name="changeType">발생한 변경 종류입니다.</param>
        /// <param name="mapUid">변경된 TableMap UID입니다.</param>
        public MapProgressChangedEventData(
            MapProgressChangeType changeType,
            int mapUid)
        {
            ChangeType = changeType;
            MapUid = mapUid;
        }
    }
}
