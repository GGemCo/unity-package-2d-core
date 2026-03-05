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
            private bool _hasTotalSuperArmor;
            private int _lastTotalSuperArmor;
            public ResourceStatModule(CharacterStat owner) => _owner = owner;

            public void Recalculate()
            {
                _owner._totalHp = StatCalculator.CalculateFinal(ConfigCommon.StatusStatHp, _owner.BaseHp, _owner._allProviders);
                _owner._totalHpTemp = StatCalculator.CalculateFinal(ConfigCommon.StatusStatHpTemp, 0, _owner._allProviders);
                _owner._totalMp = StatCalculator.CalculateFinal(ConfigCommon.StatusStatMp, _owner.BaseMp, _owner._allProviders);
                _owner._totalStamina = StatCalculator.CalculateFinal(ConfigCommon.StatusStatStamina, _owner.BaseStamina, _owner._allProviders);
                _owner._totalSuperArmor = (int)StatCalculator.CalculateFinal(ConfigCommon.StatusStatSuperArmor, _owner.BaseSuperArmor, _owner._allProviders);
            }

            public void Publish()
            {
                PublishIfChanged(_owner.TotalHp, ref _hasTotalHp, ref _lastTotalHp, _owner._totalHp);
                PublishIfChanged(_owner.TotalHpTemp, ref _hasTotalHpTemp, ref _lastTotalHpTemp, _owner._totalHpTemp);
                PublishIfChanged(_owner.TotalMp, ref _hasTotalMp, ref _lastTotalMp, _owner._totalMp);
                PublishIfChanged(_owner.TotalStamina, ref _hasTotalStamina, ref _lastTotalStamina, _owner._totalStamina);
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
            private bool _hasTotalCriticalDamage;
            private long _lastTotalCriticalDamage;
            private bool _hasTotalCriticalProbability;
            private long _lastTotalCriticalProbability;
            public CombatStatModule(CharacterStat owner) => _owner = owner;

            public void Recalculate()
            {
                _owner._totalAtk = StatCalculator.CalculateFinal(ConfigCommon.StatusStatAtk, _owner.BaseAtk, _owner._allProviders);
                _owner._totalDef = StatCalculator.CalculateFinal(ConfigCommon.StatusStatDef, _owner.BaseDef, _owner._allProviders);
                _owner._totalCriticalDamage = StatCalculator.CalculateFinal(ConfigCommon.StatusStatCriticalDamage, _owner.BaseCriticalDamage, _owner._allProviders);
                _owner._totalCriticalProbability = StatCalculator.CalculateFinal(ConfigCommon.StatusStatCriticalProbability, _owner.BaseCriticalProbability, _owner._allProviders);
            }

            public void Publish()
            {
                PublishIfChanged(_owner.TotalAtk, ref _hasTotalAtk, ref _lastTotalAtk, _owner._totalAtk);
                PublishIfChanged(_owner.TotalDef, ref _hasTotalDef, ref _lastTotalDef, _owner._totalDef);
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
                _owner._totalMoveSpeed = StatCalculator.CalculateFinal(ConfigCommon.StatusStatMoveSpeed, _owner.BaseMoveSpeed, _owner._allProviders);
                _owner._totalAttackSpeed = StatCalculator.CalculateFinal(ConfigCommon.StatusStatAttackSpeed, _owner.BaseAttackSpeed, _owner._allProviders);
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
                _owner._totalRegistFire = StatCalculator.CalculateFinal(ConfigCommon.StatusStatResistanceFire, _owner.BaseRegistFire, _owner._allProviders);
                _owner._totalRegistCold = StatCalculator.CalculateFinal(ConfigCommon.StatusStatResistanceCold, _owner.BaseRegistCold, _owner._allProviders);
                _owner._totalRegistLightning = StatCalculator.CalculateFinal(ConfigCommon.StatusStatResistanceLightning, _owner.BaseRegistLightning, _owner._allProviders);
                _owner._totalRegistPoison = StatCalculator.CalculateFinal(ConfigCommon.StatusStatResistancePoison, _owner.BaseRegistPoison, _owner._allProviders);
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