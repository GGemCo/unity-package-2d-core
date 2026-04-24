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
        /// (가중치, 유니크 그룹 제한, 숨김 필터 적용)
        /// </summary>
        private Dictionary<int, StruckTableShopItem> RollItemsBySlot(List<StruckTableShopItem> rows)
        {
            // 슬롯별 후보군 구성
            var candidatesBySlot = new Dictionary<int, List<StruckTableShopItem>>();

            foreach (var row in rows)
            {
                if (row == null || row.SlotIndex < 0) continue;

                if (!candidatesBySlot.TryGetValue(row.SlotIndex, out var candidates))
                {
                    candidates = new List<StruckTableShopItem>();
                    candidatesBySlot.Add(row.SlotIndex, candidates);
                }

                candidates.Add(row);
            }

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

                // 가중치 기반 랜덤 선택
                var picked = PickWeighted(filteredCandidates.Count > 0 ? filteredCandidates : candidates);

                rolledItems[slotIndex] = picked;

                // 유니크 그룹 등록
                RegisterUniquePick(picked, pickedItemUidsByUniqueGroup);
            }

            return rolledItems;
        }

        /// <summary>
        /// 구매 불가 + 숨김 설정된 후보를 제거합니다.
        /// </summary>
        private List<StruckTableShopItem> FilterHiddenUnavailableCandidates(List<StruckTableShopItem> candidates)
        {
            var filteredCandidates = new List<StruckTableShopItem>();

            foreach (var candidate in candidates)
            {
                if (candidate == null) continue;

                var item = new ShopDisplayItem(candidate);
                ApplyAvailability(item);

                if (!item.IsBuyable && item.SoldOutDisplayType == ShopSoldOutDisplayType.Hide)
                {
                    continue;
                }

                filteredCandidates.Add(candidate);
            }

            return filteredCandidates;
        }

        /// <summary>
        /// 동일 유니크 그룹에서 이미 선택된 아이템을 제외합니다.
        /// </summary>
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
        /// 선택된 아이템을 유니크 그룹 기록에 등록합니다.
        /// </summary>
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