namespace GGemCo2DCore
{
    public static class SaveDataConstants
    {
        private const string SaveDataFileName = "SaveData";
        public const string SaveDataFileExt = ".json";
        
        public static string DefaultFileName => $"{SaveDataFileName}{SaveDataFileExt}";
    }
}