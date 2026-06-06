using System.Collections.Generic;
using R3;

namespace GGemCo2DCore
{
    public partial class CharacterStat
    {
        private static void PublishIfChanged<T>(BehaviorSubject<T> subject, ref bool hasLast, ref T lastValue, T currentValue)
        {
            if (!hasLast || !EqualityComparer<T>.Default.Equals(lastValue, currentValue))
            {
                subject.OnNext(currentValue);
                lastValue = currentValue;
                hasLast = true;
            }
        }

        /// <summary>
        /// CharacterStat 내부에서만 사용하는 “계산/발행” 모듈 인터페이스입니다.
        /// - Recalculate: Provider/베이스 값을 바탕으로 내부 캐시(_totalX)를 갱신
        /// - Publish: 갱신된 캐시를 BehaviorSubject로 발행
        /// </summary>
        private interface ICharacterStatModule
        {
            void Recalculate();
            void Publish();
        }

        private sealed class ResourceStatModule : ICharacterStatModule
        {
            private readonly CharacterStat _owner;

            private bool _hasTotalHp;
            private long _lastTotalHp;
            private bool _hasTotalHpTemp;
            private long _lastTotalHpTemp;
            private bool _hasTotalMp;
            private long _lastTotalMp;
            private bool _hasTotalStamina;
            private long _lastTotalStamina;
            private bool _hasTotalBaseHp;
            private long _lastTotalBaseHp;
            private bool _hasTotalBaseMp;
            private long _lastTotalBaseMp;
            private bool _hasTotalBaseStamina;
            private long _lastTotalBaseStamina;
            private bool _hasTotalStatHp;
            private long _lastTotalStatHp;
            private bool _hasTotalStatMp;
            private long _lastTotalStatMp;
            private bool _hasTotalStatStamina;
            private long _lastTotalStatStamina;
            private bool _hasTotalSuperArmor;
            private int _lastTotalSuperArmor;
            public ResourceStatModule(CharacterStat owner) => _owner = owner;

            public void Recalculate()
            {
                _owner._totalBaseHp = StatCalculator.CalculateFinal(ConfigCommon.BaseStatHp, _owner.BaseHp, _owner._allProviders);
                _owner._totalBaseMp = StatCalculator.CalculateFinal(ConfigCommon.BaseStatMp, _owner.BaseMp, _owner._allProviders);
                _owner._totalBaseStamina = StatCalculator.CalculateFinal(ConfigCommon.BaseStatStamina, _owner.BaseStamina, _owner._allProviders);
                _owner._totalStatHp = StatCalculator.CalculateFinal(ConfigCommon.StatusStatHp, _owner.BaseStatHp, _owner._allProviders);
                _owner._totalStatMp = StatCalculator.CalculateFinal(ConfigCommon.StatusStatMp, _owner.BaseStatMp, _owner._allProviders);
                _owner._totalStatStamina = StatCalculator.CalculateFinal(ConfigCommon.StatusStatStamina, _owner.BaseStatStamina, _owner._allProviders);
                _owner._totalHp = _owner._totalBaseHp + _owner._totalStatHp;
                _owner._totalHpTemp = StatCalculator.CalculateFinal(ConfigCommon.StatusStatHpTemp, 0, _owner._allProviders);
                _owner._totalMp = _owner._totalBaseMp + _owner._totalStatMp;
                _owner._totalStamina = _owner._totalBaseStamina + _owner._totalStatStamina;
                _owner._totalSuperArmor = (int)StatCalculator.CalculateFinal(ConfigCommon.BaseStatSuperArmor, _owner.BaseSuperArmor, _owner._allProviders);
            }

