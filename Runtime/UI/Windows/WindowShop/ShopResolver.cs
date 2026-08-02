using System.Collections.Generic;
using UnityEngine;

namespace GGemCo2DCore
{
    /// <summary>
    /// 상점 테이블 데이터를 기반으로 실제 상점에 표시될 아이템 목록을 생성(Resolve)하는 클래스입니다.
    /// 슬롯별 랜덤 선택, 유니크 그룹 제한, 구매 가능 여부 필터링을 처리합니다.
    /// </summary>
    public sealed class ShopResolver
    {
        /// <summary>
        /// 신규 상점 아이템 테이블입니다.
        /// </summary>
        private readonly TableShopItem _tableShopItem;

        /// <summary>
        /// 레거시 상점 테이블입니다. (신규 테이블이 없을 경우 fallback)
        /// </summary>
        private readonly TableShop _legacyTableShop;

        /// <summary>
        /// 아이템 구매 가능 여부를 판단하는 서비스입니다.
        /// </summary>
        private readonly ShopAvailabilityService _availabilityService;

        /// <summary>
        /// 상점 UID별로 롤링된 결과를 캐싱합니다. (slotIndex → item)
        /// </summary>
        private readonly Dictionary<int, Dictionary<int, StruckTableShopItem>> _rolledItemsByShopUid =
            new Dictionary<int, Dictionary<int, StruckTableShopItem>>();

        /// <summary>
        /// ShopResolver를 생성합니다.
        /// </summary>
        /// <param name="tableShopItem">신규 상점 아이템 테이블입니다.</param>
        /// <param name="availabilityService">구매 가능 여부 판단 서비스입니다.</param>
        /// <param name="legacyTableShop">레거시 테이블 (선택 사항)</param>
        public ShopResolver(
            TableShopItem tableShopItem,
            ShopAvailabilityService availabilityService,
            TableShop legacyTableShop = null)
        {
            _tableShopItem = tableShopItem;
            _legacyTableShop = legacyTableShop;
            _availabilityService = availabilityService;
        }

        /// <summary>
        /// 지정한 상점 UID에 대해 UI에 표시할 아이템 목록을 생성합니다.
        /// 필요 시 슬롯별 랜덤 롤링을 수행하고 결과를 캐싱합니다.
        /// </summary>
        /// <param name="shopUid">상점 식별자입니다.</param>
        /// <param name="reroll">true일 경우 기존 캐시를 무시하고 다시 롤링합니다.</param>
        /// <returns>표시 가능한 상점 아이템 리스트입니다.</returns>
        public List<ShopDisplayItem> Resolve(int shopUid, bool reroll = false)
        {
            var rows = GetRows(shopUid);
            if (rows == null || rows.Count <= 0)
            {
                return null;
            }

            // 캐시된 롤링 결과 재사용 또는 재생성
            if (reroll || !_rolledItemsByShopUid.TryGetValue(shopUid, out var rolledItems))
            {
                rolledItems = RollItemsBySlot(rows);
                _rolledItemsByShopUid[shopUid] = rolledItems;
            }
            else
            {
                // 구매 제한 등으로 캐시된 상품이 새로 숨김 상태가 되면 해당 슬롯만 다시 추첨합니다.
                RefreshHiddenUnavailableSlots(rows, rolledItems);
            }

            var result = new List<ShopDisplayItem>(rolledItems.Count);

            foreach (var pair in rolledItems)
            {
                var item = new ShopDisplayItem(pair.Value);

                ApplyAvailability(item);

                // 구매 불가 + 숨김 타입이면 제외
                if (!item.IsBuyable && item.SoldOutDisplayType == ShopSoldOutDisplayType.Hide)
                {
                    continue;
                }

                result.Add(item);
            }

            // 슬롯 순서대로 정렬
            result.Sort((a, b) => a.SlotIndex.CompareTo(b.SlotIndex));

            return result;
        }

        /// <summary>
        /// 단일 아이템의 구매 가능 상태를 갱신합니다.
        /// </summary>
        /// <param name="item">대상 아이템입니다.</param>
        public void RefreshAvailability(ShopDisplayItem item)
        {
            ApplyAvailability(item);
        }

        /// <summary>
        /// 특정 상점의 롤링 결과 캐시를 제거합니다.
        /// </summary>
        public void ClearRoll(int shopUid)
        {
            _rolledItemsByShopUid.Remove(shopUid);
        }

        /// <summary>
        /// 모든 상점의 롤링 캐시를 초기화합니다.
        /// </summary>
        public void ClearAllRolls()
        {
            _rolledItemsByShopUid.Clear();
        }

