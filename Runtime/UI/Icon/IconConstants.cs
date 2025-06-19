using System.Collections.Generic;

namespace GGemCo.Scripts
{
    /// <summary>
    /// 아이콘 정보
    /// </summary>
    public static class IconConstants
    {
        public enum Grade { None, Normal, Magic, Rare, Unique }
        
        const string GradeNone = "None";
        const string GradeNormal = "Normal";
        const string GradeMagic = "Magic";
        const string GradeRare = "Rare";
        const string GradeUnique = "Unique";
        
        public static readonly Dictionary<Grade, string> IconGradeImagePath = new Dictionary<Grade, string>
        {
            { Grade.Normal, "rank_normal" },
            { Grade.Magic, "rank_magic" },
            { Grade.Rare, "rank_rare" },
            { Grade.Unique, "rank_unique" },
        };
        public static readonly Dictionary<string, Grade> IconGradeEnum = new Dictionary<string, Grade>
        {
            { GradeNone, Grade.None },
            { GradeNormal, Grade.Normal },
            { GradeMagic, Grade.Magic },
            { GradeRare, Grade.Rare },
            { GradeUnique, Grade.Unique },
        };
        public enum Type
        {
            None,
            Item,
            Skill,
            Buff,
        }
        public enum Status
        {
            None,
            Normal,
            Lock,
            Disable
        }
    }
}