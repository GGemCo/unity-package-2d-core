using System.Collections.Generic;

namespace GGemCo2DCore
{
    /// <summary>
    /// 세이브 슬롯별 인벤토리 지급 완료 버전을 보관합니다.
    /// 아이템 소모나 폐기 여부와 관계없이 동일한 지급이 다시 실행되지 않도록 사용합니다.
    /// </summary>
    public sealed class InventoryGrantHistoryData : ISaveData
    {
        /// <summary>
        /// 지급 식별자별로 마지막 적용 버전을 보관합니다.
        /// </summary>
        public Dictionary<string, int> AppliedVersions = new Dictionary<string, int>();

        /// <summary>
        /// 저장 컨테이너에 기록된 지급 이력을 복원합니다.
        /// </summary>
        /// <param name="saveDataContainer">로드된 Core 저장 데이터 컨테이너입니다.</param>
        public void Initialize(SaveDataContainer saveDataContainer = null)
        {
            Dictionary<string, int> loadedVersions =
                saveDataContainer?.InventoryGrantHistoryData?.AppliedVersions;

            AppliedVersions = loadedVersions != null
                ? new Dictionary<string, int>(loadedVersions)
                : new Dictionary<string, int>();
        }

        /// <summary>
        /// 지정한 지급 버전이 이미 적용되었는지 확인합니다.
        /// </summary>
        /// <param name="grantKey">지급 작업을 구분하는 고유 식별자입니다.</param>
        /// <param name="grantVersion">확인할 지급 버전입니다.</param>
        /// <returns>같거나 더 높은 버전이 적용되어 있으면 true입니다.</returns>
        public bool IsApplied(string grantKey, int grantVersion)
        {
            return !string.IsNullOrWhiteSpace(grantKey) &&
                   grantVersion > 0 &&
                   AppliedVersions != null &&
                   AppliedVersions.TryGetValue(grantKey, out int appliedVersion) &&
                   appliedVersion >= grantVersion;
        }

        /// <summary>
        /// 지정한 지급 작업의 적용 버전을 기록합니다.
        /// </summary>
        /// <param name="grantKey">지급 작업을 구분하는 고유 식별자입니다.</param>
        /// <param name="grantVersion">성공적으로 적용한 지급 버전입니다.</param>
        internal void MarkApplied(string grantKey, int grantVersion)
        {
            if (string.IsNullOrWhiteSpace(grantKey) || grantVersion <= 0)
            {
                return;
            }

            AppliedVersions ??= new Dictionary<string, int>();
            AppliedVersions[grantKey] = grantVersion;
        }
    }
}
