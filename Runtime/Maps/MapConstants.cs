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
            LoadCharacterPrefabs,   // 맵에 배치되는 몬스터, Npc 프리팹 불러오기
            CreateMonster,   // 맵에 배치되는 몬스터 생성 하기
            CreateNpc,       // 맵에 배치되는 npc 생성하기
            CreateWarp,      // 맵에 배치되는 warp 프리팹 불러오기
            LoadPlayerPrefabs,    // 맵에 배치되는 플레이어 프리팹 불러오기
            FadeOut,              // 검정색 스프라이트 페이드 아웃
            Complete,             // 완료
            Failed                // 실패
        }
        
        public const float FadeDuration = 0.7f;

    }
}