using System;
using System.Collections.Generic;
using UnityEngine;

namespace GGemCo2DCore
{
    /// <summary>
    /// 캐릭터가 실제로 받은 속성별 데미지를 누적하고, UI/확장 핸들러에 상태 변화를 전달하는 컨트롤러입니다.
    /// </summary>
    /// <remarks>
    /// 이 컨트롤러는 속성 게이지의 누적, 감쇠, 임계 도달 이벤트만 담당합니다.
    /// 임계 도달 후 효과와 임계 상태에서 같은 속성 데미지를 다시 받았을 때의 처리는 프로젝트별 핸들러로 확장합니다.
    /// </remarks>
    [DisallowMultipleComponent]
    public sealed class CharacterElementGaugeController : MonoBehaviour
    {
        private readonly List<ElementGaugeRuleDefinition> _rules = new();
        private readonly List<ElementGaugeSnapshot> _snapshots = new();

        private CharacterBase _owner;
        private ElementGaugeRuntime _runtime;
        private ElementGaugeProcessor _gaugeProcessor;
        private IElementGaugeThresholdHandler _thresholdHandler = NullElementGaugeThresholdHandler.Instance;
        private IElementGaugeRepeatedHitHandler _repeatedHitHandler = NullElementGaugeRepeatedHitHandler.Instance;

        /// <summary>
        /// 전역 기본 임계 도달 핸들러입니다.
        /// 씬/프로젝트 부트스트랩에서 설정하면 이후 생성되는 컨트롤러의 기본 핸들러로 사용됩니다.
        /// </summary>
        public static IElementGaugeThresholdHandler DefaultThresholdHandler { get; set; } = NullElementGaugeThresholdHandler.Instance;

        /// <summary>
        /// 전역 기본 임계 상태 재피격 핸들러입니다.
        /// 씬/프로젝트 부트스트랩에서 설정하면 이후 생성되는 컨트롤러의 기본 핸들러로 사용됩니다.
        /// </summary>
        public static IElementGaugeRepeatedHitHandler DefaultRepeatedHitHandler { get; set; } = NullElementGaugeRepeatedHitHandler.Instance;

        /// <summary>
        /// 속성 게이지 표시 상태가 변경되었을 때 발생합니다.
        /// </summary>
        public event Action GaugeChanged;

        /// <summary>
        /// 속성 게이지가 임계값에 처음 도달했을 때 발생합니다.
        /// </summary>
        public event Action<ElementGaugeSnapshot, ElementGaugeAccumulationContext> ThresholdReached;

        /// <summary>
        /// 이미 임계 상태인 속성에 같은 속성 데미지가 다시 들어왔을 때 발생합니다.
        /// </summary>
        public event Action<ElementGaugeSnapshot, ElementGaugeAccumulationContext> RepeatedElementDamageReceived;

        private void Awake()
        {
            _owner = GetComponent<CharacterBase>();
            InitializeRules();
            _runtime = new ElementGaugeRuntime(_rules);
            _gaugeProcessor = new ElementGaugeProcessor();
            _thresholdHandler = DefaultThresholdHandler ?? NullElementGaugeThresholdHandler.Instance;
            _repeatedHitHandler = DefaultRepeatedHitHandler ?? NullElementGaugeRepeatedHitHandler.Instance;
        }

        private void Update()
        {
            if (!CanProcessRuntime())
                return;

            ElementGaugeDecayResult decayResult = _gaugeProcessor.UpdateDecay(_runtime, Time.time, Time.deltaTime);
            if (decayResult.GaugeChanged)
                RaiseGaugeChanged();
        }

        /// <summary>
        /// 임계 도달 효과 핸들러를 교체합니다.
        /// </summary>
        /// <param name="handler">프로젝트별 임계 도달 핸들러입니다. null이면 기본 Null 핸들러를 사용합니다.</param>
        public void SetThresholdHandler(IElementGaugeThresholdHandler handler)
        {
            _thresholdHandler = handler ?? NullElementGaugeThresholdHandler.Instance;
        }

        /// <summary>
        /// 임계 상태 재피격 핸들러를 교체합니다.
        /// </summary>
        /// <param name="handler">프로젝트별 재피격 핸들러입니다. null이면 기본 Null 핸들러를 사용합니다.</param>
        public void SetRepeatedHitHandler(IElementGaugeRepeatedHitHandler handler)
        {
            _repeatedHitHandler = handler ?? NullElementGaugeRepeatedHitHandler.Instance;
        }

