using UnityEngine;

namespace GGemCo2DCore
{
    /// <summary>
    /// 스킬/AI/CC 등 외부 시스템이 캐릭터의 "짧은 모션 이동(전진/대시/러시/넉백 등)"을 요청하기 위한 공용 인터페이스입니다.
    /// - Skill 패키지는 <see cref="CharacterBase"/>에 직접 의존하지 않고, 이 인터페이스로 이동을 요청합니다.
    /// - 구현체는 Rigidbody2D(특히 Kinematic) 기반 MovePosition 이동을 권장합니다.
    /// </summary>
    public interface ICharacterMotionController
    {
        /// <summary>
        /// 모션 요청을 시작합니다.
        /// </summary>
        bool TryStartMotion(in MotionRequest request);

        /// <summary>
        /// 지정 채널의 모션을 중단합니다(스킬 캔슬/경직 등).
        /// </summary>
        void CancelMotion(MotionChannel channel, int reason = 0);

        /// <summary>
        /// 지정 채널이 모션을 재생 중인지 여부를 반환합니다.
        /// </summary>
        bool IsPlaying(MotionChannel channel);
    }
}
