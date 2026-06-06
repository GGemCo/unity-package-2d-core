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
        public void Set(string key, double value)
        {
            if (string.IsNullOrWhiteSpace(key))
                return;

            _values[key.Trim()] = double.IsNaN(value) || double.IsInfinity(value) ? 0d : value;
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
        /// 공식 계산 직전에 필요한 변수를 추가하거나 덮어씁니다.
        /// </summary>
        /// <param name="attacker">공격자 캐릭터입니다.</param>
        /// <param name="target">피격 대상 캐릭터입니다.</param>
        /// <param name="variables">변수를 등록할 컨테이너입니다.</param>
        void FillDamageFormulaVariables(CharacterBase attacker, CharacterBase target, DamageFormulaVariableBag variables);
    }
}
