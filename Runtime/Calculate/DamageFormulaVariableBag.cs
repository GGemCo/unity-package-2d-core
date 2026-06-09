using System;
using System.Collections.Generic;

namespace GGemCo2DCore
{
    /// <summary>
    /// Poly 데미지 공식에서 사용할 변수 값을 보관하는 컨테이너입니다.
    /// </summary>
    public sealed class DamageFormulaVariableBag
    {
        private readonly Dictionary<string, double> _values = new(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// 변수 값을 등록하거나 갱신합니다.
        /// </summary>
        /// <param name="key">공식에서 사용할 변수 이름입니다.</param>
        /// <param name="value">변수에 연결할 숫자 값입니다.</param>
        /// <remarks>
        /// 기본 스탯, 스킬 계수처럼 계산 컨텍스트에서 하나의 값으로 고정해야 하는 변수에 사용합니다.
        /// Affect/Passive처럼 여러 제공자가 같은 변수 ID를 제공할 수 있는 값은 <see cref="Add"/>를 사용해 누적합니다.
        /// </remarks>
        public void Set(string key, double value)
        {
            if (string.IsNullOrWhiteSpace(key))
                return;

            _values[key.Trim()] = Sanitize(value);
        }

        /// <summary>
        /// 변수 값을 기존 값에 더해 누적합니다.
        /// </summary>
        /// <param name="key">공식에서 사용할 변수 이름입니다.</param>
        /// <param name="value">기존 값에 더할 숫자 값입니다.</param>
        /// <remarks>
        /// Affect와 Passive Skill처럼 서로 다른 패키지의 <see cref="IDamageFormulaVariableProvider"/>가 같은 변수 ID를 제공할 때 사용합니다.
        /// 동일 키가 이미 존재하면 값을 합산하고, 없으면 새 변수로 등록합니다.
        /// </remarks>
        public void Add(string key, double value)
        {
            if (string.IsNullOrWhiteSpace(key))
                return;

            string normalizedKey = key.Trim();
            double safeValue = Sanitize(value);

            if (_values.TryGetValue(normalizedKey, out double currentValue))
            {
                _values[normalizedKey] = Sanitize(currentValue + safeValue);
                return;
            }

            _values.Add(normalizedKey, safeValue);
        }

        /// <summary>
        /// 공식 계산을 방해하지 않도록 NaN/Infinity 값을 0으로 보정합니다.
        /// </summary>
        /// <param name="value">검증할 값입니다.</param>
        /// <returns>공식에 안전하게 전달할 값입니다.</returns>
        private static double Sanitize(double value)
        {
            return double.IsNaN(value) || double.IsInfinity(value) ? 0d : value;
        }

        /// <summary>
        /// 등록된 변수 값을 조회합니다.
        /// </summary>
        /// <param name="key">조회할 변수 이름입니다.</param>
        /// <param name="value">조회된 변수 값입니다.</param>
        /// <returns>변수를 찾으면 <see langword="true"/>를 반환합니다.</returns>
        public bool TryGet(string key, out double value)
        {
            value = 0d;
            return !string.IsNullOrWhiteSpace(key) && _values.TryGetValue(key.Trim(), out value);
        }

        /// <summary>
        /// 등록된 모든 변수를 제거합니다.
        /// </summary>
        public void Clear()
        {
            _values.Clear();
        }
    }

    /// <summary>
    /// 캐릭터 단위로 Poly 데미지 공식 변수를 추가 제공하는 확장 인터페이스입니다.
    /// </summary>
    /// <remarks>
    /// Core는 Affect/Skill 같은 상위 패키지를 직접 참조하지 않고, 이 인터페이스를 구현한 컴포넌트를 통해 버프 배율 같은 값을 주입받습니다.
    /// </remarks>
    public interface IDamageFormulaVariableProvider
    {
        /// <summary>
        /// 공식 계산 직전에 필요한 변수를 추가하거나 병합합니다.
        /// </summary>
        /// <param name="attacker">공격자 캐릭터입니다.</param>
        /// <param name="target">피격 대상 캐릭터입니다.</param>
        /// <param name="variables">변수를 등록할 컨테이너입니다.</param>
        void FillDamageFormulaVariables(CharacterBase attacker, CharacterBase target, DamageFormulaVariableBag variables);
    }
}
