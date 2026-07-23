using System;
using System.Collections.Generic;
using System.Linq;

namespace GGemCo2DCore
{
    /// <summary>
    /// 세이브 데이터 - 인벤토리 아이템 정보
    /// </summary>
    public class InventoryData : ItemStorageData
    {
        /// <summary>
        /// 초기화. Awake 단계에서 실행
        /// </summary>
        /// <param name="loader"></param>
        /// <param name="saveDataContainer"></param>
        public override void Initialize(TableLoaderManager loader, SaveDataContainer saveDataContainer = null)
        {
            base.Initialize(loader, saveDataContainer);
            ItemCounts.Clear();
            if (saveDataContainer?.InventoryData != null)
            {
                ItemCounts = new Dictionary<int, SaveDataIcon>(saveDataContainer.InventoryData.ItemCounts);
            }
        }

        /// <summary>
        /// 여러 아이템 지급 항목을 모두 검증한 뒤 인벤토리에 한 번에 반영합니다.
        /// </summary>
        /// <remarks>
        /// 지급 도중 일부 항목만 적용되는 상황을 방지하기 위해 복제된 슬롯 데이터에서 먼저 배치를 계산합니다.
        /// 재화 아이템은 PlayerData를 변경하므로 인벤토리 원자성 범위에 포함하지 않고 지급을 거부합니다.
        /// </remarks>
        /// <param name="entries">지급할 아이템 UID와 수량 목록입니다.</param>
        /// <returns>성공 시 변경된 슬롯 아이콘 목록을 포함한 결과입니다.</returns>
        internal ResultCommon TryApplyGrantItems(IReadOnlyList<InventoryGrantEntry> entries)
        {
            if (entries == null || entries.Count == 0)
            {
                return ResultCommon.Fail(
                    "Inventory_GrantEmpty",
                    "[InventoryData] 지급할 인벤토리 아이템이 없습니다.");
            }

            if (TableLoaderManager == null || MaxSlotCount <= 0)
            {
                return ResultCommon.Fail(
                    "Inventory_GrantNotReady",
                    "[InventoryData] 아이템 테이블 또는 인벤토리 슬롯이 준비되지 않았습니다.");
            }

            Dictionary<int, int> mergedCountsByUid = new Dictionary<int, int>();
            for (int i = 0; i < entries.Count; i++)
            {
                InventoryGrantEntry entry = entries[i];
                if (entry.ItemUid <= 0 || entry.Count <= 0)
                {
                    return ResultCommon.Fail(
                        "Inventory_InvalidGrantItem",
                        $"[InventoryData] 지급 항목이 올바르지 않습니다. index: {i}, itemUid: {entry.ItemUid}, count: {entry.Count}");
                }

                mergedCountsByUid.TryGetValue(entry.ItemUid, out int currentCount);
                long mergedCount = (long)currentCount + entry.Count;
                if (mergedCount > int.MaxValue)
                {
                    return ResultCommon.Fail(
                        "Inventory_GrantCountOverflow",
                        $"[InventoryData] 지급 수량이 허용 범위를 초과했습니다. itemUid: {entry.ItemUid}");
                }

                mergedCountsByUid[entry.ItemUid] = (int)mergedCount;
            }

            Dictionary<int, SaveDataIcon> plannedItemCounts =
                new Dictionary<int, SaveDataIcon>(ItemCounts);

            foreach (KeyValuePair<int, int> grant in mergedCountsByUid)
            {
                StruckTableItem itemData =
                    TableLoaderManager.GetItemData(grant.Key, logIfMissing: false);
                if (itemData == null || itemData.Uid <= 0)
                {
                    return ResultCommon.Fail(
                        "Inventory_GrantItemNotFound",
                        $"[InventoryData] 지급할 아이템 테이블 정보를 찾을 수 없습니다. itemUid: {grant.Key}");
                }

                if (itemData.Type == ItemConstants.Type.Currency)
                {
                    return ResultCommon.Fail(
                        "Inventory_GrantCurrencyNotSupported",
                        $"[InventoryData] 인벤토리 일괄 지급에는 재화 아이템을 사용할 수 없습니다. itemUid: {grant.Key}");
                }

                if (itemData.MaxOverlayCount <= 0)
                {
                    return ResultCommon.Fail(
                        "Slot_MaxStackZero",
                        $"[InventoryData] 최대 중첩 수량이 올바르지 않습니다. itemUid: {grant.Key}");
                }

                if (!TryPlanGrantItem(
                        plannedItemCounts,
                        itemData.Uid,
                        grant.Value,
                        itemData.MaxOverlayCount))
                {
                    return ResultCommon.Fail(
                        "Inventory_NoSpace",
                        $"[InventoryData] 시작 아이템을 지급할 인벤토리 공간이 부족합니다. itemUid: {grant.Key}, count: {grant.Value}");
                }
            }

            List<SaveDataIcon> changedIcons = BuildChangedGrantIcons(plannedItemCounts);
            if (changedIcons.Count == 0)
            {
                return ResultCommon.Fail(
                    "Inventory_GrantNoChange",
                    "[InventoryData] 지급 결과에 변경된 인벤토리 슬롯이 없습니다.");
            }

            // 모든 항목의 배치 계산이 성공한 뒤에만 실제 저장 데이터에 반영합니다.
            for (int i = 0; i < changedIcons.Count; i++)
            {
                SaveDataIcon changedIcon = changedIcons[i];
                ItemCounts[changedIcon.SlotIndex] = changedIcon;
            }

            TempItemCounts.Clear();
            return ResultCommon.SuccessWithIcons(changedIcons);
        }

