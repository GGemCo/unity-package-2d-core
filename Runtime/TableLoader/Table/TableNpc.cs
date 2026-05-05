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
        public string InteractionParameters;
        public string ImageThumbnailFileName;
        public int StatHp;
        public bool ShowHpBar;
        public bool ShowNameTag;
    }
    /// <summary>
    /// Npc 테이블
    /// </summary>
    public class TableNpc : DefaultTable<StruckTableNpc>
    {
        public override string Key => ConfigAddressableTable.Npc;
        protected override void OnLoadedData(StruckTableNpc data)
        {
            if (LocalizationManager.Instance == null) return;
            data.Name = LocalizationManager.Instance.GetNpcNameByKey(data.Uid.ToString());
        }

        protected override StruckTableNpc BuildRow(Dictionary<string, string> data)
        {
            var result = CharacterConstantsNpc.TryParseAndValidate(data["Type"], data["Category"], data["SubCategory"],
                out var npcType, out var npcCategory, out var npcSub);
            if (!result)
            {
                return null;
            }

            return new StruckTableNpc
            {
                Uid = MathHelper.ParseInt(data["Uid"]),
                Name = data["Name"],
                AnimationUid = MathHelper.ParseInt(data["AnimationUid"]),
                DefaultSkin = data["DefaultSkin"],
                Type = npcType,
                Category = npcCategory,
                SubCategory = npcSub,
                Scale = MathHelper.ParseFloat(data["Scale"]),
                Grade = EnumHelper.ConvertEnum<CharacterConstants.Grade>(data["Grade"]),
                StatMoveSpeed = MathHelper.ParseInt(data["StatMoveSpeed"]),
                InteractionUid = MathHelper.ParseInt(data["InteractionUid"]),
                InteractionParameters = data.GetValueOrDefault("InteractionParameters"),
                ImageThumbnailFileName = data["ImageThumbnailFileName"],
                StatHp = MathHelper.ParseInt(data["StatHp"]),
                ShowHpBar = ConvertBoolean(data["ShowHpBar"]),
                ShowNameTag = ConvertBoolean(data["ShowNameTag"]),
            };
        }
    }
}