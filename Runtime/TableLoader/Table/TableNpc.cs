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
        public string InteractionDynamicParameterKey;
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
            TableRowReader reader = ReadRow(data);
            var result = CharacterConstantsNpc.TryParseAndValidate(reader.String("Type"), reader.String("Category"), reader.String("SubCategory"),
                out var npcType, out var npcCategory, out var npcSub);
            if (!result)
            {
                return null;
            }

            return new StruckTableNpc
            {
                Uid = reader.Int("Uid"),
                Name = reader.String("Name"),
                AnimationUid = reader.Int("AnimationUid"),
                DefaultSkin = reader.String("DefaultSkin"),
                Type = npcType,
                Category = npcCategory,
                SubCategory = npcSub,
                Scale = reader.Float("Scale"),
                Grade = reader.Enum<CharacterConstants.Grade>("Grade"),
                StatMoveSpeed = reader.Int("StatMoveSpeed"),
                InteractionUid = reader.Int("InteractionUid"),
                InteractionParameters = reader.String("InteractionParameters"),
                InteractionDynamicParameterKey = reader.String("InteractionDynamicParameterKey"),
                ImageThumbnailFileName = reader.String("ImageThumbnailFileName"),
                StatHp = reader.Int("StatHp"),
                ShowHpBar = reader.BoolYN("ShowHpBar"),
                ShowNameTag = reader.BoolYN("ShowNameTag"),
            };
        }
    }
}