        /// <summary>
        /// 복제된 인벤토리 슬롯에 단일 아이템 지급 결과를 배치합니다.
        /// </summary>
        /// <param name="plannedItemCounts">지급 결과를 계산할 임시 슬롯 데이터입니다.</param>
        /// <param name="itemUid">지급할 아이템 UID입니다.</param>
        /// <param name="itemCount">지급할 수량입니다.</param>
        /// <param name="maxOverlayCount">슬롯 하나의 최대 중첩 수량입니다.</param>
        /// <returns>전체 수량을 배치할 수 있으면 true입니다.</returns>
        private bool TryPlanGrantItem(
            Dictionary<int, SaveDataIcon> plannedItemCounts,
            int itemUid,
            int itemCount,
            int maxOverlayCount)
        {
            int remainingCount = itemCount;

            // 기존 일반 아이템 스택을 슬롯 순서대로 먼저 채웁니다.
            for (int slotIndex = 0; slotIndex < MaxSlotCount && remainingCount > 0; slotIndex++)
            {
                if (!plannedItemCounts.TryGetValue(slotIndex, out SaveDataIcon currentIcon) ||
                    currentIcon == null ||
                    currentIcon.Uid != itemUid ||
                    currentIcon.Count <= 0 ||
                    currentIcon.InstanceId > 0)
                {
                    continue;
                }

                int availableCount = maxOverlayCount - currentIcon.Count;
                if (availableCount <= 0)
                {
                    continue;
                }

                int addedCount = Math.Min(remainingCount, availableCount);
                plannedItemCounts[slotIndex] = new SaveDataIcon(
                    slotIndex,
                    itemUid,
                    currentIcon.Count + addedCount,
                    instanceId: 0,
                    iconType: IconTypeItem);
                remainingCount -= addedCount;
            }

            // 남은 수량은 빈 슬롯을 순서대로 사용해 결정적인 배치 결과를 만듭니다.
            for (int slotIndex = 0; slotIndex < MaxSlotCount && remainingCount > 0; slotIndex++)
            {
                if (plannedItemCounts.TryGetValue(slotIndex, out SaveDataIcon currentIcon) &&
                    currentIcon != null &&
                    currentIcon.Uid > 0 &&
                    currentIcon.Count > 0)
                {
                    continue;
                }

                int addedCount = Math.Min(remainingCount, maxOverlayCount);
                plannedItemCounts[slotIndex] = new SaveDataIcon(
                    slotIndex,
                    itemUid,
                    addedCount,
                    instanceId: 0,
                    iconType: IconTypeItem);
                remainingCount -= addedCount;
            }

            return remainingCount <= 0;
        }

