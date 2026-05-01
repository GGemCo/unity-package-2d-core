using System;
using System.Collections.Generic;
using System.Globalization;
using Newtonsoft.Json;

namespace GGemCo2DCore
{
    /// <summary>
    /// 세이브 파일에 저장되는 라이센스 상태 데이터입니다.
    /// </summary>
    public sealed class LicenseData : DefaultData, ISaveData
    {
        public Dictionary<string, LicenseRecord> Licenses = new Dictionary<string, LicenseRecord>();

        [JsonIgnore] private TableLoaderManager _tableLoaderManager;

        /// <summary>
        /// 저장된 라이센스 데이터를 복원하고 런타임 조회에 필요한 테이블 참조를 연결합니다.
        /// </summary>
        /// <param name="loader">라이센스 기본값을 조회할 테이블 로더입니다.</param>
        /// <param name="saveDataContainer">로드된 세이브 데이터 컨테이너입니다.</param>
        public void Initialize(TableLoaderManager loader, SaveDataContainer saveDataContainer = null)
        {
            _tableLoaderManager = loader;
            Licenses.Clear();

            LicenseData loadedData = saveDataContainer?.LicenseData;
            if (loadedData == null || loadedData.Licenses == null)
            {
                return;
            }

            Restore(loadedData.Licenses);
        }

        /// <summary>
        /// 라이센스 Key가 저장 데이터에 존재하는지 확인합니다.
        /// </summary>
        /// <param name="key">확인할 라이센스 Key입니다.</param>
        /// <returns>저장된 라이센스가 있으면 true를 반환합니다.</returns>
        public bool Has(string key)
        {
            key = NormalizeKey(key);
            return !string.IsNullOrEmpty(key) &&
                   Licenses != null &&
                   Licenses.ContainsKey(key);
        }

        /// <summary>
        /// 저장된 라이센스 값을 조회합니다.
        /// </summary>
        /// <param name="key">조회할 라이센스 Key입니다.</param>
        /// <param name="value">조회에 성공하면 저장된 값이 설정됩니다.</param>
        /// <returns>저장된 값이 있으면 true를 반환합니다.</returns>
        public bool TryGetValue(string key, out string value)
        {
            value = null;
            key = NormalizeKey(key);
            if (string.IsNullOrEmpty(key) || Licenses == null)
            {
                return false;
            }

            if (!Licenses.TryGetValue(key, out LicenseRecord record) || record == null)
            {
                return false;
            }

            value = record.Value;
            return true;
        }

        /// <summary>
        /// 라이센스 값을 문자열로 조회합니다.
        /// </summary>
        /// <param name="key">조회할 라이센스 Key입니다.</param>
        /// <param name="defaultValue">저장값과 테이블 기본값이 없을 때 반환할 값입니다.</param>
        /// <returns>조회된 문자열 값입니다.</returns>
        public string GetString(string key, string defaultValue = "")
        {
            if (TryGetValue(key, out string value))
            {
                return value;
            }

            StruckTableLicense tableRow = GetTableRow(key);
            return tableRow != null && !string.IsNullOrEmpty(tableRow.DefaultValue)
                ? tableRow.DefaultValue
                : defaultValue;
        }

        /// <summary>
        /// 라이센스 값을 bool로 조회합니다.
        /// </summary>
        /// <param name="key">조회할 라이센스 Key입니다.</param>
        /// <param name="defaultValue">저장값과 테이블 기본값이 없을 때 반환할 값입니다.</param>
        /// <returns>조회된 bool 값입니다.</returns>
        public bool GetBool(string key, bool defaultValue = false)
        {
            string value = GetString(key, defaultValue ? LicenseConstants.TrueValue : LicenseConstants.FalseValue);
            if (string.IsNullOrWhiteSpace(value))
            {
                return defaultValue;
            }

            switch (value.Trim().ToLowerInvariant())
            {
                case "1":
                case "true":
                case "yes":
                case "on":
                    return true;
                case "0":
                case "false":
                case "no":
                case "off":
                    return false;
                default:
                    return defaultValue;
            }
        }

        /// <summary>
        /// 라이센스 값을 int로 조회합니다.
        /// </summary>
        /// <param name="key">조회할 라이센스 Key입니다.</param>
        /// <param name="defaultValue">저장값과 테이블 기본값이 없을 때 반환할 값입니다.</param>
        /// <returns>조회된 int 값입니다.</returns>
        public int GetInt(string key, int defaultValue = 0)
        {
            return MathHelper.ParseInt(GetString(key, $"{defaultValue}"), defaultValue);
        }

        /// <summary>
        /// 라이센스 값을 float로 조회합니다.
        /// </summary>
        /// <param name="key">조회할 라이센스 Key입니다.</param>
        /// <param name="defaultValue">저장값과 테이블 기본값이 없을 때 반환할 값입니다.</param>
        /// <returns>조회된 float 값입니다.</returns>
        public float GetFloat(string key, float defaultValue = 0f)
        {
            return MathHelper.ParseFloat(GetString(key, defaultValue.ToString(CultureInfo.InvariantCulture)), defaultValue);
        }

