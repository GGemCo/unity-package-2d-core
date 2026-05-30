using UnityEngine;

namespace GGemCo2DCore
{
    /// <summary>
    /// 플레이어의 가드 성공 결과를 수신하기 위한 인터페이스입니다.
    /// </summary>
    /// <remarks>
    /// Control 패키지는 이 포트만 호출하고, 실제 저스트 가드 콤보 진입 같은 프로젝트 전용 처리는
    /// 상위 프로젝트 컴포넌트가 구현합니다.
    /// </remarks>
    public interface IPlayerGuardSuccessFeedbackSink
    {
        /// <summary>
        /// 플레이어 가드 성공 결과가 확정되었을 때 호출됩니다.
        /// </summary>
        /// <param name="feedback">가드 성공 결과입니다.</param>
        void NotifyPlayerGuardSuccess(in PlayerGuardSuccessFeedback feedback);
    }

    /// <summary>
    /// 플레이어 가드 성공 결과 값입니다.
    /// </summary>
    public readonly struct PlayerGuardSuccessFeedback
    {
        /// <summary>
        /// 가드를 성공시킨 플레이어 오브젝트입니다.
        /// </summary>
        public GameObject Defender { get; }

        /// <summary>
        /// 공격자 오브젝트입니다.
        /// </summary>
        public GameObject Attacker { get; }

        /// <summary>
        /// 가드 판정에 사용된 데미지 메타데이터입니다.
        /// </summary>
        public MetadataDamage MetadataDamage { get; }

        /// <summary>
        /// 저스트 가드로 판정되었는지 여부입니다.
        /// </summary>
        public bool IsJustGuard { get; }

        /// <summary>
        /// 가드 판정 결과입니다.
        /// </summary>
        public GuardResolutionOutcome Outcome { get; }

        /// <summary>
        /// 결과가 확정된 Unity 시간입니다.
        /// </summary>
        public float Time { get; }

        /// <summary>
        /// 플레이어 가드 성공 결과 값을 생성합니다.
        /// </summary>
        /// <param name="defender">가드를 성공시킨 플레이어 오브젝트입니다.</param>
        /// <param name="attacker">공격자 오브젝트입니다.</param>
        /// <param name="metadataDamage">가드 판정에 사용된 데미지 메타데이터입니다.</param>
        /// <param name="isJustGuard">저스트 가드 여부입니다.</param>
        /// <param name="outcome">가드 판정 결과입니다.</param>
        /// <param name="time">결과가 확정된 Unity 시간입니다.</param>
        public PlayerGuardSuccessFeedback(
            GameObject defender,
            GameObject attacker,
            MetadataDamage metadataDamage,
            bool isJustGuard,
            GuardResolutionOutcome outcome,
            float time)
        {
            Defender = defender;
            Attacker = attacker;
            MetadataDamage = metadataDamage;
            IsJustGuard = isJustGuard;
            Outcome = outcome;
            Time = time;
        }
    }
}
