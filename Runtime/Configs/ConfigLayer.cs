namespace GGemCo2DCore
{
    /// <summary>
    /// Unity Layer 에 사용되는 Config 값
    /// </summary>
    public abstract class ConfigLayer : DefaultConfig<ConfigLayer.Keys>
    {
        public enum Keys
        {
            // 타일맵에서 가지 못하는 영역
            TileMapWall,
            TileMapGround,
            HitAreaMonster,
            HitAreaPlayer,
            TileMapOneWayPlatform,
            // 캐릭터 이동 차단용 Body Collider 레이어 - Player
            CharacterBodyPlayer,
            // 캐릭터 이동 차단용 Body Collider 레이어 - Monster
            CharacterBodyMonster,
            // 캐릭터 이동 차단용 Body Collider 레이어 - NPC
            CharacterBodyNpc,
            // 사망 캐릭터 Body Collider 지면 유지 전용 레이어
            CharacterBodyDead
        }
    }
}