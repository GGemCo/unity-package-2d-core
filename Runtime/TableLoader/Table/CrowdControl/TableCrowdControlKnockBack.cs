using System.Collections.Generic;

namespace GGemCo2DCore
{
    public sealed class TableCrowdControlKnockBack : DefaultTable<StruckTableCrowdControlKnockBack>
    {
        public override string Key => ConfigAddressableTable.CrowdControlKnockBack;

        protected override StruckTableCrowdControlKnockBack BuildRow(Dictionary<string, string> data)
        {
            TableRowReader reader = ReadRow(data);
            var row = new StruckTableCrowdControlKnockBack
            {
                CrowdControlUid = reader.Int("CrowdControlUid"),
                DownWaitTime = reader.Float("DownWaitTime"),
                EndYMode = reader.Enum<CrowdControlConstants.EndYMode>("EndYMode"),
                EndYOffset = reader.Float("EndYOffset"),
                EndYAbsolute = reader.Float("EndYAbsolute"),
                RecoverTime = reader.Float("RecoverTime"),
                IsStopOnWall = reader.BoolYN("IsStopOnWall"),
                IsGroundOnly = reader.BoolYN("IsGroundOnly"),
                IsAirOnly = reader.BoolYN("IsAirOnly"),
                UseWallImpactReaction = reader.BoolYN("UseWallImpactReaction"),
                WallImpactMinSpeed = reader.Float("WallImpactMinSpeed"),
                WallImpactCrowdControlUid = reader.Int("WallImpactCrowdControlUid"),
            };

            return row.CrowdControlUid > 0 ? row : null;
        }
    }
}