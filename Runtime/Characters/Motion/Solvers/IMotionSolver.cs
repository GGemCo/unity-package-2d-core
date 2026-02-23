using UnityEngine;

namespace GGemCo2DCore
{
    /// <summary>
    /// 모션 진행 로직(계산)을 분리하기 위한 Solver 인터페이스입니다.
    /// - Solver는 "이번 FixedUpdate에 적용할 delta"를 계산합니다.
    /// - Rigidbody 적용 방식(MovePosition/velocity)은 <see cref="CharacterMotionController2D"/>가 담당합니다.
    /// </summary>
    internal interface IMotionSolver
    {
        /// <summary>
        /// 모션을 한 프레임 진행시키고, 이번 프레임에 적용할 증분 이동량을 계산합니다.
        /// </summary>
        /// <param name="state">진행할 모션 상태입니다.</param>
        /// <param name="dt">고정 프레임 시간(초)입니다.</param>
        /// <param name="delta">이번 프레임에 적용할 이동량(월드 좌표)입니다.</param>
        void Tick(ref CharacterMotionController2D.MotionState state, float dt, out Vector2 delta);
    }
}
