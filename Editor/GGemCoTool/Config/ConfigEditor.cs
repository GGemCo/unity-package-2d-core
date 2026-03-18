using GGemCo2DCore;

namespace GGemCo2DCoreEditor
{
    public static class ConfigEditor
    {
        public enum ToolOrdering
        {
            AutoSetting = 1,
            DefaultSetting,
            SettingAddressable,
            SettingScenePreIntro,
            SettingSceneIntro,
            SettingSceneLoading,
            SettingSceneGame,
            Development = 100,
            MapExporter = 101,
            TableEditor,
            Quest = 110,
            CreateDialogue,
            Cutscene = 120,
            CreateEffectPrefab,
            CreateUIEffectPreset,
            LocalizationUpdate =  130,
            LocalizationFind,
            SoundUIButton = 140,
            CreateHubWindow,
            Test = 200,
            CreateItem,
            DropItemRate,
            MoveMap,
            UseEffect,
            UseCrowdControl,
            UseProjectile,
            UseItem,
            OpenWindow,
            Debug = 300,
            DebugTilemapDrawCall,
            DebugFps,
            DebugPhysics2D,
            DebugMemory,
            ListEnabledDebugOptions,
            DisableAllDebugOptions,
            Etc = 900,
            PlayerPrefs,
            OpenSaveDataFolder,
            LoadAddressable,
            SpriteSlicerExporter,
            AnimatedTileBatchCreator,
        }
        private const string NameToolGGemCo = ConfigDefine.NameSDK+"Tool/";
        
        // 기본 셋팅하기
        private const string NameToolSettings = NameToolGGemCo + "설정하기/";
        public const string NameToolSettingAuto = NameToolSettings + "자동 셋팅하기";
        public const string NameToolSettingDefault = NameToolSettings + "기본 셋팅하기";
        public const string NameToolSettingAddressable = NameToolSettings + "Addressable 셋팅하기";
        public const string NameToolSettingScenePreIntro = NameToolSettings + "Pre 인트로 씬 셋팅하기";
        public const string NameToolSettingSceneIntro = NameToolSettings + "인트로 씬 셋팅하기";
        public const string NameToolSettingSceneLoading = NameToolSettings + "로딩 씬 셋팅하기";
        public const string NameToolSettingSceneGame = NameToolSettings + "게임 씬 셋팅하기";
        
        // 개발툴
        private const string NameToolDevelopment = NameToolGGemCo + "개발툴/";
        public const string NameToolMapExporter = NameToolDevelopment + "맵배치툴";
        
        public const string NameToolQuest = NameToolDevelopment + "퀘스트 생성툴";
        public const string NameToolCreateDialogue = NameToolDevelopment + "대사 생성툴";
        
        public const string NameToolCutscene = NameToolDevelopment + "연출툴";
        public const string NameToolCreateEffectPrefab = NameToolDevelopment + "이팩트 프리팹 생성툴";
        public const string NameToolCreateUIEffectPreset = NameToolDevelopment + "UI 효과 프리셋 편집툴";
        
        public const string NameToolLocalizationUpdate = NameToolDevelopment + "Localize 업데이트툴";
        public const string NameToolLocalizationFind = NameToolDevelopment + "Localize 검색기";
        
        public const string NameToolSoundUIButton = NameToolDevelopment + "UI 버튼 사운드 적용툴";
        
        public const string NameToolCreateHubWindow = NameToolDevelopment + "오브젝트 생성툴";
        public const string NameToolTableEditor = NameToolDevelopment + "데이터 테이블 에디터";
        
        // 테스트
        private const string NameToolTest = NameToolGGemCo + "테스트툴/";
        public const string NameToolDropItemRate = NameToolTest + "아이템 드랍 확률";
        public const string NameToolCreateItem = NameToolTest + "아이템 생성툴";
        public const string NameToolMoveMap = NameToolTest + "맵 이동툴";
        public const string NameToolUseEffect = NameToolTest + "이펙트 사용툴";
        public const string NameToolUseCrowdControl = NameToolTest + "CrowdControl 사용툴";
        public const string NameToolUseProjectile = NameToolTest + "프로젝타일 사용툴";
        public const string NameToolUseItem = NameToolTest + "아이템 사용툴";
        public const string NameToolOpenWindow = NameToolTest + "윈도우 열기";
        
        // 디버그
        private const string NameToolDebug = NameToolGGemCo + "디버그툴/";
        public const string NameToolTilemapDrawCall = NameToolDebug + "타일맵 드로우콜 HUD";
        public const string NameToolFps = NameToolDebug + "FPS HUD";
        public const string NameToolPhysics2D = NameToolDebug + "Physics2D HUD";
        public const string NameToolMemory = NameToolDebug + "메모리 HUD";
        public const string NameToolListEnabledDebugOptions = NameToolDebug + "디버그 설정 리스트 보기";
        public const string NameToolDisableAllDebugOptions = NameToolDebug + "디버그 설정 모두 false 변경하기";
        
        // etc
        private const string NameToolEtc = NameToolGGemCo + "기타/";
        public const string NameToolPlayerPrefs = NameToolEtc + "PlayerPrefs 데이터 관리";
        public const string NameToolOpenSaveDataFolder = NameToolEtc + "게임 데이터 관리";
        public const string NameToolLoadAddressable = NameToolEtc + "Addressable 로더 툴";
        public const string NameToolSpriteSlicerExporter = NameToolEtc + "이미지 자르기";
        public const string NameToolAnimatedTileBatchCreator = NameToolEtc + "애니메이션 타일 일괄생성기";

        // 에디터에서 사용되는 프리팹 경로
        public const string PathPackageCore = "Packages/com.ggemco.2d.core";
        private const string PathEditorResource = PathPackageCore+"/EditorResource";
        private const string PathPrefab = PathEditorResource+"/Prefabs";
        public const string PathPrefabDefaultUIButton = PathPrefab+"/UI/DefaultButton.prefab";
        public const string PathPrefabDefaultUITextMeshProGUI = PathPrefab+"/UI/DefaultText.prefab";
        public const string PathPrefabCanvasFromWorld = PathPrefab+"/UI/CanvasFromWorld.prefab";
        public const string PathPrefabCanvasBlack = PathPrefab+"/UI/CanvasBlack.prefab";
        public const string PathPrefabSystemMessageManager = PathPrefab+"/UI/SystemMessageManager.prefab";
        public const string PathPrefabPopupManager = PathPrefab+"/UI/PopupManager.prefab";
        
        // 윈도우 경로
        public const string PathUIWindow = "Assets/GGemCo/UIWindows";
    }
}