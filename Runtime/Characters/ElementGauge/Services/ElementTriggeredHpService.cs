using System;
using System.Collections.Generic;

namespace GGemCo2DCore
{
    /// <summary>
    /// 속성 임계 반응으로 확보된 HP 구간의 할당/차감/클램프/스냅샷 생성을 담당합니다.
    /// </summary>
    internal sealed class ElementTriggeredHpService
    {
        private readonly CharacterBase _owner;

        public ElementTriggeredHpService(CharacterBase owner)
        {
            _owner = owner;
        }

        public bool ApplyTriggeredHp(ElementGaugeRuntime runtime, ConfigCommon.DamageType damageType, long hpAmount)
        {
            if (_owner == null || runtime == null)
                return false;

            TriggeredHpState state = runtime.GetOrCreateTriggeredHpState(damageType);
            if (state == null)
                return false;

            long beforeBase = state.BaseHp;
            long beforeTempItem = state.TempItemHp;
            long beforeTempRuntime = state.TempRuntimeHp;
            long beforeTempPassive = state.TempPassiveHp;

            long budget = Math.Max(0L, hpAmount);
            if (budget <= 0)
            {
                state.Clear();
                runtime.ResetTriggeredHpTickElapsed(damageType);
                return !StateEquals(state, beforeBase, beforeTempItem, beforeTempRuntime, beforeTempPassive);
            }

            long remaining = budget;
            long currentTempItem = Math.Max(0L, _owner.GetItemBonusHpTempCurrent());
            long currentTempRuntime = Math.Max(0L, _owner.GetRuntimeBonusHpTempCurrent());
            long currentTempPassive = Math.Max(0L, _owner.GetPassiveBonusHpTempCurrent());
            long currentBase = Math.Max(0L, _owner.CurrentHp.Value);

            state.TempItemHp = Math.Min(remaining, currentTempItem);
            remaining -= state.TempItemHp;

            state.TempRuntimeHp = Math.Min(remaining, currentTempRuntime);
            remaining -= state.TempRuntimeHp;

            state.TempPassiveHp = Math.Min(remaining, currentTempPassive);
            remaining -= state.TempPassiveHp;

            state.BaseHp = Math.Min(remaining, currentBase);

            runtime.ResetTriggeredHpTickElapsed(damageType);
            return !StateEquals(state, beforeBase, beforeTempItem, beforeTempRuntime, beforeTempPassive);
        }

        public long ConsumeTriggeredHp(ElementGaugeRuntime runtime, ConfigCommon.DamageType damageType, long requestedAmount)
        {
            if (_owner == null || runtime == null || requestedAmount <= 0)
                return 0;

            if (!runtime.TryGetTriggeredHpState(damageType, out TriggeredHpState state) || state == null)
                return 0;

            long remainingBudget = Math.Max(0, requestedAmount);
            long totalConsumed = 0;

            if (state.TempItemHp > 0 && remainingBudget > 0)
            {
                long target = Math.Min(state.TempItemHp, remainingBudget);
                long remaining = _owner.ConsumeHpTempItem(target);
                long consumed = target - remaining;
                totalConsumed += consumed;
                remainingBudget -= consumed;
                state.TempItemHp = Math.Max(0, state.TempItemHp - consumed);
            }

            if (state.TempRuntimeHp > 0 && remainingBudget > 0)
            {
                long target = Math.Min(state.TempRuntimeHp, remainingBudget);
                long remaining = _owner.ConsumeHpTempRuntime(target);
                long consumed = target - remaining;
                totalConsumed += consumed;
                remainingBudget -= consumed;
                state.TempRuntimeHp = Math.Max(0, state.TempRuntimeHp - consumed);
            }

            if (state.TempPassiveHp > 0 && remainingBudget > 0)
            {
                long target = Math.Min(state.TempPassiveHp, remainingBudget);
                long remaining = _owner.ConsumeHpTempPassive(target);
                long consumed = target - remaining;
                totalConsumed += consumed;
                remainingBudget -= consumed;
                state.TempPassiveHp = Math.Max(0, state.TempPassiveHp - consumed);
            }

            if (state.BaseHp > 0 && remainingBudget > 0)
            {
                long consume = Math.Min(Math.Min(state.BaseHp, remainingBudget), Math.Max(0, _owner.CurrentHp.Value));
                if (consume > 0)
                {
                    _owner.CurrentHp.OnNext(Math.Max(0, _owner.CurrentHp.Value - consume));
                    state.BaseHp -= consume;
                    totalConsumed += consume;
                }
            }

            ClampTriggeredHpToCurrentResources(runtime, damageType);
            return totalConsumed;
        }

