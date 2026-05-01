using System;
using System.Collections.Generic;

namespace GGemCo2DCore
{
    /// <summary>
    /// 라이센스 테이블의 한 행을 표현합니다.
    /// </summary>
    public class StruckTableLicense : IUidName
    {
        public int Uid { get; set; }
        public string Key { get; set; }
        public string Name { get; set; }
        public LicenseConstants.ValueType ValueType;
        public string DefaultValue;
        public string Category;
        public string Memo;
    }

    /// <summary>
    /// 라이센스 정의 테이블을 로드하고 UID 및 Key 기반 조회를 제공합니다.
    /// </summary>
    public class TableLicense : DefaultTable<StruckTableLicense>
    {
        private readonly Dictionary<string, StruckTableLicense> _rowsByKey =
            new Dictionary<string, StruckTableLicense>(StringComparer.OrdinalIgnoreCase);

        public override string Key => ConfigAddressableTable.License;

        /// <summary>
        /// 테이블 재로드 전에 Key 조회 캐시를 초기화합니다.
        /// </summary>
        protected override void PreLoad()
        {
            _rowsByKey.Clear();
        }

        /// <summary>
        /// 로드된 라이센스 행을 Key 기준 캐시에 등록합니다.
        /// </summary>
        /// <param name="row">로드가 완료된 라이센스 행입니다.</param>
        protected override void OnLoadedData(StruckTableLicense row)
        {
            if (row == null)
            {
                return;
            }

            row.Key = NormalizeKey(row.Key);
            if (string.IsNullOrEmpty(row.Key))
            {
                GcLogger.LogWarning($"[TableLicense] License key is empty. uid={row.Uid}");
                return;
            }

            if (_rowsByKey.ContainsKey(row.Key))
            {
                GcLogger.LogWarning($"[TableLicense] Duplicate license key: {row.Key}");
            }

            _rowsByKey[row.Key] = row;
        }

        /// <summary>
        /// 테이블 행 데이터를 강타입 라이센스 데이터로 변환합니다.
        /// </summary>
        /// <param name="data">헤더명과 값을 담은 테이블 행 사전입니다.</param>
        /// <returns>변환된 라이센스 테이블 행입니다.</returns>
        protected override StruckTableLicense BuildRow(Dictionary<string, string> data)
        {
            return new StruckTableLicense
            {
                Uid = MathHelper.ParseInt(data.GetValueOrDefault("Uid")),
                Key = NormalizeKey(data.GetValueOrDefault("Key")),
                Name = data.GetValueOrDefault("Name"),
                ValueType = GetValueType(data.GetValueOrDefault("ValueType")),
                DefaultValue = data.GetValueOrDefault("DefaultValue"),
                Category = data.GetValueOrDefault("Category"),
                Memo = data.GetValueOrDefault("Memo"),
            };
        }

        /// <summary>
        /// 라이센스 Key로 테이블 행을 조회합니다.
        /// </summary>
        /// <param name="key">조회할 라이센스 Key입니다.</param>
        /// <returns>찾은 라이센스 행입니다. 없으면 null을 반환합니다.</returns>
        public StruckTableLicense GetDataByKey(string key)
        {
            key = NormalizeKey(key);
            return !string.IsNullOrEmpty(key) && _rowsByKey.TryGetValue(key, out StruckTableLicense row)
                ? row
                : null;
        }

        /// <summary>
        /// 라이센스 Key로 테이블 행 조회를 시도합니다.
        /// </summary>
        /// <param name="key">조회할 라이센스 Key입니다.</param>
        /// <param name="row">조회에 성공하면 라이센스 행이 설정됩니다.</param>
        /// <returns>행을 찾으면 true를 반환합니다.</returns>
        public bool TryGetDataByKey(string key, out StruckTableLicense row)
        {
            row = GetDataByKey(key);
            return row != null;
        }

        /// <summary>
        /// 라이센스 Key에서 앞뒤 공백을 제거합니다.
        /// </summary>
        /// <param name="key">정규화할 라이센스 Key입니다.</param>
        /// <returns>정규화된 Key입니다. 유효하지 않으면 null을 반환합니다.</returns>
        private static string NormalizeKey(string key)
        {
            return string.IsNullOrWhiteSpace(key) ? null : key.Trim();
        }

        /// <summary>
        /// 테이블 문자열을 라이센스 값 타입으로 변환합니다.
        /// </summary>
        /// <param name="value">테이블에 입력된 값 타입 문자열입니다.</param>
        /// <returns>변환된 라이센스 값 타입입니다.</returns>
        private static LicenseConstants.ValueType GetValueType(string value)
        {
            return string.IsNullOrWhiteSpace(value)
                ? LicenseConstants.ValueType.String
                : EnumHelper.ConvertEnum<LicenseConstants.ValueType>(value);
        }
    }
}
