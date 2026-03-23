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

        /// <summary>
        /// 레거시 상세 컬럼입니다. KnockUp 상세 테이블이 없을 때 fallback으로 사용합니다.
        /// - KnockUp 전용: 공중으로 띄우는 높이(유닛)입니다.
        /// - 0이면 수직 이동 없이 수평 이동만 처리됩니다.
        /// </summary>
        public float Height;

        /// <summary>
        /// CrowdControl 종료 시 최종 Y 위치를 어떻게 결정할지 정의합니다.
        /// </summary>
        public CrowdControlConstants.EndYMode EndYMode;

        /// <summary>
        /// 종료 Y 보정값입니다.
        /// - <see cref="CrowdControlConstants.EndYMode.AddOffsetFromStart"/>에서는 시작 Y 기준 오프셋으로 사용합니다.
        /// - <see cref="CrowdControlConstants.EndYMode.GroundAtEndX"/>에서는 탐지된 지면 Y에 더할 추가 오프셋으로 사용합니다.
        /// </summary>
        public float EndYOffset;

        /// <summary>
        /// 종료 Y 절대값입니다. <see cref="CrowdControlConstants.EndYMode.Absolute"/>에서 사용합니다.
        /// </summary>
        public float EndYAbsolute;

        /// <summary>
        /// 레거시 상세 컬럼입니다. KnockBack/KnockDown 상세 테이블이 없을 때 fallback으로 사용합니다.
        /// - KnockBack, Knockdown 전용: 밀려나기/넘어짐/눕기 상태로 유지할 시간(초)입니다.
        /// - 0이면 대기 없이 종료 구간으로 넘어갑니다.
        /// </summary>
        public float DownWaitTime;

        /// <summary>
        /// 선택: Recover(기상) 연출 시간을 데이터로 관리하고 싶을 때 사용합니다.
        /// - 현재 Core 구현에서는 End 애니메이션 종료를 우선으로 하므로, 기본값(0)으로 두어도 됩니다.
        /// </summary>
        public float RecoverTime;

        public bool IsLockControl;
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

                Height = MathHelper.ParseFloat(data.GetValueOrDefault("Height")),
                EndYMode = EnumHelper.ConvertEnum<CrowdControlConstants.EndYMode>(data.GetValueOrDefault("EndYMode")),
                EndYOffset = MathHelper.ParseFloat(data.GetValueOrDefault("EndYOffset")),
                EndYAbsolute = MathHelper.ParseFloat(data.GetValueOrDefault("EndYAbsolute")),
                DownWaitTime = MathHelper.ParseFloat(data.GetValueOrDefault("DownWaitTime")),
                RecoverTime = MathHelper.ParseFloat(data.GetValueOrDefault("RecoverTime")),

                IsLockControl = ConvertBoolean(data.GetValueOrDefault("IsLockControl")),
                IsUseKnockbackStatus = ConvertBoolean(data.GetValueOrDefault("IsUseKnockbackStatus")),
                IsUseDontControlStatus = ConvertBoolean(data.GetValueOrDefault("IsUseDontControlStatus")),

                StaggerAnimationName = data.GetValueOrDefault("StaggerAnimationName"),

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
