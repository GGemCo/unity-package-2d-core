using UnityEngine;

namespace GGemCo2DCore
{
    /// <summary>
    /// CrowdControl 이동/시간 진행을 담당하는 모션 전략 인터페이스입니다.
    /// - CharacterCrowdControlController는 Type에 따라 적절한 Motion을 선택하고 Tick만 호출합니다.
    /// </summary>
    internal interface ICrowdControlMotion
    {
        /// <summary>진행이 끝났는지 여부</summary>
        bool IsFinished { get; }

        /// <summary>
        /// 모션을 1 step 진행합니다.
        /// </summary>
        /// <param name="deltaTime">FixedDeltaTime</param>
        /// <param name="nextPosition">적용할 다음 위치</param>
        /// <returns>이 프레임에 위치 이동이 필요한지 여부</returns>
        bool Tick(float deltaTime, out Vector2 nextPosition);
    }
}
