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
        private readonly IDamageFormula _basicPhysicalFormula = new BasicPhysicalDamageFormula();
        private readonly IDamageFormula _multiplierOnlyFormula = new MultiplierOnlyDamageFormula();
        private readonly DamageFormulaRegistry _polyFormulaRegistry = new DamageFormulaRegistry();
        private readonly DamageFormulaVariableBag _polyVariables = new DamageFormulaVariableBag();

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
            RebuildDamageFormulaRegistry();
        }

        /// <summary>
        /// 현재 로드된 damage_formula 테이블을 기준으로 Poly 공식 캐시를 다시 구성합니다.
        /// </summary>
        public void RebuildDamageFormulaRegistry()
        {
            _polyFormulaRegistry.Rebuild(TableLoaderManager.Instance != null ? TableLoaderManager.Instance.TableDamageFormula : null);
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
            var context = new DamageFormulaContext(
                attacker,
                null,
                0d,
                1f,
                1f,
                ConfigCommon.DamageType.Physic,
                true);

            return CalculateDamage(DamageFormulaType.BasicPhysical, context).FinalDamage;
        }

        /// <summary>
        /// 기본 콤보 공격 단계에 설정된 공식 정보를 기준으로 일반 공격 데미지를 계산합니다.
        /// </summary>
        /// <param name="attacker">공격자 스탯입니다.</param>
        /// <param name="target">피격 대상 캐릭터입니다. Poly 공식에서 대상 방어력과 레벨 변수에 사용됩니다.</param>
        /// <param name="settings">현재 콤보 단계의 데미지 공식 설정입니다.</param>
        /// <returns>크리티컬과 기본 데미지 보정이 반영된 일반 공격 데미지입니다.</returns>
        public long CalculateBasicAttackDamage(
            CharacterStat attacker,
            CharacterBase target,
            in AttackComboDamageFormulaSettings settings)
        {
            if (!settings.useCustomFormula)
                return CalculateBasicAttackDamage(attacker);

            CharacterBase attackerCharacter = attacker as CharacterBase;
            double baseDamage = settings.ResolveBaseDamage(attacker);
            double damageRate = settings.ResolveDamageRate();
            double eventMultiplier = settings.ResolveEventMultiplier();
            double optionMultiplier = settings.ResolveOptionMultiplier();
            ConfigCommon.DamageType damageType = settings.ResolveDamageType();

            if (!settings.HasFormulaKey())
            {
                return CalculateAttackComboMultiplierDamage(
                    attacker,
                    baseDamage,
                    damageRate,
                    eventMultiplier,
                    optionMultiplier,
                    damageType,
                    settings.rollCritical);
            }

            if (!_polyFormulaRegistry.TryGet(settings.formulaKey, out _))
            {
                RebuildDamageFormulaRegistry();
            }

            if (!_polyFormulaRegistry.TryGet(settings.formulaKey, out _))
            {
                return CalculateAttackComboMultiplierDamage(
                    attacker,
                    baseDamage,
                    damageRate,
                    eventMultiplier,
                    optionMultiplier,
                    damageType,
                    settings.rollCritical);
            }

            var request = new DamageFormulaRequest(
                attackerCharacter,
                target,
                settings.formulaKey,
                baseDamage,
                damageRate,
                eventMultiplier,
                optionMultiplier,
                settings.ResolveBuffRate(),
                damageType,
                settings.rollCritical);

            return CalculateSkillDamage(request);
        }

        /// <summary>
        /// 기본 콤보 공격의 커스텀 공식 키가 없거나 유효하지 않을 때 사용할 배율 기반 데미지를 계산합니다.
        /// </summary>
        /// <param name="attacker">공격자 스탯입니다.</param>
        /// <param name="baseDamage">기준 데미지입니다.</param>
        /// <param name="damageRate">기본 공격 데미지 배율입니다.</param>
        /// <param name="eventMultiplier">이벤트 단위 배율입니다.</param>
        /// <param name="optionMultiplier">실행 옵션 단위 배율입니다.</param>
        /// <param name="damageType">데미지 타입입니다.</param>
        /// <param name="rollCritical">크리티컬 판정 여부입니다.</param>
        /// <returns>크리티컬과 기본 데미지 보정이 반영된 배율 기반 데미지입니다.</returns>
        private long CalculateAttackComboMultiplierDamage(
            CharacterStat attacker,
            double baseDamage,
            double damageRate,
            double eventMultiplier,
            double optionMultiplier,
            ConfigCommon.DamageType damageType,
            bool rollCritical)
        {
            double resolved = System.Math.Max(0d, baseDamage) * damageRate * eventMultiplier * optionMultiplier;
            resolved = ApplyCriticalIfNeeded(resolved, attacker, rollCritical);
            long rounded = RoundToLong(resolved, "round", 0L);
            return ResolveDefaultFinalDamage(rounded, damageType).FinalDamage;
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
            var context = new DamageFormulaContext(
                null,
                null,
                baseDamage,
                eventMultiplier,
                optionMultiplier,
                ConfigCommon.DamageType.None,
                false);

            return CalculateDamage(DamageFormulaType.MultiplierOnly, context).FinalDamage;
        }

        /// <summary>
        /// 스킬/이벤트/버프/레벨 차이 값을 포함해 최종 공격 데미지를 계산합니다.
        /// </summary>
        /// <param name="request">스킬 데미지 계산 요청입니다.</param>
        /// <returns>저항 적용 전 공격 데미지입니다.</returns>
        public long CalculateSkillDamage(in DamageFormulaRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.FormulaKey) || !_polyFormulaRegistry.TryGet(request.FormulaKey, out DamageFormulaEntry formulaEntry))
            {
                RebuildDamageFormulaRegistry();
            }

            if (string.IsNullOrWhiteSpace(request.FormulaKey) || !_polyFormulaRegistry.TryGet(request.FormulaKey, out formulaEntry))
            {
                // Poly 공식이 없을 때도 DamageFormulaRequest의 의미를 유지합니다.
                // BaseDamage는 순수 기준값, SkillDamageRate는 skill/skill_monster 테이블의 Damage 비율입니다.
                float skillDamageRate = request.SkillDamageRate > 0d ? (float)request.SkillDamageRate : 1f;
                float eventMultiplier = request.EventMultiplier > 0d ? (float)request.EventMultiplier : 1f;
                return CalculateAttackDamage(request.BaseDamage, skillDamageRate * eventMultiplier, (float)request.OptionMultiplier);
            }

            BuildPolyVariables(request, _polyVariables);
            double resolved = formulaEntry.Formula.Evaluate(_polyVariables);
            resolved = ApplyCriticalIfNeeded(resolved, request.Attacker, request.RollCritical);
            long rounded = RoundToLong(resolved, formulaEntry.RoundingMode, formulaEntry.MinDamage);
            return ResolveDefaultFinalDamage(rounded, request.DamageType).FinalDamage;
        }

        /// <summary>
        /// 지정된 데미지 공식을 실행하고 0 이하 기본 데미지 정책을 적용합니다.
        /// </summary>
        /// <param name="formulaType">실행할 데미지 공식 타입입니다.</param>
        /// <param name="context">데미지 계산 입력값입니다.</param>
        /// <returns>기본 데미지 정책이 반영된 데미지 계산 결과입니다.</returns>
        public DamageCalculationResult CalculateDamage(DamageFormulaType formulaType, in DamageFormulaContext context)
        {
            IDamageFormula formula = ResolveFormula(formulaType);
            long calculatedDamage = formula != null ? formula.Calculate(context) : 0L;
            return ResolveDefaultFinalDamage(calculatedDamage, context.DamageType);
        }

        /// <summary>
        /// 공식 타입에 맞는 데미지 공식 인스턴스를 반환합니다.
        /// </summary>
        /// <param name="formulaType">조회할 공식 타입입니다.</param>
        /// <returns>데미지 공식 인스턴스입니다.</returns>
        private IDamageFormula ResolveFormula(DamageFormulaType formulaType)
        {
            switch (formulaType)
            {
                case DamageFormulaType.MultiplierOnly:
                    return _multiplierOnlyFormula;
                case DamageFormulaType.BasicPhysical:
                default:
                    return _basicPhysicalFormula;
            }
        }

        /// <summary>
        /// Poly 공식 계산에 사용할 기본 변수를 구성합니다.
        /// </summary>
        /// <param name="request">데미지 공식 요청입니다.</param>
        /// <param name="variables">변수를 채울 컨테이너입니다.</param>
        private void BuildPolyVariables(in DamageFormulaRequest request, DamageFormulaVariableBag variables)
        {
            variables.Clear();

            CharacterBase attacker = request.Attacker;
            CharacterBase target = request.Target;
            int attackerLevel = ResolveCharacterLevel(attacker);
            int targetLevel = ResolveCharacterLevel(target);
            int levelDiff = attackerLevel - targetLevel;
            double levelRate = ResolveLevelRate(levelDiff);

            variables.Set("BaseDamage", System.Math.Max(0d, request.BaseDamage));
            variables.Set("SkillDamageRate", request.SkillDamageRate > 0d ? request.SkillDamageRate : 1d);
            variables.Set("EventMultiplier", request.EventMultiplier > 0d ? request.EventMultiplier : 1d);
            variables.Set("OptionMultiplier", request.OptionMultiplier > 0d ? request.OptionMultiplier : 1d);
            variables.Set("BuffRate", request.BuffRate > 0d ? request.BuffRate : 0d);
            variables.Set("LevelRate", levelRate);
            variables.Set("AttackerLevel", attackerLevel);
            variables.Set("TargetLevel", targetLevel);
            variables.Set("LevelDiff", levelDiff);
            
            // 공격자/피격 대상이 명확히 드러나는 공식 작성용 변수입니다.
            variables.Set("AttackerBaseAtk", attacker != null ? attacker.TotalBaseAtk.Value : 0d);
            variables.Set("AttackerStatAtk", attacker != null ? attacker.TotalStatAtk.Value : 0d);
            variables.Set("AttackerTotalAtk", attacker != null ? attacker.TotalAtk.Value : 0d);
            variables.Set("AttackerBaseDef", attacker != null ? attacker.TotalBaseDef.Value : 0d);
            variables.Set("AttackerStatDef", attacker != null ? attacker.TotalStatDef.Value : 0d);
            variables.Set("AttackerTotalDef", attacker != null ? attacker.TotalDef.Value : 0d);
            variables.Set("TargetBaseAtk", target != null ? target.TotalBaseAtk.Value : 0d);
            variables.Set("TargetStatAtk", target != null ? target.TotalStatAtk.Value : 0d);
            variables.Set("TargetTotalAtk", target != null ? target.TotalAtk.Value : 0d);
            variables.Set("TargetBaseDef", target != null ? target.TotalBaseDef.Value : 0d);
            variables.Set("TargetStatDef", target != null ? target.TotalStatDef.Value : 0d);
            variables.Set("TargetTotalDef", target != null ? target.TotalDef.Value : 0d);

            variables.Set("CriticalRate", attacker != null ? attacker.TotalCriticalProbability.Value / 100d : 0d);
            variables.Set("CriticalDamageRate", attacker != null ? attacker.TotalCriticalDamage.Value / 100d : 1d);

            FillVariablesFromProviders(attacker, attacker, target, variables);
            FillVariablesFromProviders(target, attacker, target, variables);
        }

        /// <summary>
        /// 캐릭터에 부착된 공식 변수 제공자에게 추가 변수를 요청합니다.
        /// </summary>
        /// <param name="owner">변수 제공자를 검색할 캐릭터입니다.</param>
        /// <param name="attacker">공격자 캐릭터입니다.</param>
        /// <param name="target">피격 대상 캐릭터입니다.</param>
        /// <param name="variables">변수 컨테이너입니다.</param>
        private static void FillVariablesFromProviders(CharacterBase owner, CharacterBase attacker, CharacterBase target, DamageFormulaVariableBag variables)
        {
            if (owner == null)
                return;

            IDamageFormulaVariableProvider[] providers = owner.GetComponents<IDamageFormulaVariableProvider>();
            if (providers == null || providers.Length == 0)
                return;

            for (int i = 0; i < providers.Length; i++)
            {
                providers[i]?.FillDamageFormulaVariables(attacker, target, variables);
            }
        }

        /// <summary>
        /// 캐릭터 타입에 맞는 현재 레벨을 반환합니다.
        /// </summary>
        /// <param name="character">레벨을 확인할 캐릭터입니다.</param>
        /// <returns>계산에 사용할 레벨입니다.</returns>
        private static int ResolveCharacterLevel(CharacterBase character)
        {
            if (character == null)
                return 1;

            if (character is Player player)
                return System.Math.Max(1, player.CurrentLevel);

            if (character is Monster monster)
                return System.Math.Max(1, monster.CurrentLevel);

            return 1;
        }

        /// <summary>
        /// 레벨 차이에 따른 데미지 배율을 테이블에서 조회합니다.
        /// </summary>
        /// <param name="levelDiff">공격자 레벨 - 대상 레벨입니다.</param>
        /// <returns>레벨 차이 배율입니다.</returns>
        private static float ResolveLevelRate(int levelDiff)
        {
            TableDamageLevelMultiplier table = TableLoaderManager.Instance != null ? TableLoaderManager.Instance.TableDamageLevelMultiplier : null;
            return table != null ? table.ResolveMultiplier(levelDiff) : 1f;
        }

        /// <summary>
        /// 크리티컬 판정을 적용합니다.
        /// </summary>
        /// <param name="damage">크리티컬 적용 전 데미지입니다.</param>
        /// <param name="attacker">공격자 캐릭터입니다.</param>
        /// <param name="rollCritical">크리티컬 판정 여부입니다.</param>
        /// <returns>크리티컬이 반영된 데미지입니다.</returns>
        private static double ApplyCriticalIfNeeded(double damage, CharacterStat attacker, bool rollCritical)
        {
            if (!rollCritical || attacker == null || damage <= 0d)
                return damage;

            float criticalChance = Mathf.Clamp01(attacker.TotalCriticalProbability.Value / 100f);
            if (!(Random.value < criticalChance))
                return damage;

            float criticalMultiplier = Mathf.Max(1f, attacker.TotalCriticalDamage.Value / 100f);
            return damage * criticalMultiplier;
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
        /// 실수 계산 결과를 테이블 반올림 정책과 최소 데미지 정책에 따라 정수 데미지로 변환합니다.
        /// </summary>
        /// <param name="value">계산된 실수 값입니다.</param>
        /// <param name="roundingMode">damage_formula 테이블의 RoundingMode 값입니다.</param>
        /// <param name="minDamage">공식 결과가 0보다 클 때 보장할 최소 데미지입니다.</param>
        /// <returns>0 이상 long 범위로 보정된 데미지 값입니다.</returns>
        private static long RoundToLong(double value, string roundingMode, long minDamage)
        {
            if (value <= 0d)
                return 0L;
            if (value >= long.MaxValue)
                return long.MaxValue;

            double rounded;
            switch ((roundingMode ?? string.Empty).Trim().ToLowerInvariant())
            {
                case "floor":
                    rounded = System.Math.Floor(value);
                    break;
                case "ceil":
                case "ceiling":
                    rounded = System.Math.Ceiling(value);
                    break;
                case "truncate":
                case "trunc":
                    rounded = System.Math.Truncate(value);
                    break;
                case "round":
                default:
                    rounded = System.Math.Round(value);
                    break;
            }

            if (rounded <= 0d)
                return 0L;

            long damage = rounded >= long.MaxValue ? long.MaxValue : (long)rounded;
            return minDamage > 0L && damage < minDamage ? minDamage : damage;
        }
    }
}
