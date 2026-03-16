using System;
using System.Collections.Generic;

namespace GGemCo2DCore
{
    /// <summary>
    /// 장비 및 인벤토리 데이터를 관리하는 공통 클래스
    /// </summary>
    public abstract class ItemStorageData : DefaultData, ISaveData
    {
        // public 으로 해야 json 으로 저장된다. 
        public Dictionary<int, SaveDataIcon> ItemCounts = new();
        // add 할때 남은 개수가 max overlay count 를 넘을때 마다 새로운 슬롯에 넣어야 한다.
        // ItemStorageData 에서는 계산만 하고 실제 window 에서 SetIconCount 를 하기 때문에
        // 임시로 저장할 배열이 필요한다.
        // FindEmptySlot 에서 빈 공간을 찾을때 사용한다.
        public Dictionary<int, SaveDataIcon> TempItemCounts = new();

        protected TableLoaderManager TableLoaderManager;
        
        /// <summary>
        /// 초기화. Awake 단계에서 실행
        /// </summary>
        /// <param name="loader"></param>
        /// <param name="saveDataContainer"></param>
        public virtual void Initialize(TableLoaderManager loader, SaveDataContainer saveDataContainer = null)
        {
            TableLoaderManager = loader;
        }
        /// <summary>
        /// 아이템 개수 설정
        /// </summary>
        public void SetItemCount(int slotIndex, int itemUid, int value, long instanceId = 0)
        {
            if (slotIndex < 0 || slotIndex >= MaxSlotCount) return;

            ItemCounts[slotIndex] = new SaveDataIcon(slotIndex, itemUid, value, instanceId: instanceId);
            SaveDatas();
        }
        /// <summary>
        /// 아이템 개수 빼기 
        /// </summary>
        /// <param name="slotIndex"></param>
        /// <param name="itemUid"></param>
        /// <param name="itemCount"></param>
        /// <returns></returns>
        public ResultCommon MinusItem(int slotIndex, int itemUid, int itemCount)
        {
            if (slotIndex < 0 || slotIndex >= MaxSlotCount)
            {
                return ResultCommon.Fail("Slot_InvalidSlot", $"slotIndex: {slotIndex}."); // 슬롯 정보가 잘 못 되었습니다.
            }
            var info = TableLoaderManager.Instance.GetItemData(itemUid);
            if (info == null || info.Uid <= 0)
            {
                return ResultCommon.Fail();
            }
            
            if (!ItemCounts.TryGetValue(slotIndex, out var item))
                return ResultCommon.Fail($"Slot_ItemNotFound", $"slotIndex: {slotIndex}");//아이템 정보가 없습니다.
            
            List<SaveDataIcon> controls = new List<SaveDataIcon>();
            
            int count = item.Count - itemCount;
            switch (count)
            {
                case < 0:
                    return ResultCommon.Fail($"Slot_NotEnoughItemCount", $"slotIndex: {slotIndex}");//슬롯에 있는 아이템 개수가 부족합니다.
                case 0:
                    controls.Add(new SaveDataIcon(slotIndex, 0));
                    break;
                default:
                    controls.Add(new SaveDataIcon(slotIndex, itemUid, count));
                    break;
            }

            return ResultCommon.SuccessWithIcons(controls);
        }

        public ResultCommon AddItem(IconPayload iconPayload)
        {
            int emptyIndex = FindEmptySlot();
            if (emptyIndex == -1)
            {
                return ResultCommon.Fail("Inventory_NoSpace");//"인벤토리에 빈 공간이 없습니다."
            }
            int itemUid = iconPayload.Uid;
            var info = TableLoaderManager.Instance.GetItemData(itemUid);
            if (info == null || info.Uid <= 0)
            {
                return ResultCommon.Fail();
            }
            
            int itemCount = iconPayload.Count;
            long instanceId = iconPayload.InstanceId;
            return AddItem(emptyIndex, itemUid, itemCount, instanceId);
        }

