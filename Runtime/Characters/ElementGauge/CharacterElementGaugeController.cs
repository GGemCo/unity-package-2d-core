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
        }

        private readonly List<ElementGaugeRuleDefinition> _rules = new();

        private readonly Dictionary<ConfigCommon.DamageType, ElementGaugeRuleDefinition> _ruleMap = new();
        private readonly Dictionary<ConfigCommon.DamageType, RuntimeGaugeState> _stateMap = new();
        private readonly List<ElementGaugeSnapshot> _snapshots = new();

        private CharacterBase _owner;
        private long _corruptedBaseHp;
        private long _corruptedTempItemHp;
        private long _corruptedTempPassiveHp;

        public event Action GaugeChanged;
        public event Action<HpCorruptionSnapshot> CorruptionChanged;

        public HpCorruptionSnapshot CurrentCorruption => new(_corruptedBaseHp, _corruptedTempItemHp, _corruptedTempPassiveHp);

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

            bool changed = false;
            float now = Time.time;
            float delta = Time.deltaTime;

            foreach (var pair in _ruleMap)
            {
                var rule = pair.Value;
                if (rule == null)
                    continue;

                if (!_stateMap.TryGetValue(pair.Key, out var state) || state == null)
                    continue;

                if (state.CurrentValue <= 0f)
                    continue;

                if (now - state.LastAccumulatedTime < Mathf.Max(0f, rule.decayDelaySeconds))
                    continue;

                state.DecayElapsed += delta;
                float tick = Mathf.Max(0.01f, rule.decayTickSeconds);
                float decayPerTick = Mathf.Max(0f, rule.gaugeMax) * Mathf.Max(0f, rule.decayPercentPerTick) * 0.01f;

                if (decayPerTick <= 0f)
                    continue;

                while (state.DecayElapsed >= tick)
                {
                    state.DecayElapsed -= tick;
                    float next = Mathf.Max(0f, state.CurrentValue - decayPerTick);
                    if (!Mathf.Approximately(next, state.CurrentValue))
                    {
                        state.CurrentValue = next;
                        changed = true;
                    }

                    if (state.CurrentValue <= 0f)
                    {
                        state.CurrentValue = 0f;
                        state.DecayElapsed = 0f;
                        break;
                    }
                }
            }

            if (changed)
                RaiseGaugeChanged();
        }

        public IReadOnlyList<ElementGaugeSnapshot> GetGaugeSnapshots()
        {
            _snapshots.Clear();

            foreach (var pair in _ruleMap)
            {
                if (!_stateMap.TryGetValue(pair.Key, out var state) || state == null)
                    continue;

                bool isBlocked = pair.Key == ConfigCommon.DamageType.Poison && HasPoisonCorruption();
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

            ClampCorruptionToCurrentResources();

            if (!HasPoisonCorruption())
                return;

            if (!ShouldConsumePoisonCorruption(metadataDamage))
                return;

            long consumed = ConsumePoisonCorruptionHp();
            if (consumed > 0)
            {
                RaiseCorruptionChanged();
            }
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

            if (rule.blockAccumulationWhileTriggered && application.DamageType == ConfigCommon.DamageType.Poison && HasPoisonCorruption())
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

            if (rule.damageType == ConfigCommon.DamageType.Poison)
            {
                ApplyPoisonCorruption(rule);
            }

            if (rule.thresholdAffectUid > 0)
            {
                AffectApi.Apply(gameObject, rule.thresholdAffectUid, source, rule.thresholdAffectDurationSeconds);
            }
        }

        private void ApplyPoisonCorruption(ElementGaugeRuleDefinition rule)
        {
            if (_owner == null)
                return;

            long corruptionBudget = Math.Max(0L, rule.corruptionHpAmount);
            if (corruptionBudget <= 0)
                return;

            long remaining = corruptionBudget;

            long currentTempItem = (long)Mathf.Max(0, _owner.GetItemBonusHpTempCurrent());
            long currentTempPassive = (long)Mathf.Max(0, _owner.GetPassiveBonusHpTempCurrent());
            long currentBase = (long)Mathf.Max(0, _owner.CurrentHp.Value);

            _corruptedTempItemHp = Math.Min(remaining, currentTempItem);
            remaining -= _corruptedTempItemHp;

            _corruptedTempPassiveHp = Math.Min(remaining, currentTempPassive);
            remaining -= _corruptedTempPassiveHp;

            _corruptedBaseHp = Math.Min(remaining, currentBase);

            RaiseCorruptionChanged();
        }

        private bool ShouldConsumePoisonCorruption(MetadataDamage metadataDamage)
        {
            if (metadataDamage == null)
                return false;

            if (!_ruleMap.TryGetValue(ConfigCommon.DamageType.Poison, out var rule) || rule == null)
                return false;

            if (!rule.consumeCorruptedHpOnMatchingDamage)
                return false;

            if (metadataDamage.damageType == ConfigCommon.DamageType.Poison)
                return true;

            var apps = metadataDamage.ElementGaugeApplications;
            if (apps == null || apps.Length == 0)
                return false;

            for (int i = 0; i < apps.Length; i++)
            {
                if (apps[i].DamageType == ConfigCommon.DamageType.Poison && apps[i].GaugeValue > 0f)
                    return true;
            }

            return false;
        }

        private long ConsumePoisonCorruptionHp()
        {
            long totalConsumed = 0;

            if (_corruptedTempItemHp > 0)
            {
                long target = _corruptedTempItemHp;
                long remaining = _owner.ConsumeHpTempItem(target);
                long consumed = target - remaining;
                totalConsumed += consumed;
                _corruptedTempItemHp = Math.Max(0, _corruptedTempItemHp - consumed);
            }

            if (_corruptedTempPassiveHp > 0)
            {
                long target = _corruptedTempPassiveHp;
                long remaining = _owner.ConsumeHpTempPassive(target);
                long consumed = target - remaining;
                totalConsumed += consumed;
                _corruptedTempPassiveHp = Math.Max(0, _corruptedTempPassiveHp - consumed);
            }

            if (_corruptedBaseHp > 0)
            {
                long consume = Math.Min(_corruptedBaseHp, Math.Max(0, _owner.CurrentHp.Value));
                if (consume > 0)
                {
                    _owner.CurrentHp.OnNext(Math.Max(0, _owner.CurrentHp.Value - consume));
                    _corruptedBaseHp -= consume;
                    totalConsumed += consume;
                }
            }

            ClampCorruptionToCurrentResources();
            return totalConsumed;
        }

        private void ClampCorruptionToCurrentResources()
        {
            long clampedBase = Math.Min(_corruptedBaseHp, Math.Max(0, _owner.CurrentHp.Value));
            long clampedTempItem = Math.Min(_corruptedTempItemHp, Math.Max(0, _owner.GetItemBonusHpTempCurrent()));
            long clampedTempPassive = Math.Min(_corruptedTempPassiveHp, Math.Max(0, _owner.GetPassiveBonusHpTempCurrent()));

            if (clampedBase == _corruptedBaseHp && clampedTempItem == _corruptedTempItemHp && clampedTempPassive == _corruptedTempPassiveHp)
                return;

            _corruptedBaseHp = clampedBase;
            _corruptedTempItemHp = clampedTempItem;
            _corruptedTempPassiveHp = clampedTempPassive;
            RaiseCorruptionChanged();
        }

        private bool HasPoisonCorruption()
        {
            return (_corruptedBaseHp + _corruptedTempItemHp + _corruptedTempPassiveHp) > 0;
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
