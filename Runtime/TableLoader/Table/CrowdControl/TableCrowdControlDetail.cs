namespace GGemCo2DCore
{
    /// <summary>
    /// Crowd Control 타입별 상세 테이블의 공통 베이스입니다.
    /// 키는 CrowdControlUid를 사용합니다.
    /// </summary>
    public abstract class StruckTableCrowdControlDetailBase
    {
        public int CrowdControlUid;
        public CrowdControlConstants.EndYMode EndYMode;
        public float EndYOffset;
        public float EndYAbsolute;
        public float RecoverTime;
        public bool IsStopOnWall;
        public bool IsGroundOnly;
        public bool IsAirOnly;
    }

    public sealed class StruckTableCrowdControlKnockBack : StruckTableCrowdControlDetailBase
    {
        public float DownWaitTime;
    }

    public sealed class StruckTableCrowdControlKnockDown : StruckTableCrowdControlDetailBase
    {
        public float DownWaitTime;
    }

    public sealed class StruckTableCrowdControlKnockUp : StruckTableCrowdControlDetailBase
    {
        public float LandEndWaitTime;

        public float Height;

        public float RiseTime;
        public float AirTime;
        public float FallTime;

        public string RiseAnimationName;
        public string AirAnimationName;
        public string FallAnimationName;
        public string LandEndAnimationName;

        public Easing.EaseType RiseEaseType;
        public Easing.EaseType FallEaseType;
    }

    public sealed class StruckTableCrowdControlKnockDownAir : StruckTableCrowdControlDetailBase
    {
        public float Height;

        public float RiseTime;
        public float AirTime;
        public float FallSpeed;

        /// <summary>
        /// 착지 후 End 애니메이션이 끝난 뒤 추가로 다운 상태를 유지할 시간(초)입니다.
        /// </summary>
        public float LandEndWaitTime;

        /// <summary>
        /// KnockDownAir의 공중(Air) 애니메이션을 루프 재생할지 여부입니다.
        /// - true : AirTime 동안 루프 재생
        /// - false: 최초 1회만 재생하고, 남은 AirTime은 채공만 유지
        /// </summary>
        public bool AirAnimationIsLoop;

        public string RiseAnimationName;
        public string AirAnimationName;
        public string FallAnimationName;
        public string LandEndAnimationName;

        public Easing.EaseType RiseEaseType;
        public Easing.EaseType FallEaseType;
    }
}
