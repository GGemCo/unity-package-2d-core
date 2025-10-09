namespace GGemCo2DCore
{
    public enum TileState
    {
        Dry = 0,
        Wet = 1,
        Hoed = 2,
        Seeded = 3,
        Grown = 4,
    }

    public enum TileAction
    {
        Water = 0,
        Hoe = 1,
        Plant = 2,
        Harvest = 3,
        TimeTick = 9, // 시간 경과 시 내부적으로 사용하는 액션
    }
}