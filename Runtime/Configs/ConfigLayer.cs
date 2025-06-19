namespace GGemCo.Scripts
{
    /// <summary>
    /// Sorting Layer 에 사용되는 Config 값
    /// </summary>
    public abstract class ConfigLayer : DefaultConfig<ConfigLayer.Keys>
    {
        public enum Keys
        {
            // 타일맵에서 가지 못하는 영역
            TileMapWall,
        }
    }
}