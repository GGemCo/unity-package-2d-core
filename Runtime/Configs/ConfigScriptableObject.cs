namespace GGemCo2DCore
{
    /// <summary>
    /// ScriptableObject 관련 설정 정의
    /// </summary>
    public static class ConfigScriptableObject
    {
        /// <summary>
        /// 메뉴 순서 정의
        /// </summary>
        public enum MenuOrdering
        {
            None,
            MainSettings,
            PlayerSettings,
            MapSettings,
            SaveSettings
        }

        private const string BasePath = ConfigDefine.NameSDK + "/Settings/";
        private const string BaseName = ConfigDefine.NameSDK;

        public static class Main
        {
            public const string FileName = BaseName + "Settings";
            public const string MenuName = BasePath + FileName;
            public const int Ordering = (int)MenuOrdering.MainSettings;
        }

        public static class Player
        {
            public const string FileName = BaseName + "PlayerSettings";
            public const string MenuName = BasePath + FileName;
            public const int Ordering = (int)MenuOrdering.PlayerSettings;
        }

        public static class Map
        {
            public const string FileName = BaseName + "MapSettings";
            public const string MenuName = BasePath + FileName;
            public const int Ordering = (int)MenuOrdering.MapSettings;
        }

        public static class Save
        {
            public const string FileName = BaseName + "SaveSettings";
            public const string MenuName = BasePath + FileName;
            public const int Ordering = (int)MenuOrdering.SaveSettings;
        }
    }
}