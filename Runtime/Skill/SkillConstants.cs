using System.Collections.Generic;

namespace GGemCo.Scripts
{
    public static class SkillConstants
    {
        public enum Target
        {
            None,
            Player, // 플레이어 자신
            Monster, //몬스터
        }
        public enum TargetType
        {
            None,
            Fixed, // 고정 타겟
            Range, // 범위
        }
        // 원소 속성 타입
        public enum DamageType
        {
            None,
            Physic,
            Fire,
            Cold,
            Lightning
        }

        public static Dictionary<DamageType, string> NameByDamageType = new Dictionary<DamageType, string>
        {
            { DamageType.None, "None" },
            { DamageType.Physic, "물리 데미지" },
            { DamageType.Fire, "파이어 데미지" },
            { DamageType.Cold, "얼음 데미지" },
            { DamageType.Lightning, "전기 데미지" },
        };
    }
}