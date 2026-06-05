using UnityEngine;

namespace GGemCo2DCore
{
    /// <summary>
    /// 전투와 관련된 최종 수치 계산을 담당하는 매니저입니다.
    /// </summary>
    public class CalculateManager : MonoBehaviour
    {
        /// <summary>현재 씬에서 사용 중인 계산 매니저입니다.</summary>
        public static CalculateManager Instance { get; private set; }

        private GGemCoSettings _settings;

        /// <summary>
        /// 계산 매니저 인스턴스를 등록합니다.
        /// </summary>
        private void Awake()
        {
            Instance = this;
        }

        /// <summary>
        /// 계산 매니저 인스턴스 등록을 해제합니다.
        /// </summary>
        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        /// <summary>
        /// 전역 계산 정책을 초기화합니다.
        /// </summary>
        /// <param name="settings">GGemCo 메인 설정입니다.</param>
        public void Initialize(GGemCoSettings settings)
        {
            _settings = settings;
        }

        /// <summary>
        /// 현재 사용 가능한 계산 매니저를 반환합니다.
        /// </summary>
        /// <returns>현재 씬에 등록된 계산 매니저입니다. 없으면 <see langword="null"/>입니다.</returns>
        public static CalculateManager GetActive()
        {
            if (Instance != null)
                return Instance;

            return SceneGame.Instance != null ? SceneGame.Instance.calculateManager : null;
        }

        /// <summary>
        /// 캐릭터의 일반 공격 최종 데미지를 계산합니다.
        /// </summary>
        /// <param name="attacker">공격자 캐릭터입니다.</param>
        /// <returns>크리티컬과 기본 데미지 보정이 반영된 일반 공격 데미지입니다.</returns>
        public long CalculateBasicAttackDamage(CharacterStat attacker)
        {
            if (attacker == null)
                return ResolveDefaultFinalDamage(0L).FinalDamage;

            long baseAttack = attacker.TotalAtk.Value;
            double finalDamage = baseAttack;
            if (baseAttack > 0L)
            {
                float criticalChance = Mathf.Clamp01(attacker.TotalCriticalProbability.Value / 100f);
                if (Random.value < criticalChance)
                {
                    float criticalMultiplier = Mathf.Max(1f, attacker.TotalCriticalDamage.Value / 100f);
                    finalDamage *= criticalMultiplier;
                }
            }

            return ResolveDefaultFinalDamage(RoundToLong(finalDamage)).FinalDamage;
        }

        /// <summary>
        /// 기본 데미지에 이벤트 배율과 실행 옵션 배율을 적용해 공격 데미지를 계산합니다.
        /// </summary>
        /// <param name="baseDamage">배율 적용 전 기본 데미지입니다.</param>
        /// <param name="eventMultiplier">이벤트 단위 데미지 배율입니다.</param>
        /// <param name="optionMultiplier">실행 옵션에서 전달된 데미지 배율입니다.</param>
        /// <returns>배율과 기본 데미지 보정이 반영된 공격 데미지입니다.</returns>
        public long CalculateAttackDamage(long baseDamage, float eventMultiplier = 1f, float optionMultiplier = 1f)
        {
            return CalculateAttackDamage((double)baseDamage, eventMultiplier, optionMultiplier);
        }

        /// <summary>
        /// 기본 데미지에 이벤트 배율과 실행 옵션 배율을 적용해 공격 데미지를 계산합니다.
        /// </summary>
        /// <param name="baseDamage">배율 적용 전 기본 데미지입니다.</param>
        /// <param name="eventMultiplier">이벤트 단위 데미지 배율입니다.</param>
        /// <param name="optionMultiplier">실행 옵션에서 전달된 데미지 배율입니다.</param>
        /// <returns>배율과 기본 데미지 보정이 반영된 공격 데미지입니다.</returns>
        public long CalculateAttackDamage(double baseDamage, float eventMultiplier = 1f, float optionMultiplier = 1f)
        {
            float safeEventMultiplier = Mathf.Max(0f, eventMultiplier);
            float safeOptionMultiplier = optionMultiplier > 0f ? optionMultiplier : 1f;
            double resolved = System.Math.Max(0d, baseDamage) * safeEventMultiplier * safeOptionMultiplier;
            return ResolveDefaultFinalDamage(RoundToLong(resolved)).FinalDamage;
        }

