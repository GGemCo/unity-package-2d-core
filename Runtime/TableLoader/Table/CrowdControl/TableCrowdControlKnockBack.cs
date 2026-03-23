using System.Collections.Generic;

namespace GGemCo2DCore
{
    public sealed class TableCrowdControlKnockBack : DefaultTable<StruckTableCrowdControlKnockBack>
    {
        public override string Key => ConfigAddressableTable.CrowdControlKnockBack;

        protected override StruckTableCrowdControlKnockBack BuildRow(Dictionary<string, string> data)
        {
            var row = new StruckTableCrowdControlKnockBack
            {
                CrowdControlUid = MathHelper.ParseInt(data.GetValueOrDefault("CrowdControlUid")),
                DownWaitTime = MathHelper.ParseFloat(data.GetValueOrDefault("DownWaitTime")),
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