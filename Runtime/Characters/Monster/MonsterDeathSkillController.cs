using System.Collections;
using UnityEngine;

namespace GGemCo2DCore
{
    /// <summary>
    /// 몬스터가 사망 확정 전에 한 번 실행할 사망 스킬을 관리합니다.
    /// </summary>
    /// <remarks>
    /// 자폭처럼 데미지 판정이 필요한 사망 연출은 일반 사망 VFX가 아니라 몬스터 스킬 런타임 시퀀스로 실행합니다.
    /// </remarks>
    [DisallowMultipleComponent]
    public sealed class MonsterDeathSkillController : MonoBehaviour, IMonsterPoolLifecycle
    {
        private const float SkillFeedbackGraceSeconds = 0.1f;
        private const float SkillFinishTimeoutSeconds = 5f;

        private Monster _owner;
        private int _deathSkillMonsterUid;
        private bool _isExecuting;
        private Coroutine _deathSkillRoutine;

        /// <summary>
        /// 컨트롤러가 제어할 몬스터를 등록합니다.
        /// </summary>
        /// <param name="owner">사망 스킬을 실행할 몬스터입니다.</param>
        public void Initialize(Monster owner)
        {
            _owner = owner;
        }

        /// <summary>
        /// 몬스터 테이블에서 읽은 사망 스킬 UID를 갱신합니다.
        /// </summary>
        /// <param name="skillUid">사망 직전 실행할 skill_monster 테이블 UID입니다.</param>
        public void SetDeathSkillMonsterUid(int skillUid)
        {
            _deathSkillMonsterUid = Mathf.Max(0, skillUid);
        }

        /// <summary>
        /// 사망 스킬이 필요하면 기존 사망 처리를 보류하고 스킬 실행 루틴을 시작합니다.
        /// </summary>
        /// <param name="dieReasonType">사망 원인입니다.</param>
        /// <param name="attacker">사망을 유발한 공격자 오브젝트입니다.</param>
        /// <param name="playDeadAnimation">스킬 종료 후 기본 사망 애니메이션을 재생할지 여부입니다.</param>
        /// <param name="deathPresentation">스킬 종료 후 적용할 사망 연출 요청입니다.</param>
        /// <returns>사망 처리를 보류했으면 <see langword="true"/>입니다.</returns>
        public bool TryBeginDeathSkill(
            CharacterConstants.DieReasonType dieReasonType,
            GameObject attacker,
            bool playDeadAnimation,
            DeathPresentationRequest deathPresentation)
        {
            if (_isExecuting)
                return true;

            if (!CanRunDeathSkill(dieReasonType))
                return false;

            ICharacterSkillDriver driver = ResolveSkillDriver();
            if (driver == null)
                return false;

            _isExecuting = true;
            _deathSkillRoutine = StartCoroutine(RunDeathSkillThenCompleteDeath(
                driver,
                dieReasonType,
                attacker,
                playDeadAnimation,
                deathPresentation));
            return true;
        }

        /// <summary>
        /// 풀에서 다시 대여될 때 진행 중인 사망 스킬 상태를 초기화합니다.
        /// </summary>
        /// <param name="owner">풀에서 대여되는 몬스터입니다.</param>
        public void OnPoolRent(Monster owner)
        {
            ResetRuntimeState();
            _owner = owner;
        }

        /// <summary>
        /// 풀로 반환될 때 대기 중인 사망 스킬 루틴을 정리합니다.
        /// </summary>
        /// <param name="owner">풀로 반환되는 몬스터입니다.</param>
        public void OnPoolReturn(Monster owner)
        {
            ResetRuntimeState();
        }

        /// <summary>
        /// 사망 스킬 실행 가능 여부를 사망 원인과 테이블 값 기준으로 판단합니다.
        /// </summary>
        /// <param name="dieReasonType">사망 원인입니다.</param>
        /// <returns>사망 스킬을 시작할 수 있으면 <see langword="true"/>입니다.</returns>
        private bool CanRunDeathSkill(CharacterConstants.DieReasonType dieReasonType)
        {
            if (_owner == null || _deathSkillMonsterUid <= 0)
                return false;

            // 맵 밖 추락 사망은 전투 맥락이 아니므로 자폭 스킬을 실행하지 않습니다.
            return dieReasonType != CharacterConstants.DieReasonType.EndTilemapY;
        }

