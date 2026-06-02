using System.Collections.Generic;

namespace GGemCo2DCore
{
    /// <summary>
    /// 애니메이션 테이블 Structure
    /// </summary>
    public class StruckTableAnimation
    {
        public int Uid;
        public string Name;
        public CharacterConstants.Type Type;
        public ConfigCommon.AnimationController Controller;
        public string PrefabName;
        public CharacterConstants.FacingDirection8 DefaultFacingDirection8;
        public float MoveStep;
        public float Width;
        public float Height;
    }
    /// <summary>
    /// 애니메이션 테이블
    /// </summary>
    public class TableAnimation : DefaultTable<StruckTableAnimation>
    {
        public override string Key => ConfigAddressableTable.Animation;
        
        protected override StruckTableAnimation BuildRow(Dictionary<string, string> data)
        {
            TableRowReader reader = ReadRow(data);
            return new StruckTableAnimation
            {
                Uid = reader.Int("Uid"),
                Name = reader.String("Name"),
                Type = reader.Enum<CharacterConstants.Type>("Type"),
                Controller = ConvertAnimationController(reader.String("Controller")),
                PrefabName = reader.String("PrefabName"),
                DefaultFacingDirection8 = ConvertFacing(reader.String("DefaultFacingDirection8")),
                Width = reader.Float("Width"),
                Height = reader.Float("Height"),
                MoveStep = reader.Float("MoveStep"),
            };
        }
    }
}