        /// <summary>
        /// 지급 전후 슬롯 데이터를 비교해 실제로 변경된 아이콘 목록을 생성합니다.
        /// </summary>
        /// <param name="plannedItemCounts">지급을 모두 반영한 임시 슬롯 데이터입니다.</param>
        /// <returns>UI 갱신과 결과 전달에 사용할 변경 아이콘 목록입니다.</returns>
        private List<SaveDataIcon> BuildChangedGrantIcons(
            Dictionary<int, SaveDataIcon> plannedItemCounts)
        {
            List<SaveDataIcon> changedIcons = new List<SaveDataIcon>();

            for (int slotIndex = 0; slotIndex < MaxSlotCount; slotIndex++)
            {
                plannedItemCounts.TryGetValue(slotIndex, out SaveDataIcon plannedIcon);
                ItemCounts.TryGetValue(slotIndex, out SaveDataIcon currentIcon);

                if (AreSameGrantIcons(currentIcon, plannedIcon))
                {
                    continue;
                }

                changedIcons.Add(plannedIcon ?? new SaveDataIcon(slotIndex, 0));
            }

            return changedIcons;
        }

        /// <summary>
        /// 두 인벤토리 아이콘이 지급 관점에서 같은 상태인지 확인합니다.
        /// </summary>
        /// <param name="left">기존 아이콘입니다.</param>
        /// <param name="right">지급 후 아이콘입니다.</param>
        /// <returns>UID, 수량, 인스턴스 ID가 모두 같으면 true입니다.</returns>
        private static bool AreSameGrantIcons(SaveDataIcon left, SaveDataIcon right)
        {
            if (ReferenceEquals(left, right))
            {
                return true;
            }

            if (left == null || right == null)
            {
                return false;
            }

            return left.Uid == right.Uid &&
                   left.Count == right.Count &&
                   left.InstanceId == right.InstanceId;
        }

        protected override int GetMaxSlotCount()
        {
            return SceneGame.Instance.uIWindowManager?
                .GetUIWindowByUid<UIWindowInventory>(UIWindowConstants.WindowUid.Inventory)?.maxCountIcon ?? 0;
        }
        
