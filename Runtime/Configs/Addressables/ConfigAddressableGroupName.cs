namespace GGemCo2DCore
{
    /// <summary>
    /// Addressables Group 네이밍 규칙(빌드/패키징 단위).
    /// </summary>
    public static class ConfigAddressableGroupName
    {
        // Common
        public const string Common = ConfigDefine.NameSDK + "_Common";

        // Characters
        public const string Monster = ConfigDefine.NameSDK + "_Character_Monster";
        public const string Npc     = ConfigDefine.NameSDK + "_Character_Npc";
        public const string Player  = ConfigDefine.NameSDK + "_Character_Player";
        public const string CharacterThumbnail  = ConfigDefine.NameSDK + "_Character_Thumbnail";
        public const string CharacterImageName  = ConfigDefine.NameSDK + "_Character_ImageName";

        // Vfxs
        public const string Vfx = ConfigDefine.NameSDK + "_Vfx";

        // Items (하위 그룹 형태로 구조화)
        public const string Item = ConfigDefine.NameSDK + "_Item";
        public static class ItemGroup
        {
            public const string DropImage  = Item + "_DropImage";
            public const string IconImage  = Item + "_IconImage";
            public const string EquipImage = Item + "_EquipImage";
        }

        // Map / Table / Narrative
        public const string Map      = ConfigDefine.NameSDK + "_Map";
        public const string Table    = ConfigDefine.NameSDK + "_Table";
        public const string Dialogue = ConfigDefine.NameSDK + "_Dialogue";
        public const string Quest    = ConfigDefine.NameSDK + "_Quest";
        public const string Cutscene = ConfigDefine.NameSDK + "_Cutscene";

        // Icons
        // Sound
        public const string Sound = ConfigDefine.NameSDK + "_Sound";

        // Input
        public const string InputAction = ConfigDefine.NameSDK + "_InputAction";
        
        // Simulation tool definition
        public const string SimulationToolDefinition = ConfigDefine.NameSDK + "_Simulation_Tool_Definition";
        // Simulation 성정
        public const string SimulationGrowth = ConfigDefine.NameSDK + "_Simulation_Growth";
        
        // 월드맵
        public const string WorldMap = ConfigDefine.NameSDK + "_WorldMap";
    }
}