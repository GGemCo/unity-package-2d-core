namespace GGemCo2DCore
{
    /// <summary>
    /// 월드맵 노드의 게임 플레이 분류를 정의합니다.
    /// </summary>
    public enum WorldMapNodeType
    {
        Normal = 0,
        Start = 1,
        Boss = 2,
        Rest = 3,
        Shop = 4,
        Hidden = 5,
    }

    /// <summary>
    /// 월드맵 연결선의 동작 분류를 정의합니다.
    /// </summary>
    public enum WorldMapEdgeType
    {
        Normal = 0,
        Locked = 1,
        Secret = 2,
    }

    /// <summary>
    /// 월드맵 연결선 스프라이트를 Image에 그리는 방식을 정의합니다.
    /// </summary>
    public enum WorldMapEdgeSpriteDrawMode
    {
        /// <summary>스프라이트를 연결선 길이에 맞춰 단순 확대합니다.</summary>
        Simple = 0,

        /// <summary>스프라이트의 9-slice border를 유지하며 연결선 길이에 맞춥니다.</summary>
        Sliced = 1,

        /// <summary>스프라이트를 연결선 길이에 맞춰 반복 표시합니다.</summary>
        Tiled = 2,
    }

    /// <summary>
    /// 월드맵 노드 포인트에 표시할 플레이어 이동 상태를 정의합니다.
    /// </summary>
    public enum WorldMapNodePointState
    {
        /// <summary>
        /// 노드 포인트를 표시하지 않습니다.
        /// </summary>
        None = 0,

        /// <summary>
        /// 현재 플레이어가 위치한 맵입니다.
        /// </summary>
        CurrentMap = 1,

        /// <summary>
        /// 현재 맵에서 바로 이동할 수 있는 맵입니다.
        /// </summary>
        MovePossible = 2,

        /// <summary>
        /// 월드맵에 보이지만 현재 맵에서 바로 이동할 수 없는 맵입니다.
        /// </summary>
        MoveImpossible = 3,
    }

    /// <summary>
    /// 월드맵 검증 메시지의 심각도를 정의합니다.
    /// </summary>
    public enum WorldMapValidationSeverity
    {
        Info = 0,
        Warning = 1,
        Error = 2,
    }
}
