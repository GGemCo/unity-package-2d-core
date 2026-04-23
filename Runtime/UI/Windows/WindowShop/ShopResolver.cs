using System.Collections.Generic;
using UnityEngine;

namespace GGemCo2DCore
{
    /// <summary>
    /// Resolves shop table candidates into items displayed in the shop UI.
    /// </summary>
    public sealed class ShopResolver
    {
        private readonly TableShop _tableShop;
        private readonly ShopAvailabilityService _availabilityService;
        private readonly Dictionary<int, Dictionary<int, StruckTableShop>> _rolledItemsByShopUid =
            new Dictionary<int, Dictionary<int, StruckTableShop>>();

        public ShopResolver(TableShop tableShop, ShopAvailabilityService availabilityService)
        {
            _tableShop = tableShop;
            _availabilityService = availabilityService;
        }

        public List<ShopDisplayItem> Resolve(int shopUid, bool reroll = false)
        {
            var rows = _tableShop?.GetItemByUid(shopUid);
            if (rows == null || rows.Count <= 0)
            {
                return null;
            }

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
                result.Add(item);
            }

            result.Sort((a, b) => a.SlotIndex.CompareTo(b.SlotIndex));
            return result;
        }

        public void RefreshAvailability(ShopDisplayItem item)
        {
            ApplyAvailability(item);
        }

        public void ClearRoll(int shopUid)
        {
            _rolledItemsByShopUid.Remove(shopUid);
        }

        public void ClearAllRolls()
        {
            _rolledItemsByShopUid.Clear();
        }

        private Dictionary<int, StruckTableShop> RollItemsBySlot(List<StruckTableShop> rows)
        {
            var candidatesBySlot = new Dictionary<int, List<StruckTableShop>>();
            foreach (var row in rows)
            {
                if (row == null || row.SlotIndex < 0) continue;
                if (!candidatesBySlot.TryGetValue(row.SlotIndex, out var candidates))
                {
                    candidates = new List<StruckTableShop>();
                    candidatesBySlot.Add(row.SlotIndex, candidates);
                }

                candidates.Add(row);
            }

            var slotIndices = new List<int>(candidatesBySlot.Keys);
            slotIndices.Sort();

            var pickedItemUidsByUniqueGroup = new Dictionary<int, HashSet<int>>();
            var rolledItems = new Dictionary<int, StruckTableShop>();
            foreach (var slotIndex in slotIndices)
            {
                var candidates = candidatesBySlot[slotIndex];
                var filteredCandidates = FilterUniqueCandidates(candidates, pickedItemUidsByUniqueGroup);
                var picked = PickWeighted(filteredCandidates.Count > 0 ? filteredCandidates : candidates);

                rolledItems[slotIndex] = picked;
                RegisterUniquePick(picked, pickedItemUidsByUniqueGroup);
            }

            return rolledItems;
        }

        private List<StruckTableShop> FilterUniqueCandidates(
            List<StruckTableShop> candidates,
            Dictionary<int, HashSet<int>> pickedItemUidsByUniqueGroup)
        {
            var filteredCandidates = new List<StruckTableShop>();
            foreach (var candidate in candidates)
            {
                if (candidate == null) continue;
                if (candidate.ItemUid <= 0)
                {
                    filteredCandidates.Add(candidate);
                    continue;
                }

                if (candidate.UniqueGroup <= 0)
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

        private void RegisterUniquePick(
            StruckTableShop picked,
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

        private StruckTableShop PickWeighted(List<StruckTableShop> candidates)
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

            return candidates[candidates.Count - 1];
        }

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
