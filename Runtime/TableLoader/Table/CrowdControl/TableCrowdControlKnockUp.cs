using System.Collections.Generic;

namespace GGemCo2DCore
{
    public sealed class TableCrowdControlKnockUp : DefaultTable<StruckTableCrowdControlKnockUp>
    {
        public override string Key => ConfigAddressableTable.CrowdControlKnockUp;

        protected override StruckTableCrowdControlKnockUp BuildRow(Dictionary<string, string> data)
        {
            var row = new StruckTableCrowdControlKnockUp
            {
                CrowdControlUid = MathHelper.ParseInt(data.GetValueOrDefault("CrowdControlUid")),
                Height = MathHelper.ParseFloat(data.GetValueOrDefault("Height")),
                RiseTime = MathHelper.ParseFloat(data.GetValueOrDefault("RiseTime")),
                AirTime = MathHelper.ParseFloat(data.GetValueOrDefault("AirTime")),
                FallTime = MathHelper.ParseFloat(data.GetValueOrDefault("FallTime")),
                RiseAnimationName = data.GetValueOrDefault("RiseAnimationName"),
                AirAnimationName = data.GetValueOrDefault("AirAnimationName"),
                FallAnimationName = data.GetValueOrDefault("FallAnimationName"),
                LandEndAnimationName = data.GetValueOrDefault("LandEndAnimationName"),
                RiseEaseType = EnumHelper.ConvertEnum<Easing.EaseType>(data.GetValueOrDefault("RiseEaseType")),
                FallEaseType = EnumHelper.ConvertEnum<Easing.EaseType>(data.GetValueOrDefault("FallEaseType")),
                EndYMode = EnumHelper.ConvertEnum<CrowdControlConstants.EndYMode>(data.GetValueOrDefault("EndYMode")),
                EndYOffset = MathHelper.ParseFloat(data.GetValueOrDefault("EndYOffset")),
                EndYAbsolute = MathHelper.ParseFloat(data.GetValueOrDefault("EndYAbsolute")),
                RecoverTime = MathHelper.ParseFloat(data.GetValueOrDefault("RecoverTime")),
                IsStopOnWall = ConvertBoolean(data.GetValueOrDefault("IsStopOnWall")),
                IsGroundOnly = ConvertBoolean(data.GetValueOrDefault("IsGroundOnly")),
                IsAirOnly = ConvertBoolean(data.GetValueOrDefault("IsAirOnly")),
            };

            return row.CrowdControlUid > 0 ? row : null;
        }
    }
}