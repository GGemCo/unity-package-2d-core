namespace GGemCo2DCore
{
    /// <summary>
    /// 캐릭터의 액션/상태 전환을 외부 시스템(스킬, AI 등)에서 요청하기 위한 공용 인터페이스입니다.
    /// Skill 패키지는 <see cref="CharacterBase"/>에 직접 의존하지 않고, 이 인터페이스로 상태 전환을 요청합니다.
    /// </summary>
    public interface ICharacterActionController
    {
        /// <summary>
        /// 캐릭터에게 액션(상태 전환)을 요청합니다.
        /// </summary>
        /// <param name="request">요청 데이터</param>
        /// <returns>요청을 수락하여 적용했으면 true</returns>
        bool RequestAction(in CharacterActionRequest request);

        /// <summary>
        /// 특정 상태를 종료합니다(현재 상태가 일치할 때만 해제).
        /// </summary>
        /// <param name="status">해제할 상태</param>
        void ClearAction(CharacterConstants.CharacterStatus status);
    }

    /// <summary>
    /// 캐릭터 상태 전환 요청 데이터입니다.
    /// </summary>
    public readonly struct CharacterActionRequest
    {
        /// <summary>
        /// 전환할 상태
        /// </summary>
        public CharacterConstants.CharacterStatus Status { get; }

        /// <summary>
        /// true면 이동 입력/방향을 즉시 중단합니다.
        /// </summary>
        public bool StopMove { get; }
        public bool LockMove { get; }
        public bool LockFacing { get; }

        /// <summary>
        /// 요청 생성자
        /// </summary>
        public CharacterActionRequest(
            CharacterConstants.CharacterStatus status,
            int skillUid = 0,
            bool stopMove = true,
            bool lockMove = false,
            bool lockFacing = false)
        {
            Status = status;
            StopMove = stopMove;
            LockMove = lockMove;
            LockFacing = lockFacing;
        }
    }
}