        /// <summary>
        /// 상점 UID에 해당하는 테이블 행을 가져옵니다.
        /// 신규 테이블이 없을 경우 레거시 테이블을 변환하여 사용합니다.
        /// </summary>
        private List<StruckTableShopItem> GetRows(int shopUid)
        {
            var rows = _tableShopItem?.GetItemsByShopUid(shopUid);
            if (rows != null && rows.Count > 0) return rows;

            var legacyRows = _legacyTableShop?.GetItemByUid(shopUid);
            if (legacyRows == null || legacyRows.Count <= 0) return null;

            var converted = new List<StruckTableShopItem>(legacyRows.Count);

            foreach (var row in legacyRows)
            {
                var convertedRow = StruckTableShopItem.FromLegacyShopRow(row);
                if (convertedRow != null)
                {
                    converted.Add(convertedRow);
                }
            }

            return converted;
        }

        /// <summary>
        /// 슬롯별 후보군에서 조건에 맞는 아이템을 랜덤으로 선택합니다.
        /// (숨김 필터, 유니크 그룹 제한, 우선순위, 가중치 적용)
        /// </summary>
        /// <param name="rows">현재 상점에 등록된 전체 상품 행입니다.</param>
        /// <returns>슬롯 인덱스를 키로 사용하는 추첨 결과입니다.</returns>
        private Dictionary<int, StruckTableShopItem> RollItemsBySlot(List<StruckTableShopItem> rows)
        {
            var candidatesBySlot = BuildCandidatesBySlot(rows);

            var slotIndices = new List<int>(candidatesBySlot.Keys);
            slotIndices.Sort();

            // 유니크 그룹 중복 방지용
            var pickedItemUidsByUniqueGroup = new Dictionary<int, HashSet<int>>();

            var rolledItems = new Dictionary<int, StruckTableShopItem>();

            foreach (var slotIndex in slotIndices)
            {
                // 숨김 처리된 후보 제거
                var candidates = FilterHiddenUnavailableCandidates(candidatesBySlot[slotIndex]);
                if (candidates.Count <= 0) continue;

                // 유니크 그룹 중복 제거
                var filteredCandidates = FilterUniqueCandidates(candidates, pickedItemUidsByUniqueGroup);

                // 가장 높은 우선순위 후보군만 가중치 추첨에 참여
                var prioritizedCandidates = FilterHighestRollPriority(
                    filteredCandidates.Count > 0 ? filteredCandidates : candidates);

                // 가중치 기반 랜덤 선택
                var picked = PickWeighted(prioritizedCandidates);

                rolledItems[slotIndex] = picked;

                // 유니크 그룹 등록
                RegisterUniquePick(picked, pickedItemUidsByUniqueGroup);
            }

            return rolledItems;
        }

        /// <summary>
        /// 캐시된 추첨 결과 중 구매 불가 상태에서 숨겨야 하는 상품이 있는 슬롯만 다시 추첨합니다.
        /// 영향받지 않은 슬롯은 기존 결과를 유지하여 한 상품의 품절로 상점 전체 구성이 바뀌지 않도록 합니다.
        /// </summary>
        /// <param name="rows">현재 상점에 등록된 전체 상품 행입니다.</param>
        /// <param name="rolledItems">슬롯 인덱스를 키로 사용하는 현재 추첨 결과입니다.</param>
        private void RefreshHiddenUnavailableSlots(
            List<StruckTableShopItem> rows,
            Dictionary<int, StruckTableShopItem> rolledItems)
        {
            if (rows == null || rolledItems == null || rolledItems.Count <= 0) return;

            var invalidSlotIndices = new HashSet<int>();
            foreach (var pair in rolledItems)
            {
                if (ShouldExcludeFromRoll(pair.Value))
                {
                    invalidSlotIndices.Add(pair.Key);
                }
            }

            if (invalidSlotIndices.Count <= 0) return;

            var candidatesBySlot = BuildCandidatesBySlot(rows);
            var pickedItemUidsByUniqueGroup = new Dictionary<int, HashSet<int>>();

            // 유지되는 슬롯을 먼저 등록하여 다시 추첨하는 슬롯이 기존 상품과 중복되지 않게 합니다.
            foreach (var pair in rolledItems)
            {
                if (!invalidSlotIndices.Contains(pair.Key))
                {
                    RegisterUniquePick(pair.Value, pickedItemUidsByUniqueGroup);
                }
            }

            var sortedInvalidSlotIndices = new List<int>(invalidSlotIndices);
            sortedInvalidSlotIndices.Sort();

            for (int i = 0; i < sortedInvalidSlotIndices.Count; i++)
            {
                int slotIndex = sortedInvalidSlotIndices[i];
                if (!candidatesBySlot.TryGetValue(slotIndex, out var slotCandidates))
                {
                    rolledItems.Remove(slotIndex);
                    continue;
                }

                var availableCandidates = FilterHiddenUnavailableCandidates(slotCandidates);
                if (availableCandidates.Count <= 0)
                {
                    // 후보가 모두 숨김 상태라면 슬롯 자체를 표시하지 않습니다.
                    rolledItems.Remove(slotIndex);
                    continue;
                }

                var uniqueCandidates = FilterUniqueCandidates(
                    availableCandidates,
                    pickedItemUidsByUniqueGroup);
                var prioritizedCandidates = FilterHighestRollPriority(
                    uniqueCandidates.Count > 0 ? uniqueCandidates : availableCandidates);
                StruckTableShopItem picked = PickWeighted(prioritizedCandidates);
                if (picked == null)
                {
                    rolledItems.Remove(slotIndex);
                    continue;
                }

                rolledItems[slotIndex] = picked;
                RegisterUniquePick(picked, pickedItemUidsByUniqueGroup);
            }
        }