        /// <summary>
        /// 인벤토리 아이템을 정렬/병합합니다.
        /// 일반 아이템(InstanceId == 0)은 같은 UID 기준으로 병합하고,
        /// 인스턴스 아이템(InstanceId &gt; 0)은 고유성을 유지하기 위해 병합하지 않습니다.
        /// </summary>
        public void MergeAllItems()
        {
            // 1. 기존 아이템 데이터를 백업 (초기화 전에 저장)
            var itemBackup = ItemCounts.ToDictionary(entry => entry.Key, entry => entry.Value);

            // 2. SubCategory와 기존 슬롯 순서를 기준으로 정렬된 원본 목록 생성
            var sortedItems = itemBackup
                .Where(p => p.Value.Uid > 0) // 빈 슬롯 제외
                .Select(p =>
                {
                    var info = TableLoaderManager.GetItemData(p.Value.Uid);
                    if (info == null || info.Uid <= 0)
                    {
                        return null;
                    }

                    return new
                    {
                        SlotIndex = p.Key,
                        ItemUid = p.Value.Uid,
                        ItemCount = p.Value.Count,
                        InstanceId = p.Value.InstanceId,
                        SubCategory = info.SubCategory == ItemConstants.SubCategory.None ? int.MaxValue : (int)info.SubCategory // SubCategory가 없으면 가장 뒤로 정렬
                    };
                })
                .Where(item => item != null)
                .OrderBy(item => item.SubCategory)  // SubCategory 기준 정렬
                .ThenBy(item => item.SlotIndex)     // 같은 SubCategory 내에서는 슬롯 인덱스 기준 정렬
                .ToList();

            Dictionary<int, int> stackableTotalsByUid = new Dictionary<int, int>();
            Dictionary<int, int> stackableSubCategoryByUid = new Dictionary<int, int>();
            Dictionary<int, int> stackableFirstSlotByUid = new Dictionary<int, int>();
            List<(int ItemUid, int Count, long InstanceId, int SubCategory, int SlotIndex)> instanceEntries =
                new List<(int ItemUid, int Count, long InstanceId, int SubCategory, int SlotIndex)>();

            // 3. 일반 아이템/인스턴스 아이템을 분리 수집한다.
            foreach (var item in sortedItems)
            {
                if (item.InstanceId > 0)
                {
                    instanceEntries.Add((item.ItemUid, item.ItemCount, item.InstanceId, item.SubCategory, item.SlotIndex));
                    continue;
                }

                if (!stackableTotalsByUid.ContainsKey(item.ItemUid))
                {
                    stackableTotalsByUid[item.ItemUid] = 0;
                    stackableSubCategoryByUid[item.ItemUid] = item.SubCategory;
                    stackableFirstSlotByUid[item.ItemUid] = item.SlotIndex;
                }

                stackableTotalsByUid[item.ItemUid] += item.ItemCount;
            }

            // 4. 기존 데이터를 확실히 초기화
            ItemCounts.Clear();

            var stackableEntries = stackableTotalsByUid.Keys
                .Select(uid => new
                {
                    Kind = 0, // 0: 스택 아이템, 1: 인스턴스 아이템
                    ItemUid = uid,
                    Count = stackableTotalsByUid[uid],
                    InstanceId = 0L,
                    SubCategory = stackableSubCategoryByUid[uid],
                    SlotIndex = stackableFirstSlotByUid[uid]
                });

            var instanceOrderedEntries = instanceEntries.Select(item => new
            {
                Kind = 1,
                ItemUid = item.ItemUid,
                Count = item.Count,
                item.InstanceId,
                item.SubCategory,
                item.SlotIndex
            });

            // 5. 같은 정렬 축(SubCategory, 기존 슬롯 순서)을 유지한 채로 재배치한다.
            var orderedEntries = stackableEntries
                .Concat(instanceOrderedEntries)
                .OrderBy(item => item.SubCategory)
                .ThenBy(item => item.SlotIndex)
                .ThenBy(item => item.Kind)
                .ToList();

            int newSlotIndex = 0;
            foreach (var entry in orderedEntries)
            {
                // 인스턴스 아이템은 병합하지 않고 원래 Count/InstanceId를 보존한다.
                if (entry.InstanceId > 0)
                {
                    int preservedCount = Math.Max(entry.Count, 1);
                    ItemCounts[newSlotIndex] = new SaveDataIcon(newSlotIndex, entry.ItemUid, preservedCount,
                        instanceId: entry.InstanceId, iconType: IconTypeItem);
                    newSlotIndex++;
                    continue;
                }

                var info = TableLoaderManager.GetItemData(entry.ItemUid);
                if (info == null || info.Uid <= 0) continue;

                int maxOverlayCount = Math.Max(info.MaxOverlayCount, 1);
                int totalItemCount = entry.Count;

                // 일반 아이템은 최대 중첩 수량 기준으로 병합 배치한다.
                while (totalItemCount > 0)
                {
                    int addAmount = Math.Min(totalItemCount, maxOverlayCount);
                    ItemCounts[newSlotIndex] = new SaveDataIcon(newSlotIndex, entry.ItemUid, addAmount, iconType: IconTypeItem);
                    totalItemCount -= addAmount;
                    newSlotIndex++;
                }
            }

            // 6. 변경된 데이터 저장
            SaveDatas();
        }
        /// <summary>
        /// 아이템 나누기
        /// </summary>
        /// <param name="slotIndex">인벤토리에 있는 슬롯 index</param>
        /// <param name="itemUid"></param>
        /// <param name="itemCount">원래 가지고 있던 count</param>
        /// <param name="splitItemCount">나누려고 하는 count</param>
        public ResultCommon SplitItem(int slotIndex, int itemUid, int itemCount, int splitItemCount)
        {
            TempItemCounts.Clear();
            int emptySlot = FindEmptySlot();
            if (emptySlot < 0)
            {
                return ResultCommon.Fail("Inventory_NoSpace"); //"인벤토리에 빈 공간이 없습니다."
            }
            if (itemUid <= 0)
            {
                return ResultCommon.Fail("Inventory_Split_NoInfo");//"나누려고 하는 아이템 정보가 없습니다."
            }
            if (splitItemCount <= 0)
            {
                return ResultCommon.Fail("Inventory_Split_InvalidCount");//"나누려고 하는 아이템 개수가 잘 못되었습니다."
            }
            var info = TableLoaderManager.GetItemData(itemUid);
            if (info == null || info.Uid <= 0)
            {
                return ResultCommon.Fail();
            }

            List<SaveDataIcon> controls = new List<SaveDataIcon>();
            int count = itemCount - splitItemCount;
            controls.Add(count <= 0 ? new SaveDataIcon(slotIndex, 0) : new SaveDataIcon(slotIndex, itemUid, count, iconType: IconTypeItem));

            controls.Add(new SaveDataIcon(emptySlot, itemUid, splitItemCount, iconType: IconTypeItem));
            
            return ResultCommon.SuccessWithIcons(controls);
        }

