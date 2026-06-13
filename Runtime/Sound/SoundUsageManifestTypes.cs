namespace GGemCo2DCore
{
    /// <summary>
    /// 자동 생성 사운드 사용 매니페스트가 사운드를 유지할 런타임 범위를 정의합니다.
    /// </summary>
    public enum SoundUsageManifestScopeType
    {
        /// <summary>유효한 범위가 지정되지 않았습니다.</summary>
        None = 0,

        /// <summary>특정 맵이 활성화된 동안 유지하는 사운드입니다.</summary>
        Map = 1,

        /// <summary>특정 UI 윈도우가 활성화된 동안 유지하는 사운드입니다.</summary>
        UiWindow = 2,
    }

    /// <summary>
    /// 자동 분석기가 사운드 사용처를 발견한 원본 종류를 정의합니다.
    /// 런타임 로딩에는 영향을 주지 않으며 에디터 보고서와 누락 추적에 사용합니다.
    /// </summary>
    public enum SoundUsageManifestSourceType
    {
        /// <summary>원본 종류를 판정하지 못했습니다.</summary>
        Unknown = 0,

        /// <summary>맵에 배치된 몬스터 프리팹의 애니메이션 이벤트입니다.</summary>
        MonsterAnimation = 1,

        /// <summary>맵에 배치된 NPC 프리팹의 애니메이션 이벤트입니다.</summary>
        NpcAnimation = 2,

        /// <summary>UI 윈도우 프리팹의 애니메이션 이벤트입니다.</summary>
        UiAnimation = 3,

        /// <summary>UI 윈도우 프리팹의 명시적 클릭 사운드 UID입니다.</summary>
        UiClick = 4,

        /// <summary>UIWindowSoundUsageDeclaration에 직접 등록된 사운드 UID입니다.</summary>
        UiDeclaration = 5,
    }
}
