namespace GGemCo.Scripts
{
    public static class ConfigCommon
    {
        public const float CharacterFadeSec = 0.7f;
        public static string PathPlayerPrefab = "Characters/Player/Player";
        
        public enum SuffixType
        {
            None,
            Plus,
            Minus,
            Increase,
            Decrease,
        }
        
        public const string StatusStatAtk = "STAT_ATK";
        public const string StatusStatDef = "STAT_DEF";
        public const string StatusStatHp = "STAT_HP";
        public const string StatusStatMp = "STAT_MP";
        public const string StatusStatMoveSpeed = "STAT_MOVE_SPEED";
        public const string StatusStatAttackSpeed = "STAT_ATTACK_SPEED";
        public const string StatusStatCriticalDamage = "STAT_CRITICAL_DAMAGE";
        public const string StatusStatCriticalProbability = "STAT_CRITICAL_PROBABILITY";
        public const string StatusStatResistanceFire = "STAT_REGISTANCE_FIRE";
        public const string StatusStatResistanceCold = "STAT_REGISTANCE_COLD";
        public const string StatusStatResistanceLightning = "STAT_REGISTANCE_LIGHTNING";
        public const string StatusAffectId = "AFFECT_UID";
        public class StruckStatus
        {
            public string ID;
            public SuffixType SuffixType;
            public float Value;

            public StruckStatus(string id, SuffixType suffixType, float value)
            {
                ID = id;
                SuffixType = suffixType;
                Value = value;
            }
        }
    }
}