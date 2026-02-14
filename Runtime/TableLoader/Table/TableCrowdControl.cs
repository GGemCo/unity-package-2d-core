using System.Collections.Generic;

namespace GGemCo2DCore
{
    /// <summary>
    /// CrowdControl 테이블의 한 행(row) 데이터입니다.
    /// </summary>
    public sealed class StruckTableCrowdControl
    {
        public int Uid;
        public string Id;

        public CrowdControlConstants.Type Type;
        public CrowdControlConstants.DirectionType DirectionType;

        public float FixedDirectionX;
        public float FixedDirectionY;

        /// <summary>
        /// CrowdControlDuration 동안 이동할 총 거리(유닛)입니다.
        /// 방향은 <see cref="DirectionType"/> 규칙으로 결정됩니다.
        /// </summary>
        public float Distance;

        /// <summary>
        /// <see cref="Duration"/> 동안의 이동 보간(Easing) 타입입니다.
        /// </summary>
        public Easing.EaseType EaseType;

        public float Duration;

        public bool IsLockControl;
        public bool IsUseKnockbackStatus;
        public bool IsUseDontControlStatus;

        public CrowdControlConstants.StaggerAnimationType StaggerAnimationType;

        public bool IsStopOnWall;
        public bool IsGroundOnly;
        public bool IsAirOnly;
    }

    /// <summary>
    /// CrowdControl 정의 테이블 파서입니다. (crowd_control.txt)
    /// </summary>
    public sealed class TableCrowdControl : DefaultTable<StruckTableCrowdControl>
    {
        public override string Key => ConfigAddressableTable.CrowdControl;

        private readonly Dictionary<string, StruckTableCrowdControl> _byId = new();

        protected override void PreLoad()
        {
            _byId.Clear();
        }

        protected override void OnLoadedData(StruckTableCrowdControl data)
        {
            if (data == null) return;
            if (!string.IsNullOrWhiteSpace(data.Id))
                _byId[data.Id] = data;
        }

        public StruckTableCrowdControl GetDataById(string id)
        {
            if (string.IsNullOrWhiteSpace(id)) return null;
            return _byId.TryGetValue(id, out var row) ? row : null;
        }

        protected override StruckTableCrowdControl BuildRow(Dictionary<string, string> data)
        {
            // 필수
            var row = new StruckTableCrowdControl
            {
                Uid = MathHelper.ParseInt(data.GetValueOrDefault("Uid")),
                Id = data.GetValueOrDefault("Id"),

                Type = EnumHelper.ConvertEnum<CrowdControlConstants.Type>(data.GetValueOrDefault("Type")),
                DirectionType = EnumHelper.ConvertEnum<CrowdControlConstants.DirectionType>(data.GetValueOrDefault("DirectionType")),

                FixedDirectionX = MathHelper.ParseFloat(data.GetValueOrDefault("FixedDirectionX")),
                FixedDirectionY = MathHelper.ParseFloat(data.GetValueOrDefault("FixedDirectionY")),

                Distance = MathHelper.ParseFloat(data.GetValueOrDefault("Distance")),
                EaseType = EnumHelper.ConvertEnum<Easing.EaseType>(data.GetValueOrDefault("EaseType")),

                Duration = MathHelper.ParseFloat(data.GetValueOrDefault("Duration")),

                IsLockControl = ConvertBoolean(data.GetValueOrDefault("IsLockControl")),
                IsUseKnockbackStatus = ConvertBoolean(data.GetValueOrDefault("IsUseKnockbackStatus")),
                IsUseDontControlStatus = ConvertBoolean(data.GetValueOrDefault("IsUseDontControlStatus")),

                StaggerAnimationType = EnumHelper.ConvertEnum<CrowdControlConstants.StaggerAnimationType>(data.GetValueOrDefault("StaggerAnimationType")),

                IsStopOnWall = ConvertBoolean(data.GetValueOrDefault("IsStopOnWall")),
                IsGroundOnly = ConvertBoolean(data.GetValueOrDefault("IsGroundOnly")),
                IsAirOnly = ConvertBoolean(data.GetValueOrDefault("IsAirOnly")),
            };

            // 유효성 최소 보정
            if (row.Uid <= 0) return null;

            // 누락 시 기본값 보정
            if (string.IsNullOrWhiteSpace(data.GetValueOrDefault("EaseType")))
                row.EaseType = Easing.EaseType.Linear;

            return row;
        }
    }
}
