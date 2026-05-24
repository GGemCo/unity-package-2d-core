using System;

namespace GGemCo2DCore
{
    /// <summary>
    /// NPC 상호작용을 일시 차단하는 이유를 나타냅니다.
    /// 외부 패키지는 Core 의존성을 유지한 상태에서 이 값만 전달하여 상호작용 시작을 제어합니다.
    /// </summary>
    public enum InteractionBlockReason
    {
        /// <summary>
        /// 차단 사유가 지정되지 않았습니다.
        /// </summary>
        Unknown = 0,

        /// <summary>
        /// 컷씬 또는 연출 재생 중이라 상호작용을 차단합니다.
        /// </summary>
        Cutscene = 1,

        /// <summary>
        /// 씬 인트로 또는 시작 연출 중이라 상호작용을 차단합니다.
        /// </summary>
        SceneIntro = 2,

        /// <summary>
        /// 씬/데이터 로딩 중이라 상호작용을 차단합니다.
        /// </summary>
        Loading = 3,

        /// <summary>
        /// 모달 UI 또는 시스템 UI가 입력을 점유하고 있어 상호작용을 차단합니다.
        /// </summary>
        UiModal = 4,

        /// <summary>
        /// 프로젝트별 커스텀 정책으로 상호작용을 차단합니다.
        /// </summary>
        Custom = 100
    }

    /// <summary>
    /// NPC 상호작용 차단 획득/해제를 위한 토큰입니다.
    /// 획득한 토큰을 해제하지 않으면 상호작용 차단 상태가 유지됩니다.
    /// </summary>
    [Serializable]
    public readonly struct InteractionBlockToken : IEquatable<InteractionBlockToken>
    {
        public static InteractionBlockToken None => default;

        public readonly int id;
        public readonly InteractionBlockReason reason;

        /// <summary>
        /// 상호작용 차단 토큰을 생성합니다.
        /// </summary>
        /// <param name="id">토큰 식별자입니다.</param>
        /// <param name="reason">상호작용 차단 사유입니다.</param>
        public InteractionBlockToken(int id, InteractionBlockReason reason)
        {
            this.id = id;
            this.reason = reason;
        }

        /// <summary>
        /// 해제 가능한 유효 토큰인지 확인합니다.
        /// </summary>
        public bool IsValid => id != 0;

        public bool Equals(InteractionBlockToken other) => id == other.id;
        public override bool Equals(object obj) => obj is InteractionBlockToken other && Equals(other);
        public override int GetHashCode() => id;
        public static bool operator ==(InteractionBlockToken a, InteractionBlockToken b) => a.Equals(b);
        public static bool operator !=(InteractionBlockToken a, InteractionBlockToken b) => !a.Equals(b);
    }
}
