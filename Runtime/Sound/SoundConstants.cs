namespace GGemCo2DCore
{
    public static class SoundConstants
    {
        public enum Type
        {
            None,
            Bgm,
            Sfx
        }
        public enum SubType
        {
            None,
            Player,
            UI,
            Skill
        }

        public const string NameExposedParameterMaster = "GGemCoVolumeMaster";
        public const string NameExposedParameterBGM = "GGemCoVolumeBGM";
        public const string NameExposedParameterSfx = "GGemCoVolumeSfx";
    }
}