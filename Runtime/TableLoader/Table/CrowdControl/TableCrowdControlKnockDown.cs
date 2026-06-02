using System.Collections.Generic;

namespace GGemCo2DCore
{
    public sealed class TableCrowdControlKnockDown : DefaultTable<StruckTableCrowdControlKnockDown>
    {
        public override string Key => ConfigAddressableTable.CrowdControlKnockDown;

        protected override StruckTableCrowdControlKnockDown BuildRow(Dictionary<string, string> data)
        {
            TableRowReader reader = ReadRow(data);
            var row = new StruckTableCrowdControlKnockDown
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
            };

            return row.CrowdControlUid > 0 ? row : null;
        }
    }
}