        public bool ClampAllTriggeredHpToCurrentResources(ElementGaugeRuntime runtime)
        {
            if (runtime == null)
                return false;

            bool changed = false;
            IReadOnlyList<ElementGaugeRuleDefinition> rules = runtime.Rules;
            for (int i = 0; i < rules.Count; i++)
            {
                ElementGaugeRuleDefinition rule = rules[i];
                if (rule == null)
                    continue;

                changed |= ClampTriggeredHpToCurrentResources(runtime, rule.damageType);
            }

            return changed;
        }

        public bool ClampTriggeredHpToCurrentResources(ElementGaugeRuntime runtime, ConfigCommon.DamageType damageType)
        {
            if (_owner == null || runtime == null)
                return false;

            if (!runtime.TryGetTriggeredHpState(damageType, out TriggeredHpState state) || state == null)
                return false;

            long clampedBase = Math.Min(state.BaseHp, Math.Max(0, _owner.CurrentHp.Value));
            long clampedTempItem = Math.Min(state.TempItemHp, Math.Max(0, _owner.GetItemBonusHpTempCurrent()));
            long clampedTempRuntime = Math.Min(state.TempRuntimeHp, Math.Max(0, _owner.GetRuntimeBonusHpTempCurrent()));
            long clampedTempPassive = Math.Min(state.TempPassiveHp, Math.Max(0, _owner.GetPassiveBonusHpTempCurrent()));

            if (clampedBase == state.BaseHp &&
                clampedTempItem == state.TempItemHp &&
                clampedTempRuntime == state.TempRuntimeHp &&
                clampedTempPassive == state.TempPassiveHp)
            {
                return false;
            }

            state.BaseHp = clampedBase;
            state.TempItemHp = clampedTempItem;
            state.TempRuntimeHp = clampedTempRuntime;
            state.TempPassiveHp = clampedTempPassive;
            return true;
        }

        public bool HasTriggeredHp(ElementGaugeRuntime runtime, ConfigCommon.DamageType damageType)
        {
            return GetTriggeredHpTotal(runtime, damageType) > 0;
        }

        public long GetTriggeredHpTotal(ElementGaugeRuntime runtime, ConfigCommon.DamageType damageType)
        {
            if (runtime == null)
                return 0L;

            return runtime.TryGetTriggeredHpState(damageType, out TriggeredHpState state) && state != null
                ? state.TotalHp
                : 0L;
        }

        public ElementTriggeredHpCollectionSnapshot BuildTriggeredHpCollectionSnapshot(
            ElementGaugeRuntime runtime,
            List<ElementTriggeredHpSnapshot> buffer)
        {
            buffer.Clear();
            if (runtime == null)
                return ElementTriggeredHpCollectionSnapshot.Empty;

            IReadOnlyList<ElementGaugeRuleDefinition> rules = runtime.Rules;
            for (int i = 0; i < rules.Count; i++)
            {
                ElementGaugeRuleDefinition rule = rules[i];
                if (rule == null)
                    continue;

                if (!runtime.TryGetTriggeredHpState(rule.damageType, out TriggeredHpState state) || state == null || state.TotalHp <= 0)
                    continue;

                buffer.Add(new ElementTriggeredHpSnapshot(
                    rule.damageType,
                    ResolveTriggeredHudStateKey(rule),
                    state.BaseHp,
                    state.TempItemHp,
                    state.TempRuntimeHp,
                    state.TempPassiveHp));
            }

            return buffer.Count > 0
                ? new ElementTriggeredHpCollectionSnapshot(buffer.ToArray())
                : ElementTriggeredHpCollectionSnapshot.Empty;
        }

        private static bool StateEquals(TriggeredHpState state, long baseHp, long tempItemHp, long tempRuntimeHp, long tempPassiveHp)
        {
            return state.BaseHp == baseHp &&
                   state.TempItemHp == tempItemHp &&
                   state.TempRuntimeHp == tempRuntimeHp &&
                   state.TempPassiveHp == tempPassiveHp;
        }

        private static string ResolveTriggeredHudStateKey(ElementGaugeRuleDefinition rule)
        {
            string configured = rule != null ? rule.triggeredHudStateKey : string.Empty;
            if (!string.IsNullOrWhiteSpace(configured))
                return configured.Trim();

            return rule != null
                ? rule.damageType.ToString().ToLowerInvariant()
                : string.Empty;
        }
    }
}
