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
}
