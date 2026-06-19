using UnityEngine;

namespace GGemCo2DCore
{
    public static class SoundConstants
    {
        public enum Type
        {
            None,
            Bgm,
            Ambient,
            Sfx
        }

        public enum SubType
        {
            None,
            Player,
            UI,
            Skill
        }

        public enum ResolveMode
        {
            [Tooltip("연결된 실제 리소스 테이블 행 1개를 직접 재생합니다.")]
            Direct,
            [Tooltip("sound_variant 테이블의 후보 목록 중 하나를 선택해 재생합니다.")]
            Variant
        }

        public enum SelectionMode
        {
            [Tooltip("가중치를 동일하게 보고 후보 중 하나를 무작위로 선택합니다.")]
            RandomEqual,
            [Tooltip("sound_variant.Weight 값을 기준으로 후보 중 하나를 선택합니다.")]
            WeightedRandom,
            [Tooltip("등록된 후보 순서대로 하나씩 선택합니다.")]
            Sequence,
            [Tooltip("최근 선택 후보를 가능한 한 피하면서 무작위로 선택합니다.")]
            ShuffleBag
        }

        public const string NameExposedParameterMaster = "GGemCoVolumeMaster";
        public const string NameExposedParameterBGM = "GGemCoVolumeBGM";
        public const string NameExposedParameterSfx = "GGemCoVolumeSfx";
        public const string NameExposedParameterAmbient = "GGemCoVolumeAmbient";
        
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
