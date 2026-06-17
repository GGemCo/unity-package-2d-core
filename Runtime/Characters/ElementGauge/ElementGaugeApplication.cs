using System;
using UnityEngine;

namespace GGemCo2DCore
{
    /// <summary>
    /// 속성 데미지 누적 처리에 사용할 입력 컨텍스트입니다.
    /// </summary>
    /// <remarks>
    /// Core는 누적 상태와 이벤트만 관리하고, 임계 도달 후 실제 효과는 프로젝트별 핸들러가 이 컨텍스트를 해석하여 처리합니다.
    /// </remarks>
    public readonly struct ElementGaugeAccumulationContext
    {
        /// <summary>
        /// 속성 게이지를 소유한 캐릭터입니다.
        /// </summary>
        public CharacterBase Owner { get; }

        /// <summary>
        /// 데미지를 발생시킨 오브젝트입니다.
        /// </summary>
        public GameObject Source { get; }

        /// <summary>
        /// 원본 데미지 메타데이터입니다.
        /// </summary>
        public MetadataDamage MetadataDamage { get; }

        /// <summary>
        /// 누적 대상 속성 타입입니다.
        /// </summary>
        public ConfigCommon.DamageType DamageType { get; }

        /// <summary>
        /// 게이지 변환에 사용한 원본 수치입니다.
        /// </summary>
        public long DamageAmount { get; }

        /// <summary>
        /// 규칙 변환 후 실제 게이지에 누적한 값입니다.
        /// </summary>
        public float GaugeAmount { get; }

        /// <summary>
        /// 속성 게이지 누적 컨텍스트를 생성합니다.
        /// </summary>
        /// <param name="owner">속성 게이지를 소유한 캐릭터입니다.</param>
        /// <param name="source">데미지를 발생시킨 오브젝트입니다.</param>
        /// <param name="metadataDamage">원본 데미지 메타데이터입니다.</param>
        /// <param name="damageType">누적 대상 속성 타입입니다.</param>
        /// <param name="damageAmount">게이지 변환에 사용한 원본 수치입니다.</param>
        /// <param name="gaugeAmount">규칙 변환 후 실제 게이지에 누적한 값입니다.</param>
        public ElementGaugeAccumulationContext(
            CharacterBase owner,
            GameObject source,
            MetadataDamage metadataDamage,
            ConfigCommon.DamageType damageType,
            long damageAmount,
            float gaugeAmount = -1f)
        {
            Owner = owner;
            Source = source;
            MetadataDamage = metadataDamage;
            DamageType = damageType;
            DamageAmount = Math.Max(0L, damageAmount);
            GaugeAmount = gaugeAmount >= 0f ? gaugeAmount : DamageAmount;
        }
    }

    /// <summary>
    /// 속성 게이지 누적 처리 결과입니다.
    /// </summary>
    public readonly struct ElementGaugeAccumulationResult
    {
        /// <summary>
        /// 값이 변경되지 않은 기본 결과입니다.
        /// </summary>
        public static ElementGaugeAccumulationResult None => new(false, false, false, default);

        /// <summary>
        /// 누적 결과를 생성합니다.
        /// </summary>
        /// <param name="gaugeChanged">게이지 표시 값이 변경되었는지 여부입니다.</param>
        /// <param name="thresholdReached">이번 누적으로 임계점에 처음 도달했는지 여부입니다.</param>
        /// <param name="repeatedElementDamage">이미 임계 상태인 속성에 같은 속성 데미지가 다시 들어왔는지 여부입니다.</param>
        /// <param name="snapshot">처리 후 스냅샷입니다.</param>
        public ElementGaugeAccumulationResult(
            bool gaugeChanged,
            bool thresholdReached,
            bool repeatedElementDamage,
            ElementGaugeSnapshot snapshot)
        {
            GaugeChanged = gaugeChanged;
            ThresholdReached = thresholdReached;
            RepeatedElementDamage = repeatedElementDamage;
            Snapshot = snapshot;
        }

        /// <summary>
        /// 게이지 표시 값이 변경되었는지 여부입니다.
        /// </summary>
        public bool GaugeChanged { get; }

        /// <summary>
        /// 이번 누적으로 임계점에 처음 도달했는지 여부입니다.
        /// </summary>
        public bool ThresholdReached { get; }

        /// <summary>
        /// 이미 임계 상태인 속성에 같은 속성 데미지가 다시 들어왔는지 여부입니다.
        /// </summary>
        public bool RepeatedElementDamage { get; }

        /// <summary>
        /// 처리 후 스냅샷입니다.
        /// </summary>
        public ElementGaugeSnapshot Snapshot { get; }
    }

