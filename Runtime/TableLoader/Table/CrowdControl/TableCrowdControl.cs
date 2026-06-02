using System.Collections.Generic;

namespace GGemCo2DCore
{
    /// <summary>
    /// CrowdControl 공통 테이블의 한 행(row) 데이터입니다.
    /// </summary>
    public sealed class StruckTableCrowdControl
    {
        public int Uid;
        public string Name;

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

        public bool IsUseKnockbackStatus;
        public bool IsUseDontControlStatus;

        public string StaggerAnimationName;

    }

    /// <summary>
    /// CrowdControl 정의 테이블 파서입니다. (crowd_control.txt)
    /// </summary>
    public sealed class TableCrowdControl : DefaultTable<StruckTableCrowdControl>
    {
        public override string Key => ConfigAddressableTable.CrowdControl;

        protected override StruckTableCrowdControl BuildRow(Dictionary<string, string> data)
        {
            TableRowReader reader = ReadRow(data);
            // 필수
            var row = new StruckTableCrowdControl
            {
                Uid = reader.Int("Uid"),
                Name = reader.String("Name"),

                Type = reader.Enum<CrowdControlConstants.Type>("Type"),
                DirectionType = reader.Enum<CrowdControlConstants.DirectionType>("DirectionType"),

                FixedDirectionX = reader.Float("FixedDirectionX"),
                FixedDirectionY = reader.Float("FixedDirectionY"),

                Distance = reader.Float("Distance"),
                EaseType = reader.Enum<Easing.EaseType>("EaseType"),

                Duration = reader.Float("Duration"),

                IsUseKnockbackStatus = reader.BoolYN("IsUseKnockbackStatus"),
                IsUseDontControlStatus = reader.BoolYN("IsUseDontControlStatus"),

                StaggerAnimationName = reader.String("StaggerAnimationName"),
            };

            // 유효성 최소 보정
            if (row.Uid <= 0) return null;

            // 누락 시 기본값 보정
            if (string.IsNullOrWhiteSpace(reader.String("EaseType")))
                row.EaseType = Easing.EaseType.Linear;

            return row;
        }
    }
}
