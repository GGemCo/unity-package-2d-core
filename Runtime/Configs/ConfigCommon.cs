using System.Collections.Generic;

namespace GGemCo2DCore
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
        public enum AnimationController
        {
            Sprite,
            Spine,
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
        public const string StatusKnockBack = "KNOCK_BACK";
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
        public enum DirectionType
        {
            Left,
            Right,
        }
        private static readonly Dictionary<string, DirectionType> MapDirectionType =
            new Dictionary<string, DirectionType>
            {
                {"Left", DirectionType.Left},
                {"Right", DirectionType.Right},
            };
        public static DirectionType GetDirectionType(string type) =>
            MapDirectionType.GetValueOrDefault(type, DirectionType.Left);

        public enum PositionYType
        {
            None,
            CharacterHeight
        }
        public const string TitleHeaderRequired = "[필수 항목]";
        public const string TitleHeaderOption = "[선택 항목]";
        
        public enum ClimateId { Spring = 0, Summer = 1, Autumn = 2, Winter = 3 }
    }
}