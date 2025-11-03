using System.Collections.Generic;

namespace GGemCo2DCore
{
    /// <summary>
    /// Npc 테이블 Structure
    /// </summary>
    public class StruckTableNpc : IUidName
    {
        public int Uid { get; set; }
        public string Name { get; set; }
        public int AnimationUid;
        public string DefaultSkin;
        public CharacterConstantsNpc.NpcType Type;
        public CharacterConstantsNpc.NpcCategory Category;
        public CharacterConstantsNpc.NpcSubCategory SubCategory;
        public float Scale;
        public CharacterConstants.Grade Grade;
        public int StatMoveSpeed;
        public int InteractionUid;
        public string ImageThumbnailFileName;
        public int StatHp;
        public bool ShowHpBar;
        public bool ShowNameTag;
    }
    /// <summary>
    /// Npc 테이블
    /// </summary>
    public class TableNpc : DefaultTable
    {
        private static readonly Dictionary<string, CharacterConstants.Grade> MapGrade;

        static TableNpc()
        {
            MapGrade = new Dictionary<string, CharacterConstants.Grade>
            {
                { "Common", CharacterConstants.Grade.Common },
                { "Boss", CharacterConstants.Grade.Boss },
            };
        }

        private CharacterConstants.Grade ConvertGrade(string grade) => MapGrade.GetValueOrDefault(grade, CharacterConstants.Grade.None);

        protected override void OnLoadedData(Dictionary<string, string> data)
        {
            int uid = int.Parse(data["Uid"]);
            if (LocalizationManager.Instance != null)
            {
                data["Name"] = LocalizationManager.Instance.GetNpcNameByKey(uid.ToString());   
            }
        }

        public StruckTableNpc GetDataByUid(int uid)
        {
            if (uid <= 0)
            {
                GcLogger.LogError("uid is 0.");
                return null;
            }
            var data = GetData(uid);
            if (data == null) return null;
            var result = CharacterConstantsNpc.TryParseAndValidate(data["Type"], data["Category"], data["SubCategory"],
                out var npcType, out var npcCategory, out var npcSub);
            if (!result)
            {
                return null;
            }

            return new StruckTableNpc
            {
                Uid = int.Parse(data["Uid"]),
                Name = data["Name"],
                AnimationUid = int.Parse(data["AnimationUid"]),
                DefaultSkin = data["DefaultSkin"],
                Type = npcType,
                Category = npcCategory,
                SubCategory = npcSub,
                Scale = float.Parse(data["Scale"]),
                Grade = ConvertGrade(data["Grade"]),
                StatMoveSpeed = int.Parse(data["StatMoveSpeed"]),
                InteractionUid = int.Parse(data["InteractionUid"]),
                ImageThumbnailFileName = data["ImageThumbnailFileName"],
                StatHp = int.Parse(data["StatHp"]),
                ShowHpBar = ConvertBoolean(data["ShowHpBar"]),
                ShowNameTag = ConvertBoolean(data["ShowNameTag"]),
            };
        }
        public override bool TryGetDataByUid(int uid, out object info)
        {
            info = GetDataByUid(uid);
            return info != null && ((StruckTableNpc)info).Uid > 0;
        }
    }
}