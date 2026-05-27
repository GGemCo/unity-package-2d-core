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
            return new TResource
            {
                Uid = MathHelper.ParseInt(data["Uid"]),
                Name = GetValue(data, "Name", string.Empty),
                SoundUid = MathHelper.ParseInt(GetValue(data, "SoundUid", "0")),
                SubType = EnumHelper.ConvertEnum<SoundConstants.SubType>(GetValue(data, "SubType", "None")),
                FileName = GetValue(data, "FileName", string.Empty),
                MaxPlayCount = MathHelper.ParseInt(GetValue(data, "MaxPlayCount", "0")),
                Volume = MathHelper.ParseFloat(GetValue(data, "Volume", "1")),
                PitchMin = MathHelper.ParseFloat(GetValue(data, "PitchMin", "1")),
                PitchMax = MathHelper.ParseFloat(GetValue(data, "PitchMax", "1")),
                Loop = ConvertBooleanLoose(GetValue(data, "Loop", type == SoundConstants.Type.Sfx ? "N" : "Y")),
                FadeDuration = MathHelper.ParseFloat(GetValue(data, "FadeDuration", "0.7")),
                UseIntroScene = ConvertBooleanLoose(GetValue(data, "UseIntroScene", "N")),
                Type = type,
            };
        }

        /// <summary>
        /// 헤더가 없을 수 있는 테이블에서 값을 안전하게 읽습니다.
        /// </summary>
        /// <param name="data">헤더명과 값의 사전입니다.</param>
        /// <param name="key">조회할 헤더명입니다.</param>
        /// <param name="defaultValue">헤더가 없거나 값이 비어 있을 때 사용할 기본값입니다.</param>
        /// <returns>조회된 값 또는 기본값입니다.</returns>
        private static string GetValue(Dictionary<string, string> data, string key, string defaultValue)
        {
            if (data == null || string.IsNullOrWhiteSpace(key))
                return defaultValue;

            return data.TryGetValue(key, out string value) && !string.IsNullOrWhiteSpace(value)
                ? value
                : defaultValue;
        }

        /// <summary>
        /// Y/N, true/false, 1/0 형식의 bool 값을 느슨하게 파싱합니다.
        /// </summary>
        /// <param name="value">원본 문자열입니다.</param>
        /// <returns>true로 해석되는 값이면 true를 반환합니다.</returns>
        private static bool ConvertBooleanLoose(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return false;

            string trimmed = value.Trim();
            return trimmed == "Y"
                   || trimmed == "1"
                   || string.Equals(trimmed, "true", System.StringComparison.OrdinalIgnoreCase)
                   || string.Equals(trimmed, "yes", System.StringComparison.OrdinalIgnoreCase)
                   || string.Equals(trimmed, "on", System.StringComparison.OrdinalIgnoreCase);
        }
    }
}
