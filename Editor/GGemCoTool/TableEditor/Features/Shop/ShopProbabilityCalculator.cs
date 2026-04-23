using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace GGemCo2DCoreEditor
{
    internal sealed class ShopProbabilityResult
    {
        public int ShopItemUid;
        public int ShopUid;
        public int SlotIndex;
        public int ItemUid;
        public int Rate;
        public int UniqueGroup;
        public double BaseProbability;
        public double EstimatedProbability;
    }

    internal static class ShopProbabilityCalculator
    {
        private sealed class Candidate
        {
            public int ShopItemUid;
            public int ShopUid;
            public int SlotIndex;
            public int ItemUid;
            public int Rate;
            public int UniqueGroup;
            public int Order;
        }

        private readonly struct ResultKey : IEquatable<ResultKey>
        {
            public readonly int ShopItemUid;
            public readonly int ShopUid;
            public readonly int SlotIndex;
            public readonly int ItemUid;

            public ResultKey(int shopItemUid, int shopUid, int slotIndex, int itemUid)
            {
                ShopItemUid = shopItemUid;
                ShopUid = shopUid;
                SlotIndex = slotIndex;
                ItemUid = itemUid;
            }

            public bool Equals(ResultKey other)
            {
                return ShopItemUid == other.ShopItemUid
                       && ShopUid == other.ShopUid
                       && SlotIndex == other.SlotIndex
                       && ItemUid == other.ItemUid;
            }

            public override bool Equals(object obj)
            {
                return obj is ResultKey other && Equals(other);
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    int hash = ShopItemUid;
                    hash = (hash * 397) ^ ShopUid;
                    hash = (hash * 397) ^ SlotIndex;
                    hash = (hash * 397) ^ ItemUid;
                    return hash;
                }
            }
        }

        public static List<ShopProbabilityResult> Calculate(TableEditorDocument document, int iterations, int seed = 1001)
        {
            return Calculate(document, iterations, 0, seed);
        }

        public static List<ShopProbabilityResult> Calculate(TableEditorDocument document, int iterations, int shopUid, int seed)
        {
            iterations = Math.Max(1, iterations);

            var candidates = ReadCandidates(document);
            if (shopUid > 0)
                candidates = candidates.Where(candidate => candidate.ShopUid == shopUid).ToList();

            var candidatesByShop = candidates
                .GroupBy(static c => c.ShopUid)
                .ToDictionary(static g => g.Key, static g => g.ToList());

            var results = BuildBaseResults(candidatesByShop);
            var counts = new Dictionary<ResultKey, int>();
            var rng = new Random(seed);

            foreach (var shopPair in candidatesByShop)
            {
                for (int i = 0; i < iterations; i++)
                {
                    var pickedBySlot = RollShop(shopPair.Value, rng);
                    foreach (var pickedPair in pickedBySlot)
                    {
                        Candidate picked = pickedPair.Value;
                        if (picked == null) continue;

                        var key = new ResultKey(picked.ShopItemUid, picked.ShopUid, picked.SlotIndex, picked.ItemUid);
                        counts.TryGetValue(key, out int count);
                        counts[key] = count + 1;
                    }
                }
            }

            for (int i = 0; i < results.Count; i++)
            {
                ShopProbabilityResult result = results[i];
                var key = new ResultKey(result.ShopItemUid, result.ShopUid, result.SlotIndex, result.ItemUid);
                counts.TryGetValue(key, out int count);
                result.EstimatedProbability = count / (double)iterations;
            }

            return results
                .OrderBy(static r => r.ShopUid)
                .ThenBy(static r => r.SlotIndex)
                .ThenBy(static r => r.ShopItemUid)
                .ThenBy(static r => r.ItemUid)
                .ToList();
        }

        public static List<int> GetShopUids(TableEditorDocument document)
        {
            return ReadCandidates(document)
                .Select(static candidate => candidate.ShopUid)
                .Distinct()
                .OrderBy(static shopUid => shopUid)
                .ToList();
        }

        public static string BuildTsv(IReadOnlyList<ShopProbabilityResult> results)
        {
            var lines = new List<string>
            {
                "ShopItemUid\tShopUid\tSlotIndex\tItemUid\tLabel\tRate\tUniqueGroup\tBaseProbability\tEstimatedProbability"
            };

            if (results != null)
            {
                foreach (ShopProbabilityResult result in results)
                {
                    lines.Add(string.Join("\t",
                        result.ShopItemUid.ToString(CultureInfo.InvariantCulture),
                        result.ShopUid.ToString(CultureInfo.InvariantCulture),
                        result.SlotIndex.ToString(CultureInfo.InvariantCulture),
                        result.ItemUid.ToString(CultureInfo.InvariantCulture),
                        result.ItemUid <= 0 ? "Empty" : result.ItemUid.ToString(CultureInfo.InvariantCulture),
                        result.Rate.ToString(CultureInfo.InvariantCulture),
                        result.UniqueGroup.ToString(CultureInfo.InvariantCulture),
                        FormatPercent(result.BaseProbability),
                        FormatPercent(result.EstimatedProbability)));
                }
            }

            return string.Join("\n", lines);
        }

        public static string FormatPercent(double value)
        {
            return (value * 100d).ToString("0.####", CultureInfo.InvariantCulture) + "%";
        }

        private static List<Candidate> ReadCandidates(TableEditorDocument document)
        {
            var candidates = new List<Candidate>();
            if (document == null) return candidates;

            int order = 0;
            bool hasShopUid = document.Headers.Contains("ShopUid");
            foreach (TableEditorDocumentRow row in document.GetRows())
            {
                int uid = ParseInt(row, "Uid");
                var candidate = new Candidate
                {
                    ShopItemUid = hasShopUid ? uid : order + 1,
                    ShopUid = hasShopUid ? ParseInt(row, "ShopUid") : uid,
                    SlotIndex = ParseInt(row, "SlotIndex", -1),
                    ItemUid = ParseInt(row, "ItemUid"),
                    Rate = ParseInt(row, "Rate", 100),
                    UniqueGroup = ParseInt(row, "UniqueGroup"),
                    Order = order++,
                };

                if (candidate.ShopUid <= 0 || candidate.SlotIndex < 0 || (hasShopUid && candidate.ShopItemUid <= 0))
                    continue;

                candidates.Add(candidate);
            }

            return candidates;
        }

        private static List<ShopProbabilityResult> BuildBaseResults(Dictionary<int, List<Candidate>> candidatesByShop)
        {
            var resultByKey = new Dictionary<ResultKey, ShopProbabilityResult>();

            foreach (var shopPair in candidatesByShop)
            {
                foreach (var slotGroup in shopPair.Value.GroupBy(static c => c.SlotIndex))
                {
                    var slotCandidates = slotGroup.OrderBy(static c => c.Order).ToList();
                    int totalRate = slotCandidates.Sum(static c => Math.Max(0, c.Rate));
                    Candidate fallback = slotCandidates.FirstOrDefault();

                    foreach (Candidate candidate in slotCandidates)
                    {
                        var key = new ResultKey(candidate.ShopItemUid, candidate.ShopUid, candidate.SlotIndex, candidate.ItemUid);
                        if (!resultByKey.TryGetValue(key, out ShopProbabilityResult result))
                        {
                            result = new ShopProbabilityResult
                            {
                                ShopItemUid = candidate.ShopItemUid,
                                ShopUid = candidate.ShopUid,
                                SlotIndex = candidate.SlotIndex,
                                ItemUid = candidate.ItemUid,
                                UniqueGroup = candidate.UniqueGroup,
                            };
                            resultByKey.Add(key, result);
                        }

                        result.Rate += Math.Max(0, candidate.Rate);
                        if (result.UniqueGroup <= 0 && candidate.UniqueGroup > 0)
                            result.UniqueGroup = candidate.UniqueGroup;
                    }

                    foreach (Candidate candidate in slotCandidates)
                    {
                        var key = new ResultKey(candidate.ShopItemUid, candidate.ShopUid, candidate.SlotIndex, candidate.ItemUid);
                        ShopProbabilityResult result = resultByKey[key];
                        if (totalRate > 0)
                        {
                            result.BaseProbability = result.Rate / (double)totalRate;
                        }
                        else
                        {
                            result.BaseProbability = fallback != null && fallback.ItemUid == candidate.ItemUid ? 1d : 0d;
                        }
                    }
                }
            }

            return resultByKey.Values.ToList();
        }

        private static Dictionary<int, Candidate> RollShop(List<Candidate> rows, Random rng)
        {
            var candidatesBySlot = rows
                .GroupBy(static c => c.SlotIndex)
                .ToDictionary(static g => g.Key, static g => g.OrderBy(static c => c.Order).ToList());

            var slotIndices = candidatesBySlot.Keys.ToList();
            slotIndices.Sort();

            var pickedItemUidsByUniqueGroup = new Dictionary<int, HashSet<int>>();
            var pickedBySlot = new Dictionary<int, Candidate>();
            foreach (int slotIndex in slotIndices)
            {
                var candidates = candidatesBySlot[slotIndex];
                var filteredCandidates = FilterUniqueCandidates(candidates, pickedItemUidsByUniqueGroup);
                Candidate picked = PickWeighted(filteredCandidates.Count > 0 ? filteredCandidates : candidates, rng);
                pickedBySlot[slotIndex] = picked;
                RegisterUniquePick(picked, pickedItemUidsByUniqueGroup);
            }

            return pickedBySlot;
        }

        private static List<Candidate> FilterUniqueCandidates(
            List<Candidate> candidates,
            Dictionary<int, HashSet<int>> pickedItemUidsByUniqueGroup)
        {
            var filteredCandidates = new List<Candidate>();
            foreach (Candidate candidate in candidates)
            {
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

        private static void RegisterUniquePick(Candidate picked, Dictionary<int, HashSet<int>> pickedItemUidsByUniqueGroup)
        {
            if (picked == null || picked.ItemUid <= 0 || picked.UniqueGroup <= 0) return;

            if (!pickedItemUidsByUniqueGroup.TryGetValue(picked.UniqueGroup, out var pickedItemUids))
            {
                pickedItemUids = new HashSet<int>();
                pickedItemUidsByUniqueGroup.Add(picked.UniqueGroup, pickedItemUids);
            }

            pickedItemUids.Add(picked.ItemUid);
        }

        private static Candidate PickWeighted(List<Candidate> candidates, Random rng)
        {
            if (candidates == null || candidates.Count <= 0) return null;
            if (candidates.Count == 1) return candidates[0];

            int totalRate = candidates.Sum(static c => Math.Max(0, c.Rate));
            if (totalRate <= 0)
                return candidates[0];

            int roll = rng.Next(0, totalRate);
            int cumulativeRate = 0;
            foreach (Candidate candidate in candidates)
            {
                cumulativeRate += Math.Max(0, candidate.Rate);
                if (roll < cumulativeRate)
                    return candidate;
            }

            return candidates[candidates.Count - 1];
        }

        private static int ParseInt(TableEditorDocumentRow row, string headerName, int defaultValue = 0)
        {
            if (row == null || !row.Values.TryGetValue(headerName, out string raw))
                return defaultValue;

            return int.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out int value)
                ? value
                : defaultValue;
        }
    }
}
