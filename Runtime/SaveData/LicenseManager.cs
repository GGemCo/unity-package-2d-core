namespace GGemCo2DCore
{
    /// <summary>
    /// 라이센스 상태를 설정하고 조회하는 런타임 진입점입니다.
    /// </summary>
    public sealed class LicenseManager
    {
        private readonly SaveDataManager _saveDataManager;

        /// <summary>
        /// 세이브 매니저를 기준으로 라이센스 매니저를 생성합니다.
        /// </summary>
        /// <param name="saveDataManager">라이센스 데이터를 소유한 세이브 매니저입니다.</param>
        public LicenseManager(SaveDataManager saveDataManager)
        {
            _saveDataManager = saveDataManager;
        }

        /// <summary>
        /// 라이센스 Key가 저장 데이터에 존재하는지 확인합니다.
        /// </summary>
        /// <param name="key">확인할 라이센스 Key입니다.</param>
        /// <returns>저장된 라이센스가 있으면 true를 반환합니다.</returns>
        public bool Has(string key)
        {
            return GetData()?.Has(key) == true;
        }

        /// <summary>
        /// 라이센스 UID가 가리키는 Key의 저장 여부를 확인합니다.
        /// </summary>
        /// <param name="licenseUid">license 테이블의 UID입니다.</param>
        /// <returns>저장된 라이센스가 있으면 true를 반환합니다.</returns>
        public bool HasByUid(int licenseUid)
        {
            return TryGetKeyByUid(licenseUid, out string key) && Has(key);
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
            return GetData()?.TryGetValue(key, out value) == true;
        }

        /// <summary>
        /// 라이센스 값을 문자열로 조회합니다.
        /// </summary>
        /// <param name="key">조회할 라이센스 Key입니다.</param>
        /// <param name="defaultValue">저장값과 테이블 기본값이 없을 때 반환할 값입니다.</param>
        /// <returns>조회된 문자열 값입니다.</returns>
        public string GetString(string key, string defaultValue = "")
        {
            LicenseData data = GetData();
            return data != null ? data.GetString(key, defaultValue) : defaultValue;
        }

        /// <summary>
        /// 라이센스 값을 bool로 조회합니다.
        /// </summary>
        /// <param name="key">조회할 라이센스 Key입니다.</param>
        /// <param name="defaultValue">저장값과 테이블 기본값이 없을 때 반환할 값입니다.</param>
        /// <returns>조회된 bool 값입니다.</returns>
        public bool GetBool(string key, bool defaultValue = false)
        {
            LicenseData data = GetData();
            return data != null ? data.GetBool(key, defaultValue) : defaultValue;
        }

        /// <summary>
        /// 라이센스 값을 int로 조회합니다.
        /// </summary>
        /// <param name="key">조회할 라이센스 Key입니다.</param>
        /// <param name="defaultValue">저장값과 테이블 기본값이 없을 때 반환할 값입니다.</param>
        /// <returns>조회된 int 값입니다.</returns>
        public int GetInt(string key, int defaultValue = 0)
        {
            LicenseData data = GetData();
            return data != null ? data.GetInt(key, defaultValue) : defaultValue;
        }

        /// <summary>
        /// 라이센스 값을 float로 조회합니다.
        /// </summary>
        /// <param name="key">조회할 라이센스 Key입니다.</param>
        /// <param name="defaultValue">저장값과 테이블 기본값이 없을 때 반환할 값입니다.</param>
        /// <returns>조회된 float 값입니다.</returns>
        public float GetFloat(string key, float defaultValue = 0f)
        {
            LicenseData data = GetData();
            return data != null ? data.GetFloat(key, defaultValue) : defaultValue;
        }

        /// <summary>
        /// 라이센스 값을 문자열로 저장합니다.
        /// </summary>
        /// <param name="key">저장할 라이센스 Key입니다.</param>
        /// <param name="value">저장할 값입니다.</param>
        /// <returns>저장 데이터가 변경되면 true를 반환합니다.</returns>
        public bool Set(string key, string value = LicenseConstants.TrueValue)
        {
            return GetData()?.SetValue(key, value) == true;
        }

        /// <summary>
        /// 라이센스 UID가 가리키는 Key에 문자열 값을 저장합니다.
        /// </summary>
        /// <param name="licenseUid">license 테이블의 UID입니다.</param>
        /// <param name="value">저장할 값입니다.</param>
        /// <returns>저장 데이터가 변경되면 true를 반환합니다.</returns>
        public bool SetByUid(int licenseUid, string value = LicenseConstants.TrueValue)
        {
            return TryGetKeyByUid(licenseUid, out string key) && Set(key, value);
        }

        /// <summary>
        /// 라이센스 값을 bool로 저장합니다.
        /// </summary>
        /// <param name="key">저장할 라이센스 Key입니다.</param>
        /// <param name="value">저장할 bool 값입니다.</param>
        /// <returns>저장 데이터가 변경되면 true를 반환합니다.</returns>
        public bool SetBool(string key, bool value = true)
        {
            return GetData()?.SetBool(key, value) == true;
        }

        /// <summary>
        /// 라이센스 값을 int로 저장합니다.
        /// </summary>
        /// <param name="key">저장할 라이센스 Key입니다.</param>
        /// <param name="value">저장할 int 값입니다.</param>
        /// <returns>저장 데이터가 변경되면 true를 반환합니다.</returns>
        public bool SetInt(string key, int value)
        {
            return GetData()?.SetInt(key, value) == true;
        }

        /// <summary>
        /// 라이센스 값을 float로 저장합니다.
        /// </summary>
        /// <param name="key">저장할 라이센스 Key입니다.</param>
        /// <param name="value">저장할 float 값입니다.</param>
        /// <returns>저장 데이터가 변경되면 true를 반환합니다.</returns>
        public bool SetFloat(string key, float value)
        {
            return GetData()?.SetFloat(key, value) == true;
        }

        /// <summary>
        /// 저장된 라이센스를 제거합니다.
        /// </summary>
        /// <param name="key">제거할 라이센스 Key입니다.</param>
        /// <returns>라이센스가 제거되면 true를 반환합니다.</returns>
        public bool Remove(string key)
        {
            return GetData()?.Remove(key) == true;
        }

        /// <summary>
        /// 라이센스 UID가 가리키는 Key의 저장값을 제거합니다.
        /// </summary>
        /// <param name="licenseUid">license 테이블의 UID입니다.</param>
        /// <returns>라이센스가 제거되면 true를 반환합니다.</returns>
        public bool RemoveByUid(int licenseUid)
        {
            return TryGetKeyByUid(licenseUid, out string key) && Remove(key);
        }

        /// <summary>
        /// SaveDataManager가 보유한 라이센스 데이터를 반환합니다.
        /// </summary>
        /// <returns>라이센스 데이터입니다. 초기화 전이면 null을 반환합니다.</returns>
        private LicenseData GetData()
        {
            return _saveDataManager != null ? _saveDataManager.License : null;
        }

        /// <summary>
        /// license 테이블 UID를 라이센스 Key로 변환합니다.
        /// </summary>
        /// <param name="licenseUid">license 테이블의 UID입니다.</param>
        /// <param name="key">조회에 성공하면 라이센스 Key가 설정됩니다.</param>
        /// <returns>Key를 찾으면 true를 반환합니다.</returns>
        private static bool TryGetKeyByUid(int licenseUid, out string key)
        {
            key = null;
            if (licenseUid <= 0 || TableLoaderManager.Instance == null)
            {
                return false;
            }

            StruckTableLicense row = TableLoaderManager.Instance.GetLicenseData(licenseUid, false);
            if (row == null || string.IsNullOrWhiteSpace(row.Key))
            {
                return false;
            }

            key = row.Key;
            return true;
        }
    }
}
