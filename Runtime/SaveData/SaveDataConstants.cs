namespace GGemCo2DCore
{
    public static class SaveDataConstants
    {
        public const string SaveDataFileExt = ".json";
        public const string SaveDataFileNameWithoutExtension = "SaveData";
        public const string BackupFileNameWithoutExtension = "SaveData.backup";
        public const string TempFileNameWithoutExtension = "SaveData.tmp";
        public const string InvalidDirectoryName = "Invalid";

        public static string DefaultFileName => $"{SaveDataFileNameWithoutExtension}{SaveDataFileExt}";
        public static string BackupFileName => $"{BackupFileNameWithoutExtension}{SaveDataFileExt}";
        public static string TempFileName => $"{TempFileNameWithoutExtension}{SaveDataFileExt}";
    }
}
