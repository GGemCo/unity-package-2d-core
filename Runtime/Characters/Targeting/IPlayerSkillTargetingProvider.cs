using UnityEngine;

namespace GGemCo2DCore
{
    /// <summary>
    /// 플레이어 입력/UI 레이어가 스킬 UID를 기준으로 실제 실행 요청 컨텍스트를 구성할 수 있도록 제공하는 포트입니다.
    /// Core는 구체적인 락온/조준/스킬 테이블 구현을 알지 않고, 상위 패키지가 이를 구현합니다.
    /// </summary>
    public interface IPlayerSkillTargetingProvider
    {
        /// <summary>
        /// 지정한 스킬 UID와 캐스터 기준으로 실행 가능한 요청 컨텍스트를 구성합니다.
        /// </summary>
        /// <param name="caster">스킬을 사용하는 캐스터입니다.</param>
        /// <param name="skillUid">사용할 스킬 UID입니다.</param>
        /// <param name="source">스킬 테이블 소스입니다.</param>
        /// <param name="request">구성된 드라이버 요청입니다.</param>
        /// <returns>요청 구성이 성공하면 true, 타겟을 확보하지 못했거나 정의를 해석할 수 없으면 false입니다.</returns>
        bool TryBuildSkillRequest(GameObject caster, int skillUid, ConfigCommon.SkillTableSource source,
            out SkillDriverRequest request);

        /// <summary>
        /// 지정한 스킬 UID와 캐스터 기준으로 실행 가능한 요청 컨텍스트를 구성하고, 실패 시 원인을 함께 반환합니다.
        /// </summary>
        /// <param name="caster">스킬을 사용하는 캐스터입니다.</param>
        /// <param name="skillUid">사용할 스킬 UID입니다.</param>
        /// <param name="source">스킬 테이블 소스입니다.</param>
        /// <param name="request">구성된 드라이버 요청입니다.</param>
        /// <param name="failReason">요청 구성이 실패한 경우의 원인입니다.</param>
        /// <returns>요청 구성이 성공하면 true, 실패하면 false입니다.</returns>
        bool TryBuildSkillRequest(GameObject caster, int skillUid, ConfigCommon.SkillTableSource source,
            out SkillDriverRequest request, out SkillUseFailReason failReason);
    }

    /// <summary>
    /// 명시적으로 고정된 타겟을 제공하는 선택적 포트입니다.
    /// </summary>
    public interface IPlayerLockedTargetProvider
    {
        /// <summary>
        /// 현재 유효한 락온 타겟을 반환합니다.
        /// </summary>
        bool TryGetLockedTarget(GameObject caster, out Transform lockedTarget);
    }

    /// <summary>
    /// 조준 방향/지면 좌표를 제공하는 선택적 포트입니다.
    /// </summary>
    public interface IPlayerAimProvider
    {
        /// <summary>
        /// 현재 스킬 조준 방향을 반환합니다.
        /// </summary>
        bool TryGetAimDirection(GameObject caster, out Vector2 direction);

        /// <summary>
        /// 현재 스킬 조준 지면 좌표를 반환합니다.
        /// </summary>
        bool TryGetAimGroundPoint(GameObject caster, out Vector3 groundPoint);
    }
}