        /// <summary>
        /// 현재 속성 게이지 스냅샷 목록을 반환합니다.
        /// </summary>
        /// <returns>속성별 게이지 스냅샷 목록입니다.</returns>
        public IReadOnlyList<ElementGaugeSnapshot> GetGaugeSnapshots()
        {
            return _gaugeProcessor != null
                ? _gaugeProcessor.BuildSnapshots(_runtime, _snapshots)
                : _snapshots;
        }

        /// <summary>
        /// 특정 속성 게이지 스냅샷을 조회합니다.
        /// </summary>
        /// <param name="damageType">조회할 속성 타입입니다.</param>
        /// <param name="snapshot">조회된 스냅샷입니다.</param>
        /// <returns>대상 속성 게이지가 있으면 <see langword="true"/>입니다.</returns>
        public bool TryGetGaugeSnapshot(ConfigCommon.DamageType damageType, out ElementGaugeSnapshot snapshot)
        {
            IReadOnlyList<ElementGaugeSnapshot> snapshots = GetGaugeSnapshots();
            for (int i = 0; i < snapshots.Count; i++)
            {
                if (snapshots[i].DamageType != damageType)
                    continue;

                snapshot = snapshots[i];
                return true;
            }

            snapshot = default;
            return false;
        }

        /// <summary>
        /// 실제 피격 정보를 기준으로 속성 게이지를 누적합니다.
        /// </summary>
        /// <param name="metadataDamage">데미지 메타데이터입니다.</param>
        /// <param name="damageAmount">이번 피격에서 확정된 최종 HP 데미지량입니다.</param>
        /// <returns>누적 처리 결과입니다.</returns>
        public ElementGaugeAccumulationResult AccumulateFromDamage(MetadataDamage metadataDamage, long damageAmount)
        {
            if (!CanProcessRuntime() || metadataDamage == null)
                return ElementGaugeAccumulationResult.None;

            ConfigCommon.DamageType damageType = metadataDamage.damageType;
            if (damageType == ConfigCommon.DamageType.None || damageType == ConfigCommon.DamageType.Physic)
                return ElementGaugeAccumulationResult.None;

            if (!_runtime.TryGetRule(damageType, out ElementGaugeRuleDefinition rule) || rule == null)
                return ElementGaugeAccumulationResult.None;

            long sourceAmount = ResolveGaugeSourceAmount(rule, metadataDamage, damageType, damageAmount);
            float gaugeAmount = CalculateGaugeAmount(rule, sourceAmount);
            if (sourceAmount <= 0L || gaugeAmount <= 0f)
                return ElementGaugeAccumulationResult.None;

            ElementGaugeApplyResult result = _gaugeProcessor.AccumulateDamage(_runtime, damageType, gaugeAmount, Time.time);
            if (!result.GaugeChanged && !result.ThresholdReached && !result.RepeatedElementDamage)
                return ElementGaugeAccumulationResult.None;

            var context = new ElementGaugeAccumulationContext(_owner, metadataDamage.attacker, metadataDamage, damageType, sourceAmount, gaugeAmount);

            if (result.GaugeChanged)
                RaiseGaugeChanged();

            if (result.ThresholdReached)
            {
                ThresholdReached?.Invoke(result.Snapshot, context);
                _thresholdHandler.OnThresholdReached(result.Snapshot, context);
            }
            else if (result.RepeatedElementDamage)
            {
                RepeatedElementDamageReceived?.Invoke(result.Snapshot, context);
                _repeatedHitHandler.OnRepeatedElementDamage(result.Snapshot, context);
            }

            return new ElementGaugeAccumulationResult(
                result.GaugeChanged,
                result.ThresholdReached,
                result.RepeatedElementDamage,
                result.Snapshot);
        }

        /// <summary>
        /// 속성별 데미지 분해 결과를 기준으로 속성 게이지를 누적합니다.
        /// </summary>
        /// <param name="metadataDamage">이번 피격을 설명하는 데미지 메타데이터입니다.</param>
        /// <param name="breakdown">속성별 데미지 분해 결과입니다.</param>
        /// <returns>마지막으로 변화가 발생한 속성 게이지 누적 결과입니다.</returns>
        /// <remarks>
        /// 신규 데미지 파트 정보가 없으면 기존 단일 데미지 타입 기반 누적 로직으로 되돌아갑니다.
        /// 물리 파트는 게이지 대상이 아니므로 건너뛰고, 화염/냉기/번개/독 파트만 누적합니다.
        /// </remarks>
        public ElementGaugeAccumulationResult AccumulateFromDamageBreakdown(
            MetadataDamage metadataDamage,
            DamageCalculationBreakdown breakdown)
        {
            if (!CanProcessRuntime() || metadataDamage == null)
                return ElementGaugeAccumulationResult.None;

            if (breakdown == null || !breakdown.HasParts)
                return AccumulateFromDamage(metadataDamage, metadataDamage.damage);

            ElementGaugeAccumulationResult lastChangedResult = ElementGaugeAccumulationResult.None;
            IReadOnlyList<DamagePartResult> parts = breakdown.Parts;
            for (int i = 0; i < parts.Count; i++)
            {
                DamagePartResult part = parts[i];
                if (part.DamageType == ConfigCommon.DamageType.None || part.DamageType == ConfigCommon.DamageType.Physic)
                    continue;

                ElementGaugeAccumulationResult result = AccumulateFromDamagePart(metadataDamage, part);
                if (result.GaugeChanged || result.ThresholdReached || result.RepeatedElementDamage)
                    lastChangedResult = result;
            }

            return lastChangedResult;
        }

