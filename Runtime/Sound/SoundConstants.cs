using UnityEngine;

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
        
        public enum UIButtonType
        {
            [Tooltip("디폴트 버튼")]
            Default,
            [Tooltip("확인 버튼")]
            Confirm,
            [Tooltip("취소 버튼")]
            Cancel,
            [Tooltip("윈도우 닫기 버튼")]
            CloseWindow,
        }

    }
}