        public int GetCountByItemUid(int itemUid)
        {
            if (itemUid <= 0) return 0;
            int totalCount = 0;
            foreach (var info in ItemCounts)
            {
                SaveDataIcon saveDataIcon = info.Value;
                if (saveDataIcon.Uid == itemUid)
                {
                    totalCount += saveDataIcon.Count;
                }
            }
            return totalCount;
        }

        /// <summary>
        /// 지정한 아이템 카테고리와 서브 카테고리에 해당하는 모든 아이템을 인벤토리에 추가합니다.
        /// 서브 카테고리 목록이 비어 있으면 카테고리만 기준으로 필터링합니다.
        /// </summary>
        /// <param name="category">생성할 아이템의 카테고리입니다.</param>
        /// <param name="subCategories">생성할 아이템의 서브 카테고리 목록입니다. null 또는 빈 목록이면 전체 서브 카테고리를 허용합니다.</param>
        /// <param name="itemCount">아이템별로 추가할 수량입니다.</param>
        /// <returns>추가된 아이콘 변경 목록을 포함한 처리 결과입니다.</returns>
        public ResultCommon AddItemsByCategory(
            ItemConstants.Category category,
            IReadOnlyList<ItemConstants.SubCategory> subCategories,
            int itemCount = 1)
        {
            if (category == ItemConstants.Category.None)
            {
                return ResultCommon.Fail("Inventory_InvalidCategory");
            }

            if (itemCount <= 0)
            {
                return ResultCommon.Fail("Slot_InvalidItemCount", $"itemCount: {itemCount}");
            }

            TableItem tableItem = TableLoaderManager?.TableItem;
            if (tableItem == null)
            {
                return ResultCommon.Fail("Inventory_ItemTableNotReady");
            }

            List<SaveDataIcon> resultIcons = new List<SaveDataIcon>();
            int addedItemTypeCount = 0;

            foreach (StruckTableItem itemData in tableItem.GetAll().Values)
            {
                if (itemData == null ||
                    itemData.Uid <= 0 ||
                    itemData.Category != category ||
                    !ContainsSubCategory(subCategories, itemData.SubCategory))
                {
                    continue;
                }

                ResultCommon addResult = AddItem(itemData.Uid, itemCount);
                if (addResult == null || addResult.Result != ResultCommon.ResultType.Success)
                {
                    return addResult ?? ResultCommon.Fail("Inventory_AddItemByCategoryFailed");
                }

                // AddItem은 변경될 아이콘 목록만 반환하므로, 다음 아이템 배치 계산을 위해 임시로 저장 데이터에 반영한다.
                ApplyResultIconsToItemCounts(addResult.ResultIcons, resultIcons);
                addedItemTypeCount++;
            }

            if (addedItemTypeCount <= 0)
            {
                return ResultCommon.Fail(
                    "Inventory_NoMatchedItemCategory",
                    $"category: {category}, subCategories: {FormatSubCategories(subCategories)}");
            }

            SaveDatas();
            return ResultCommon.SuccessWithIcons(resultIcons);
        }