            public void Publish()
            {
                PublishIfChanged(_owner.TotalHp, ref _hasTotalHp, ref _lastTotalHp, _owner._totalHp);
                PublishIfChanged(_owner.TotalHpTemp, ref _hasTotalHpTemp, ref _lastTotalHpTemp, _owner._totalHpTemp);
                PublishIfChanged(_owner.TotalMp, ref _hasTotalMp, ref _lastTotalMp, _owner._totalMp);
                PublishIfChanged(_owner.TotalStamina, ref _hasTotalStamina, ref _lastTotalStamina, _owner._totalStamina);
                PublishIfChanged(_owner.TotalBaseHp, ref _hasTotalBaseHp, ref _lastTotalBaseHp, _owner._totalBaseHp);
                PublishIfChanged(_owner.TotalBaseMp, ref _hasTotalBaseMp, ref _lastTotalBaseMp, _owner._totalBaseMp);
                PublishIfChanged(_owner.TotalBaseStamina, ref _hasTotalBaseStamina, ref _lastTotalBaseStamina, _owner._totalBaseStamina);
                PublishIfChanged(_owner.TotalStatHp, ref _hasTotalStatHp, ref _lastTotalStatHp, _owner._totalStatHp);
                PublishIfChanged(_owner.TotalStatMp, ref _hasTotalStatMp, ref _lastTotalStatMp, _owner._totalStatMp);
                PublishIfChanged(_owner.TotalStatStamina, ref _hasTotalStatStamina, ref _lastTotalStatStamina, _owner._totalStatStamina);
                PublishIfChanged(_owner.TotalSuperArmor, ref _hasTotalSuperArmor, ref _lastTotalSuperArmor, _owner._totalSuperArmor);
            }
        }

        private sealed class CombatStatModule : ICharacterStatModule
        {
            private readonly CharacterStat _owner;

            private bool _hasTotalAtk;
            private long _lastTotalAtk;
            private bool _hasTotalDef;
            private long _lastTotalDef;
            private bool _hasTotalBaseAtk;
            private long _lastTotalBaseAtk;
            private bool _hasTotalBaseDef;
            private long _lastTotalBaseDef;
            private bool _hasTotalStatAtk;
            private long _lastTotalStatAtk;
            private bool _hasTotalStatDef;
            private long _lastTotalStatDef;
            private bool _hasTotalCriticalDamage;
            private long _lastTotalCriticalDamage;
            private bool _hasTotalCriticalProbability;
            private long _lastTotalCriticalProbability;
            public CombatStatModule(CharacterStat owner) => _owner = owner;

            public void Recalculate()
            {
                _owner._totalBaseAtk = StatCalculator.CalculateFinal(ConfigCommon.BaseStatAtk, _owner.BaseAtk, _owner._allProviders);
                _owner._totalBaseDef = StatCalculator.CalculateFinal(ConfigCommon.BaseStatDef, _owner.BaseDef, _owner._allProviders);
                _owner._totalStatAtk = StatCalculator.CalculateFinal(ConfigCommon.StatusStatAtk, _owner.BaseStatAtk, _owner._allProviders);
                _owner._totalStatDef = StatCalculator.CalculateFinal(ConfigCommon.StatusStatDef, _owner.BaseStatDef, _owner._allProviders);
                _owner._totalAtk = _owner._totalBaseAtk + _owner._totalStatAtk;
                _owner._totalDef = _owner._totalBaseDef + _owner._totalStatDef;
                _owner._totalCriticalDamage = StatCalculator.CalculateFinal(ConfigCommon.BaseStatCriticalDamage, _owner.BaseCriticalDamage, _owner._allProviders);
                _owner._totalCriticalProbability = StatCalculator.CalculateFinal(ConfigCommon.BaseStatCriticalProbability, _owner.BaseCriticalProbability, _owner._allProviders);
            }

