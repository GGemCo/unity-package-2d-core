using System.Collections.Generic;

namespace GGemCo2DCore
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

        public static readonly Dictionary<DamageType, string> NameByDamageType = new Dictionary<DamageType, string>
        {
            { DamageType.None, "None" },
            { DamageType.Physic, "Physic DMG" },
            { DamageType.Fire, "Fire DMG" },
            { DamageType.Cold, "Cold DMG" },
            { DamageType.Lightning, "Lighting DMG" },
        };
    }
}