        /// <summary>
        /// 서브 카테고리 필터에 지정한 값이 포함되어 있는지 확인합니다.
        /// 필터가 비어 있으면 모든 서브 카테고리를 허용합니다.
        /// </summary>
        /// <param name="subCategories">허용할 서브 카테고리 목록입니다.</param>
        /// <param name="subCategory">검사할 서브 카테고리입니다.</param>
        /// <returns>허용 목록에 포함되거나 허용 목록이 비어 있으면 true입니다.</returns>
        private static bool ContainsSubCategory(
            IReadOnlyList<ItemConstants.SubCategory> subCategories,
            ItemConstants.SubCategory subCategory)
        {
            if (subCategories == null || subCategories.Count == 0)
            {
                return true;
            }

            for (int i = 0; i < subCategories.Count; i++)
            {
                if (subCategories[i] == subCategory)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// 아이템 추가 결과를 인벤토리 저장 데이터에 반영하고, UI 갱신용 결과 목록에 누적합니다.
        /// </summary>
        /// <param name="sourceIcons">현재 아이템 추가로 변경된 아이콘 목록입니다.</param>
        /// <param name="destinationIcons">외부로 반환할 누적 아이콘 목록입니다.</param>
        private void ApplyResultIconsToItemCounts(
            IReadOnlyList<SaveDataIcon> sourceIcons,
            List<SaveDataIcon> destinationIcons)
        {
            if (sourceIcons == null)
            {
                return;
            }

            for (int i = 0; i < sourceIcons.Count; i++)
            {
                SaveDataIcon icon = sourceIcons[i];
                if (icon == null)
                {
                    continue;
                }

                ItemCounts[icon.SlotIndex] = new SaveDataIcon(
                    icon.SlotIndex,
                    icon.Uid,
                    icon.Count,
                    icon.Level,
                    icon.IsLearned,
                    icon.InstanceId,
                    icon.IconType);

                destinationIcons?.Add(icon);
            }
        }

        /// <summary>
        /// 디버그 로그에 사용할 서브 카테고리 목록 문자열을 생성합니다.
        /// </summary>
        /// <param name="subCategories">표시할 서브 카테고리 목록입니다.</param>
        /// <returns>쉼표로 연결한 서브 카테고리 문자열입니다.</returns>
        private static string FormatSubCategories(IReadOnlyList<ItemConstants.SubCategory> subCategories)
        {
            if (subCategories == null || subCategories.Count == 0)
            {
                return "All";
            }

            string result = subCategories[0].ToString();
            for (int i = 1; i < subCategories.Count; i++)
            {
                result += $",{subCategories[i]}";
            }

            return result;
        }

        /// <summary>
        /// 퀵슬롯이 참조할 실제 인벤토리 슬롯을 찾습니다.
        /// 인스턴스 아이템은 instanceId 를 우선 매칭하고,
        /// 그렇지 않으면 같은 itemUid 를 가진 첫 번째 점유 슬롯을 사용합니다.
        /// </summary>
        public bool TryFindUsableSlot(int itemUid, long instanceId, out int slotIndex)
        {
            slotIndex = -1;
            if (itemUid <= 0)
                return false;

            if (instanceId > 0)
            {
                foreach (var pair in ItemCounts)
                {
                    var icon = pair.Value;
                    if (icon == null || icon.Uid != itemUid || icon.Count <= 0)
                        continue;

                    if (icon.InstanceId != instanceId)
                        continue;

                    slotIndex = pair.Key;
                    return true;
                }
            }

            foreach (var pair in ItemCounts)
            {
                var icon = pair.Value;
                if (icon == null || icon.Uid != itemUid || icon.Count <= 0)
                    continue;

                slotIndex = pair.Key;
                return true;
            }

            return false;
        }

        public ResultCommon UpgradeItem(int iconSlotIndex, int resultItemUid)
        {
            SaveDataIcon saveDataIcon = ItemCounts[iconSlotIndex];
            if (saveDataIcon == null) return ResultCommon.Fail("Inventory_Upgrade_NoItemInfo");//"강화하려는 아이템 정보가 없습니다."
            saveDataIcon.SetUid(resultItemUid);
            
            List<SaveDataIcon> controls = new List<SaveDataIcon> { new SaveDataIcon(saveDataIcon.SlotIndex, resultItemUid, saveDataIcon.Count, iconType: saveDataIcon.IconType) };
            return ResultCommon.SuccessWithIcons(controls); 
        }

        public void ClearEmptyInfo()
        {
            List<int> emptyKey = new List<int>();
            foreach (var data in ItemCounts)
            {
                if (data.Value.Uid <= 0)
                {
                    emptyKey.Add(data.Key);
                }
            }
            foreach (var key in emptyKey)
            {
                ItemCounts.Remove(key);
            }
        }
    }
}