            public void Publish()
            {
                PublishIfChanged(_owner.TotalAtk, ref _hasTotalAtk, ref _lastTotalAtk, _owner._totalAtk);
                PublishIfChanged(_owner.TotalDef, ref _hasTotalDef, ref _lastTotalDef, _owner._totalDef);
                PublishIfChanged(_owner.TotalBaseAtk, ref _hasTotalBaseAtk, ref _lastTotalBaseAtk, _owner._totalBaseAtk);
                PublishIfChanged(_owner.TotalBaseDef, ref _hasTotalBaseDef, ref _lastTotalBaseDef, _owner._totalBaseDef);
                PublishIfChanged(_owner.TotalStatAtk, ref _hasTotalStatAtk, ref _lastTotalStatAtk, _owner._totalStatAtk);
                PublishIfChanged(_owner.TotalStatDef, ref _hasTotalStatDef, ref _lastTotalStatDef, _owner._totalStatDef);
                PublishIfChanged(_owner.TotalCriticalDamage, ref _hasTotalCriticalDamage, ref _lastTotalCriticalDamage, _owner._totalCriticalDamage);
                PublishIfChanged(_owner.TotalCriticalProbability, ref _hasTotalCriticalProbability, ref _lastTotalCriticalProbability, _owner._totalCriticalProbability);
            }
        }

        private sealed class MovementStatModule : ICharacterStatModule
        {
            private readonly CharacterStat _owner;
            
            private bool _hasTotalMoveSpeed;
            private long _lastTotalMoveSpeed;
            private bool _hasTotalAttackSpeed;
            private long _lastTotalAttackSpeed;
            public MovementStatModule(CharacterStat owner) => _owner = owner;

            public void Recalculate()
            {
                _owner._totalMoveSpeed = StatCalculator.CalculateFinal(ConfigCommon.BaseStatMoveSpeed, _owner.BaseMoveSpeed, _owner._allProviders);
                _owner._totalAttackSpeed = StatCalculator.CalculateFinal(ConfigCommon.BaseStatAttackSpeed, _owner.BaseAttackSpeed, _owner._allProviders);
            }

            public void Publish()
            {
                PublishIfChanged(_owner.TotalMoveSpeed, ref _hasTotalMoveSpeed, ref _lastTotalMoveSpeed, _owner._totalMoveSpeed);
                PublishIfChanged(_owner.TotalAttackSpeed, ref _hasTotalAttackSpeed, ref _lastTotalAttackSpeed, _owner._totalAttackSpeed);
            }
        }

        private sealed class ResistanceStatModule : ICharacterStatModule
        {
            private readonly CharacterStat _owner;
            
            private bool _hasTotalRegistFire;
            private long _lastTotalRegistFire;
            private bool _hasTotalRegistCold;
            private long _lastTotalRegistCold;
            private bool _hasTotalRegistLightning;
            private long _lastTotalRegistLightning;
            private bool _hasTotalRegistPoison;
            private long _lastTotalRegistPoison;
            public ResistanceStatModule(CharacterStat owner) => _owner = owner;

            public void Recalculate()
            {
                _owner._totalRegistFire = StatCalculator.CalculateFinal(ConfigCommon.BaseStatResistanceFire, _owner.BaseRegistFire, _owner._allProviders);
                _owner._totalRegistCold = StatCalculator.CalculateFinal(ConfigCommon.BaseStatResistanceCold, _owner.BaseRegistCold, _owner._allProviders);
                _owner._totalRegistLightning = StatCalculator.CalculateFinal(ConfigCommon.BaseStatResistanceLightning, _owner.BaseRegistLightning, _owner._allProviders);
                _owner._totalRegistPoison = StatCalculator.CalculateFinal(ConfigCommon.BaseStatResistancePoison, _owner.BaseRegistPoison, _owner._allProviders);
            }

            public void Publish()
            {
                PublishIfChanged(_owner.TotalRegistFire, ref _hasTotalRegistFire, ref _lastTotalRegistFire, _owner._totalRegistFire);
                PublishIfChanged(_owner.TotalRegistCold, ref _hasTotalRegistCold, ref _lastTotalRegistCold, _owner._totalRegistCold);
                PublishIfChanged(_owner.TotalRegistLightning, ref _hasTotalRegistLightning, ref _lastTotalRegistLightning, _owner._totalRegistLightning);
                PublishIfChanged(_owner.TotalRegistPoison, ref _hasTotalRegistPoison, ref _lastTotalRegistPoison, _owner._totalRegistPoison);
            }
        }
    }
}