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
        public enum CalculateType
        {
            Flat = 0,
            PercentOfMax = 1,
        }
        
        public const string StatusStatAtk = "STAT_ATK";
        public const string StatusStatDef = "STAT_DEF";
        public const string StatusStatHp = "STAT_HP";
        /// <summary>
        /// 임시(Temporary) 최대 HP(추가 하트/보호막 등) 스탯 키
        /// - 기본값(Base)은 0이며, Provider를 통해서만 증가합니다.
        /// </summary>
        public const string StatusStatHpTemp = "STAT_HP_TEMP";
        public const string StatusStatMp = "STAT_MP";
        public const string StatusStatStamina = "STAT_STAMINA";
        public const string StatusStatSuperArmor = "STAT_SUPER_ARMOR";
        public const string StatusStatMoveSpeed = "STAT_MOVE_SPEED";
        public const string StatusStatAttackSpeed = "STAT_ATTACK_SPEED";
        public const string StatusStatCriticalDamage = "STAT_CRITICAL_DAMAGE";
        public const string StatusStatCriticalProbability = "STAT_CRITICAL_PROBABILITY";
        public const string StatusStatResistanceFire = "STAT_RESISTANCE_FIRE";
        public const string StatusStatResistanceCold = "STAT_RESISTANCE_COLD";
        public const string StatusStatResistanceLightning = "STAT_RESISTANCE_LIGHTNING";
        public const string StatusStatResistancePoison = "STAT_RESISTANCE_POISON";
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
        
        /// <summary>
        /// 캐릭터의 방향 타입
        /// - 2방향: 좌/우
        /// - 4방향: 상/하/좌/우
        /// - 8방향: 대각 포함
        /// </summary>
        public enum FacingDirectionType
        {
            TwoWay   = 2,  // 좌우
            FourWay  = 4,  // 상하좌우
            EightWay = 8   // 상하좌우 + 대각
        }
        /// <summary>
        /// 패키지 메인 클래스 실행 순서
        /// </summary>
        public enum ExecutionOrdering
        {
            Control,
            Simulation,
            Affect,
            Skill,
            AiBt
        }
        
         // 원소 속성 타입
         public enum DamageType
         {
             None,
             Physic,
             Fire,
             Cold,
             Lightning,
             Poison
         }

         public static class DamageTypeString
         {
             public const string Physic = "DT_Physic";
             public const string Fire = "DT_Fire";
             public const string Cold = "DT_Cold";
             public const string Lightning = "DT_Lightning";
             public const string Poison = "DT_Poison";
         }

         public static readonly Dictionary<DamageType, string> NameByDamageType = new Dictionary<DamageType, string>
         {
             { DamageType.None, "None" },
             { DamageType.Physic, "Physic DMG" },
             { DamageType.Fire, "Fire DMG" },
             { DamageType.Cold, "Cold DMG" },
             { DamageType.Lightning, "Lighting DMG" },
             { DamageType.Poison, "Poison DMG" },
         };
         /// <summary>
         /// 스킬 정의를 조회할 원본 테이블 종류입니다.
         /// </summary>
         public enum SkillTableSource
         {
             Player = 0,
             Monster = 1,
         }
         
         public enum ThumbnailPositionType { None, Left, Right }
    }
}