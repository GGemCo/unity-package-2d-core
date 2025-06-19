namespace GGemCo.Editor
{
    public class ConfigEditor
    {
        public enum ToolOrdering
        {
            DefaultSetting = 1,
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
        }
        private const string NameToolGGemCo = "GGemCoTool/";
        // 기본 셋팅하기
        public const string NameToolDefaultSetting = NameToolGGemCo + "기본 셋팅하기";
        
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
        
    }
}