        public ResultCommon AddItem(int slotIndex, IconPayload iconPayload)
        {
            int itemUid = iconPayload.Uid;
            int itemCount = iconPayload.Count;
            long instanceId = iconPayload.InstanceId;
            return AddItem(slotIndex, itemUid, itemCount, instanceId);
        }
        /// <summary>
        /// 아이템 추가.
        /// </summary>
        public ResultCommon AddItem(int itemUid, int itemCount)
        {
            var info = TableLoaderManager.Instance.GetItemData(itemUid);
            if (info == null || info.Uid <= 0)
            {
                return ResultCommon.Fail();
            }

            // 재화 별도 처리 
            if (info.Type == ItemConstants.Type.Currency)
            {
                CurrencyConstants.Type currencyType = CurrencyConstants.Type.None;
                if (info.Category == ItemConstants.Category.Gold)
                {
                    currencyType = CurrencyConstants.Type.Gold;
                }
                else if (info.Category == ItemConstants.Category.Silver)
                {
                    currencyType = CurrencyConstants.Type.Silver;
                }
                return SceneGame.Instance.saveDataManager.Player.AddCurrency(currencyType, itemCount);
            }
            else
            {
                int maxOverlayCount = info.MaxOverlayCount;
                if (maxOverlayCount <= 0)
                {
                    return ResultCommon.Fail("Slot_MaxStackZero", $"itemUid: {itemUid}");//최대 중첩 개수가 0 입니다.
                }
                int remainingValue = itemCount;

                List<SaveDataIcon> controls = new List<SaveDataIcon>();
                // 1. 기존 중첩 가능한 아이템 채우기
                foreach (var structInventoryIcon in ItemCounts)
                {
                    var slotIndex = structInventoryIcon.Key;
                    var item = structInventoryIcon.Value;
                    if (item.Uid != itemUid) continue;
                    int availableSpace = maxOverlayCount - item.Count;
                    if (availableSpace > 0)
                    {
                        int addedAmount = Math.Min(remainingValue, availableSpace);
                        int count = item.Count + addedAmount;
                        
                        controls.Add(new SaveDataIcon(slotIndex, itemUid, count));
                        
                        remainingValue -= addedAmount;
                        if (remainingValue <= 0) break;
                    }
                }

                TempItemCounts.Clear();
                // 2. 남은 개수를 새 슬롯에 추가
                while (remainingValue > 0)
                {
                    int emptyIndex = FindEmptySlot();
                    if (emptyIndex == -1)
                    {
                        return ResultCommon.Fail("Inventory_NoSpace");//"인벤토리에 빈 공간이 없습니다."
                    }

                    int addedAmount = Math.Min(remainingValue, maxOverlayCount);
                    controls.Add(new SaveDataIcon(emptyIndex, itemUid, addedAmount));
                    TempItemCounts.TryAdd(emptyIndex, new SaveDataIcon(emptyIndex, itemUid, addedAmount));
                    remainingValue -= addedAmount;
                }

                SaveDatas();
                return ResultCommon.SuccessWithIcons(controls);
            }
        }

