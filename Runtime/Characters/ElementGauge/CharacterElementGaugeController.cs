using System;
using System.Collections.Generic;
using UnityEngine;

namespace GGemCo2DCore
{
    /// <summary>
    /// 캐릭터의 속성 게이지 누적/감쇠/임계 반응을 관리합니다.
    /// 1차 구현에서는 Poison 임계 반응으로 독 하트 오염 상태를 지원합니다.
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

        private readonly List<ElementGaugeRuleDefinition> _rules = new();

        private readonly Dictionary<ConfigCommon.DamageType, ElementGaugeRuleDefinition> _ruleMap = new();
        private readonly Dictionary<ConfigCommon.DamageType, RuntimeGaugeState> _stateMap = new();
        private readonly List<ElementGaugeSnapshot> _snapshots = new();

        private CharacterBase _owner;
        private long _corruptedBaseHp;
        private long _corruptedTempItemHp;
        private long _corruptedTempRuntimeHp;
        private long _corruptedTempPassiveHp;

        public event Action GaugeChanged;
        public event Action<HpCorruptionSnapshot> CorruptionChanged;

        public HpCorruptionSnapshot CurrentCorruption => new(_corruptedBaseHp, _corruptedTempItemHp, _corruptedTempRuntimeHp, _corruptedTempPassiveHp);

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
            bool corruptionChanged = false;
            bool requiresDeathFinalize = false;
            float now = Time.time;
            float delta = Time.deltaTime;

            foreach (var pair in _ruleMap)
            {
                var damageType = pair.Key;
                var rule = pair.Value;
                if (rule == null)
                    continue;

                if (!_stateMap.TryGetValue(damageType, out var state) || state == null)
                    continue;

                if (state.CurrentValue > 0f)
                {
                    if (now - state.LastAccumulatedTime >= Mathf.Max(0f, rule.decayDelaySeconds))
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
                }

                ClampTriggeredCorruptionToCurrentResources(damageType, raiseChangedEvent: false);

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
                        corruptionChanged = true;
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

            if (corruptionChanged)
                RaiseCorruptionChanged();

            if (requiresDeathFinalize)
                FinalizeDeathFromTriggeredCorruption();
        }

