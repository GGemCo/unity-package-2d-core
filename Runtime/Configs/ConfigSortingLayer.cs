namespace GGemCo2DCore
{
    /// <summary>
    /// Sorting Layer 에 사용되는 Config 값
    /// </summary>
    public abstract class ConfigSortingLayer : DefaultConfig<ConfigSortingLayer.Keys>
    {
        public enum Keys
        {
            // 맵 터레인
            MapBackground,
            Map_Under,
            Map_Ground,
            Map_Upper,
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

        public static Keys ConvertKeys(string value)
        {
            return value switch
            {
                "MapBackground" => Keys.MapBackground,
                "Map_Under" => Keys.Map_Under,
                "Map_Ground" => Keys.Map_Ground,
                "Map_Upper" => Keys.Map_Upper,
                "MapObject" => Keys.MapObject,
                "CharacterBottom" => Keys.CharacterBottom,
                "Character" => Keys.Character,
                "CharacterTop" => Keys.CharacterTop,
                "UI" => Keys.UI,
                _ => Keys.Character
            };
        }
    }
}