using System.Collections.Generic;
using UnityEngine;

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
        public int SpineUid;
        public string DefaultSkin;
        public CharacterConstants.AttackType AttackType;
        public float Scale;
        public CharacterConstants.Grade Grade;
        public int Level;
        public int StatHp;
        public int StatAtk;
        public int StatDef;
        public int StatMoveSpeed;
        public int StatAttackSpeed;
        public long RewardExp;
        public int RewardGold;
        public int RegistFire;
        public int RegistCold;
        public int RegistLightning;
    }
    /// <summary>
    /// 몬스터 테이블
    /// </summary>
    public class TableMonster : DefaultTable
    {
        private static readonly Dictionary<string, CharacterConstants.Grade> MapGrade;
        private static readonly Dictionary<string, CharacterConstants.AttackType> MapAttackType;

        static TableMonster()
        {
            MapGrade = new Dictionary<string, CharacterConstants.Grade>
            {
                { "Common", CharacterConstants.Grade.Common },
                { "Boss", CharacterConstants.Grade.Boss },
            };
            MapAttackType = new Dictionary<string, CharacterConstants.AttackType>
            {
                { "PassiveDefense", CharacterConstants.AttackType.PassiveDefense },
                { "AggroFirst", CharacterConstants.AttackType.AggroFirst },
            };
        }

        private CharacterConstants.Grade ConvertGrade(string grade) => MapGrade.GetValueOrDefault(grade, CharacterConstants.Grade.None);
        private CharacterConstants.AttackType ConvertAttackType(string grade) => MapAttackType.GetValueOrDefault(grade, CharacterConstants.AttackType.None);

        public StruckTableMonster GetDataByUid(int uid)
        {
            if (uid <= 0)
            {
                GcLogger.LogError("uid is 0.");
                return null;
            }
            var data = GetData(uid);
            if (data == null) return null;
            return new StruckTableMonster
            {
                Uid = int.Parse(data["Uid"]),
                Name = data["Name"],
                ImageThumbnailFileName = data["ImageThumbnailFileName"],
                SpineUid = int.Parse(data["SpineUid"]),
                DefaultSkin = data["DefaultSkin"],
                AttackType = ConvertAttackType(data["Type"]),
                Scale = float.Parse(data["Scale"]),
                Grade = ConvertGrade(data["Grade"]),
                Level = int.Parse(data["Level"]),
                StatHp = int.Parse(data["StatHp"]),
                StatAtk = int.Parse(data["StatAtk"]),
                StatDef = int.Parse(data["StatDef"]),
                StatMoveSpeed = int.Parse(data["StatMoveSpeed"]),
                StatAttackSpeed = int.Parse(data["StatAttackSpeed"]),
                RewardExp = long.Parse(data["RewardExp"]),
                RegistFire = int.Parse(data["RegistFire"]),
                RegistCold = int.Parse(data["RegistCold"]),
                RegistLightning = int.Parse(data["RegistLightning"]),
                RewardGold = int.Parse(data["RewardGold"]),
            };
        }
        public override bool TryGetDataByUid(int uid, out object info)
        {
            info = GetDataByUid(uid);
            return info != null && ((StruckTableMonster)info).Uid > 0;
        }
    }
}