        public IReadOnlyList<ElementGaugeSnapshot> GetGaugeSnapshots()
        {
            _snapshots.Clear();

            foreach (var pair in _ruleMap)
            {
                if (!_stateMap.TryGetValue(pair.Key, out var state) || state == null)
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

            ClampTriggeredCorruptionToCurrentResources(ConfigCommon.DamageType.Poison, raiseChangedEvent: false);

            if (!HasTriggeredCorruption(ConfigCommon.DamageType.Poison))
                return;

            if (!ShouldConsumeTriggeredCorruption(ConfigCommon.DamageType.Poison, metadataDamage))
                return;

            long consumed = ConsumeTriggeredCorruptionHp(ConfigCommon.DamageType.Poison, GetTriggeredCorruptionTotalHp(ConfigCommon.DamageType.Poison));
            if (consumed > 0)
                RaiseCorruptionChanged();
        }


        private bool ShouldConsumeTriggeredCorruption(ConfigCommon.DamageType damageType, MetadataDamage metadataDamage)
        {
            if (metadataDamage == null)
                return false;

            if (!_ruleMap.TryGetValue(damageType, out var rule) || rule == null)
                return false;

            var hasExplicitPolicies = rule.consumePolicies != null && rule.consumePolicies.Count > 0;
            if (!hasExplicitPolicies && !rule.consumeCorruptedHpOnMatchingDamage)
                return false;

            if (hasExplicitPolicies)
            {
                for (int i = 0; i < rule.consumePolicies.Count; i++)
                {
                    var policy = rule.consumePolicies[i];
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

            var apps = metadataDamage.ElementGaugeApplications;
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

            if (!_ruleMap.TryGetValue(application.DamageType, out var rule) || rule == null)
                return false;

            if (rule.blockAccumulationWhileTriggered && HasTriggeredCorruption(application.DamageType))
                return false;

            if (!_stateMap.TryGetValue(application.DamageType, out var state) || state == null)
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

            switch (rule.damageType)
            {
                case ConfigCommon.DamageType.Poison:
                    ApplyPoisonCorruption(rule.corruptionHpAmount);
                    break;
            }
        }

        private void ApplyPoisonCorruption(long corruptionHpAmount)
        {
            if (_owner == null)
                return;

            long corruptionBudget = Math.Max(0L, corruptionHpAmount);
            if (corruptionBudget <= 0)
                return;

            long remaining = corruptionBudget;

            long currentTempItem = (long)Mathf.Max(0, _owner.GetItemBonusHpTempCurrent());
            long currentTempRuntime = (long)Mathf.Max(0, _owner.GetRuntimeBonusHpTempCurrent());
            long currentTempPassive = (long)Mathf.Max(0, _owner.GetPassiveBonusHpTempCurrent());
            long currentBase = (long)Mathf.Max(0, _owner.CurrentHp.Value);

            _corruptedTempItemHp = Math.Min(remaining, currentTempItem);
            remaining -= _corruptedTempItemHp;

            _corruptedTempRuntimeHp = Math.Min(remaining, currentTempRuntime);
            remaining -= _corruptedTempRuntimeHp;

            _corruptedTempPassiveHp = Math.Min(remaining, currentTempPassive);
            remaining -= _corruptedTempPassiveHp;

            _corruptedBaseHp = Math.Min(remaining, currentBase);

            if (_stateMap.TryGetValue(ConfigCommon.DamageType.Poison, out var state) && state != null)
                state.CorruptedHpTickElapsed = 0f;

            RaiseCorruptionChanged();
        }

        private long ConsumeTriggeredCorruptionHp(ConfigCommon.DamageType damageType, long requestedAmount)
        {
            if (requestedAmount <= 0)
                return 0;

            return damageType switch
            {
                ConfigCommon.DamageType.Poison => ConsumePoisonCorruptionHp(requestedAmount),
                _ => 0,
            };
        }

        private long ConsumePoisonCorruptionHp(long requestedAmount)
        {
            long remainingBudget = Math.Max(0, requestedAmount);
            long totalConsumed = 0;

            if (_corruptedTempItemHp > 0 && remainingBudget > 0)
            {
                long target = Math.Min(_corruptedTempItemHp, remainingBudget);
                long remaining = _owner.ConsumeHpTempItem(target);
                long consumed = target - remaining;
                totalConsumed += consumed;
                remainingBudget -= consumed;
                _corruptedTempItemHp = Math.Max(0, _corruptedTempItemHp - consumed);
            }

            if (_corruptedTempRuntimeHp > 0 && remainingBudget > 0)
            {
                long target = Math.Min(_corruptedTempRuntimeHp, remainingBudget);
                long remaining = _owner.ConsumeHpTempRuntime(target);
                long consumed = target - remaining;
                totalConsumed += consumed;
                remainingBudget -= consumed;
                _corruptedTempRuntimeHp = Math.Max(0, _corruptedTempRuntimeHp - consumed);
            }

            if (_corruptedTempPassiveHp > 0 && remainingBudget > 0)
            {
                long target = Math.Min(_corruptedTempPassiveHp, remainingBudget);
                long remaining = _owner.ConsumeHpTempPassive(target);
                long consumed = target - remaining;
                totalConsumed += consumed;
                remainingBudget -= consumed;
                _corruptedTempPassiveHp = Math.Max(0, _corruptedTempPassiveHp - consumed);
            }

            if (_corruptedBaseHp > 0 && remainingBudget > 0)
            {
                long consume = Math.Min(Math.Min(_corruptedBaseHp, remainingBudget), Math.Max(0, _owner.CurrentHp.Value));
                if (consume > 0)
                {
                    _owner.CurrentHp.OnNext(Math.Max(0, _owner.CurrentHp.Value - consume));
                    _corruptedBaseHp -= consume;
                    totalConsumed += consume;
                }
            }

            ClampTriggeredCorruptionToCurrentResources(ConfigCommon.DamageType.Poison, raiseChangedEvent: false);
            return totalConsumed;
        }

        private void ClampTriggeredCorruptionToCurrentResources(ConfigCommon.DamageType damageType, bool raiseChangedEvent)
        {
            switch (damageType)
            {
                case ConfigCommon.DamageType.Poison:
                    ClampPoisonCorruptionToCurrentResources(raiseChangedEvent);
                    break;
            }
        }

        private void ClampPoisonCorruptionToCurrentResources(bool raiseChangedEvent)
        {
            long clampedBase = Math.Min(_corruptedBaseHp, Math.Max(0, _owner.CurrentHp.Value));
            long clampedTempItem = Math.Min(_corruptedTempItemHp, Math.Max(0, _owner.GetItemBonusHpTempCurrent()));
            long clampedTempRuntime = Math.Min(_corruptedTempRuntimeHp, Math.Max(0, _owner.GetRuntimeBonusHpTempCurrent()));
            long clampedTempPassive = Math.Min(_corruptedTempPassiveHp, Math.Max(0, _owner.GetPassiveBonusHpTempCurrent()));

            if (clampedBase == _corruptedBaseHp &&
                clampedTempItem == _corruptedTempItemHp &&
                clampedTempRuntime == _corruptedTempRuntimeHp &&
                clampedTempPassive == _corruptedTempPassiveHp)
                return;

            _corruptedBaseHp = clampedBase;
            _corruptedTempItemHp = clampedTempItem;
            _corruptedTempRuntimeHp = clampedTempRuntime;
            _corruptedTempPassiveHp = clampedTempPassive;

            if (raiseChangedEvent)
                RaiseCorruptionChanged();
        }

        private bool HasTriggeredCorruption(ConfigCommon.DamageType damageType)
        {
            return GetTriggeredCorruptionTotalHp(damageType) > 0;
        }

        private long GetTriggeredCorruptionTotalHp(ConfigCommon.DamageType damageType)
        {
            return damageType switch
            {
                ConfigCommon.DamageType.Poison => _corruptedBaseHp + _corruptedTempItemHp + _corruptedTempRuntimeHp + _corruptedTempPassiveHp,
                _ => 0,
            };
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

            var settings = ResolvePlayerSettings();
            var configuredRules = settings != null ? settings.elementGaugeRules : null;
            if (configuredRules != null)
            {
                for (int i = 0; i < configuredRules.Count; i++)
                {
                    var rule = configuredRules[i];
                    if (rule == null)
                        continue;

                    _rules.Add(rule.Clone());
                }
            }

            if (_rules.Count == 0)
            {
                _rules.AddRange(ElementGaugeRuleDefinition.CreateDefaultPlayerRules());
            }
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

            for (int i = 0; i < _rules.Count; i++)
            {
                var rule = _rules[i];
                if (rule == null)
                    continue;
                if (rule.damageType == ConfigCommon.DamageType.None || rule.damageType == ConfigCommon.DamageType.Physic)
                    continue;

                _ruleMap[rule.damageType] = rule;
                _stateMap[rule.damageType] = new RuntimeGaugeState();
            }
        }

        private void RaiseGaugeChanged()
        {
            GaugeChanged?.Invoke();
        }

        private void RaiseCorruptionChanged()
        {
            CorruptionChanged?.Invoke(CurrentCorruption);
        }
    }
}