        /// <summary>
        /// 라이센스 값을 문자열로 저장합니다.
        /// </summary>
        /// <param name="key">저장할 라이센스 Key입니다.</param>
        /// <param name="value">저장할 값입니다.</param>
        /// <returns>저장 데이터가 변경되면 true를 반환합니다.</returns>
        public bool SetValue(string key, string value = LicenseConstants.TrueValue)
        {
            key = NormalizeKey(key);
            if (string.IsNullOrEmpty(key))
            {
                return false;
            }

            Licenses ??= new Dictionary<string, LicenseRecord>();
            value ??= string.Empty;

            string now = DateTime.UtcNow.ToString("o");
            if (!Licenses.TryGetValue(key, out LicenseRecord record) || record == null)
            {
                Licenses[key] = new LicenseRecord
                {
                    Key = key,
                    Value = value,
                    CreatedAtUtc = now,
                    UpdatedAtUtc = now,
                    SetCount = 1,
                };
                SaveDatas();
                return true;
            }

            if (record.Value == value)
            {
                return false;
            }

            record.Key = key;
            record.Value = value;
            record.UpdatedAtUtc = now;
            record.SetCount = Math.Max(0, record.SetCount) + 1;
            if (string.IsNullOrEmpty(record.CreatedAtUtc))
            {
                record.CreatedAtUtc = now;
            }

            SaveDatas();
            return true;
        }

        /// <summary>
        /// 라이센스 값을 bool로 저장합니다.
        /// </summary>
        /// <param name="key">저장할 라이센스 Key입니다.</param>
        /// <param name="value">저장할 bool 값입니다.</param>
        /// <returns>저장 데이터가 변경되면 true를 반환합니다.</returns>
        public bool SetBool(string key, bool value = true)
        {
            return SetValue(key, value ? LicenseConstants.TrueValue : LicenseConstants.FalseValue);
        }

        /// <summary>
        /// 라이센스 값을 int로 저장합니다.
        /// </summary>
        /// <param name="key">저장할 라이센스 Key입니다.</param>
        /// <param name="value">저장할 int 값입니다.</param>
        /// <returns>저장 데이터가 변경되면 true를 반환합니다.</returns>
        public bool SetInt(string key, int value)
        {
            return SetValue(key, $"{value}");
        }

        /// <summary>
        /// 라이센스 값을 float로 저장합니다.
        /// </summary>
        /// <param name="key">저장할 라이센스 Key입니다.</param>
        /// <param name="value">저장할 float 값입니다.</param>
        /// <returns>저장 데이터가 변경되면 true를 반환합니다.</returns>
        public bool SetFloat(string key, float value)
        {
            return SetValue(key, value.ToString(CultureInfo.InvariantCulture));
        }

        /// <summary>
        /// 저장된 라이센스를 제거합니다.
        /// </summary>
        /// <param name="key">제거할 라이센스 Key입니다.</param>
        /// <returns>라이센스가 제거되면 true를 반환합니다.</returns>
        public bool Remove(string key)
        {
            key = NormalizeKey(key);
            if (string.IsNullOrEmpty(key) || Licenses == null || !Licenses.Remove(key))
            {
                return false;
            }

            SaveDatas();
            return true;
        }

        /// <summary>
        /// 저장 데이터에서 읽은 라이센스 기록을 유효한 항목만 복원합니다.
        /// </summary>
        /// <param name="records">세이브 파일에서 읽은 라이센스 기록입니다.</param>
        private void Restore(Dictionary<string, LicenseRecord> records)
        {
            foreach (var pair in records)
            {
                string key = NormalizeKey(pair.Key);
                LicenseRecord record = pair.Value;
                if (string.IsNullOrEmpty(key) || record == null)
                {
                    continue;
                }

                record.Key = key;
                record.Value ??= string.Empty;
                record.SetCount = Math.Max(1, record.SetCount);
                Licenses[key] = record;
            }
        }

        /// <summary>
        /// 라이센스 테이블에서 Key에 해당하는 행을 조회합니다.
        /// </summary>
        /// <param name="key">조회할 라이센스 Key입니다.</param>
        /// <returns>찾은 라이센스 행입니다. 없으면 null을 반환합니다.</returns>
        private StruckTableLicense GetTableRow(string key)
        {
            key = NormalizeKey(key);
            return string.IsNullOrEmpty(key)
                ? null
                : _tableLoaderManager?.TableLicense?.GetDataByKey(key);
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
        /// 라이센스 데이터는 슬롯 기반 개수를 사용하지 않으므로 0을 반환합니다.
        /// </summary>
        /// <returns>항상 0을 반환합니다.</returns>
        protected override int GetMaxSlotCount()
        {
            return 0;
        }
    }

    /// <summary>
    /// 세이브 파일에 기록되는 단일 라이센스 값입니다.
    /// </summary>
    public sealed class LicenseRecord
    {
        public string Key;
        public string Value;
        public string CreatedAtUtc;
        public string UpdatedAtUtc;
        public int SetCount;
    }
}
