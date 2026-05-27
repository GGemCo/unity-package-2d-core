using System.Collections.Generic;

namespace GGemCo2DCore
{
    /// <summary>
    /// 대표 sound UID가 Variant 방식으로 재생될 때 사용할 후보 리소스와 확률 보정값입니다.
    /// </summary>
    public class StruckTableSoundVariant : IUidName
    {
        public int Uid { get; set; }
        public string Name { get; set; }
        public int SoundUid;
        public int CandidateResourceUid;
        public int Weight;
        public float VolumeScale;
        public float PitchMinOverride;
        public float PitchMaxOverride;
        public bool Enabled;
    }

    public class TableSoundVariant : DefaultTable<StruckTableSoundVariant>
    {
        private readonly Dictionary<int, List<StruckTableSoundVariant>> _variantsBySoundUid = new Dictionary<int, List<StruckTableSoundVariant>>();

        public override string Key => ConfigAddressableTable.SoundVariant;

        protected override void PreLoad()
        {
            _variantsBySoundUid.Clear();
        }

        protected override StruckTableSoundVariant BuildRow(Dictionary<string, string> data)
        {
            return new StruckTableSoundVariant
            {
                Uid = MathHelper.ParseInt(data["Uid"]),
                Name = GetValue(data, "Name", string.Empty),
                SoundUid = MathHelper.ParseInt(GetValue(data, "SoundUid", "0")),
                CandidateResourceUid = MathHelper.ParseInt(GetValue(data, "CandidateResourceUid", GetValue(data, "CandidateUid", "0"))),
                Weight = MathHelper.ParseInt(GetValue(data, "Weight", "1")),
                VolumeScale = MathHelper.ParseFloat(GetValue(data, "VolumeScale", "1")),
                PitchMinOverride = MathHelper.ParseFloat(GetValue(data, "PitchMinOverride", "0")),
                PitchMaxOverride = MathHelper.ParseFloat(GetValue(data, "PitchMaxOverride", "0")),
                Enabled = ConvertBooleanLoose(GetValue(data, "Enabled", "Y")),
            };
        }

        protected override void OnLoadedData(StruckTableSoundVariant row)
        {
            if (row == null || row.SoundUid <= 0)
                return;

            if (!_variantsBySoundUid.TryGetValue(row.SoundUid, out List<StruckTableSoundVariant> variants))
            {
                variants = new List<StruckTableSoundVariant>();
                _variantsBySoundUid[row.SoundUid] = variants;
            }

            variants.Add(row);
        }

        /// <summary>
        /// 대표 sound UID에 연결된 후보 목록을 반환합니다.
        /// </summary>
        /// <param name="soundUid">대표 sound UID입니다.</param>
        /// <returns>후보 목록입니다. 없으면 빈 목록입니다.</returns>
        public IReadOnlyList<StruckTableSoundVariant> GetVariants(int soundUid)
        {
            return _variantsBySoundUid.TryGetValue(soundUid, out List<StruckTableSoundVariant> variants)
                ? variants
                : System.Array.Empty<StruckTableSoundVariant>();
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