        /// <summary>
        /// 지정한 속성 게이지를 초기화합니다.
        /// </summary>
        /// <param name="damageType">초기화할 속성 타입입니다.</param>
        public void ResetGauge(ConfigCommon.DamageType damageType)
        {
            if (_runtime == null)
                return;

            if (_runtime.ResetGaugeState(damageType))
                RaiseGaugeChanged();
        }

        /// <summary>
        /// 모든 속성 게이지를 초기화합니다.
        /// </summary>
        public void ResetAllGauges()
        {
            if (_runtime == null)
                return;

            bool changed = false;
            IReadOnlyList<ElementGaugeRuleDefinition> rules = _runtime.Rules;
            for (int i = 0; i < rules.Count; i++)
            {
                if (rules[i] == null)
                    continue;

                changed |= _runtime.ResetGaugeState(rules[i].damageType);
            }

            if (changed)
                RaiseGaugeChanged();
        }

        private bool CanProcessRuntime()
        {
            return _owner != null && !_owner.IsStatusDead() && _runtime != null && _gaugeProcessor != null;
        }

        /// <summary>
        /// 게이지 규칙에 따라 누적 원본 수치를 결정합니다.
        /// </summary>
        /// <param name="rule">현재 속성 게이지 규칙입니다.</param>
        /// <param name="metadataDamage">데미지 메타데이터입니다.</param>
        /// <param name="damageType">누적 대상 속성 타입입니다.</param>
        /// <param name="finalDamageAmount">이번 피격에서 확정된 최종 HP 데미지량입니다.</param>
        /// <returns>게이지 변환에 사용할 원본 수치입니다.</returns>
        private static long ResolveGaugeSourceAmount(
            ElementGaugeRuleDefinition rule,
            MetadataDamage metadataDamage,
            ConfigCommon.DamageType damageType,
            long finalDamageAmount)
        {
            if (rule == null)
                return 0L;

            long safeFinalDamage = Math.Max(0L, finalDamageAmount);
            long attackerElementDamage = ResolveAttackerElementDamage(metadataDamage, damageType);

            return rule.fillSourcePolicy switch
            {
                ElementGaugeFillSourcePolicy.FinalDamage => safeFinalDamage,
                ElementGaugeFillSourcePolicy.AttackerElementDamage => attackerElementDamage,
                ElementGaugeFillSourcePolicy.AttackerElementDamageOrFinalDamage => attackerElementDamage > 0L ? attackerElementDamage : safeFinalDamage,
                _ => attackerElementDamage > 0L ? attackerElementDamage : safeFinalDamage
            };
        }

        /// <summary>
        /// 단일 데미지 파트를 기준으로 속성 게이지를 누적합니다.
        /// </summary>
        /// <param name="metadataDamage">이번 피격을 설명하는 데미지 메타데이터입니다.</param>
        /// <param name="part">누적 대상 데미지 파트입니다.</param>
        /// <returns>속성 게이지 누적 결과입니다.</returns>
        private ElementGaugeAccumulationResult AccumulateFromDamagePart(
            MetadataDamage metadataDamage,
            in DamagePartResult part)
        {
            if (!_runtime.TryGetRule(part.DamageType, out ElementGaugeRuleDefinition rule) || rule == null)
                return ElementGaugeAccumulationResult.None;

            long sourceAmount = ResolveGaugeSourceAmount(rule, part.FinalDamage, part.AttackerElementDamage);
            float gaugeAmount = CalculateGaugeAmount(rule, sourceAmount);
            if (sourceAmount <= 0L || gaugeAmount <= 0f)
                return ElementGaugeAccumulationResult.None;

            ElementGaugeApplyResult result = _gaugeProcessor.AccumulateDamage(_runtime, part.DamageType, gaugeAmount, Time.time);
            if (!result.GaugeChanged && !result.ThresholdReached && !result.RepeatedElementDamage)
                return ElementGaugeAccumulationResult.None;

            var context = new ElementGaugeAccumulationContext(
                _owner,
                metadataDamage.attacker,
                metadataDamage,
                part.DamageType,
                sourceAmount,
                gaugeAmount);

            if (result.GaugeChanged)
                RaiseGaugeChanged();

            if (result.ThresholdReached)
            {
                ThresholdReached?.Invoke(result.Snapshot, context);
                _thresholdHandler.OnThresholdReached(result.Snapshot, context);
            }
            else if (result.RepeatedElementDamage)
            {
                RepeatedElementDamageReceived?.Invoke(result.Snapshot, context);
                _repeatedHitHandler.OnRepeatedElementDamage(result.Snapshot, context);
            }

            return new ElementGaugeAccumulationResult(
                result.GaugeChanged,
                result.ThresholdReached,
                result.RepeatedElementDamage,
                result.Snapshot);
        }

