namespace GGemCo2DCore
{
    /// <summary>
    /// 저장/복원에 참여하는 외부 패키지 확장 포인트.
    /// </summary>
    public interface ISaveContributor
    {
        /// <summary>고유 섹션 키 (예: "simulation.gridinfo")</summary>
        string SectionKey { get; }

        /// <summary>현재 상태를 Envelope에 기록</summary>
        void Capture(SaveEnvelope env);

        /// <summary>Envelope에서 자신의 섹션을 읽어 복원</summary>
        void Restore(SaveEnvelope env);

        /// <summary>저장/복원 호출 우선순위 (낮을수록 먼저)</summary>
        int Priority => 100;
    }
}