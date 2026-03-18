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
            return new StruckTableMonster
            {
                Uid = MathHelper.ParseInt(data["Uid"]),
                Name = data["Name"],
                ImageThumbnailFileName = data["ImageThumbnailFileName"],
                AnimationUid = MathHelper.ParseInt(data["AnimationUid"]),
                DefaultSkin = data["DefaultSkin"],
                AttackType = EnumHelper.ConvertEnum<CharacterConstants.AttackType>(data["AttackType"]),
                Scale = MathHelper.ParseFloat(data["Scale"]),
                Grade = EnumHelper.ConvertEnum<CharacterConstants.Grade>(data["Grade"]),
                Level = MathHelper.ParseInt(data["Level"]),
                StatHp = MathHelper.ParseInt(data["StatHp"]),
                StatAtk = MathHelper.ParseInt(data["StatAtk"]),
                StatDef = MathHelper.ParseInt(data["StatDef"]),
                StatSuperArmor = MathHelper.ParseInt(data["StatSuperArmor"]),
                StatMoveSpeed = MathHelper.ParseInt(data["StatMoveSpeed"]),
                StatAttackSpeed = MathHelper.ParseInt(data["StatAttackSpeed"]),
                RewardExp = MathHelper.ParseLong(data["RewardExp"]),
                RegistFire = MathHelper.ParseInt(data["RegistFire"]),
                RegistCold = MathHelper.ParseInt(data["RegistCold"]),
                RegistLightning = MathHelper.ParseInt(data["RegistLightning"]),
                RegistPoison = MathHelper.ParseInt(data["RegistPoison"]),
                RewardGold = MathHelper.ParseInt(data["RewardGold"]),
                SkillMonsterUid = ConvertIntArray(data["SkillMonsterUid"]),
                BtFileName = (data["BtFileName"]),
            };
        }
    }
}