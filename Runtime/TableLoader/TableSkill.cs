using System.Collections.Generic;

namespace GGemCo.Scripts
{
    /// <summary>
    /// 맵 테이블 Structure
    /// </summary>
    public class StruckTableSkill
    {
        public int Uid;
        public string Name;
        public string IconFileName;
        public int Level;
        public int Maxlevel;
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
        public float EffectMoveSpeed;
        public int NeedMp;
        public float TickTime;
        public float Duration;
        public float CoolTime;
        public int AffectUid;
        public int AffectRate;
    }
    /// <summary>
    /// 맵 테이블
    /// </summary>
    public class TableSkill : DefaultTable
    {
        // 레벨 1인 것만 모아놓은 dictionary
        private readonly Dictionary<int, StruckTableSkill> skills = new Dictionary<int, StruckTableSkill>();
        // 레벨 별로 모아놓은 dictionary
        private readonly Dictionary<int, Dictionary<int, StruckTableSkill>> skillsByLevel = new Dictionary<int, Dictionary<int, StruckTableSkill>>();
        
        private static readonly Dictionary<string, SkillConstants.Target> MapTarget;
        private static readonly Dictionary<string, SkillConstants.TargetType> MapTargetType;
        private static readonly Dictionary<string, SkillConstants.DamageType> MapDamageType;
        static TableSkill()
        {
            MapTarget = new Dictionary<string, SkillConstants.Target>
            {
                { "Player", SkillConstants.Target.Player },
                { "Monster", SkillConstants.Target.Monster },
            };
            MapTargetType = new Dictionary<string, SkillConstants.TargetType>
            {
                { "Fixed", SkillConstants.TargetType.Fixed },
                { "Range", SkillConstants.TargetType.Range },
            };
            MapDamageType = new Dictionary<string, SkillConstants.DamageType>
            {
                { "Physic", SkillConstants.DamageType.Physic },
                { "Fire", SkillConstants.DamageType.Fire },
                { "Cold", SkillConstants.DamageType.Cold },
                { "Lightning", SkillConstants.DamageType.Lightning },
            };
        }
        private static SkillConstants.Target ConvertTarget(string type) => MapTarget.GetValueOrDefault(type, SkillConstants.Target.None);
        private static SkillConstants.TargetType ConvertTargetType(string type) => MapTargetType.GetValueOrDefault(type, SkillConstants.TargetType.None);
        private static SkillConstants.DamageType ConvertDamageType(string type) => MapDamageType.GetValueOrDefault(type, SkillConstants.DamageType.None);

        public Dictionary<int, StruckTableSkill> GetSkills()
        {
            return skills;
        }
        protected override void OnLoadedData(Dictionary<string, string> data)
        {
            int uid = int.Parse(data["Uid"]);
            int level = int.Parse(data["Level"]);

            if (!skillsByLevel.ContainsKey(uid))
            {
                skillsByLevel.TryAdd(uid, new Dictionary<int, StruckTableSkill>());
            }
            if (!skillsByLevel[uid].ContainsKey(level))
            {
                skillsByLevel[uid].TryAdd(level, new StruckTableSkill());
            }

            StruckTableSkill struckTableItemDropGroup = new StruckTableSkill
            {
                Uid = int.Parse(data["Uid"]),
                Name = data["Name"],
                IconFileName = data["IconFileName"],
                Level = int.Parse(data["Level"]),
                Maxlevel = int.Parse(data["Maxlevel"]),
                NeedPlayerLevel = int.Parse(data["NeedPlayerLevel"]),
                NeedCurrencyType = ConvertCurrencyType(data["NeedCurrencyType"]),
                NeedCurrencyValue = int.Parse(data["NeedCurrencyValue"]),
                Target = ConvertTarget(data["Target"]),
                TargetType = ConvertTargetType(data["TargetType"]),
                DamageType = ConvertDamageType(data["DamageType"]),
                DamageValue = int.Parse(data["DamageValue"]),
                DamageRange = int.Parse(data["DamageRange"]),
                Distance = int.Parse(data["Distance"]),
                EffectUid = int.Parse(data["EffectUid"]),
                EffectScale = float.Parse(data["EffectScale"]),
                EffectMoveSpeed = float.Parse(data["EffectMoveSpeed"]),
                NeedMp = int.Parse(data["NeedMp"]),
                TickTime = float.Parse(data["TickTime"]),
                Duration = float.Parse(data["Duration"]),
                CoolTime = float.Parse(data["CoolTime"]),
                AffectUid = int.Parse(data["AffectUid"]),
                AffectRate = int.Parse(data["AffectRate"]),
            };
            skillsByLevel[uid][level] = struckTableItemDropGroup;
            if (!skills.ContainsKey(uid))
            {
                skills.TryAdd(uid, struckTableItemDropGroup);
            }

        }

        public StruckTableSkill GetDataByUidLevel(int uid, int level)
        {
            if (uid > 0 && level > 0)
            {
                Dictionary<int, StruckTableSkill> struckTableSkill = skillsByLevel.GetValueOrDefault(uid);
                if (struckTableSkill != null)
                {
                    return struckTableSkill.GetValueOrDefault(level);
                }
            }
            GcLogger.LogError("고유번호가 없거나 레벨 값이 없습니다.");
            return null;
        }

        public StruckTableSkill GetDataByUid(int uid)
        {
            GcLogger.LogError("사용할 수 없습니다.");
            return null;
        }
    }
}