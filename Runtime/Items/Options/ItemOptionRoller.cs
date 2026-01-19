using System;
using System.Collections.Generic;

namespace GGemCo2DCore
{
    /// <summary>
    /// 드랍/획득 시 아이템에 랜덤 Affix를 부여한다.
    /// </summary>
    public sealed class ItemOptionRoller
    {
        private readonly TableLoaderManager _tables;

        public ItemOptionRoller(TableLoaderManager tables)
        {
            _tables = tables;
        }

        /// <summary>
        /// 랜덤 옵션을 롤링하여 ItemInstanceData를 생성한다.
        /// </summary>
        public ItemInstanceData CreateInstance(int itemUid, ItemRarity rarity, int dropLevel, int seed)
        {
            var instance = new ItemInstanceData
            {
                InstanceId = 0,
                ItemUid = itemUid,
                Rarity = rarity,
            };

            RollAffixes(instance, dropLevel, seed);
            return instance;
        }

        /// <summary>
        /// 주어진 인스턴스에 랜덤 옵션을 부여한다.
        /// </summary>
        public void RollAffixes(ItemInstanceData instance, int dropLevel, int seed)
        {
            if (_tables == null || instance == null || instance.ItemUid <= 0)
                return;

            var item = _tables.TableItem?.GetDataByUid(instance.ItemUid);
            if (item == null) return;

            var rule = _tables.TableItemRollRule?.GetByRarity(instance.Rarity);
            if (rule == null) return;

            var poolRows = _tables.TableItemAffixPool?.GetCandidates(item);
            if (poolRows == null || poolRows.Count <= 0) return;

            var defTable = _tables.TableItemAffixDef;
            if (defTable == null) return;

            var rng = new Random(seed);

            int affixCount = NextRangeInclusive(rng, rule.MinAffixCount, rule.MaxAffixCount);
            affixCount = Math.Max(0, affixCount);
            if (affixCount <= 0) return;

            int prefixLeft = rule.MaxPrefix <= 0 ? affixCount : rule.MaxPrefix;
            int suffixLeft = rule.MaxSuffix <= 0 ? affixCount : rule.MaxSuffix;
            var usedGroups = new HashSet<int>();

            // 후보 목록을 사전 필터링(레벨, 정의 존재)
            var candidates = new List<(StruckTableItemAffixDef def, int weight)>(poolRows.Count);
            for (int i = 0; i < poolRows.Count; i++)
            {
                var row = poolRows[i];
                var def = defTable.GetByUid(row.AffixUid);
                if (def == null) continue;
                if (dropLevel > 0 && def.MinLevel > dropLevel) continue;

                int w = row.WeightOverride > 0 ? row.WeightOverride : Math.Max(1, def.Weight);
                candidates.Add((def, w));
            }
            if (candidates.Count <= 0) return;

            // 선택 루프
            for (int pick = 0; pick < affixCount; pick++)
            {
                // 타입 제한 반영한 후보 구성
                var filtered = new List<(StruckTableItemAffixDef def, int weight)>(candidates.Count);
                for (int i = 0; i < candidates.Count; i++)
                {
                    var c = candidates[i];
                    if (!rule.AllowDuplicateGroup && c.def.GroupId != 0 && usedGroups.Contains(c.def.GroupId))
                        continue;
                    if (c.def.AffixType == ItemAffixType.Prefix && prefixLeft <= 0) continue;
                    if (c.def.AffixType == ItemAffixType.Suffix && suffixLeft <= 0) continue;
                    filtered.Add(c);
                }

                if (filtered.Count <= 0) break;

                var chosen = PickWeighted(rng, filtered);
                if (chosen == null) break;

                float value = NextFloatRange(rng, chosen.MinValue, chosen.MaxValue);
                instance.RolledAffixes.Add(new ItemAffixRoll(chosen.AffixUid, value));

                if (chosen.GroupId != 0) usedGroups.Add(chosen.GroupId);
                if (chosen.AffixType == ItemAffixType.Prefix) prefixLeft--;
                else suffixLeft--;
            }
        }

        private static int NextRangeInclusive(Random rng, int min, int max)
        {
            if (max < min) (min, max) = (max, min);
            // Random.Next는 maxExclusive
            return rng.Next(min, max + 1);
        }

        private static float NextFloatRange(Random rng, float min, float max)
        {
            if (max < min) (min, max) = (max, min);
            double t = rng.NextDouble();
            return (float)(min + (max - min) * t);
        }

        private static StruckTableItemAffixDef PickWeighted(Random rng, List<(StruckTableItemAffixDef def, int weight)> list)
        {
            int total = 0;
            for (int i = 0; i < list.Count; i++) total += Math.Max(0, list[i].weight);
            if (total <= 0) return list[0].def;

            int roll = rng.Next(0, total);
            int acc = 0;
            for (int i = 0; i < list.Count; i++)
            {
                acc += Math.Max(0, list[i].weight);
                if (roll < acc) return list[i].def;
            }
            return list[list.Count - 1].def;
        }
    }
}
