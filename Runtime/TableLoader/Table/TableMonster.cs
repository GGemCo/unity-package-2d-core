using System.Collections.Generic;

namespace GGemCo2DCore
{
    /// <summary>
    /// 몬스터 테이블 Structure
    /// </summary>
    public class StruckTableMonster : IUidName
    {
        public int Uid { get; set; }
        public string Name { get; set; }
        public string ImageThumbnailFileName;
        public int AnimationUid;
        public string DefaultSkin;
        public CharacterConstants.AttackType AttackType;
        /// <summary>monster_combat_profile 테이블에서 사용할 전투 범위 프로필 UID입니다.</summary>
        public int CombatProfileUid;
        public float Scale;
        public CharacterConstants.Grade Grade;
        public int MinLevel;
        public int MaxLevel;
        public int BaseHp;
        public int BaseAtk;
        public int BaseDef;
        public int BaseMp;
        public int BaseStamina;
        public int BaseSuperArmor;
        public int BaseMoveSpeed;
        public int BaseAttackSpeed;
        public int BaseCriticalDamage;
        public int BaseCriticalProbability;
        public int BaseRegistFire;
        public int BaseRegistCold;
        public int BaseRegistLightning;
        public int BaseRegistPoison;
        public int StatHp;
        public int StatAtk;
        public int StatDef;
        public int StatMp;
        public int StatStamina;
        public long RewardExp;
        public int RewardGold;
        public int[] SkillMonsterUid;
        public int DeathSkillMonsterUid;
        public string BtFileName;
    }
    /// <summary>
    /// 몬스터 테이블
    /// </summary>
    public class TableMonster : DefaultTable<StruckTableMonster>
    {
        public override string Key => ConfigAddressableTable.Monster;
        protected override void OnLoadedData(StruckTableMonster data)
        {
            if (LocalizationManager.Instance == null) return;
            data.Name = LocalizationManager.Instance.GetMonsterNameByKey(data.Uid.ToString());
        }
        protected override StruckTableMonster BuildRow(Dictionary<string, string> data)
        {
            TableRowReader reader = ReadRow(data);
            int minLevel = ReadMonsterMinLevel(data);
            int maxLevel = ReadMonsterMaxLevel(data, minLevel);
            return new StruckTableMonster
            {
                Uid = reader.Int("Uid"),
                Name = reader.String("Name"),
                ImageThumbnailFileName = reader.String("ImageThumbnailFileName"),
                AnimationUid = reader.Int("AnimationUid"),
                DefaultSkin = reader.String("DefaultSkin"),
                AttackType = reader.Enum<CharacterConstants.AttackType>("AttackType"),
                CombatProfileUid = ReadOptionalInt(data, "CombatProfileUid", 0),
                Scale = reader.Float("Scale"),
                Grade = reader.Enum<CharacterConstants.Grade>("Grade"),
                MinLevel = minLevel,
                MaxLevel = maxLevel,
                BaseHp = ReadOptionalInt(data, "BaseHp", 0),
                BaseAtk = ReadOptionalInt(data, "BaseAtk", 0),
                BaseDef = ReadOptionalInt(data, "BaseDef", 0),
                BaseMp = ReadOptionalInt(data, "BaseMp", 0),
                BaseStamina = ReadOptionalInt(data, "BaseStamina", 0),
                BaseSuperArmor = ReadOptionalInt(data, "BaseSuperArmor", 0),
                BaseMoveSpeed = ReadOptionalInt(data, "BaseMoveSpeed", 0),
                BaseAttackSpeed = ReadOptionalInt(data, "BaseAttackSpeed", 0),
                BaseCriticalDamage = ReadOptionalInt(data, "BaseCriticalDamage", 0),
                BaseCriticalProbability = ReadOptionalInt(data, "BaseCriticalProbability", 0),
                BaseRegistFire = ReadOptionalInt(data, "BaseRegistFire", 0),
                BaseRegistCold = ReadOptionalInt(data, "BaseRegistCold", 0),
                BaseRegistLightning = ReadOptionalInt(data, "BaseRegistLightning", 0),
                BaseRegistPoison = ReadOptionalInt(data, "BaseRegistPoison", 0),
                StatHp = reader.Int("StatHp"),
                StatAtk = reader.Int("StatAtk"),
                StatDef = reader.Int("StatDef"),
                StatMp = reader.Int("StatMp"),
                StatStamina = reader.Int("StatStamina"),
                RewardExp = reader.Long("RewardExp"),
                RewardGold = reader.Int("RewardGold"),
                SkillMonsterUid = reader.IntArray("SkillMonsterUid"),
                DeathSkillMonsterUid = data.TryGetValue("DeathSkillMonsterUid", out string deathSkillMonsterUid)
                    ? MathHelper.ParseInt(deathSkillMonsterUid)
                    : 0,
                BtFileName = (reader.String("BtFileName")),
            };
        }

        /// <summary>
        /// monster 테이블의 최소 레벨 컬럼을 읽습니다.
        /// </summary>
        /// <param name="data">테이블 row 데이터입니다.</param>
        /// <returns>몬스터 스폰 시 사용할 최소 레벨입니다.</returns>
        private static int ReadMonsterMinLevel(Dictionary<string, string> data)
        {
            int legacyLevel = ReadOptionalInt(data, "Level", 1);
            return System.Math.Max(1, ReadOptionalInt(data, "MinLevel", legacyLevel));
        }

        /// <summary>
        /// monster 테이블의 최대 레벨 컬럼을 읽고 최소 레벨보다 낮으면 최소 레벨로 보정합니다.
        /// </summary>
        /// <param name="data">테이블 row 데이터입니다.</param>
        /// <param name="minLevel">이미 파싱한 최소 레벨입니다.</param>
        /// <returns>몬스터 스폰 시 사용할 최대 레벨입니다.</returns>
        private static int ReadMonsterMaxLevel(Dictionary<string, string> data, int minLevel)
        {
            int maxLevel = ReadOptionalInt(data, "MaxLevel", minLevel);
            return System.Math.Max(minLevel, maxLevel);
        }

        /// <summary>
        /// 신규 컬럼이 없는 기존 테이블을 읽을 수 있도록 선택 컬럼을 안전하게 파싱합니다.
        /// </summary>
        /// <param name="data">테이블 row 데이터입니다.</param>
        /// <param name="columnName">읽을 컬럼 이름입니다.</param>
        /// <param name="fallback">컬럼이 없거나 비어있을 때 사용할 값입니다.</param>
        /// <returns>파싱된 정수 값입니다.</returns>
        private static int ReadOptionalInt(Dictionary<string, string> data, string columnName, int fallback)
        {
            if (data == null) return fallback;
            return data.TryGetValue(columnName, out string value) && !string.IsNullOrWhiteSpace(value)
                ? MathHelper.ParseInt(value)
                : fallback;
        }
    }
}
