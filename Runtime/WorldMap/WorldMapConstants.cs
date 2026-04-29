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
    /// 월드맵 검증 메시지의 심각도를 정의합니다.
    /// </summary>
    public enum WorldMapValidationSeverity
    {
        Info = 0,
        Warning = 1,
        Error = 2,
    }
}