        /// <summary>
        /// 현재 몬스터 오브젝트에 부착된 공용 스킬 드라이버를 조회합니다.
        /// </summary>
        /// <returns>스킬 드라이버가 있으면 해당 인스턴스, 없으면 null입니다.</returns>
        private ICharacterSkillDriver ResolveSkillDriver()
        {
            return _owner != null ? _owner.GetComponent<ICharacterSkillDriver>() : null;
        }

        /// <summary>
        /// 기존 스킬을 사망 사유로 정리한 뒤 사망 스킬을 실행하고, 종료 시 실제 사망 처리를 완료합니다.
        /// </summary>
        /// <param name="driver">스킬 실행을 위임할 드라이버입니다.</param>
        /// <param name="dieReasonType">사망 원인입니다.</param>
        /// <param name="attacker">사망을 유발한 공격자 오브젝트입니다.</param>
        /// <param name="playDeadAnimation">스킬 종료 후 기본 사망 애니메이션을 재생할지 여부입니다.</param>
        /// <param name="deathPresentation">스킬 종료 후 적용할 사망 연출 요청입니다.</param>
        /// <returns>Unity 코루틴 열거자입니다.</returns>
        private IEnumerator RunDeathSkillThenCompleteDeath(
            ICharacterSkillDriver driver,
            CharacterConstants.DieReasonType dieReasonType,
            GameObject attacker,
            bool playDeadAnimation,
            DeathPresentationRequest deathPresentation)
        {
            if (driver is ISkillCancelableDriver cancelableDriver)
            {
                cancelableDriver.RequestCancelSkill(SkillCancelReason.Death);
            }

            MonsterSkillTarget target = BuildDeathSkillTarget(attacker);
            SkillUseResult result = TryUseDeathSkill(driver, target);

            if (result.IsStarted)
            {
                yield return WaitForDeathSkillCompletion(driver, _deathSkillMonsterUid);
            }
            else
            {
                GcLogger.LogWarning($"[MonsterDeathSkill] 사망 스킬 실행 실패. monster={_owner?.name}, skillUid={_deathSkillMonsterUid}, reason={result.FailReason}");
            }

            // CharacterBase.Dead()가 사망 보류 플래그를 확정할 수 있도록 최소 1프레임 뒤에 최종 사망 처리를 재개합니다.
            yield return null;
            CompleteDeath(dieReasonType, attacker, playDeadAnimation, deathPresentation);
        }

        /// <summary>
        /// 공격자 또는 현재 바라보는 방향을 기준으로 사망 스킬 타겟 정보를 구성합니다.
        /// </summary>
        /// <param name="attacker">사망을 유발한 공격자 오브젝트입니다.</param>
        /// <returns>몬스터 스킬 드라이버에 전달할 타겟 정보입니다.</returns>
        private MonsterSkillTarget BuildDeathSkillTarget(GameObject attacker)
        {
            Transform lockedTarget = attacker != null ? attacker.transform : _owner != null ? _owner.attackerTransform : null;
            Vector2 forward = ResolveForward(lockedTarget);
            Vector3 groundPoint = _owner != null ? _owner.transform.position : transform.position;
            return new MonsterSkillTarget(lockedTarget, groundPoint, forward);
        }

        /// <summary>
        /// 사망 스킬의 전방 방향을 공격자, 이동 방향, 현재 바라보는 방향 순서로 계산합니다.
        /// </summary>
        /// <param name="lockedTarget">우선 기준으로 사용할 타겟 Transform입니다.</param>
        /// <returns>정규화된 2D 전방 벡터입니다.</returns>
        private Vector2 ResolveForward(Transform lockedTarget)
        {
            if (_owner == null)
                return Vector2.right;

            if (lockedTarget != null)
            {
                Vector2 toTarget = lockedTarget.position - _owner.transform.position;
                if (toTarget.sqrMagnitude > 1e-6f)
                    return toTarget.normalized;
            }

            if (_owner.directionNormalize.sqrMagnitude > 1e-6f)
                return _owner.directionNormalize.normalized;

            return CharacterConstants.FacingToVector2(_owner.CurrentFacing);
        }