        /// <summary>
        /// 특정 슬롯에 아이템 개수 추가
        /// </summary>
        public ResultCommon AddItem(int slotIndex, int itemUid, int itemCount, long instanceId = 0)
        {
            if (slotIndex < 0 || slotIndex >= MaxSlotCount)
            {
                return ResultCommon.Fail("Slot_InvalidIndex");//"잘못된 슬롯 인덱스입니다."
            }

            var info = TableLoaderManager.Instance.GetItemData(itemUid);
            if (info == null || info.Uid <= 0)
            {
                return ResultCommon.Fail();
            }
            // 여유 있게 1개의 공간이 남아있는지 체크 
            int emptyIndex = FindEmptySlot();
            if (emptyIndex == -1)
            {
                return ResultCommon.Fail("Inventory_NoSpace");//"인벤토리에 공간이 부족합니다."
            }

            int maxOverlayCount = info.MaxOverlayCount;
            int remainingValue = itemCount;

            List<SaveDataIcon> controls = new List<SaveDataIcon>();

            // 1. 기존 중첩 가능한 아이템 채우기
            if (ItemCounts.TryGetValue(slotIndex, out var item))
            {
                int availableSpace = maxOverlayCount - item.Count;
                // todo. InstanceId값이 있으면 중첩 안되도록 처리해야 함
                if ((item.Uid == itemUid || item.Uid == 0) && availableSpace > 0)
                {
                    int addedAmount = Math.Min(remainingValue, availableSpace);
                    int count = item.Count + addedAmount;
                    controls.Add(new SaveDataIcon(slotIndex, itemUid, count, instanceId: instanceId));

                    remainingValue -= addedAmount;
                }
            }
            else
            {
                controls.Add(new SaveDataIcon(slotIndex, itemUid, itemCount, instanceId: instanceId));
                remainingValue -= itemCount;
            }
        
            TempItemCounts.Clear();
            // 2. 남은 개수를 새 슬롯에 추가
            while (remainingValue > 0)
            {
                emptyIndex = FindEmptySlot();
                if (emptyIndex == -1)
                {
                    return ResultCommon.Fail("Inventory_NoSpace");//"인벤토리에 공간이 부족합니다."
                }

                int addedAmount = Math.Min(remainingValue, maxOverlayCount);
                controls.Add(new SaveDataIcon(emptyIndex, itemUid, addedAmount, instanceId: instanceId));
                TempItemCounts.TryAdd(emptyIndex, new SaveDataIcon(emptyIndex, itemUid, addedAmount, instanceId: instanceId));
                remainingValue -= addedAmount;
            }
            return ResultCommon.SuccessWithIcons(controls);
        }
        /// <summary>
        /// 특정 슬롯 제거
        /// </summary>
        public void RemoveItemCount(int slotIndex)
        {
            if (!ItemCounts.ContainsKey(slotIndex)) return;
            ItemCounts[slotIndex] = new SaveDataIcon(slotIndex, 0);
            SaveDatas();
        }
        /// <summary>
        /// 빈 슬롯 찾기
        /// </summary>
        protected int FindEmptySlot()
        {
            for (int i = 0; i < MaxSlotCount; i++)
            {
                if (!ItemCounts.ContainsKey(i) || ItemCounts[i].Uid <= 0 || ItemCounts[i].Count <= 0)
                {
                    if (!TempItemCounts.ContainsKey(i) || TempItemCounts[i].Uid <= 0 || TempItemCounts[i].Count <= 0)
                    {
                        return i;
                    }
                }
            }
            return -1;
        }

        /// <summary>
        /// 모든 아이템 목록 가져오기
        /// </summary>
        public Dictionary<int, SaveDataIcon> GetAllItemCounts()
        {
            return ItemCounts;
        }

