using System;
using System.Collections.Generic;
using UnityEngine;

namespace GGemCo2DCore
{
    /// <summary>
    /// 캐릭터의 속성 게이지 누적/감쇠/임계 반응을 관리합니다.
    /// 임계 반응이 발생하면 속성별로 표시/소모할 HP 구간을 별도로 추적합니다.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class CharacterElementGaugeController : MonoBehaviour
    {
        private sealed class RuntimeGaugeState
        {
            public float CurrentValue;
            public float LastAccumulatedTime = float.MinValue;
            public float DecayElapsed;
            public float CorruptedHpTickElapsed;
        }

        private sealed class TriggeredHpState
        {
            public long BaseHp;
            public long TempItemHp;
            public long TempRuntimeHp;
            public long TempPassiveHp;

            public long TotalHp => BaseHp + TempItemHp + TempRuntimeHp + TempPassiveHp;

            public void Clear()
            {
                BaseHp = 0;
                TempItemHp = 0;
                TempRuntimeHp = 0;
                TempPassiveHp = 0;
            }
        }

        private readonly List<ElementGaugeRuleDefinition> _rules = new();
        private readonly Dictionary<ConfigCommon.DamageType, ElementGaugeRuleDefinition> _ruleMap = new();
        private readonly Dictionary<ConfigCommon.DamageType, RuntimeGaugeState> _stateMap = new();
        private readonly Dictionary<ConfigCommon.DamageType, TriggeredHpState> _triggeredStateMap = new();
        private readonly List<ElementGaugeSnapshot> _snapshots = new();
        private readonly List<ElementTriggeredHpSnapshot> _triggeredSnapshotBuffer = new();

        private CharacterBase _owner;

        public event Action GaugeChanged;
        public event Action<ElementTriggeredHpCollectionSnapshot> TriggeredHpChanged;
        public event Action<HpCorruptionSnapshot> CorruptionChanged;

        public ElementTriggeredHpCollectionSnapshot CurrentTriggeredHpStates => BuildTriggeredHpCollectionSnapshot();
        public HpCorruptionSnapshot CurrentCorruption => CurrentTriggeredHpStates.GetLegacyCorruptionSnapshot(ConfigCommon.DamageType.Poison);

        private void Awake()
        {
            _owner = GetComponent<CharacterBase>();
            InitializeRules();
            RebuildCaches();
        }

        private void Update()
        {
            if (_owner == null || _owner.IsStatusDead())
                return;

            if (!(_owner is Player))
                return;

            bool gaugeChanged = false;
            bool triggeredHpChanged = false;
            bool requiresDeathFinalize = false;
            float now = Time.time;
            float delta = Time.deltaTime;

            foreach (var pair in _ruleMap)
            {
                ConfigCommon.DamageType damageType = pair.Key;
                ElementGaugeRuleDefinition rule = pair.Value;
                if (rule == null)
                    continue;

                if (!_stateMap.TryGetValue(damageType, out RuntimeGaugeState state) || state == null)
                    continue;

                if (state.CurrentValue > 0f && now - state.LastAccumulatedTime >= Mathf.Max(0f, rule.decayDelaySeconds))
                {
                    state.DecayElapsed += delta;
                    float tick = Mathf.Max(0.01f, rule.decayTickSeconds);
                    float decayPerTick = Mathf.Max(0f, rule.gaugeMax) * Mathf.Max(0f, rule.decayPercentPerTick) * 0.01f;

                    if (decayPerTick > 0f)
                    {
                        while (state.DecayElapsed >= tick)
                        {
                            state.DecayElapsed -= tick;
                            float next = Mathf.Max(0f, state.CurrentValue - decayPerTick);
                            if (!Mathf.Approximately(next, state.CurrentValue))
                            {
                                state.CurrentValue = next;
                                gaugeChanged = true;
                            }

                            if (state.CurrentValue <= 0f)
                            {
                                state.CurrentValue = 0f;
                                state.DecayElapsed = 0f;
                                break;
                            }
                        }
                    }
                }

                triggeredHpChanged |= ClampTriggeredCorruptionToCurrentResources(damageType, raiseChangedEvent: false);

                if (!rule.useCorruptedHpTickDamage)
                {
                    state.CorruptedHpTickElapsed = 0f;
                    continue;
                }

                if (!HasTriggeredCorruption(damageType))
                {
                    state.CorruptedHpTickElapsed = 0f;
                    continue;
                }

                long tickHpAmount = Math.Max(0, rule.corruptedHpTickHpAmount);
                if (tickHpAmount <= 0)
                {
                    state.CorruptedHpTickElapsed = 0f;
                    continue;
                }

                state.CorruptedHpTickElapsed += delta;
                float tickInterval = Mathf.Max(0.01f, rule.corruptedHpTickIntervalSeconds);

                while (state.CorruptedHpTickElapsed >= tickInterval)
                {
                    state.CorruptedHpTickElapsed -= tickInterval;
                    long consumed = ConsumeTriggeredCorruptionHp(damageType, tickHpAmount);
                    if (consumed > 0)
                    {
                        triggeredHpChanged = true;
                        if (_owner.CurrentHp.Value <= 0)
                            requiresDeathFinalize = true;
                    }

                    if (!HasTriggeredCorruption(damageType) || _owner.IsStatusDead())
                    {
                        state.CorruptedHpTickElapsed = 0f;
                        break;
                    }
                }
            }

            if (gaugeChanged)
                RaiseGaugeChanged();

            if (triggeredHpChanged)
                RaiseTriggeredHpChanged();

            if (requiresDeathFinalize)
                FinalizeDeathFromTriggeredCorruption();
        }

        public IReadOnlyList<ElementGaugeSnapshot> GetGaugeSnapshots()
        {
            _snapshots.Clear();

            foreach (var pair in _ruleMap)
            {
                if (!_stateMap.TryGetValue(pair.Key, out RuntimeGaugeState state) || state == null)
                    continue;

                bool isBlocked = HasTriggeredCorruption(pair.Key);
                _snapshots.Add(new ElementGaugeSnapshot(pair.Key, state.CurrentValue, Mathf.Max(1f, pair.Value.gaugeMax), isBlocked));
            }

            return _snapshots;
        }

        public void ApplyGauge(ElementGaugeApplication application, GameObject source = null)
        {
            if (!application.IsValid)
                return;

            if (ApplyGaugeInternal(application, source))
                RaiseGaugeChanged();
        }

        public void ApplyGauge(IReadOnlyList<ElementGaugeApplication> applications, GameObject source = null)
        {
            if (applications == null || applications.Count == 0)
                return;

            bool changed = false;
            for (int i = 0; i < applications.Count; i++)
            {
                changed |= ApplyGaugeInternal(applications[i], source);
            }

            if (changed)
                RaiseGaugeChanged();
        }

        public void HandleAfterIncomingDamage(MetadataDamage metadataDamage)
        {
            if (_owner == null || !(_owner is Player))
                return;

            bool triggeredHpChanged = false;
            bool requiresDeathFinalize = false;

            foreach (var pair in _ruleMap)
            {
                ConfigCommon.DamageType damageType = pair.Key;
                ClampTriggeredCorruptionToCurrentResources(damageType, raiseChangedEvent: false);

                if (!HasTriggeredCorruption(damageType))
                    continue;

                if (!ShouldConsumeTriggeredCorruption(damageType, metadataDamage))
                    continue;

                long consumed = ConsumeTriggeredCorruptionHp(damageType, GetTriggeredCorruptionTotalHp(damageType));
                if (consumed <= 0)
                    continue;

                triggeredHpChanged = true;
                if (_owner.CurrentHp.Value <= 0)
                    requiresDeathFinalize = true;
            }

            if (triggeredHpChanged)
                RaiseTriggeredHpChanged();

            if (requiresDeathFinalize)
                FinalizeDeathFromTriggeredCorruption();
        }

        private bool ShouldConsumeTriggeredCorruption(ConfigCommon.DamageType damageType, MetadataDamage metadataDamage)
        {
            if (metadataDamage == null)
                return false;

            if (!_ruleMap.TryGetValue(damageType, out ElementGaugeRuleDefinition rule) || rule == null)
                return false;

            bool hasExplicitPolicies = rule.consumePolicies != null && rule.consumePolicies.Count > 0;
            if (!hasExplicitPolicies && !rule.consumeCorruptedHpOnMatchingDamage)
                return false;

            if (hasExplicitPolicies)
            {
                for (int i = 0; i < rule.consumePolicies.Count; i++)
                {
                    ElementGaugeCorruptedHpConsumePolicyDefinition policy = rule.consumePolicies[i];
                    if (policy == null)
                        continue;

                    if (IsMatchedCorruptedHpConsumePolicy(policy, metadataDamage))
                        return true;
                }

                return false;
            }

            return MatchesLegacyConsumePolicy(damageType, metadataDamage);
        }

        private bool IsMatchedCorruptedHpConsumePolicy(ElementGaugeCorruptedHpConsumePolicyDefinition policy, MetadataDamage metadataDamage)
        {
            if (policy == null || metadataDamage == null)
                return false;

            switch (policy.triggerType)
            {
                case ElementGaugeCorruptedHpConsumeTriggerType.IncomingDamageType:
                    return metadataDamage.damageType == policy.damageType;

                case ElementGaugeCorruptedHpConsumeTriggerType.IncomingGaugeApplication:
                    return HasMatchingGaugeApplication(metadataDamage, policy.damageType);

                case ElementGaugeCorruptedHpConsumeTriggerType.IncomingDamageIfAttackerHasAffect:
                    return HasRequiredAttackerAffect(metadataDamage.attacker, policy.requiredAttackerAffectUid);
            }

            return false;
        }

        private static bool MatchesLegacyConsumePolicy(ConfigCommon.DamageType damageType, MetadataDamage metadataDamage)
        {
            if (metadataDamage == null)
                return false;

            if (metadataDamage.damageType == damageType)
                return true;

            return HasMatchingGaugeApplication(metadataDamage, damageType);
        }

        private static bool HasMatchingGaugeApplication(MetadataDamage metadataDamage, ConfigCommon.DamageType damageType)
        {
            if (metadataDamage == null)
                return false;

            ElementGaugeApplication[] apps = metadataDamage.ElementGaugeApplications;
            if (apps == null || apps.Length == 0)
                return false;

            for (int i = 0; i < apps.Length; i++)
            {
                if (apps[i].DamageType == damageType && apps[i].GaugeValue > 0f)
                    return true;
            }

            return false;
        }

        private static bool HasRequiredAttackerAffect(GameObject attacker, int affectUid)
        {
            if (attacker == null || affectUid <= 0)
                return false;

            return AffectApi.HasAttached(attacker, affectUid);
        }

        private bool ApplyGaugeInternal(ElementGaugeApplication application, GameObject source)
        {
            if (_owner == null || _owner.IsStatusDead())
                return false;

            if (!(_owner is Player))
                return false;

            if (!application.IsValid)
                return false;

            if (!_ruleMap.TryGetValue(application.DamageType, out ElementGaugeRuleDefinition rule) || rule == null)
                return false;

            if (rule.blockAccumulationWhileTriggered && HasTriggeredCorruption(application.DamageType))
                return false;

            if (!_stateMap.TryGetValue(application.DamageType, out RuntimeGaugeState state) || state == null)
            {
                state = new RuntimeGaugeState();
                _stateMap[application.DamageType] = state;
            }

            float resistanceMultiplier = ResolveResistanceMultiplier(application.DamageType);
            float appliedValue = Mathf.Max(0f, application.GaugeValue * resistanceMultiplier);
            if (appliedValue <= 0f)
                return false;

            state.CurrentValue += appliedValue;
            state.LastAccumulatedTime = Time.time;
            state.DecayElapsed = 0f;

            if (state.CurrentValue >= Mathf.Max(1f, rule.gaugeMax))
            {
                HandleThresholdReached(rule, source);
                state.CurrentValue = 0f;
                state.DecayElapsed = 0f;
            }

            return true;
        }

        private void HandleThresholdReached(ElementGaugeRuleDefinition rule, GameObject source)
        {
            if (rule == null)
                return;

            ApplyTriggeredCorruption(rule);

            if (rule.thresholdAffectUid > 0)
            {
                AffectApi.Apply(gameObject, rule.thresholdAffectUid, source, rule.thresholdAffectDurationSeconds);
            }
        }

        private void ApplyTriggeredCorruption(ElementGaugeRuleDefinition rule)
        {
            if (rule == null)
                return;

            ApplyTriggeredCorruption(rule.damageType, rule.corruptionHpAmount);
        }

        private void ApplyTriggeredCorruption(ConfigCommon.DamageType damageType, long corruptionHpAmount)
        {
            if (_owner == null)
                return;

            TriggeredHpState state = GetOrCreateTriggeredState(damageType);
            if (state == null)
                return;

            long corruptionBudget = Math.Max(0L, corruptionHpAmount);
            if (corruptionBudget <= 0)
            {
                state.Clear();
                ResetCorruptedHpTickElapsed(damageType);
                RaiseTriggeredHpChanged();
                return;
            }

            long remaining = corruptionBudget;
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

            ResetCorruptedHpTickElapsed(damageType);
            RaiseTriggeredHpChanged();
        }

        private long ConsumeTriggeredCorruptionHp(ConfigCommon.DamageType damageType, long requestedAmount)
        {
            if (requestedAmount <= 0 || _owner == null)
                return 0;

            if (!_triggeredStateMap.TryGetValue(damageType, out TriggeredHpState state) || state == null)
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

            ClampTriggeredCorruptionToCurrentResources(damageType, raiseChangedEvent: false);
            return totalConsumed;
        }

        private bool ClampTriggeredCorruptionToCurrentResources(ConfigCommon.DamageType damageType, bool raiseChangedEvent)
        {
            if (!_triggeredStateMap.TryGetValue(damageType, out TriggeredHpState state) || state == null || _owner == null)
                return false;

            long clampedBase = Math.Min(state.BaseHp, Math.Max(0, _owner.CurrentHp.Value));
            long clampedTempItem = Math.Min(state.TempItemHp, Math.Max(0, _owner.GetItemBonusHpTempCurrent()));
            long clampedTempRuntime = Math.Min(state.TempRuntimeHp, Math.Max(0, _owner.GetRuntimeBonusHpTempCurrent()));
            long clampedTempPassive = Math.Min(state.TempPassiveHp, Math.Max(0, _owner.GetPassiveBonusHpTempCurrent()));

            if (clampedBase == state.BaseHp &&
                clampedTempItem == state.TempItemHp &&
                clampedTempRuntime == state.TempRuntimeHp &&
                clampedTempPassive == state.TempPassiveHp)
                return false;

            state.BaseHp = clampedBase;
            state.TempItemHp = clampedTempItem;
            state.TempRuntimeHp = clampedTempRuntime;
            state.TempPassiveHp = clampedTempPassive;

            if (raiseChangedEvent)
                RaiseTriggeredHpChanged();

            return true;
        }

        private bool HasTriggeredCorruption(ConfigCommon.DamageType damageType)
        {
            return GetTriggeredCorruptionTotalHp(damageType) > 0;
        }

        private long GetTriggeredCorruptionTotalHp(ConfigCommon.DamageType damageType)
        {
            return _triggeredStateMap.TryGetValue(damageType, out TriggeredHpState state) && state != null
                ? state.TotalHp
                : 0L;
        }

        private void FinalizeDeathFromTriggeredCorruption()
        {
            if (_owner == null || _owner.IsStatusDead())
                return;

            if (_owner.BaseHp < 0 && _owner.CurrentHp.Value <= 0)
            {
                _owner.CurrentHp.OnNext(1);
                return;
            }

            if (_owner.CurrentHp.Value > 0)
                return;

            _owner.CurrentMp.OnNext(0);
            _owner.Dead(CharacterConstants.DieReasonType.Battle, null, playDeadAnimation: true);
        }

        private float ResolveResistanceMultiplier(ConfigCommon.DamageType damageType)
        {
            if (_owner == null)
                return 1f;

            long resistance = damageType switch
            {
                ConfigCommon.DamageType.Fire => _owner.TotalRegistFire.Value,
                ConfigCommon.DamageType.Cold => _owner.TotalRegistCold.Value,
                ConfigCommon.DamageType.Lightning => _owner.TotalRegistLightning.Value,
                ConfigCommon.DamageType.Poison => _owner.TotalRegistPoison.Value,
                _ => 0L,
            };

            return Mathf.Clamp01((100f - Mathf.Clamp(resistance, 0f, 100f)) / 100f);
        }

        private void InitializeRules()
        {
            _rules.Clear();

            if (!(_owner is Player))
                return;

            GGemCoPlayerSettings settings = ResolvePlayerSettings();
            List<ElementGaugeRuleDefinition> configuredRules = settings != null ? settings.elementGaugeRules : null;
            if (configuredRules != null)
            {
                for (int i = 0; i < configuredRules.Count; i++)
                {
                    ElementGaugeRuleDefinition rule = configuredRules[i];
                    if (rule == null)
                        continue;

                    _rules.Add(rule.Clone());
                }
            }

            if (_rules.Count == 0)
                _rules.AddRange(ElementGaugeRuleDefinition.CreateDefaultPlayerRules());
        }

        private GGemCoPlayerSettings ResolvePlayerSettings()
        {
            if (!(_owner is Player))
                return null;

            return AddressableLoaderSettings.Instance != null ? AddressableLoaderSettings.Instance.playerSettings : null;
        }

        private void RebuildCaches()
        {
            _ruleMap.Clear();
            _stateMap.Clear();
            _triggeredStateMap.Clear();

            for (int i = 0; i < _rules.Count; i++)
            {
                ElementGaugeRuleDefinition rule = _rules[i];
                if (rule == null)
                    continue;
                if (rule.damageType == ConfigCommon.DamageType.None || rule.damageType == ConfigCommon.DamageType.Physic)
                    continue;

                _ruleMap[rule.damageType] = rule;
                _stateMap[rule.damageType] = new RuntimeGaugeState();
                _triggeredStateMap[rule.damageType] = new TriggeredHpState();
            }
        }

        private TriggeredHpState GetOrCreateTriggeredState(ConfigCommon.DamageType damageType)
        {
            if (_triggeredStateMap.TryGetValue(damageType, out TriggeredHpState state) && state != null)
                return state;

            state = new TriggeredHpState();
            _triggeredStateMap[damageType] = state;
            return state;
        }

        private void ResetCorruptedHpTickElapsed(ConfigCommon.DamageType damageType)
        {
            if (_stateMap.TryGetValue(damageType, out RuntimeGaugeState state) && state != null)
                state.CorruptedHpTickElapsed = 0f;
        }

        private string ResolveTriggeredHudStateKey(ConfigCommon.DamageType damageType)
        {
            if (_ruleMap.TryGetValue(damageType, out ElementGaugeRuleDefinition rule) && rule != null)
            {
                string configured = rule.triggeredHudStateKey;
                if (!string.IsNullOrWhiteSpace(configured))
                    return configured.Trim();
            }

            return damageType.ToString().ToLowerInvariant();
        }

        private ElementTriggeredHpCollectionSnapshot BuildTriggeredHpCollectionSnapshot()
        {
            _triggeredSnapshotBuffer.Clear();

            for (int i = 0; i < _rules.Count; i++)
            {
                ElementGaugeRuleDefinition rule = _rules[i];
                if (rule == null)
                    continue;

                ConfigCommon.DamageType damageType = rule.damageType;
                if (damageType == ConfigCommon.DamageType.None || damageType == ConfigCommon.DamageType.Physic)
                    continue;

                if (!_triggeredStateMap.TryGetValue(damageType, out TriggeredHpState state) || state == null || state.TotalHp <= 0)
                    continue;

                _triggeredSnapshotBuffer.Add(new ElementTriggeredHpSnapshot(
                    damageType,
                    ResolveTriggeredHudStateKey(damageType),
                    state.BaseHp,
                    state.TempItemHp,
                    state.TempRuntimeHp,
                    state.TempPassiveHp));
            }

            return _triggeredSnapshotBuffer.Count > 0
                ? new ElementTriggeredHpCollectionSnapshot(_triggeredSnapshotBuffer.ToArray())
                : ElementTriggeredHpCollectionSnapshot.Empty;
        }

        private void RaiseGaugeChanged()
        {
            GaugeChanged?.Invoke();
        }

        private void RaiseTriggeredHpChanged()
        {
            ElementTriggeredHpCollectionSnapshot snapshot = CurrentTriggeredHpStates;
            TriggeredHpChanged?.Invoke(snapshot);
            CorruptionChanged?.Invoke(snapshot.GetLegacyCorruptionSnapshot(ConfigCommon.DamageType.Poison));
        }
    }
}
