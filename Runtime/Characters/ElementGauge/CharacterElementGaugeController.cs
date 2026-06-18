using System;
using System.Collections.Generic;
using UnityEngine;

namespace GGemCo2DCore
{
    /// <summary>
    /// 캐릭터가 받은 속성 게이지 입력을 속성별로 누적하고, UI와 확장 핸들러에 상태 변화를 전달합니다.
    /// </summary>
    /// <remarks>
    /// 이 컨트롤러는 속성 게이지의 누적, 감쇠, 임계 도달 이벤트만 담당합니다.
    /// 임계 도달 후의 효과와 임계 상태에서 같은 속성 데미지를 다시 받았을 때의 처리는 프로젝트별 핸들러로 확장합니다.
    /// </remarks>
    [DisallowMultipleComponent]
    public sealed class CharacterElementGaugeController : MonoBehaviour
    {
        private readonly List<ElementGaugeRuleDefinition> _rules = new();
        private readonly List<ElementGaugeSnapshot> _snapshots = new();

        private CharacterBase _owner;
        private ElementGaugeRuntime _runtime;
        private ElementGaugeProcessor _gaugeProcessor;
        private ElementGaugeAccumulationMode _accumulationMode = ElementGaugeAccumulationMode.DamageDerived;
        private IElementGaugeThresholdHandler _thresholdHandler = NullElementGaugeThresholdHandler.Instance;
        private IElementGaugeRepeatedHitHandler _repeatedHitHandler = NullElementGaugeRepeatedHitHandler.Instance;
        private IElementGaugeAccumulationPolicy _accumulationPolicy = AllowAllElementGaugeAccumulationPolicy.Instance;

        /// <summary>
        /// 전역 기본 임계 도달 핸들러입니다.
        /// 프로젝트 부트스트랩에서 설정하면 이후 생성되는 컨트롤러의 기본 핸들러로 사용합니다.
        /// </summary>
        public static IElementGaugeThresholdHandler DefaultThresholdHandler { get; set; } = NullElementGaugeThresholdHandler.Instance;

        /// <summary>
        /// 전역 기본 임계 상태 반복 피격 핸들러입니다.
        /// 프로젝트 부트스트랩에서 설정하면 이후 생성되는 컨트롤러의 기본 핸들러로 사용합니다.
        /// </summary>
        public static IElementGaugeRepeatedHitHandler DefaultRepeatedHitHandler { get; set; } = NullElementGaugeRepeatedHitHandler.Instance;

        /// <summary>
        /// 전역 기본 속성 게이지 누적 정책입니다.
        /// 프로젝트 부트스트랩에서 설정하면 이후 생성되는 컨트롤러의 기본 정책으로 사용합니다.
        /// </summary>
        public static IElementGaugeAccumulationPolicy DefaultAccumulationPolicy { get; set; } = AllowAllElementGaugeAccumulationPolicy.Instance;

        /// <summary>
        /// 속성 게이지 표시 상태가 변경되었을 때 발생합니다.
        /// </summary>
        public event Action GaugeChanged;

        /// <summary>
        /// 속성 게이지가 임계값에 처음 도달했을 때 발생합니다.
        /// </summary>
        public event Action<ElementGaugeSnapshot, ElementGaugeAccumulationContext> ThresholdReached;

        /// <summary>
        /// 이미 임계 상태인 속성에 같은 속성 수치가 다시 들어왔을 때 임계 사이클당 한 번 발생합니다.
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
            _accumulationPolicy = DefaultAccumulationPolicy ?? AllowAllElementGaugeAccumulationPolicy.Instance;
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
        /// 임계 상태 반복 피격 핸들러를 교체합니다.
        /// </summary>
        /// <param name="handler">프로젝트별 반복 피격 핸들러입니다. null이면 기본 Null 핸들러를 사용합니다.</param>
        public void SetRepeatedHitHandler(IElementGaugeRepeatedHitHandler handler)
        {
            _repeatedHitHandler = handler ?? NullElementGaugeRepeatedHitHandler.Instance;
        }

