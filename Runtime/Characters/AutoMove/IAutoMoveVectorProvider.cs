using UnityEngine;

namespace GGemCo2DCore
{
    /// <summary>
    /// Control 패키지(InputManager)가 자동 이동을 우선 적용할 수 있도록,
    /// 이동 벡터 오버라이드와 입력 잠금/취소 신호를 제공하는 인터페이스입니다.
    /// </summary>
    public interface IAutoMoveVectorProvider
    {
        /// <summary>자동 이동이 현재 활성화되어 있는지 여부</summary>
        bool IsAutoMoveActive { get; }

        /// <summary>
        /// 자동 이동 중 입력을 잠글지 여부
        /// - true인 경우: 공격/점프/대시/상호작용 등 수동 입력을 차단(또는 최소화)하는 것을 권장
        /// </summary>
        bool IsInputLocked { get; }

        /// <summary>
        /// 자동 이동 중 특정 입력을 차단해야 하는지 여부를 반환합니다.
        /// - 전역 설정(GGemCoSettings.autoMoveLockMovementOnly) 및 요청의 lockInput 정책을 반영합니다.
        /// </summary>
        bool ShouldBlockInput(AutoMoveInputType inputType);

        /// <summary>현재 프레임에 적용할 이동 벡터(정규화 권장)</summary>
        Vector2 GetMoveVector();

        /// <summary>
        /// 수동 입력이 들어왔을 때 호출(취소 정책 판정용)
        /// </summary>
        void NotifyPlayerInput(AutoMoveInputType inputType, Vector2 value);
    }
}