        /// <summary>
        /// 피격 대상의 속성 저항과 기본 데미지 정책을 적용한 최종 피격 데미지를 계산합니다.
        /// </summary>
        /// <param name="damage">저항 적용 전 데미지입니다.</param>
        /// <param name="damageType">데미지 타입입니다.</param>
        /// <param name="target">피격 대상 캐릭터입니다.</param>
        /// <returns>속성 저항과 기본 데미지 보정 결과입니다.</returns>
        public DamageCalculationResult CalculateIncomingDamage(
            long damage,
            ConfigCommon.DamageType damageType,
            CharacterBase target)
        {
            long originalDamage = damage;
            long resolvedDamage = ApplyDamageTypeResistance(damage, damageType, target);
            DamageCalculationResult defaultResolved = ResolveDefaultFinalDamage(resolvedDamage, damageType);

            return new DamageCalculationResult(
                originalDamage,
                defaultResolved.FinalDamage,
                resolvedDamage <= 0L,
                defaultResolved.AppliedDefaultDamage,
                defaultResolved.IsImmune,
                damageType);
        }

        /// <summary>
        /// 데미지가 0 이하일 때 GGemCoSettings의 기본 데미지를 적용합니다.
        /// </summary>
        /// <param name="damage">보정 전 데미지입니다.</param>
        /// <param name="damageType">데미지 타입입니다.</param>
        /// <returns>기본 데미지 보정 결과입니다.</returns>
        public DamageCalculationResult ResolveDefaultFinalDamage(
            long damage,
            ConfigCommon.DamageType damageType = ConfigCommon.DamageType.None)
        {
            if (damage > 0L)
            {
                return new DamageCalculationResult(
                    damage,
                    damage,
                    false,
                    false,
                    false,
                    damageType);
            }

            long defaultDamage = GetDefaultFinalDamageWhenZeroOrLess();
            bool appliedDefaultDamage = defaultDamage > 0L;
            return new DamageCalculationResult(
                damage,
                appliedDefaultDamage ? defaultDamage : 0L,
                true,
                appliedDefaultDamage,
                !appliedDefaultDamage,
                damageType);
        }

        /// <summary>
        /// 데미지 타입별 저항을 적용합니다.
        /// </summary>
        /// <param name="damage">저항 적용 전 데미지입니다.</param>
        /// <param name="damageType">데미지 타입입니다.</param>
        /// <param name="target">피격 대상 캐릭터입니다.</param>
        /// <returns>저항이 반영된 데미지입니다.</returns>
        private static long ApplyDamageTypeResistance(long damage, ConfigCommon.DamageType damageType, CharacterBase target)
        {
            if (damage <= 0L || target == null || damageType == ConfigCommon.DamageType.None)
                return damage;

            float resistance = 0f;
            switch (damageType)
            {
                case ConfigCommon.DamageType.Fire:
                    resistance = target.TotalRegistFire.Value;
                    break;
                case ConfigCommon.DamageType.Cold:
                    resistance = target.TotalRegistCold.Value;
                    break;
                case ConfigCommon.DamageType.Lightning:
                    resistance = target.TotalRegistLightning.Value;
                    break;
                case ConfigCommon.DamageType.Poison:
                    resistance = target.TotalRegistPoison.Value;
                    break;
                default:
                    return damage;
            }

            double multiplier = (100d - resistance) / 100d;
            double resolved = damage * multiplier;
            if (resolved <= 0d)
                return 0L;
            if (resolved >= long.MaxValue)
                return long.MaxValue;

            return (long)resolved;
        }

        /// <summary>
        /// 설정에 등록된 0 이하 최종 데미지 보정값을 반환합니다.
        /// </summary>
        /// <returns>0 이상으로 보정된 기본 데미지입니다.</returns>
        private long GetDefaultFinalDamageWhenZeroOrLess()
        {
            if (_settings == null && AddressableLoaderSettings.Instance != null)
            {
                _settings = AddressableLoaderSettings.Instance.settings;
            }

            return _settings != null ? Mathf.Max(0, _settings.defaultFinalDamageWhenZeroOrLess) : 0L;
        }

        /// <summary>
        /// 실수 계산 결과를 데미지 정수 값으로 안전하게 변환합니다.
        /// </summary>
        /// <param name="value">계산된 실수 값입니다.</param>
        /// <returns>0 이상 long 범위로 보정된 데미지 값입니다.</returns>
        private static long RoundToLong(double value)
        {
            if (value <= 0d)
                return 0L;
            if (value >= long.MaxValue)
                return long.MaxValue;

            return (long)System.Math.Round(value);
        }
    }
}
