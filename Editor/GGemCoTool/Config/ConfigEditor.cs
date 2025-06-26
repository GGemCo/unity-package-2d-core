using GGemCo2DCore;

namespace GGemCo2DCoreEditor
{
    public static class ConfigEditor
    {
        public enum ToolOrdering
        {
            DefaultSetting = 1,
            SettingAddressable = 2,
            SettingSceneIntro = 3,
            SettingSceneLoading = 4,
            SettingSceneGame = 5,
            Development = 100,
            CreateDialogue,
            MapExporter,
            CreateItem,
            Cutscene,
            Quest,
            Test = 200,
            DropItemRate,
            Etc = 900,
            PlayerPrefs,
            OpenSaveDataFolder,
            LoadAddressable,
        }
        private const string NameToolGGemCo = ConfigDefine.NameSDK+"Tool/";
        
        // 오브젝트 생성시 사용
        public const string NamePrefixCore = ConfigDefine.NameSDK + "_" + ConfigDefine.NamePackageCore;
        
        // 기본 셋팅하기
        private const string NameToolSettings = NameToolGGemCo + "설정하기/";
        public const string NameToolSettingDefault = NameToolSettings + "기본 셋팅하기";
        public const string NameToolSettingAddressable = NameToolSettings + "Addressable 셋팅하기";
        public const string NameToolSettingSceneIntro = NameToolSettings + "인트로 씬 셋팅하기";
        public const string NameToolSettingSceneLoading = NameToolSettings + "로딩 씬 셋팅하기";
        public const string NameToolSettingSceneGame = NameToolSettings + "게임 씬 셋팅하기";
        
        // 개발툴
        private const string NameToolDevelopment = NameToolGGemCo + "개발툴/";
        public const string NameToolCreateDialogue = NameToolDevelopment + "대사 생성툴";
        public const string NameToolMapExporter = NameToolDevelopment + "맵배치툴";
        public const string NameToolCreateItem = NameToolDevelopment + "아이템 생성툴";
        public const string NameToolCutscene = NameToolDevelopment + "연출툴";
        public const string NameToolQuest = NameToolDevelopment + "퀘스트 생성툴";
        
        // 테스트
        private const string NameToolTest = NameToolGGemCo + "태스트툴/";
        public const string NameToolDropItemRate = NameToolTest + "아이템 드랍 확률";
        
        // etc
        private const string NameToolEtc = NameToolGGemCo + "기타/";
        public const string NameToolPlayerPrefs = NameToolEtc + "PlayerPrefs 데이터 관리";
        public const string NameToolOpenSaveDataFolder = NameToolEtc + "게임 데이터 관리";
        public const string NameToolLoadAddressable = NameToolEtc + "Addressable 로더 툴";

        // 에디터에서 사용되는 프리팹 경로
        public const string PathResource = "Packages/com.ggemco.2d.core/EditorResource";
        public const string PathPrefab = PathResource+"/Prefabs";
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