        /// <summary>
        /// 몬스터 전용 드라이버가 있으면 몬스터 타겟 컨텍스트로, 없으면 공용 요청 컨텍스트로 사망 스킬을 실행합니다.
        /// </summary>
        /// <param name="driver">스킬 실행을 위임할 드라이버입니다.</param>
        /// <param name="target">사망 스킬 타겟 정보입니다.</param>
        /// <returns>스킬 실행 시도 결과입니다.</returns>
        private SkillUseResult TryUseDeathSkill(ICharacterSkillDriver driver, MonsterSkillTarget target)
        {
            if (driver == null)
                return SkillUseResult.Fail(SkillUseFailReason.InvalidSource);

            if (driver is IMonsterSkillDriver monsterSkillDriver)
                return monsterSkillDriver.TryUseSkill(_deathSkillMonsterUid, target);

            var request = new SkillDriverRequest(target, ConfigCommon.SkillTableSource.Monster);
            return driver.TryUseSkill(_deathSkillMonsterUid, request);
        }

        /// <summary>
        /// 스킬 피드백 인터페이스를 통해 사망 스킬이 끝날 때까지 기다립니다.
        /// </summary>
        /// <param name="driver">스킬 실행을 위임한 드라이버입니다.</param>
        /// <param name="skillUid">대기할 사망 스킬 UID입니다.</param>
        /// <returns>Unity 코루틴 열거자입니다.</returns>
        private IEnumerator WaitForDeathSkillCompletion(ICharacterSkillDriver driver, int skillUid)
        {
            IMonsterSkillDriverFeedback feedback = driver as IMonsterSkillDriverFeedback;
            if (feedback == null && _owner != null)
                feedback = _owner.GetComponent<IMonsterSkillDriverFeedback>();

            if (feedback == null)
            {
                yield return null;
                yield break;
            }

            bool observedRunning = false;
            float startTime = Time.time;
            float deadline = startTime + SkillFinishTimeoutSeconds;

            while (Time.time < deadline)
            {
                if (feedback.ConsumeLastSkillResult(skillUid, out _))
                    yield break;

                bool isRunning = feedback.IsRunningSkill(skillUid);
                observedRunning |= isRunning;

                if (!isRunning && (observedRunning || Time.time - startTime >= SkillFeedbackGraceSeconds))
                    yield break;

                yield return null;
            }

            GcLogger.LogWarning($"[MonsterDeathSkill] 사망 스킬 완료 대기 시간이 초과되었습니다. monster={_owner?.name}, skillUid={skillUid}");
        }

        /// <summary>
        /// 사망 스킬 대기 루틴을 정리하고 몬스터의 실제 사망 처리를 재개합니다.
        /// </summary>
        /// <param name="dieReasonType">사망 원인입니다.</param>
        /// <param name="attacker">사망을 유발한 공격자 오브젝트입니다.</param>
        /// <param name="playDeadAnimation">기본 사망 애니메이션 폴백 허용 여부입니다.</param>
        /// <param name="deathPresentation">사망 원인별 전용 연출 요청입니다.</param>
        private void CompleteDeath(
            CharacterConstants.DieReasonType dieReasonType,
            GameObject attacker,
            bool playDeadAnimation,
            DeathPresentationRequest deathPresentation)
        {
            _isExecuting = false;
            _deathSkillRoutine = null;
            _owner?.CompleteDeathSkillAction(dieReasonType, attacker, playDeadAnimation, deathPresentation);
        }

        /// <summary>
        /// 실행 중인 사망 스킬 루틴과 내부 상태를 초기화합니다.
        /// </summary>
        private void ResetRuntimeState()
        {
            if (_deathSkillRoutine != null)
            {
                StopCoroutine(_deathSkillRoutine);
                _deathSkillRoutine = null;
            }

            _isExecuting = false;
        }
    }
}
