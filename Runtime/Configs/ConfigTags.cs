namespace GGemCo.Scripts
{
    /// <summary>
    /// 게임에서 사용되는 tag 관리
    /// </summary>
    public abstract class ConfigTags : DefaultConfig<ConfigTags.Keys>
    {
        public enum Keys
        {
            // 플레이어
            Player,
            // 몬스터
            Monster,
            // Npc
            Npc,
            // 맵
            Map,
            // 맵에 배치되는 오브젝트
            MapObject,
            // 맵에 배치되는 워프
            MapObjectWarp,
            // 타일 맵이 들어가는 부모 grid object
            GridTileMap,
            // 드랍된 아이템 
            DropItem,
        }
    }
}