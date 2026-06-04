using System;

namespace GGemCo2DCore
{
    /// <summary>
    /// AutoMove를 일시정지하는 이유(디버그/로그/정책 분기를 위한 값)입니다.
    /// </summary>
    public enum AutoMoveSuspendReason
    {
        Unknown,
        WallAction,
        Cutscene,
        UiModal,
        GuardAction,
        PlayerAttackRange,
        ControlLocked,
        Skill
    }

    /// <summary>
    /// AutoMove Suspend 획득/해제를 위한 토큰입니다.
    /// - 값 타입으로 전달하고, 컨트롤러 내부에서 id로 유효성을 판단합니다.
    /// </summary>
    [Serializable]
    public readonly struct AutoMoveSuspendToken : IEquatable<AutoMoveSuspendToken>
    {
        public static AutoMoveSuspendToken None => default;

        public readonly int id;
        public readonly AutoMoveSuspendReason reason;

        public AutoMoveSuspendToken(int id, AutoMoveSuspendReason reason)
        {
            this.id = id;
            this.reason = reason;
        }

        public bool IsValid => id != 0;

        public bool Equals(AutoMoveSuspendToken other) => id == other.id;
        public override bool Equals(object obj) => obj is AutoMoveSuspendToken other && Equals(other);
        public override int GetHashCode() => id;
        public static bool operator ==(AutoMoveSuspendToken a, AutoMoveSuspendToken b) => a.Equals(b);
        public static bool operator !=(AutoMoveSuspendToken a, AutoMoveSuspendToken b) => !a.Equals(b);
    }
}
