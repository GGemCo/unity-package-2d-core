namespace GGemCo.Scripts
{
    public static class MapConstants
    {
        public enum Category
        {
            None,
            Tiled
        }
        public enum Type
        {
            None,
            Common,
        }
        public enum SubType
        {
            None,
        }

        public enum State
        {
            None,
            FadeIn,               // 검정색 스프라이트 페이드 인
            UnloadPreviousStage,  // 이전 스테이지의 몬스터 인스턴스를 제거하고 메모리 정리를 진행
            LoadTilemapPrefab,    // 맵에 필요한 타일맵 프리팹 불러오기
            LoadMonsterPrefabs,   // 맵에 배치되는 몬스터 스파인 프리팹 불러오기
            LoadNpcPrefabs,       // 맵에 배치되는 npc스파인 프리팹 불러오기
            LoadWarpPrefabs,      // 맵에 배치되는 warp 프리팹 불러오기
            LoadPlayerPrefabs,    // 맵에 배치되는 플레이어 프리팹 불러오기
            FadeOut,              // 검정색 스프라이트 페이드 아웃
            Complete,             // 완료
            Failed                // 실패
        }
        
        public const string ResourceMapPath = "Maps/";
        public const string ResourceMapCommonPath = ResourceMapPath + "Common/";
        public const string FileNameTilemap = "tilemap";
        public const string FileNameRegenNpc = "regen_npc";
        public const string FileNameRegenMonster = "regen_monster";
        public const string FileNameWarp = "warp";
        private const string PrefabNameWarp = "ObjectWarp";
        public const string FileExt = ".json";
        
        public const float FadeDuration = 0.7f;
        
        public const string PathPrefabWarp = ResourceMapCommonPath+PrefabNameWarp;
    }
}