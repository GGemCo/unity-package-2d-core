namespace GGemCo.Scripts
{
    /// <summary>
    /// Sorting Layer 에 사용되는 Config 값
    /// </summary>
    public abstract class ConfigSortingLayer : DefaultConfig<ConfigSortingLayer.Keys>
    {
        public enum Keys
        {
            // 맵 터레인
            MapTerrain,
            // 맵 오브젝트
            MapObject,
            // 캐릭터 밑에
            CharacterBottom,
            // 캐릭터
            Character,
            // 캐릭터 위에 
            CharacterTop,
            // UI 
            UI,
        }
    }
}