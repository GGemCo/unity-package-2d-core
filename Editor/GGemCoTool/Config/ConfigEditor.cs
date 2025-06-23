using GGemCo.Scripts;

namespace GGemCo.Editor
{
    public static class ConfigEditor
    {
        public enum ToolOrdering
        {
            DefaultSetting = 1,
            SettingAddressable = 2,
            SettingSceneIntro = 3,
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
        
        // 프리팹 경로
        public const string PrefabPathDefaultUIButton = "Packages/com.ggemco.2d.core/Editor/Data/Prefabs/UI/DefaultButton.prefab";
    }
}