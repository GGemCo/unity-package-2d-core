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

        /// <summary>
        /// stat 테이블 항목의 사용 분류입니다.
        /// </summary>
        public enum StatGroup
        {
            /// <summary>
            /// 분류가 지정되지 않은 항목입니다.
            /// </summary>
            None = 0,

            /// <summary>
            /// BASE_* 계열 기본 항목입니다. 장비, 아이템, Affect로 보정할 수 있습니다.
            /// </summary>
            Base = 1,

            /// <summary>
            /// STAT_* 계열 성장 스탯 항목입니다. 스탯 포인트와 성장형 옵션에서 사용합니다.
            /// </summary>
            Growth = 2,

            /// <summary>
            /// 런타임에서만 누적되는 특수 스탯 항목입니다.
            /// </summary>
            Runtime = 3,
        }
        
        public const string BaseStatAtk = "BASE_ATK";
        public const string BaseStatDef = "BASE_DEF";
        public const string BaseStatHp = "BASE_HP";
        public const string BaseStatMp = "BASE_MP";
        public const string BaseStatStamina = "BASE_STAMINA";
        public const string BaseStatSuperArmor = "BASE_SUPER_ARMOR";
        public const string BaseStatMoveSpeed = "BASE_MOVE_SPEED";
        public const string BaseStatAttackSpeed = "BASE_ATTACK_SPEED";
        public const string BaseStatCriticalDamage = "BASE_CRITICAL_DAMAGE";
        public const string BaseStatCriticalProbability = "BASE_CRITICAL_PROBABILITY";
        public const string BaseStatRegistFire = "BASE_REGIST_FIRE";
        public const string BaseStatRegistCold = "BASE_REGIST_COLD";
        public const string BaseStatRegistLightning = "BASE_REGIST_LIGHTNING";
        public const string BaseStatRegistPoison = "BASE_REGIST_POISON";
        public const string BaseStatMoveStep = "BASE_MOVE_STEP";
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

        /// <summary>
        /// 스탯 ID가 BASE_* 계열 기본 항목인지 확인합니다.
        /// </summary>
        /// <param name="statId">확인할 스탯 ID입니다.</param>
        /// <returns>BASE_* 계열이면 true입니다.</returns>
        public static bool IsBaseStatId(string statId) =>
            !string.IsNullOrWhiteSpace(statId) && statId.StartsWith("BASE_", System.StringComparison.Ordinal);

        /// <summary>
        /// 스탯 ID가 STAT_* 계열 성장/런타임 항목인지 확인합니다.
        /// </summary>
        /// <param name="statId">확인할 스탯 ID입니다.</param>
        /// <returns>STAT_* 계열이면 true입니다.</returns>
        public static bool IsStatusStatId(string statId) =>
            !string.IsNullOrWhiteSpace(statId) && statId.StartsWith("STAT_", System.StringComparison.Ordinal);

        /// <summary>
        /// stat 테이블의 Group 컬럼이 비어 있을 때, ID prefix 기준으로 기본 분류를 추론합니다.
        /// </summary>
        /// <param name="statId">분류를 추론할 스탯 ID입니다.</param>
        /// <returns>추론된 스탯 분류입니다.</returns>
        public static StatGroup ResolveStatGroupById(string statId)
        {
            if (string.IsNullOrWhiteSpace(statId)) return StatGroup.None;
            if (statId == StatusStatHpTemp) return StatGroup.Runtime;
            if (IsBaseStatId(statId)) return StatGroup.Base;
            if (IsStatusStatId(statId)) return StatGroup.Growth;
            return StatGroup.None;
        }

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