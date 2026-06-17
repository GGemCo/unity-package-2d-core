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
        /// 실제로 받은 속성 데미지를 게이지에 누적합니다.
        /// </summary>
        /// <param name="metadataDamage">데미지 메타데이터입니다.</param>
        /// <param name="damageAmount">게이지에 누적할 실제 속성 데미지량입니다.</param>
        /// <returns>누적 처리 결과입니다.</returns>
        public ElementGaugeAccumulationResult AccumulateFromDamage(MetadataDamage metadataDamage, long damageAmount)
        {
            if (!CanProcessRuntime() || metadataDamage == null)
                return ElementGaugeAccumulationResult.None;

            ConfigCommon.DamageType damageType = metadataDamage.damageType;
            if (damageType == ConfigCommon.DamageType.None || damageType == ConfigCommon.DamageType.Physic || damageAmount <= 0L)
                return ElementGaugeAccumulationResult.None;

            ElementGaugeApplyResult result = _gaugeProcessor.AccumulateDamage(_runtime, damageType, damageAmount, Time.time);
            if (!result.GaugeChanged && !result.ThresholdReached && !result.RepeatedElementDamage)
                return ElementGaugeAccumulationResult.None;

            var context = new ElementGaugeAccumulationContext(_owner, metadataDamage.attacker, metadataDamage, damageType, damageAmount);

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
