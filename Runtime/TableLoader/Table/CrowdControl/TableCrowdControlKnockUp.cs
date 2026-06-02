using System.Collections.Generic;

namespace GGemCo2DCore
{
    public sealed class TableCrowdControlKnockUp : DefaultTable<StruckTableCrowdControlKnockUp>
    {
        public override string Key => ConfigAddressableTable.CrowdControlKnockUp;

        protected override StruckTableCrowdControlKnockUp BuildRow(Dictionary<string, string> data)
        {
            TableRowReader reader = ReadRow(data);
            var row = new StruckTableCrowdControlKnockUp
            {
                CrowdControlUid = reader.Int("CrowdControlUid"),
                LandEndWaitTime = reader.Float("LandEndWaitTime"),
                Height = reader.Float("Height"),
                RiseTime = reader.Float("RiseTime"),
                AirTime = reader.Float("AirTime"),
                FallTime = reader.Float("FallTime"),
                RiseAnimationName = reader.String("RiseAnimationName"),
                AirAnimationName = reader.String("AirAnimationName"),
                FallAnimationName = reader.String("FallAnimationName"),
                LandEndAnimationName = reader.String("LandEndAnimationName"),
                RiseEaseType = reader.Enum<Easing.EaseType>("RiseEaseType"),
                FallEaseType = reader.Enum<Easing.EaseType>("FallEaseType"),
                EndYMode = reader.Enum<CrowdControlConstants.EndYMode>("EndYMode"),
                EndYOffset = reader.Float("EndYOffset"),
                EndYAbsolute = reader.Float("EndYAbsolute"),
                RecoverTime = reader.Float("RecoverTime"),
                IsStopOnWall = reader.BoolYN("IsStopOnWall"),
                IsGroundOnly = reader.BoolYN("IsGroundOnly"),
                IsAirOnly = reader.BoolYN("IsAirOnly"),
            };

            return row.CrowdControlUid > 0 ? row : null;
        }
    }
}