        /// <summary>
        /// 모든 아이템 삭제
        /// </summary>
        public void RemoveAllItems()
        {
            ItemCounts.Clear();
            TempItemCounts.Clear();
        }
        
        
        /// <summary>
        /// 같은 아이템 uid 끼리 합치기 
        /// </summary>
        /// <param name="fromIndex">옮길 아이템이 있는 슬롯</param>
        /// <param name="toIndex">옮길 대상 슬롯</param>
        /// <returns>성공 여부</returns>
        public ResultCommon MergeItem(int fromIndex, int toIndex)
        {
            // 이동할 슬롯과 대상 슬롯이 동일하면 무시
            if (fromIndex == toIndex)
            {
                return ResultCommon.Fail("Slot_CannotMoveToSame");//"같은 슬롯으로 이동할 수 없습니다."
            }

            // fromIndex 아이템 존재 확인
            if (!ItemCounts.TryGetValue(fromIndex, out var fromItem) || fromItem.Count <= 0)
            {
                return ResultCommon.Fail($"Slot_ItemNotAvailable", $"fromIndex: {fromIndex}");//이동할 아이템이 없습니다.
            }

            // toIndex 아이템 존재 확인
            if (!ItemCounts.TryGetValue(toIndex, out var toItem) || toItem.Count <= 0)
            {
                return ResultCommon.Fail($"Slot_TargetSlotEmpty", $"toIndex: {toIndex}");//대상 슬롯에 아이템이 없습니다.
            }

            // 같은 아이템인지 확인
            if (fromItem.Uid != toItem.Uid)
            {
                return ResultCommon.Fail("Slot_MergeDifferentTypes");//"다른 종류의 아이템은 합칠 수 없습니다."
            }

            // 아이템 정보 가져오기 (최대 중첩 개수 확인)
            var info = TableLoaderManager.Instance.GetItemData(fromItem.Uid);
            if (info == null || info.Uid <= 0)
            {
                return ResultCommon.Fail();
            }

            int maxOverlayCount = info.MaxOverlayCount; // 최대 중첩 개수
            int availableSpace = maxOverlayCount - toItem.Count; // toIndex 슬롯의 남은 공간

            // 합칠 공간이 없는 경우
            if (availableSpace <= 0)
            {
                return ResultCommon.Fail("Slot_MergeNoSpace");//"대상 슬롯에 합칠 공간이 없습니다."
            }

            // 이동 가능한 개수 계산
            int moveAmount = Math.Min(fromItem.Count, availableSpace);

            List<SaveDataIcon> controls = new List<SaveDataIcon>();
            // 아이템 이동
            int toItemCount = toItem.Count + moveAmount;
            controls.Add(new SaveDataIcon(toIndex, toItem.Uid, toItemCount));
            
            int fromItemCount = fromItem.Count - moveAmount;

            // fromIndex 슬롯이 비었으면 제거
            controls.Add(fromItemCount <= 0
                ? new SaveDataIcon(fromIndex, 0)
                : new SaveDataIcon(fromIndex, fromItem.Uid, fromItemCount));

            // string message = string.Format(LocalizationManager.Instance.GetSystemByKey("Slot_MoveSuccess"), moveAmount, fromIndex, toIndex);
            string message = "";
            return ResultCommon.Success(message, controls);
        }
        /// <summary>
        /// 아이템 uid 로 찾아서 개수 빼기 
        /// </summary>
        /// <param name="itemUid"></param>
        /// <param name="itemCount"></param>
        /// <returns></returns>
        public ResultCommon MinusItem(int itemUid, int itemCount)
        {
            var info = TableLoaderManager.Instance.GetItemData(itemUid);
            if (info == null || info.Uid <= 0)
            {
                return ResultCommon.Fail();
            }

            int remainCount = itemCount;
            List<SaveDataIcon> controls = new List<SaveDataIcon>();
            // 남은 개수가 0 이 될때까지 같은 item uid 를 찾아서 빼준다.
            foreach (var data in ItemCounts)
            {
                SaveDataIcon saveDataIcon = data.Value;
                if (saveDataIcon.Uid == itemUid)
                {
                    remainCount = saveDataIcon.Count - remainCount;
                    if (remainCount < 0)
                    {
                        remainCount = Math.Abs(remainCount);
                        controls.Add(new SaveDataIcon(saveDataIcon.SlotIndex, 0));
                    }
                    else if (remainCount == 0)
                    {
                        controls.Add(new SaveDataIcon(saveDataIcon.SlotIndex, 0));
                    }
                    else
                    {
                        controls.Add(new SaveDataIcon(saveDataIcon.SlotIndex, itemUid, remainCount));
                    }
                }

                if (remainCount == 0)
                {
                    break;
                }
            }

            return ResultCommon.SuccessWithIcons(controls);
        }
    }
}
