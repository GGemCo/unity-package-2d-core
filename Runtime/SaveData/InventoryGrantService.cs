using System.Collections.Generic;

namespace GGemCo2DCore
{
    /// <summary>
    /// 인벤토리에 일괄 지급할 아이템 UID와 수량을 나타냅니다.
    /// </summary>
    public readonly struct InventoryGrantEntry
    {
        /// <summary>
        /// 지급할 아이템 UID입니다.
        /// </summary>
        public int ItemUid { get; }

        /// <summary>
        /// 지급할 아이템 수량입니다.
        /// </summary>
        public int Count { get; }

        /// <summary>
        /// 인벤토리 일괄 지급 항목을 생성합니다.
        /// </summary>
        /// <param name="itemUid">지급할 아이템 UID입니다.</param>
        /// <param name="count">지급할 아이템 수량입니다.</param>
        public InventoryGrantEntry(int itemUid, int count)
        {
            ItemUid = itemUid;
            Count = count;
        }
    }

    /// <summary>
    /// 세이브 슬롯 단위로 인벤토리 아이템을 중복 없이 일괄 지급합니다.
    /// </summary>
    public static class InventoryGrantService
    {
        /// <summary>
        /// 아이템 목록 전체를 검증한 뒤 인벤토리와 지급 이력에 한 번에 반영합니다.
        /// </summary>
        /// <remarks>
        /// 지급 항목 중 하나라도 유효하지 않거나 공간이 부족하면 인벤토리를 변경하지 않습니다.
        /// 기본적으로 인벤토리와 지급 이력을 같은 Core 저장 파일에 기록하기 위해 지급 성공 직후 저장합니다.
        /// 상위 계층이 신규 슬롯 확정 후 일괄 저장해야 하면 즉시 저장을 생략할 수 있습니다.
        /// 인벤토리 UI는 직접 갱신하지 않으며, 창이 열릴 때 저장 데이터를 기준으로 표시를 구성합니다.
        /// </remarks>
        /// <param name="saveDataManager">Core 저장 데이터 관리자입니다.</param>
        /// <param name="grantKey">지급 작업을 구분하는 고유 식별자입니다.</param>
        /// <param name="grantVersion">지급 목록의 버전입니다.</param>
        /// <param name="entries">지급할 아이템 목록입니다.</param>
        /// <returns>지급 결과와 변경된 인벤토리 아이콘 목록입니다.</returns>
        public static ResultCommon TryGrantItems(
            SaveDataManager saveDataManager,
            string grantKey,
            int grantVersion,
            IReadOnlyList<InventoryGrantEntry> entries)
        {
            return TryGrantItems(
                saveDataManager,
                grantKey,
                grantVersion,
                entries,
                saveImmediately: true);
        }

        /// <summary>
        /// 아이템 목록 전체를 검증한 뒤 인벤토리와 지급 이력에 반영하고 저장 시점을 선택합니다.
        /// </summary>
        /// <remarks>
        /// 신규 게임 슬롯 확정 전처럼 상위 계층이 이후에 전체 데이터를 저장할 경우
        /// <paramref name="saveImmediately"/>를 false로 지정해 메모리에만 반영할 수 있습니다.
        /// </remarks>
        /// <param name="saveDataManager">Core 저장 데이터 관리자입니다.</param>
        /// <param name="grantKey">지급 작업을 구분하는 고유 식별자입니다.</param>
        /// <param name="grantVersion">지급 목록의 버전입니다.</param>
        /// <param name="entries">지급할 아이템 목록입니다.</param>
        /// <param name="saveImmediately">지급 성공 직후 Core 저장 파일에 즉시 기록할지 여부입니다.</param>
        /// <returns>지급 결과와 변경된 인벤토리 아이콘 목록입니다.</returns>
        public static ResultCommon TryGrantItems(
            SaveDataManager saveDataManager,
            string grantKey,
            int grantVersion,
            IReadOnlyList<InventoryGrantEntry> entries,
            bool saveImmediately)
        {
            if (saveDataManager == null ||
                saveDataManager.Inventory == null ||
                saveDataManager.InventoryGrantHistory == null)
            {
                return ResultCommon.Fail(
                    "Inventory_GrantSaveDataNotReady",
                    "[InventoryGrantService] Core 저장 데이터가 준비되지 않았습니다.");
            }

            string normalizedGrantKey = grantKey?.Trim();
            if (string.IsNullOrEmpty(normalizedGrantKey) || grantVersion <= 0)
            {
                return ResultCommon.Fail(
                    "Inventory_InvalidGrantKey",
                    $"[InventoryGrantService] 지급 식별자 또는 버전이 올바르지 않습니다. key: {grantKey}, version: {grantVersion}");
            }

            if (saveDataManager.InventoryGrantHistory.IsApplied(
                    normalizedGrantKey,
                    grantVersion))
            {
                return ResultCommon.SuccessWithIcons(null);
            }

            ResultCommon grantResult =
                saveDataManager.Inventory.TryApplyGrantItems(entries);
            if (grantResult == null ||
                grantResult.Result != ResultCommon.ResultType.Success)
            {
                return grantResult ??
                       ResultCommon.Fail(
                           "Inventory_GrantFailed",
                           $"[InventoryGrantService] 인벤토리 지급에 실패했습니다. key: {normalizedGrantKey}");
            }

            saveDataManager.InventoryGrantHistory.MarkApplied(
                normalizedGrantKey,
                grantVersion);

            if (!saveImmediately)
            {
                // 신규 게임 확인 전에는 인벤토리와 지급 이력을 메모리에 유지하고,
                // 상위 게임 계층이 슬롯을 확정한 뒤 전체 저장 데이터와 함께 기록합니다.
                return grantResult;
            }

            // 지급 서비스는 UI 수명주기에 의존하지 않고 데이터와 지급 이력을 먼저 영속화합니다.
            // 인벤토리 창은 실제로 열릴 때 저장 데이터를 기준으로 아이콘을 다시 구성합니다.
            if (!saveDataManager.SaveData())
            {
                return ResultCommon.Fail(
                    "Inventory_GrantSaveFailed",
                    $"[InventoryGrantService] 지급 결과 저장에 실패했습니다. key: {normalizedGrantKey}");
            }

            return grantResult;
        }
    }
}
