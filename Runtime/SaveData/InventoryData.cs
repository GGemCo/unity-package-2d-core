using System;
using System.Collections.Generic;
using System.Linq;

namespace GGemCo.Scripts
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
        public void Initialize(TableLoaderManager loader, SaveDataContainer saveDataContainer = null)
        {
            ItemCounts.Clear();
            if (saveDataContainer?.InventoryData != null)
            {
                ItemCounts = new Dictionary<int, SaveDataIcon>(saveDataContainer.InventoryData.ItemCounts);
            }
        }

        protected override int GetMaxSlotCount()
        {
            return SceneGame.Instance.uIWindowManager
                .GetUIWindowByUid<UIWindowInventory>(UIWindowManager.WindowUid.Inventory)?.maxCountIcon ?? 0;
        }
        /// <summary>
        /// 인벤토리에 있는 같은 아이템을 작은 인덱스부터 최대 중첩 개수까지 합친다.
        /// </summary>
        public void MergeAllItems()
        {
            Dictionary<int, List<int>> itemSlotGroups = new Dictionary<int, List<int>>();
            Dictionary<int, int> itemSubCategoryMap = new Dictionary<int, int>();
            
            // 1. 기존 아이템 데이터를 백업 (초기화 전에 저장)
            var itemBackup = ItemCounts.ToDictionary(entry => entry.Key, entry => entry.Value);

            // 2. 아이템 정렬을 위해 SubCategory 정보 가져오기
            var sortedItems = itemBackup
                .Where(p => p.Value.Uid > 0) // 빈 슬롯 제외
                .Select(p =>
                {
                    var info = TableLoaderManager.Instance.TableItem.GetDataByUid(p.Value.Uid);
                    return new
                    {
                        SlotIndex = p.Key,
                        ItemUid = p.Value.Uid,
                        ItemCount = p.Value.Count,
                        SubCategory = info.SubCategory == ItemConstants.SubCategory.None ? int.MaxValue : (int)info.SubCategory // SubCategory가 없으면 가장 뒤로 정렬
                    };
                })
                .OrderBy(item => item.SubCategory)  // SubCategory 기준 정렬
                .ThenBy(item => item.SlotIndex)     // 같은 SubCategory 내에서는 슬롯 인덱스 기준 정렬
                .ToList();

            // 3. 정렬된 아이템을 그룹화
            foreach (var item in sortedItems)
            {
                if (!itemSlotGroups.ContainsKey(item.ItemUid))
                {
                    itemSlotGroups[item.ItemUid] = new List<int>();
                    itemSubCategoryMap[item.ItemUid] = item.SubCategory;
                }
                itemSlotGroups[item.ItemUid].Add(item.SlotIndex);
            }

            // 4. 기존 데이터를 확실히 초기화
            ItemCounts.Clear();

            // 5. 각 아이템 그룹별 병합 후 새로운 정렬된 슬롯에 배치
            int newSlotIndex = 0;

            foreach (var group in itemSlotGroups.OrderBy(g => itemSubCategoryMap[g.Key])) // SubCategory 기준으로 병합 순서 결정
            {
                int itemUid = group.Key;
                List<int> slots = group.Value;

                if (slots.Count < 1) continue;

                // 아이템 정보 가져오기
                var info = TableLoaderManager.Instance.TableItem.GetDataByUid(itemUid);
                if (info == null || info.Uid <= 0) continue;

                int maxOverlayCount = info.MaxOverlayCount; // 최대 중첩 개수

                // 기존 백업 데이터에서 총 개수 가져오기
                int totalItemCount = slots.Sum(slot => itemBackup.TryGetValue(slot, out var value) ? value.Count : 0);

                // 6. 병합 후 새로운 슬롯에 재배치
                while (totalItemCount > 0)
                {
                    int addAmount = Math.Min(totalItemCount, maxOverlayCount);
                    ItemCounts[newSlotIndex] = new SaveDataIcon(newSlotIndex, itemUid, addAmount);
                    totalItemCount -= addAmount;
                    newSlotIndex++;
                }
            }

            // 7. 변경된 데이터 저장
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
                return new ResultCommon(ResultCommon.Type.Fail, "인벤토리에 빈 공간이 없습니다.");
            }
            if (itemUid <= 0)
            {
                return new ResultCommon(ResultCommon.Type.Fail, "나누려고 하는 아이템 정보가 없습니다.");
            }
            if (splitItemCount <= 0)
            {
                return new ResultCommon(ResultCommon.Type.Fail, "나누려고 하는 아이템 개수가 잘 못되었습니다.");
            }
            var info = TableLoaderManager.Instance.TableItem.GetDataByUid(itemUid);
            if (info == null || info.Uid <= 0)
            {
                return new ResultCommon(ResultCommon.Type.Fail, "나누려고 하는 아이템 테이블 정보가 없습니다.");
            }

            List<SaveDataIcon> controls = new List<SaveDataIcon>();
            int count = itemCount - splitItemCount;
            controls.Add(count <= 0 ? new SaveDataIcon(slotIndex, 0) : new SaveDataIcon(slotIndex, itemUid, count));

            controls.Add(new SaveDataIcon(emptySlot, itemUid, splitItemCount));
            
            return new ResultCommon(ResultCommon.Type.Success, "", controls); 
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

        public ResultCommon UpgradeItem(int iconSlotIndex, int resultItemUid)
        {
            SaveDataIcon saveDataIcon = ItemCounts[iconSlotIndex];
            if (saveDataIcon == null) return new ResultCommon(ResultCommon.Type.Fail, "강화하려는 아이템 정보가 없습니다.");
            saveDataIcon.SetUid(resultItemUid);
            
            List<SaveDataIcon> controls = new List<SaveDataIcon>();
            controls.Add(new SaveDataIcon(saveDataIcon.SlotIndex, resultItemUid, saveDataIcon.Count));
            return new ResultCommon(ResultCommon.Type.Success, "", controls); 
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