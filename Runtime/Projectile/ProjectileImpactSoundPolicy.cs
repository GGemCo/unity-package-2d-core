using System;

namespace GGemCo2DCore
{
    /// <summary>
    /// 프로젝타일 충돌 사운드를 재생할 수명주기 지점을 정의합니다.
    /// 여러 지점을 함께 사용해야 하는 경우 비트 플래그로 조합합니다.
    /// </summary>
    [Flags]
    public enum ProjectileImpactSoundTrigger
    {
        /// <summary>
        /// 충돌 사운드를 재생하지 않습니다.
        /// </summary>
        None = 0,

        /// <summary>
        /// 프로젝타일이 데미지 대상 캐릭터와 충돌했을 때 재생합니다.
        /// </summary>
        TargetHit = 1 << 0,

        /// <summary>
        /// 프로젝타일이 설정된 환경 Collider와 충돌했을 때 재생합니다.
        /// </summary>
        EnvironmentHit = 1 << 1,

        /// <summary>
        /// 프로젝타일이 경로의 종착 지점에 처음 도달했을 때 재생합니다.
        /// </summary>
        Arrived = 1 << 2,
    }

    /// <summary>
    /// 하나의 프로젝타일에서 충돌 사운드를 반복 재생하는 방식을 정의합니다.
    /// </summary>
    public enum ProjectileImpactSoundRepeatPolicy
    {
        /// <summary>
        /// 활성화된 트리거 중 가장 먼저 발생한 지점에서 한 번만 재생합니다.
        /// 타겟 충돌과 경로 도착이 같은 프레임에 겹치는 폭발형 프로젝타일에 적합합니다.
        /// </summary>
        OncePerProjectile = 0,

        /// <summary>
        /// 활성화된 각 충돌 또는 도착 지점마다 재생합니다.
        /// 관통형 프로젝타일처럼 여러 대상의 충돌음을 각각 재생해야 할 때 사용합니다.
        /// </summary>
        PerImpact = 1,
    }
}
