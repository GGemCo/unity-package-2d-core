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
        public const string BaseStatDamageFire = "BASE_DAMAGE_FIRE";
        public const string BaseStatDamageCold = "BASE_DAMAGE_COLD";
        public const string BaseStatDamageLightning = "BASE_DAMAGE_LIGHTNING";
        public const string BaseStatDamagePoison = "BASE_DAMAGE_POISON";
        public const string BaseStatMoveStep = "BASE_MOVE_STEP";
        public const string StatusStatAtk = "STAT_ATK";
        public const string StatusStatDef = "STAT_DEF";
        public const string StatusStatHp = "STAT_HP";
        /// <summary>
        /// 임시(Temporary) 최대 HP(추가 하트/보호막 등)의 Base 계열 스탯 키입니다.
        /// - 일반 HP의 <see cref="BaseStatHp"/>와 합산하지 않고, <c>TotalHpTemp</c> 계산에만 사용합니다.
        /// - 아이템, 패시브, 런타임 보호막 Provider가 이 키를 통해 보호막 하트 최대치를 증가시킵니다.
        /// </summary>
        public const string BaseStatHpTemp = "BASE_HP_TEMP";

        /// <summary>
        /// 기존 STAT_HP_TEMP 데이터와 코드 참조를 위한 마이그레이션 호환 키입니다.
        /// - 신규 데이터는 <see cref="BaseStatHpTemp"/>를 사용해야 합니다.
        /// </summary>
        public const string LegacyStatusStatHpTemp = "STAT_HP_TEMP";

        /// <summary>
        /// 임시 HP 스탯 키의 이전 이름입니다.
        /// - 컴파일 호환을 위해 유지하되, 실제 값은 <see cref="BaseStatHpTemp"/>로 연결합니다.
        /// </summary>
        [System.Obsolete("STAT_HP_TEMP는 BASE_HP_TEMP로 변경되었습니다. 신규 코드는 BaseStatHpTemp를 사용해주세요.")]
        public const string StatusStatHpTemp = BaseStatHpTemp;
        public const string StatusStatMp = "STAT_MP";
        public const string StatusStatStamina = "STAT_STAMINA";

        /// <summary>
        /// 스탯 ID가 BASE_* 계열 기본 항목인지 확인합니다.
        /// </summary>
        /// <param name="statId">확인할 스탯 ID입니다.</param>
        /// <returns>BASE_* 계열이면 true입니다.</returns>
        public static bool IsBaseStatId(string statId)
        {
            string normalized = NormalizeStatId(statId);
            return !string.IsNullOrWhiteSpace(normalized) && normalized.StartsWith("BASE_", System.StringComparison.Ordinal);
        }

        /// <summary>
        /// 스탯 ID가 STAT_* 계열 성장/런타임 항목인지 확인합니다.
        /// </summary>
        /// <param name="statId">확인할 스탯 ID입니다.</param>
        /// <returns>STAT_* 계열이면 true입니다.</returns>
        public static bool IsStatusStatId(string statId)
        {
            string normalized = NormalizeStatId(statId);
            return !string.IsNullOrWhiteSpace(normalized) && normalized.StartsWith("STAT_", System.StringComparison.Ordinal);
        }

        /// <summary>
        /// 스탯 ID를 현재 런타임에서 사용하는 표준 ID로 정규화합니다.
        /// </summary>
        /// <param name="statId">정규화할 스탯 ID입니다.</param>
        /// <returns>마이그레이션 키가 있으면 현재 표준 ID를, 그렇지 않으면 trim 처리된 원본 ID를 반환합니다.</returns>
        public static string NormalizeStatId(string statId)
        {
            if (string.IsNullOrWhiteSpace(statId)) return string.Empty;

            string normalized = statId.Trim();
            if (normalized == LegacyStatusStatHpTemp) return BaseStatHpTemp;
            return normalized;
        }

        /// <summary>
        /// stat 테이블의 Group 컬럼이 비어 있을 때, ID prefix 기준으로 기본 분류를 추론합니다.
        /// </summary>
        /// <param name="statId">분류를 추론할 스탯 ID입니다.</param>
        /// <returns>추론된 스탯 분류입니다.</returns>
        public static StatGroup ResolveStatGroupById(string statId)
        {
            if (string.IsNullOrWhiteSpace(statId)) return StatGroup.None;
            statId = NormalizeStatId(statId);
            if (IsHpTempStatId(statId)) return StatGroup.Base;
            if (IsBaseStatId(statId)) return StatGroup.Base;
            if (IsStatusStatId(statId)) return StatGroup.Growth;
            return StatGroup.None;
        }

        /// <summary>
        /// 임시 HP(보호막 하트) 최대치에 사용되는 스탯 키인지 확인합니다.
        /// </summary>
        /// <param name="statId">확인할 스탯 ID입니다.</param>
        /// <returns><see cref="BaseStatHpTemp"/> 또는 마이그레이션 호환 키이면 true입니다.</returns>
        public static bool IsHpTempStatId(string statId)
        {
            if (string.IsNullOrWhiteSpace(statId)) return false;
            string normalized = NormalizeStatId(statId);
            return normalized == BaseStatHpTemp;
        }

        /// <summary>
        /// 기본 속성 데미지에 사용되는 스탯 키인지 확인합니다.
        /// </summary>
        /// <param name="statId">확인할 스탯 ID입니다.</param>
        /// <returns>화염/냉기/번개/독 기본 속성 데미지 키이면 true입니다.</returns>
        public static bool IsElementDamageStatId(string statId)
        {
            if (string.IsNullOrWhiteSpace(statId)) return false;
            string normalized = NormalizeStatId(statId);
            return normalized == BaseStatDamageFire
                   || normalized == BaseStatDamageCold
                   || normalized == BaseStatDamageLightning
                   || normalized == BaseStatDamagePoison;
        }

        /// <summary>
        /// 데미지 타입에 대응되는 기본 속성 데미지 스탯 키를 반환합니다.
        /// </summary>
        /// <param name="damageType">조회할 데미지 타입입니다.</param>
        /// <returns>지원되는 속성 데미지 타입이면 BASE_DAMAGE_* 키를, 아니면 빈 문자열을 반환합니다.</returns>
        public static string GetElementDamageStatId(DamageType damageType)
        {
            return damageType switch
            {
                DamageType.Fire => BaseStatDamageFire,
                DamageType.Cold => BaseStatDamageCold,
                DamageType.Lightning => BaseStatDamageLightning,
                DamageType.Poison => BaseStatDamagePoison,
                _ => string.Empty
            };
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