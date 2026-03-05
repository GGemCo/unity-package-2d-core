namespace GGemCo2DCore
{
    public partial class CharacterStat
    {
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
                _owner.TotalHp.OnNext(_owner._totalHp);
                _owner.TotalHpTemp.OnNext(_owner._totalHpTemp);
                _owner.TotalMp.OnNext(_owner._totalMp);
                _owner.TotalStamina.OnNext(_owner._totalStamina);
                _owner.TotalSuperArmor.OnNext(_owner._totalSuperArmor);
            }
        }

        private sealed class CombatStatModule : ICharacterStatModule
        {
            private readonly CharacterStat _owner;

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
                _owner.TotalAtk.OnNext(_owner._totalAtk);
                _owner.TotalDef.OnNext(_owner._totalDef);
                _owner.TotalCriticalDamage.OnNext(_owner._totalCriticalDamage);
                _owner.TotalCriticalProbability.OnNext(_owner._totalCriticalProbability);
            }
        }

        private sealed class MovementStatModule : ICharacterStatModule
        {
            private readonly CharacterStat _owner;

            public MovementStatModule(CharacterStat owner) => _owner = owner;

            public void Recalculate()
            {
                _owner._totalMoveSpeed = StatCalculator.CalculateFinal(ConfigCommon.StatusStatMoveSpeed, _owner.BaseMoveSpeed, _owner._allProviders);
                _owner._totalAttackSpeed = StatCalculator.CalculateFinal(ConfigCommon.StatusStatAttackSpeed, _owner.BaseAttackSpeed, _owner._allProviders);
            }

            public void Publish()
            {
                _owner.TotalMoveSpeed.OnNext(_owner._totalMoveSpeed);
                _owner.TotalAttackSpeed.OnNext(_owner._totalAttackSpeed);
            }
        }

        private sealed class ResistanceStatModule : ICharacterStatModule
        {
            private readonly CharacterStat _owner;

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
                _owner.TotalRegistFire.OnNext(_owner._totalRegistFire);
                _owner.TotalRegistCold.OnNext(_owner._totalRegistCold);
                _owner.TotalRegistLightning.OnNext(_owner._totalRegistLightning);
                _owner.TotalRegistPoison.OnNext(_owner._totalRegistPoison);
            }
        }
    }
}
