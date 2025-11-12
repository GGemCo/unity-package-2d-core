using System.Collections.Generic;

namespace GGemCo2DCore
{
    /// <summary>
    /// 스킬 테이블 Structure
    /// </summary>
    public class StruckTableSkill
    {
        public int Uid;
        public string Name;
        public string IconFileName;
        public int Level;
        public int MaxLevel;
        public int NeedPlayerLevel;
        public CurrencyConstants.Type NeedCurrencyType;
        public int NeedCurrencyValue;
        public SkillConstants.Target Target;
        public SkillConstants.TargetType TargetType;
        public SkillConstants.DamageType DamageType;
        public int DamageValue;
        public int DamageRange;
        public int Distance;
        public int EffectUid;
        public float EffectScale;
        public int ProjectileUid;
        public int NeedMp;
        public float TickTime;
        public float Duration;
        public float CoolTime;
        public int AffectUid;
        public int AffectRate;
    }
    /// <summary>
    /// 스킬 테이블
    /// </summary>
    public class TableSkill : DefaultTable<StruckTableSkill>
    {
        public override string Key => ConfigAddressableTable.Skill;
        // 레벨 1인 것만 모아놓은 dictionary
        private readonly Dictionary<int, StruckTableSkill> _skills = new Dictionary<int, StruckTableSkill>();
        // 레벨 별로 모아놓은 dictionary
        private readonly Dictionary<int, Dictionary<int, StruckTableSkill>> _skillsByLevel = new Dictionary<int, Dictionary<int, StruckTableSkill>>();
        
        public Dictionary<int, StruckTableSkill> GetSkills()
        {
            return _skills;
        }
        protected override void OnLoadedData(StruckTableSkill data)
        {
            int uid = data.Uid;
            int level = data.Level;

            if (LocalizationManager.Instance != null)
            {
                data.Name = LocalizationManager.Instance.GetSkillNameByKey(uid.ToString());   
            }
            
            if (!_skillsByLevel.ContainsKey(uid))
            {
                _skillsByLevel.TryAdd(uid, new Dictionary<int, StruckTableSkill>());
            }
            if (!_skillsByLevel[uid].ContainsKey(level))
            {
                _skillsByLevel[uid].TryAdd(level, new StruckTableSkill());
            }

            if (data.Duration > data.CoolTime)
            {
                GcLogger.LogWarning($"Uid: {uid}, Level: {level}, Nmae: {data.Name}. Duration: {data.Duration} > CoolTime: {data.CoolTime}. ");
            }

            _skillsByLevel[uid][level] = data;
            if (!_skills.ContainsKey(uid))
            {
                _skills.TryAdd(uid, data);
            }

        }

        protected override StruckTableSkill BuildRow(Dictionary<string, string> data)
        {
            return new StruckTableSkill
            {
                Uid = MathHelper.ParseInt(data["Uid"]),
                Name = data["Name"],
                IconFileName = data["IconFileName"],
                Level = MathHelper.ParseInt(data["Level"]),
                MaxLevel = MathHelper.ParseInt(data["MaxLevel"]),
                NeedPlayerLevel = MathHelper.ParseInt(data["NeedPlayerLevel"]),
                NeedCurrencyType = ConvertCurrencyType(data["NeedCurrencyType"]),
                NeedCurrencyValue = MathHelper.ParseInt(data["NeedCurrencyValue"]),
                Target = EnumHelper.ConvertEnum<SkillConstants.Target>(data["Target"]),
                TargetType = EnumHelper.ConvertEnum<SkillConstants.TargetType>(data["TargetType"]),
                DamageType = EnumHelper.ConvertEnum<SkillConstants.DamageType>(data["DamageType"]),
                DamageValue = MathHelper.ParseInt(data["DamageValue"]),
                DamageRange = MathHelper.ParseInt(data["DamageRange"]),
                Distance = MathHelper.ParseInt(data["Distance"]),
                EffectUid = MathHelper.ParseInt(data["EffectUid"]),
                EffectScale = MathHelper.ParseFloat(data["EffectScale"]),
                ProjectileUid = MathHelper.ParseInt(data["ProjectileUid"]),
                NeedMp = MathHelper.ParseInt(data["NeedMp"]),
                TickTime = MathHelper.ParseFloat(data["TickTime"]),
                Duration = MathHelper.ParseFloat(data["Duration"]),
                CoolTime = MathHelper.ParseFloat(data["CoolTime"]),
                AffectUid = MathHelper.ParseInt(data["AffectUid"]),
                AffectRate = MathHelper.ParseInt(data["AffectRate"]),
            };
        }
        public StruckTableSkill GetDataByUidLevel(int uid, int level)
        {
            if (uid > 0 && level > 0)
            {
                Dictionary<int, StruckTableSkill> struckTableSkill = _skillsByLevel.GetValueOrDefault(uid);
                if (struckTableSkill != null)
                {
                    return struckTableSkill.GetValueOrDefault(level);
                }
            }
            GcLogger.LogError("고유번호가 없거나 레벨 값이 없습니다.");
            return null;
        }

        public override StruckTableSkill GetDataByUid(int uid)
        {
            GcLogger.LogError("사용할 수 없습니다.");
            return null;
        }
    }
}