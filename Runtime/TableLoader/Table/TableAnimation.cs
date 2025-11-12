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
            return new StruckTableAnimation
            {
                Uid = MathHelper.ParseInt(data["Uid"]),
                Name = data["Name"],
                Type = EnumHelper.ConvertEnum<CharacterConstants.Type>(data["Type"]),
                Controller = ConvertAnimationController(data["Controller"]),
                PrefabName = data["PrefabName"],
                DefaultFacingDirection8 = ConvertFacing(data["DefaultFacing"]),
                Width = MathHelper.ParseFloat(data["Width"]),
                Height = MathHelper.ParseFloat(data["Height"]),
                MoveStep = MathHelper.ParseFloat(data["MoveStep"]),
            };
        }
    }
}