        /// <summary>
        /// 상점 상품 행을 슬롯별 후보 목록으로 구성합니다.
        /// </summary>
        /// <param name="rows">현재 상점에 등록된 전체 상품 행입니다.</param>
        /// <returns>슬롯 인덱스를 키로 사용하는 후보 목록입니다.</returns>
        private static Dictionary<int, List<StruckTableShopItem>> BuildCandidatesBySlot(
            List<StruckTableShopItem> rows)
        {
            var candidatesBySlot = new Dictionary<int, List<StruckTableShopItem>>();
            if (rows == null) return candidatesBySlot;

            for (int i = 0; i < rows.Count; i++)
            {
                StruckTableShopItem row = rows[i];
                if (row == null || row.SlotIndex < 0) continue;

                if (!candidatesBySlot.TryGetValue(row.SlotIndex, out var candidates))
                {
                    candidates = new List<StruckTableShopItem>();
                    candidatesBySlot.Add(row.SlotIndex, candidates);
                }

                candidates.Add(row);
            }

            return candidatesBySlot;
        }

        /// <summary>
        /// 구매 불가 + 숨김 설정된 후보를 제거합니다.
        /// </summary>
        /// <param name="candidates">구매 가능 여부를 검사할 슬롯 후보 목록입니다.</param>
        /// <returns>숨김 조건에 해당하지 않는 후보 목록입니다.</returns>
        private List<StruckTableShopItem> FilterHiddenUnavailableCandidates(List<StruckTableShopItem> candidates)
        {
            var filteredCandidates = new List<StruckTableShopItem>();

            foreach (var candidate in candidates)
            {
                if (candidate == null) continue;

                if (ShouldExcludeFromRoll(candidate)) continue;

                filteredCandidates.Add(candidate);
            }

            return filteredCandidates;
        }

        /// <summary>
        /// 지정한 상품이 현재 추첨 후보에서 제외되어야 하는지 확인합니다.
        /// 구매할 수 없더라도 비활성 표시 정책이면 후보로 유지하고, 숨김 정책일 때만 제외합니다.
        /// </summary>
        /// <param name="candidate">검사할 상점 상품 행입니다.</param>
        /// <returns>현재 추첨 후보에서 제외해야 하면 <see langword="true"/>입니다.</returns>
        private bool ShouldExcludeFromRoll(StruckTableShopItem candidate)
        {
            if (candidate == null) return true;

            var item = new ShopDisplayItem(candidate);
            ApplyAvailability(item);
            return !item.IsBuyable && item.SoldOutDisplayType == ShopSoldOutDisplayType.Hide;
        }

        /// <summary>
        /// 동일 유니크 그룹에서 이미 선택된 아이템을 제외합니다.
        /// </summary>
        /// <param name="candidates">유니크 그룹을 검사할 슬롯 후보 목록입니다.</param>
        /// <param name="pickedItemUidsByUniqueGroup">유니크 그룹별로 이미 선택된 아이템 UID 기록입니다.</param>
        /// <returns>현재 상점 추첨에서 중복되지 않는 후보 목록입니다.</returns>
        private List<StruckTableShopItem> FilterUniqueCandidates(
            List<StruckTableShopItem> candidates,
            Dictionary<int, HashSet<int>> pickedItemUidsByUniqueGroup)
        {
            var filteredCandidates = new List<StruckTableShopItem>();

            foreach (var candidate in candidates)
            {
                if (candidate == null) continue;

                // 유니크 제한 없음
                if (candidate.ItemUid <= 0 || candidate.UniqueGroup <= 0)
                {
                    filteredCandidates.Add(candidate);
                    continue;
                }

                if (!pickedItemUidsByUniqueGroup.TryGetValue(candidate.UniqueGroup, out var pickedItemUids) ||
                    !pickedItemUids.Contains(candidate.ItemUid))
                {
                    filteredCandidates.Add(candidate);
                }
            }

            return filteredCandidates;
        }

