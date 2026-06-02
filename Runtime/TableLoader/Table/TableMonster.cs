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
        public float Scale;
        public CharacterConstants.Grade Grade;
        public int Level;
        public int StatHp;
        public int StatAtk;
        public int StatDef;
        public int StatSuperArmor;
        public int StatMoveSpeed;
        public int StatAttackSpeed;
        public long RewardExp;
        public int RewardGold;
        public int RegistFire;
        public int RegistCold;
        public int RegistLightning;
        public int RegistPoison;
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
            return new StruckTableMonster
            {
                Uid = reader.Int("Uid"),
                Name = reader.String("Name"),
                ImageThumbnailFileName = reader.String("ImageThumbnailFileName"),
                AnimationUid = reader.Int("AnimationUid"),
                DefaultSkin = reader.String("DefaultSkin"),
                AttackType = reader.Enum<CharacterConstants.AttackType>("AttackType"),
                Scale = reader.Float("Scale"),
                Grade = reader.Enum<CharacterConstants.Grade>("Grade"),
                Level = reader.Int("Level"),
                StatHp = reader.Int("StatHp"),
                StatAtk = reader.Int("StatAtk"),
                StatDef = reader.Int("StatDef"),
                StatSuperArmor = reader.Int("StatSuperArmor"),
                StatMoveSpeed = reader.Int("StatMoveSpeed"),
                StatAttackSpeed = reader.Int("StatAttackSpeed"),
                RewardExp = reader.Long("RewardExp"),
                RegistFire = reader.Int("RegistFire"),
                RegistCold = reader.Int("RegistCold"),
                RegistLightning = reader.Int("RegistLightning"),
                RegistPoison = reader.Int("RegistPoison"),
                RewardGold = reader.Int("RewardGold"),
                SkillMonsterUid = reader.IntArray("SkillMonsterUid"),
                DeathSkillMonsterUid = data.TryGetValue("DeathSkillMonsterUid", out string deathSkillMonsterUid)
                    ? MathHelper.ParseInt(deathSkillMonsterUid)
                    : 0,
                BtFileName = (reader.String("BtFileName")),
            };
        }
    }
}
