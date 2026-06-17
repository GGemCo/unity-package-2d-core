using System;
using System.Collections.Generic;
using UnityEngine;

namespace GGemCo2DCore
{
    /// <summary>
    /// 캐릭터의 속성 게이지 시스템 오케스트레이터입니다.
    /// 게이지 누적/감쇠, 임계 처리, Triggered HP Tick/소모를 전용 프로세서에 위임합니다.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class CharacterElementGaugeController : MonoBehaviour
    {
        private readonly List<ElementGaugeRuleDefinition> _rules = new();
        private readonly List<ElementGaugeSnapshot> _snapshots = new();
        private readonly List<ElementTriggeredHpSnapshot> _triggeredSnapshotBuffer = new();

        private CharacterBase _owner;
        private ElementGaugeRuntime _runtime;
        private ElementGaugeProcessor _gaugeProcessor;
        private ElementGaugeThresholdProcessor _thresholdProcessor;
        private ElementTriggeredHpService _triggeredHpService;
        private ElementGaugeThresholdAffectService _thresholdAffectService;
        private ElementTriggeredHpTickProcessor _tickProcessor;
        private ElementTriggeredHpConsumeProcessor _consumeProcessor;

        public event Action GaugeChanged;
        public event Action<ElementTriggeredHpCollectionSnapshot> TriggeredHpChanged;
        public event Action<HpCorruptionSnapshot> CorruptionChanged;

        public ElementTriggeredHpCollectionSnapshot CurrentTriggeredHpStates =>
            _triggeredHpService != null
                ? _triggeredHpService.BuildTriggeredHpCollectionSnapshot(_runtime, _triggeredSnapshotBuffer)
                : ElementTriggeredHpCollectionSnapshot.Empty;

        public HpCorruptionSnapshot CurrentCorruption =>
            CurrentTriggeredHpStates.GetLegacyCorruptionSnapshot(ConfigCommon.DamageType.Poison);

        private void Awake()
        {
            _owner = GetComponent<CharacterBase>();
            InitializeRules();
            _runtime = new ElementGaugeRuntime(_rules);
            _triggeredHpService = new ElementTriggeredHpService(_owner);
            _thresholdAffectService = new ElementGaugeThresholdAffectService(_owner);
            _gaugeProcessor = new ElementGaugeProcessor(_owner);
            _thresholdProcessor = new ElementGaugeThresholdProcessor(_owner, _triggeredHpService, _thresholdAffectService);
            _tickProcessor = new ElementTriggeredHpTickProcessor(_owner, _triggeredHpService, _thresholdAffectService);
            _consumeProcessor = new ElementTriggeredHpConsumeProcessor(_owner, _triggeredHpService, _thresholdAffectService);
        }

        private void Update()
        {
            if (!CanProcessRuntime())
                return;

            ElementGaugeDecayResult decayResult = _gaugeProcessor.UpdateDecay(_runtime, Time.time, Time.deltaTime);
            ElementTriggeredHpTickResult tickResult = _tickProcessor.UpdateTick(_runtime, Time.deltaTime);

            if (decayResult.GaugeChanged || tickResult.GaugeChanged)
                RaiseGaugeChanged();

            if (tickResult.TriggeredHpChanged)
                RaiseTriggeredHpChanged();

            if (tickResult.RequiresDeathFinalize)
                FinalizeDeathFromTriggeredCorruption();
        }

        public IReadOnlyList<ElementGaugeSnapshot> GetGaugeSnapshots()
        {
            return _gaugeProcessor != null
                ? _gaugeProcessor.BuildSnapshots(_runtime, _triggeredHpService, _snapshots)
                : _snapshots;
        }

        public void ApplyGauge(ElementGaugeApplication application, GameObject source = null)
        {
            if (!application.IsValid)
                return;

            bool gaugeChanged = false;
            bool triggeredHpChanged = false;
            ProcessGaugeApplication(application, source, ref gaugeChanged, ref triggeredHpChanged);
            RaisePendingEvents(gaugeChanged, triggeredHpChanged);
        }

        public void ApplyGauge(IReadOnlyList<ElementGaugeApplication> applications, GameObject source = null)
        {
            if (applications == null || applications.Count == 0)
                return;

            bool gaugeChanged = false;
            bool triggeredHpChanged = false;
            for (int i = 0; i < applications.Count; i++)
            {
                ProcessGaugeApplication(applications[i], source, ref gaugeChanged, ref triggeredHpChanged);
            }

            RaisePendingEvents(gaugeChanged, triggeredHpChanged);
        }

        public void HandleAfterIncomingDamage(MetadataDamage metadataDamage)
        {
            if (!CanProcessRuntime())
                return;

            ElementTriggeredHpConsumeResult result = _consumeProcessor.HandleAfterIncomingDamage(_runtime, metadataDamage);
            if (result.GaugeChanged)
                RaiseGaugeChanged();

            if (result.TriggeredHpChanged)
                RaiseTriggeredHpChanged();

            if (result.RequiresDeathFinalize)
                FinalizeDeathFromTriggeredCorruption();
        }

        private void ProcessGaugeApplication(
            ElementGaugeApplication application,
            GameObject source,
            ref bool gaugeChanged,
            ref bool triggeredHpChanged)
        {
            if (!CanProcessRuntime())
                return;

            ElementGaugeApplyResult result = _gaugeProcessor.ApplyGauge(_runtime, application, Time.time);
            if (!result.GaugeChanged && !result.ThresholdReached)
                return;

            gaugeChanged |= result.GaugeChanged;
            if (!result.ThresholdReached)
                return;

            ElementGaugeThresholdResult thresholdResult = _thresholdProcessor.ProcessThreshold(_runtime, result.DamageType, source);
            triggeredHpChanged |= thresholdResult.TriggeredHpChanged;
        }

        private bool CanProcessRuntime()
        {
            return _owner != null && !_owner.IsStatusDead() && _owner is Player && _runtime != null;
        }

        private void RaisePendingEvents(bool gaugeChanged, bool triggeredHpChanged)
        {
            if (gaugeChanged)
                RaiseGaugeChanged();

            if (triggeredHpChanged)
                RaiseTriggeredHpChanged();
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
