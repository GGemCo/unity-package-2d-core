using System.Collections.Generic;

namespace GGemCo2DCore
{
    public sealed class TableCrowdControlKnockDownAir : DefaultTable<StruckTableCrowdControlKnockDownAir>
    {
        public override string Key => ConfigAddressableTable.CrowdControlKnockDownAir;

        protected override StruckTableCrowdControlKnockDownAir BuildRow(Dictionary<string, string> data)
        {
            TableRowReader reader = ReadRow(data);
            var row = new StruckTableCrowdControlKnockDownAir
            {
                CrowdControlUid = reader.Int("CrowdControlUid"),
                Height = reader.Float("Height"),
                RiseTime = reader.Float("RiseTime"),
                AirTime = reader.Float("AirTime"),
                FallSpeed = reader.Float("FallSpeed"),
                LandEndWaitTime = reader.Float("LandEndWaitTime"),
                AirAnimationIsLoop = reader.BoolYN("AirAnimationIsLoop"),
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