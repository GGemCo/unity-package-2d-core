using System.Collections.Generic;

namespace GGemCo2DCore
{
    /// <summary>
    /// 대표 VFX UID가 Variant 방식으로 재생될 때 사용할 실제 VFX 리소스 후보와 보정값입니다.
    /// </summary>
    public class StruckTableVfxVariant : IUidName
    {
        public int Uid { get; set; }
        public string Name { get; set; }
        public int VfxUid;
        public int CandidateVfxResourceUid;
        public VfxConstants.AssetKind CandidateAssetKind;
        public int Weight;
        public float ScaleOverride;
        public float DurationOverride;
        public string ColorOverride;
        public bool Enabled;
    }

    public class TableVfxVariant : DefaultTable<StruckTableVfxVariant>
    {
        private readonly Dictionary<int, List<StruckTableVfxVariant>> _variantsByVfxUid = new Dictionary<int, List<StruckTableVfxVariant>>();

        public override string Key => ConfigAddressableTable.VfxVariant;

        /// <summary>
        /// 테이블 재로딩 전 대표 VFX UID 기반 후보 인덱스를 초기화합니다.
        /// </summary>
        protected override void PreLoad()
        {
            _variantsByVfxUid.Clear();
        }

        /// <summary>
        /// vfx_variant 테이블 한 줄을 후보 VFX 행으로 변환합니다.
        /// </summary>
        /// <param name="data">헤더명과 값의 사전입니다.</param>
        /// <returns>후보 VFX 행입니다.</returns>
        protected override StruckTableVfxVariant BuildRow(Dictionary<string, string> data)
        {
            return new StruckTableVfxVariant
            {
                Uid = MathHelper.ParseInt(GetValue(data, "Uid", "0")),
                Name = GetValue(data, "Name", string.Empty),
                VfxUid = MathHelper.ParseInt(GetValue(data, "VfxUid", "0")),
                CandidateVfxResourceUid = MathHelper.ParseInt(GetValue(data, "CandidateVfxResourceUid", GetValue(data, "CandidateResourceUid", GetValue(data, "CandidateUid", "0")))),
                CandidateAssetKind = EnumHelper.ConvertEnum<VfxConstants.AssetKind>(GetValue(data, "CandidateAssetKind", "None")),
                Weight = MathHelper.ParseInt(GetValue(data, "Weight", "1")),
                ScaleOverride = MathHelper.ParseFloat(GetValue(data, "ScaleOverride", "0")),
                DurationOverride = MathHelper.ParseFloat(GetValue(data, "DurationOverride", "0")),
                ColorOverride = GetValue(data, "ColorOverride", string.Empty),
                Enabled = ConvertBooleanLoose(GetValue(data, "Enabled", "Y")),
            };
        }

        /// <summary>
        /// 로드된 후보를 대표 VFX UID 기준 인덱스에 추가합니다.
        /// </summary>
        /// <param name="row">방금 로드된 후보 VFX 행입니다.</param>
        protected override void OnLoadedData(StruckTableVfxVariant row)
        {
            if (row == null || row.VfxUid <= 0)
                return;

            if (!_variantsByVfxUid.TryGetValue(row.VfxUid, out List<StruckTableVfxVariant> variants))
            {
                variants = new List<StruckTableVfxVariant>();
                _variantsByVfxUid[row.VfxUid] = variants;
            }

            variants.Add(row);
        }

        /// <summary>
        /// 대표 VFX UID에 연결된 후보 목록을 반환합니다.
        /// </summary>
        /// <param name="vfxUid">대표 VFX UID입니다.</param>
        /// <returns>후보 목록입니다. 없으면 빈 목록입니다.</returns>
        public IReadOnlyList<StruckTableVfxVariant> GetVariants(int vfxUid)
        {
            return _variantsByVfxUid.TryGetValue(vfxUid, out List<StruckTableVfxVariant> variants)
                ? variants
                : System.Array.Empty<StruckTableVfxVariant>();
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
