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

        /// <summary>
        /// CC 종료 시 재생할 종료 애니메이션의 접미사입니다.
        /// - Animator에서 Start → Wait 전환을 구성하는 경우, Wait는 자동으로 전환됩니다.
        /// - End는 본 컨트롤러가 명시적으로 재생합니다.
        /// </summary>
        public const string StaggerAnimationWaitSuffix = "_wait";
        public const string StaggerAnimationEndSuffix = "_end";

    }

    /// <summary>
    /// CrowdControl 정의 테이블 파서입니다. (crowd_control.txt)
    /// </summary>
    public sealed class TableCrowdControl : DefaultTable<StruckTableCrowdControl>
    {
        public override string Key => ConfigAddressableTable.CrowdControl;

        protected override StruckTableCrowdControl BuildRow(Dictionary<string, string> data)
        {
            // 필수
            var row = new StruckTableCrowdControl
            {
                Uid = MathHelper.ParseInt(data.GetValueOrDefault("Uid")),
                Name = data.GetValueOrDefault("Name"),

                Type = EnumHelper.ConvertEnum<CrowdControlConstants.Type>(data.GetValueOrDefault("Type")),
                DirectionType = EnumHelper.ConvertEnum<CrowdControlConstants.DirectionType>(data.GetValueOrDefault("DirectionType")),

                FixedDirectionX = MathHelper.ParseFloat(data.GetValueOrDefault("FixedDirectionX")),
                FixedDirectionY = MathHelper.ParseFloat(data.GetValueOrDefault("FixedDirectionY")),

                Distance = MathHelper.ParseFloat(data.GetValueOrDefault("Distance")),
                EaseType = EnumHelper.ConvertEnum<Easing.EaseType>(data.GetValueOrDefault("EaseType")),

                Duration = MathHelper.ParseFloat(data.GetValueOrDefault("Duration")),

                IsUseKnockbackStatus = ConvertBoolean(data.GetValueOrDefault("IsUseKnockbackStatus")),
                IsUseDontControlStatus = ConvertBoolean(data.GetValueOrDefault("IsUseDontControlStatus")),

                StaggerAnimationName = data.GetValueOrDefault("StaggerAnimationName"),
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
