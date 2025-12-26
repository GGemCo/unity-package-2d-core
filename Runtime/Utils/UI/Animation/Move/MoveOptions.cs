using System;

namespace GGemCo2DCore
{
    /// <summary>
    /// UI/Transform 이동(Move) 연출에서 공통으로 사용하는 옵션 묶음입니다.
    /// </summary>
    /// <remarks>
    /// 지연, 시간 스케일 적용 여부, 이징, 완료 시 스냅 여부를 통해
    /// 이동 연출의 일관된 동작을 구성합니다.
    /// </remarks>
    [Serializable]
    public struct MoveOptions
    {
        /// <summary>
        /// Move 시작 전 지연 시간(초)입니다.
        /// </summary>
        public float delay;

        /// <summary>
        /// <see cref="UnityEngine.Time.timeScale"/>의 영향을 받지 않는 시간을 사용할지 여부입니다.
        /// </summary>
        /// <remarks>
        /// true일 경우 <c>Time.unscaledDeltaTime</c> 기반으로 진행되는 이동 루프에서 사용됩니다.
        /// </remarks>
        public bool useUnscaledTime;

        /// <summary>
        /// 이동 진행에 적용할 이징 타입입니다.
        /// </summary>
        /// <remarks>
        /// 이 값은 보통 0..1 정규화 진행률에 대해 이징 함수를 적용할 때 사용됩니다.
        /// </remarks>
        public Easing.EaseType easeType;

        /// <summary>
        /// 이동 완료 시 목표 위치로 강제 스냅할지 여부입니다.
        /// </summary>
        /// <remarks>
        /// 부동소수 오차 누적이나 중도 중단/프레임 드랍 등으로 인해
        /// 최종 위치가 미세하게 어긋나는 상황을 방지하기 위해 사용합니다.
        /// </remarks>
        public bool snapToTargetOnComplete;

        /// <summary>
        /// 일반적인 Move 연출에 적합한 기본 옵션을 반환합니다.
        /// </summary>
        public static MoveOptions Default => new MoveOptions
        {
            delay = 0f,
            useUnscaledTime = false,
            easeType = Easing.EaseType.Linear,
            snapToTargetOnComplete = true
        };
    }
}