        /// <summary>
        /// 게이지 규칙에 따라 데미지 파트에서 누적 기준 수치를 선택합니다.
        /// </summary>
        /// <param name="rule">현재 속성 게이지 규칙입니다.</param>
        /// <param name="finalDamageAmount">파트별 최종 HP 데미지입니다.</param>
        /// <param name="attackerElementDamage">공격자 속성 데미지 스탯에서 유래한 수치입니다.</param>
        /// <returns>게이지 변화에 사용할 기준 수치입니다.</returns>
        private static long ResolveGaugeSourceAmount(
            ElementGaugeRuleDefinition rule,
            long finalDamageAmount,
            long attackerElementDamage)
        {
            if (rule == null)
                return 0L;

            long safeFinalDamage = Math.Max(0L, finalDamageAmount);
            long safeAttackerElementDamage = Math.Max(0L, attackerElementDamage);

            return rule.fillSourcePolicy switch
            {
                ElementGaugeFillSourcePolicy.FinalDamage => safeFinalDamage,
                ElementGaugeFillSourcePolicy.AttackerElementDamage => safeAttackerElementDamage,
                ElementGaugeFillSourcePolicy.AttackerElementDamageOrFinalDamage => safeAttackerElementDamage > 0L ? safeAttackerElementDamage : safeFinalDamage,
                _ => safeAttackerElementDamage > 0L ? safeAttackerElementDamage : safeFinalDamage
            };
        }

        /// <summary>
        /// 공격자 캐릭터가 보유한 현재 기본 속성 데미지 값을 조회합니다.
        /// </summary>
        /// <param name="metadataDamage">데미지 메타데이터입니다.</param>
        /// <param name="damageType">조회할 속성 타입입니다.</param>
        /// <returns>공격자의 해당 속성 데미지 값입니다.</returns>
        private static long ResolveAttackerElementDamage(MetadataDamage metadataDamage, ConfigCommon.DamageType damageType)
        {
            if (metadataDamage == null || metadataDamage.attacker == null)
                return 0L;

            CharacterBase attacker = metadataDamage.attacker.GetComponentInParent<CharacterBase>();
            return attacker != null ? Math.Max(0L, attacker.GetElementDamageValue(damageType)) : 0L;
        }

        /// <summary>
        /// 원본 수치를 게이지 누적량으로 변환합니다.
        /// </summary>
        /// <param name="rule">현재 속성 게이지 규칙입니다.</param>
        /// <param name="sourceAmount">게이지 변환에 사용할 원본 수치입니다.</param>
        /// <returns>실제로 게이지에 누적할 값입니다.</returns>
        private static float CalculateGaugeAmount(ElementGaugeRuleDefinition rule, long sourceAmount)
        {
            if (rule == null || sourceAmount <= 0L)
                return 0f;

            float gaugeAmount = sourceAmount * Mathf.Max(0f, rule.gaugeFillPerElementDamage);
            if (rule.flatGaugeFillOnElementHit > 0f)
                gaugeAmount += rule.flatGaugeFillOnElementHit;

            if (rule.minGaugeFillPerHit > 0f)
                gaugeAmount = Mathf.Max(rule.minGaugeFillPerHit, gaugeAmount);

            if (rule.maxGaugeFillPerHit > 0f)
                gaugeAmount = Mathf.Min(rule.maxGaugeFillPerHit, gaugeAmount);

            return Mathf.Max(0f, gaugeAmount);
        }

        private void InitializeRules()
        {
            _rules.Clear();

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
            return AddressableLoaderSettings.Instance != null ? AddressableLoaderSettings.Instance.playerSettings : null;
        }

        private void RaiseGaugeChanged()
        {
            GaugeChanged?.Invoke();
        }
    }
}