        /// <summary>
        /// 속성 게이지 누적 가능 여부를 판정하는 정책을 교체합니다.
        /// </summary>
        /// <param name="policy">프로젝트별 누적 정책입니다. null이면 모든 누적을 허용하는 기본 정책을 사용합니다.</param>
        public void SetAccumulationPolicy(IElementGaugeAccumulationPolicy policy)
        {
            _accumulationPolicy = policy ?? AllowAllElementGaugeAccumulationPolicy.Instance;
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
        /// <remarks>
        /// <see cref="ElementGaugeAccumulationMode.ExplicitOnly"/>에서는 데미지와 게이지를 분리하기 위해 누적하지 않습니다.
        /// </remarks>
        public ElementGaugeAccumulationResult AccumulateFromDamage(MetadataDamage metadataDamage, long damageAmount)
        {
            if (!CanProcessRuntime() || metadataDamage == null)
                return ElementGaugeAccumulationResult.None;

            if (!CanAccumulateFromDamage())
                return ElementGaugeAccumulationResult.None;

            ConfigCommon.DamageType damageType = metadataDamage.damageType;
            if (!CanAccumulateDamageType(damageType) || !CanAccumulateByPolicy(damageType))
                return ElementGaugeAccumulationResult.None;

            if (!_runtime.TryGetRule(damageType, out ElementGaugeRuleDefinition rule) || rule == null)
                return ElementGaugeAccumulationResult.None;

            long sourceAmount = ResolveAttackerElementDamage(metadataDamage, damageType);
            if (sourceAmount <= 0L)
            {
                // 데미지 분해 결과가 없는 레거시 경로에서는 기존 단일 damageType 값을 기준으로 최종 피해량을 사용합니다.
                sourceAmount = Math.Max(0L, damageAmount);
            }

            return AccumulateResolvedAmount(metadataDamage, metadataDamage.attacker, damageType, sourceAmount, sourceAmount);
        }

        /// <summary>
        /// 속성별 데미지 분해 결과를 기준으로 속성 게이지를 누적합니다.
        /// </summary>
        /// <param name="metadataDamage">이번 피격을 설명하는 데미지 메타데이터입니다.</param>
        /// <param name="breakdown">속성별 데미지 분해 결과입니다.</param>
        /// <returns>마지막으로 변화가 발생한 속성 게이지 누적 결과입니다.</returns>
        /// <remarks>
        /// 분해 결과가 없으면 기존 단일 데미지 타입 기반 누적 로직으로 되돌아갑니다.
        /// 물리 파트는 게이지 대상이 아니므로 건너뛰고, 화염/냉기/번개/독 파트만 누적합니다.
        /// <see cref="ElementGaugeAccumulationMode.ExplicitOnly"/>에서는 일반 피해와 지속 피해를 모두 누적하지 않습니다.
        /// </remarks>
        public ElementGaugeAccumulationResult AccumulateFromDamageBreakdown(
            MetadataDamage metadataDamage,
            DamageCalculationBreakdown breakdown)
        {
            if (!CanProcessRuntime() || metadataDamage == null)
                return ElementGaugeAccumulationResult.None;

            if (!CanAccumulateFromDamage())
                return ElementGaugeAccumulationResult.None;

            if (breakdown == null || !breakdown.HasParts)
                return AccumulateFromDamage(metadataDamage, metadataDamage.damage);

            ElementGaugeAccumulationResult lastChangedResult = ElementGaugeAccumulationResult.None;
            IReadOnlyList<DamagePartResult> parts = breakdown.Parts;
            for (int i = 0; i < parts.Count; i++)
            {
                DamagePartResult part = parts[i];
                if (!CanAccumulateDamageType(part.DamageType) || !CanAccumulateByPolicy(part.DamageType))
                    continue;

                ElementGaugeAccumulationResult result = AccumulateFromDamagePart(metadataDamage, part);
                if (result.GaugeChanged || result.ThresholdReached || result.RepeatedElementDamage)
                    lastChangedResult = result;
            }

            return lastChangedResult;
        }

        /// <summary>
        /// 데미지 처리 없이 지정한 속성 게이지를 직접 누적합니다.
        /// </summary>
        /// <param name="damageType">누적할 속성 타입입니다.</param>
        /// <param name="gaugeAmount">속성 게이지에 직접 더할 수치입니다.</param>
        /// <param name="source">게이지를 발생시킨 원인 GameObject입니다.</param>
        /// <param name="metadataDamage">게이지 원인을 설명하는 데미지 메타데이터입니다. 데미지가 없는 누적이면 null을 허용합니다.</param>
        /// <returns>누적 처리 결과입니다.</returns>
        /// <remarks>
        /// Affect의 ElementGauge Modifier처럼 HP 피해와 분리된 게이지 전용 효과에서 사용합니다.
        /// 이 메서드는 <see cref="CharacterBase.TakeDamage"/>를 호출하지 않으므로 피격 반응, Hit VFX, 넉백 처리를 발생시키지 않습니다.
        /// </remarks>
        public ElementGaugeAccumulationResult AccumulateDirect(
            ConfigCommon.DamageType damageType,
            float gaugeAmount,
            GameObject source,
            MetadataDamage metadataDamage = null)
        {
            if (!CanProcessRuntime() || gaugeAmount <= 0f)
                return ElementGaugeAccumulationResult.None;

            if (!CanAccumulateDamageType(damageType) || !CanAccumulateByPolicy(damageType))
                return ElementGaugeAccumulationResult.None;

            if (!_runtime.TryGetRule(damageType, out ElementGaugeRuleDefinition rule) || rule == null)
                return ElementGaugeAccumulationResult.None;

            return AccumulateResolvedAmount(metadataDamage, source, damageType, 0L, gaugeAmount);
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

        /// <summary>
        /// 런타임 처리가 가능한 상태인지 확인합니다.
        /// </summary>
        /// <returns>소유 캐릭터가 살아 있고 게이지 런타임이 준비되었으면 <see langword="true"/>입니다.</returns>
        private bool CanProcessRuntime()
        {
            return _owner != null && !_owner.IsStatusDead() && _runtime != null && _gaugeProcessor != null;
        }

        /// <summary>
        /// 현재 설정에서 확정 데미지를 속성 게이지 입력으로 사용할 수 있는지 확인합니다.
        /// </summary>
        /// <returns>데미지 기반 누적을 허용하면 <see langword="true"/>입니다.</returns>
        /// <remarks>
        /// 명시적 누적 모드에서는 일반 속성 피해와 지속 속성 피해를 모두 게이지 입력에서 제외합니다.
        /// 게이지는 Affect의 ElementGauge Modifier 등에서 <see cref="AccumulateDirect"/>를 호출할 때만 누적됩니다.
        /// </remarks>
        private bool CanAccumulateFromDamage()
        {
            return _accumulationMode == ElementGaugeAccumulationMode.DamageDerived;
        }

        /// <summary>
        /// 속성 게이지 누적 대상 데미지 타입인지 확인합니다.
        /// </summary>
        /// <param name="damageType">검사할 데미지 타입입니다.</param>
        /// <returns>물리와 None이 아닌 속성이면 <see langword="true"/>입니다.</returns>
        private static bool CanAccumulateDamageType(ConfigCommon.DamageType damageType)
        {
            return damageType != ConfigCommon.DamageType.None && damageType != ConfigCommon.DamageType.Physic;
        }

        /// <summary>
        /// 외부 정책을 통해 현재 속성 게이지 누적이 허용되는지 확인합니다.
        /// </summary>
        /// <param name="damageType">누적하려는 속성 타입입니다.</param>
        /// <returns>정책이 없거나 정책이 허용하면 <see langword="true"/>입니다.</returns>
        private bool CanAccumulateByPolicy(ConfigCommon.DamageType damageType)
        {
            return _accumulationPolicy == null || _accumulationPolicy.CanAccumulateElementGauge(_owner, damageType);
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

            long sourceAmount = Math.Max(0L, part.AttackerElementDamage);
            if (sourceAmount <= 0L)
            {
                // 분해 결과가 공격자 속성 데미지를 제공하지 않는 경우에만 파트 최종 피해량을 보조 기준으로 사용합니다.
                sourceAmount = Math.Max(0L, part.FinalDamage);
            }

            return AccumulateResolvedAmount(metadataDamage, metadataDamage.attacker, part.DamageType, sourceAmount, sourceAmount);
        }

        /// <summary>
        /// 이미 확정된 누적량을 런타임 게이지에 반영하고 관련 이벤트를 발행합니다.
        /// </summary>
        /// <param name="metadataDamage">게이지 원인을 설명하는 데미지 메타데이터입니다.</param>
        /// <param name="source">게이지를 발생시킨 원인 GameObject입니다.</param>
        /// <param name="damageType">누적 대상 속성 타입입니다.</param>
        /// <param name="sourceAmount">컨텍스트에 기록할 원본 수치입니다.</param>
        /// <param name="gaugeAmount">실제 게이지에 더할 수치입니다.</param>
        /// <returns>속성 게이지 누적 결과입니다.</returns>
        private ElementGaugeAccumulationResult AccumulateResolvedAmount(
            MetadataDamage metadataDamage,
            GameObject source,
            ConfigCommon.DamageType damageType,
            long sourceAmount,
            float gaugeAmount)
        {
            if (sourceAmount <= 0L && gaugeAmount <= 0f)
                return ElementGaugeAccumulationResult.None;

            ElementGaugeApplyResult result = _gaugeProcessor.AccumulateDamage(_runtime, damageType, gaugeAmount, Time.time);
            if (!result.GaugeChanged && !result.ThresholdReached && !result.RepeatedElementDamage)
                return ElementGaugeAccumulationResult.None;

            var context = new ElementGaugeAccumulationContext(
                _owner,
                source,
                metadataDamage,
                damageType,
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
        /// 플레이어 설정에서 게이지 규칙을 읽어 런타임용 목록을 초기화합니다.
        /// </summary>
        private void InitializeRules()
        {
            _rules.Clear();

            GGemCoPlayerSettings settings = ResolvePlayerSettings();
            _accumulationMode = settings != null
                ? settings.elementGaugeAccumulationMode
                : ElementGaugeAccumulationMode.DamageDerived;

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

        /// <summary>
        /// Addressables 설정 로더에서 플레이어 설정을 조회합니다.
        /// </summary>
        /// <returns>로드된 플레이어 설정입니다. 설정이 없으면 null입니다.</returns>
        private GGemCoPlayerSettings ResolvePlayerSettings()
        {
            return AddressableLoaderSettings.Instance != null ? AddressableLoaderSettings.Instance.playerSettings : null;
        }

        /// <summary>
        /// 게이지 변경 이벤트를 발생시킵니다.
        /// </summary>
        private void RaiseGaugeChanged()
        {
            GaugeChanged?.Invoke();
        }
    }
}
