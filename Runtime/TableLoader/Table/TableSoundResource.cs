using System.Collections.Generic;

namespace GGemCo2DCore
{
    /// <summary>
    /// sound_bgm/sound_ambient/sound_sfx 테이블의 실제 AudioClip 리소스 공통 행입니다.
    /// </summary>
    public abstract class StruckTableSoundResource : IUidName
    {
        public int Uid { get; set; }
        public string Name { get; set; }
        public int SoundUid { get; set; }
        public SoundConstants.Type Type { get; set; }
        public SoundConstants.SubType SubType;
        public string FileName;
        public int MaxPlayCount;
        public float Volume;
        public float PitchMin;
        public float PitchMax;
        public bool Loop;
        public float FadeDuration;
        public bool UseIntroScene;
        public bool PreLoad;

        /// <summary>
        /// 실제 AudioClip Addressables 키를 생성합니다.
        /// </summary>
        /// <returns>사운드 Addressables address 키입니다.</returns>
        public string BuildAddressKey()
        {
            return string.IsNullOrWhiteSpace(FileName)
                ? string.Empty
                : $"{ConfigAddressableGroupName.Sound}_{FileName}";
        }

        /// <summary>
        /// 피치 범위가 잘못 입력된 경우에도 안전하게 사용할 수 있도록 보정된 최소값을 반환합니다.
        /// </summary>
        /// <returns>보정된 최소 피치입니다.</returns>
        public float GetSafePitchMin()
        {
            return PitchMin > 0f ? PitchMin : 1f;
        }

        /// <summary>
        /// 피치 범위가 잘못 입력된 경우에도 안전하게 사용할 수 있도록 보정된 최대값을 반환합니다.
        /// </summary>
        /// <returns>보정된 최대 피치입니다.</returns>
        public float GetSafePitchMax()
        {
            return PitchMax > 0f ? PitchMax : GetSafePitchMin();
        }
    }

    internal static class TableSoundResourceParser
    {
        /// <summary>
        /// sound_bgm/sound_ambient/sound_sfx 공통 컬럼을 파싱합니다.
        /// </summary>
        /// <typeparam name="TResource">생성할 사운드 리소스 행 타입입니다.</typeparam>
        /// <param name="data">헤더명과 값의 사전입니다.</param>
        /// <param name="type">리소스 테이블이 의미하는 사운드 타입입니다.</param>
        /// <returns>파싱된 리소스 행입니다.</returns>
        public static TResource BuildResourceRow<TResource>(Dictionary<string, string> data, SoundConstants.Type type)
            where TResource : StruckTableSoundResource, new()
        {
            TableRowReader reader = new TableRowReader(data, nameof(TableSoundResourceParser));

            return new TResource
            {
                Uid = reader.Int("Uid"),
                Name = reader.String("Name", string.Empty),
                SoundUid = reader.Int("SoundUid", 0),
                SubType = reader.Enum<SoundConstants.SubType>("SubType"),
                FileName = reader.String("FileName", string.Empty),
                MaxPlayCount = reader.Int("MaxPlayCount", 0),
                Volume = reader.Float("Volume", 1f),
                PitchMin = reader.Float("PitchMin", 1f),
                PitchMax = reader.Float("PitchMax", 1f),
                Loop = reader.BoolYN("Loop"),
                FadeDuration = reader.Float("FadeDuration", 0.7f),
                UseIntroScene = reader.BoolYN("UseIntroScene"),
                PreLoad = reader.BoolYN("PreLoad"),
                Type = type,
            };
        }
    }
}