    /// <summary>
    /// HUD 등 외부 시스템에서 참조할 수 있는 속성 게이지 스냅샷입니다.
    /// </summary>
    public readonly struct ElementGaugeSnapshot
    {
        /// <summary>
        /// 속성 게이지 스냅샷을 생성합니다.
        /// </summary>
        /// <param name="damageType">속성 데미지 타입입니다.</param>
        /// <param name="currentValue">현재 누적 값입니다.</param>
        /// <param name="maxValue">최대 누적 값입니다.</param>
        /// <param name="isThresholdReached">임계 도달 상태인지 여부입니다.</param>
        public ElementGaugeSnapshot(ConfigCommon.DamageType damageType, float currentValue, float maxValue, bool isThresholdReached)
        {
            DamageType = damageType;
            CurrentValue = Mathf.Max(0f, currentValue);
            MaxValue = Mathf.Max(1f, maxValue);
            IsThresholdReached = isThresholdReached;
        }

        /// <summary>
        /// 속성 데미지 타입입니다.
        /// </summary>
        public ConfigCommon.DamageType DamageType { get; }

        /// <summary>
        /// 현재 누적 값입니다.
        /// </summary>
        public float CurrentValue { get; }

        /// <summary>
        /// 최대 누적 값입니다.
        /// </summary>
        public float MaxValue { get; }

        /// <summary>
        /// 임계 도달 상태인지 여부입니다.
        /// </summary>
        public bool IsThresholdReached { get; }

        /// <summary>
        /// 기존 UI의 차단 오버레이 바인딩과 호환하기 위한 별칭입니다.
        /// </summary>
        public bool IsBlockedByTriggeredState => IsThresholdReached;
    }

    /// <summary>
    /// 이전 직접 게이지 누적 API와의 컴파일 호환을 위한 구조체입니다.
    /// 신규 로직은 실제 속성 데미지를 통해 자동 누적되므로 직접 사용하지 않습니다.
    /// </summary>
    [Obsolete("속성 게이지는 실제 속성 데미지로 자동 누적됩니다. CharacterElementGaugeController.AccumulateFromDamage를 사용하세요.")]
    [Serializable]
    public struct ElementGaugeApplication
    {
        public ConfigCommon.DamageType damageType;
        public float gaugeValue;
        public bool requireDamageDealt;

        /// <summary>
        /// 이전 직접 게이지 누적 데이터를 생성합니다.
        /// </summary>
        /// <param name="damageType">속성 타입입니다.</param>
        /// <param name="gaugeValue">게이지 누적 값입니다.</param>
        /// <param name="requireDamageDealt">실제 데미지 필요 여부입니다.</param>
        public ElementGaugeApplication(ConfigCommon.DamageType damageType, float gaugeValue, bool requireDamageDealt = false)
        {
            this.damageType = damageType;
            this.gaugeValue = gaugeValue;
            this.requireDamageDealt = requireDamageDealt;
        }

        public bool IsValid => damageType != ConfigCommon.DamageType.None && damageType != ConfigCommon.DamageType.Physic && gaugeValue > 0f;
    }
}