        /// <summary>
        /// 슬롯 후보 중 가장 높은 추첨 우선순위를 가진 후보만 반환합니다.
        /// 구매 제한 등으로 높은 우선순위 상품이 숨겨지면 다음 우선순위 후보군이 자동으로 활성화됩니다.
        /// </summary>
        /// <param name="candidates">우선순위를 비교할 슬롯 후보 목록입니다.</param>
        /// <returns>가장 높은 <see cref="StruckTableShopItem.RollPriority"/>를 가진 후보 목록입니다.</returns>
        private static List<StruckTableShopItem> FilterHighestRollPriority(List<StruckTableShopItem> candidates)
        {
            var prioritizedCandidates = new List<StruckTableShopItem>();
            if (candidates == null || candidates.Count <= 0) return prioritizedCandidates;

            int highestPriority = int.MinValue;
            for (int i = 0; i < candidates.Count; i++)
            {
                StruckTableShopItem candidate = candidates[i];
                if (candidate == null) continue;

                if (candidate.RollPriority > highestPriority)
                {
                    highestPriority = candidate.RollPriority;
                    prioritizedCandidates.Clear();
                    prioritizedCandidates.Add(candidate);
                    continue;
                }

                if (candidate.RollPriority == highestPriority)
                {
                    prioritizedCandidates.Add(candidate);
                }
            }

            return prioritizedCandidates;
        }

        /// <summary>
        /// 선택된 아이템을 유니크 그룹 기록에 등록합니다.
        /// </summary>
        /// <param name="picked">이번 슬롯에서 선택된 상품입니다.</param>
        /// <param name="pickedItemUidsByUniqueGroup">유니크 그룹별 선택 기록입니다.</param>
        private void RegisterUniquePick(
            StruckTableShopItem picked,
            Dictionary<int, HashSet<int>> pickedItemUidsByUniqueGroup)
        {
            if (picked == null || picked.ItemUid <= 0 || picked.UniqueGroup <= 0) return;

            if (!pickedItemUidsByUniqueGroup.TryGetValue(picked.UniqueGroup, out var pickedItemUids))
            {
                pickedItemUids = new HashSet<int>();
                pickedItemUidsByUniqueGroup.Add(picked.UniqueGroup, pickedItemUids);
            }

            pickedItemUids.Add(picked.ItemUid);
        }

        /// <summary>
        /// 가중치(Rate)를 기반으로 랜덤 아이템을 선택합니다.
        /// </summary>
        /// <param name="candidates">동일한 우선순위를 가진 추첨 후보 목록입니다.</param>
        /// <returns>가중치 추첨으로 선택된 상품입니다.</returns>
        private StruckTableShopItem PickWeighted(List<StruckTableShopItem> candidates)
        {
            if (candidates == null || candidates.Count <= 0) return null;
            if (candidates.Count == 1) return candidates[0];

            int totalRate = 0;

            foreach (var candidate in candidates)
            {
                if (candidate == null) continue;
                totalRate += Mathf.Max(0, candidate.Rate);
            }

            if (totalRate <= 0)
            {
                return candidates[0];
            }

            int roll = Random.Range(0, totalRate);
            int cumulativeRate = 0;

            foreach (var candidate in candidates)
            {
                if (candidate == null) continue;

                cumulativeRate += Mathf.Max(0, candidate.Rate);

                if (roll < cumulativeRate)
                {
                    return candidate;
                }
            }

            // fallback (이론상 도달하지 않지만 안전 처리)
            return candidates[candidates.Count - 1];
        }

        /// <summary>
        /// 아이템의 구매 가능 여부를 적용합니다.
        /// </summary>
        private void ApplyAvailability(ShopDisplayItem item)
        {
            if (item == null) return;

            if (item.IsEmpty)
            {
                item.SetAvailability(false);
                return;
            }

            if (_availabilityService == null)
            {
                item.SetAvailability(true);
                return;
            }

            bool isBuyable = _availabilityService.CanBuy(item, out var disabledReason);
            item.SetAvailability(isBuyable, disabledReason);
        }
    }
}
