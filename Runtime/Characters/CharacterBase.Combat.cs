using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GGemCo2DCore
{
    /// <summary>
    /// <see cref="CharacterBase"/>의 전투, 피격, Crowd Control 연동을 담당하는 partial 구현입니다.
    /// </summary>
    public partial class CharacterBase
    {
        private CharacterConstants.AttackType _attackType;
        private Coroutine _deathPresentationFreezeCoroutine;
        private bool _isDeathPending;

        /// <summary>
        /// 사망 확정 전 액션이 실행 중이어서 최종 사망 처리가 잠시 보류되었는지 여부입니다.
        /// </summary>
        public bool IsDeathPending => _isDeathPending;

        /// <summary>
        /// 캐릭터를 사망 상태로 전환하고 사망 후처리를 실행합니다.
        /// </summary>
        /// <param name="dieReasonType">사망 원인입니다.</param>
        /// <param name="attacker">사망을 유발한 공격자 오브젝트입니다.</param>
        /// <param name="playDeadAnimation">기본 사망 애니메이션 폴백 허용 여부입니다.</param>
        public void Dead(CharacterConstants.DieReasonType dieReasonType = CharacterConstants.DieReasonType.None, GameObject attacker = null, bool playDeadAnimation = true)
        {
            Dead(dieReasonType, attacker, playDeadAnimation, null);
        }

        /// <summary>
        /// 캐릭터를 사망 상태로 전환하고 원인별 사망 연출을 포함한 후처리를 실행합니다.
        /// </summary>
        /// <param name="dieReasonType">사망 원인입니다.</param>
        /// <param name="attacker">사망을 유발한 공격자 오브젝트입니다.</param>
        /// <param name="playDeadAnimation">기본 사망 애니메이션 폴백 허용 여부입니다.</param>
        /// <param name="deathPresentation">사망 원인별 전용 연출 요청입니다.</param>
        /// <remarks>
        /// Core는 Affect/Skill 같은 상위 패키지를 직접 알지 않고,
        /// 상위 패키지가 변환해 전달한 <see cref="DeathPresentationRequest"/>만 실행합니다.
        /// </remarks>
        public void Dead(
            CharacterConstants.DieReasonType dieReasonType,
            GameObject attacker,
            bool playDeadAnimation,
            DeathPresentationRequest deathPresentation)
        {
            if (IsStatusDead() || _isDeathPending)
                return;

            if (TryBeginPreDeathAction(dieReasonType, attacker, playDeadAnimation, deathPresentation))
            {
                _isDeathPending = true;
                NotifyCharacterBodyDeathStateChanged();
                return;
            }

            CompleteDeath(dieReasonType, attacker, playDeadAnimation, deathPresentation);
        }

        /// <summary>
        /// 보류 중인 사망 처리를 즉시 완료합니다.
        /// </summary>
        /// <param name="dieReasonType">사망 원인입니다.</param>
        /// <param name="attacker">사망을 유발한 공격자 오브젝트입니다.</param>
        /// <param name="playDeadAnimation">기본 사망 애니메이션 폴백 허용 여부입니다.</param>
        /// <param name="deathPresentation">사망 원인별 전용 연출 요청입니다.</param>
        /// <remarks>
        /// 사망 직전 스킬처럼 비동기 선처리가 필요한 하위 클래스가 액션 완료 후 호출합니다.
        /// </remarks>
        protected void CompleteDeferredDeath(
            CharacterConstants.DieReasonType dieReasonType,
            GameObject attacker,
            bool playDeadAnimation,
            DeathPresentationRequest deathPresentation)
        {
            CompleteDeath(dieReasonType, attacker, playDeadAnimation, deathPresentation);
        }

        /// <summary>
        /// 사망 확정 전에 실행할 액션을 시작할 수 있는 확장 지점입니다.
        /// </summary>
        /// <param name="dieReasonType">사망 원인입니다.</param>
        /// <param name="attacker">사망을 유발한 공격자 오브젝트입니다.</param>
        /// <param name="playDeadAnimation">기본 사망 애니메이션 폴백 허용 여부입니다.</param>
        /// <param name="deathPresentation">사망 원인별 전용 연출 요청입니다.</param>
        /// <returns>사망 처리를 보류하고 선처리 액션을 기다려야 하면 <see langword="true"/>입니다.</returns>
        protected virtual bool TryBeginPreDeathAction(
            CharacterConstants.DieReasonType dieReasonType,
            GameObject attacker,
            bool playDeadAnimation,
            DeathPresentationRequest deathPresentation)
        {
            return false;
        }

        /// <summary>
        /// 보류 상태와 관계없이 실제 사망 상태 전환, 연출, 후처리를 수행합니다.
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
            if (IsStatusDead())
                return;

            _isDeathPending = false;
            SetStatusDead();
            SetBattleStatusNone();
            NotifyCharacterBodyDeathStateChanged();

            if (dieReasonType != CharacterConstants.DieReasonType.EndTilemapY)
            {
                PlayDeathPresentation(deathPresentation, playDeadAnimation);
            }

            AffectRuntimeBridge.RemoveAll(gameObject);
            OnDead(dieReasonType, attacker);
        }

        /// <summary>
        /// 풀 재사용 또는 런타임 초기화 시 사망 보류 플래그를 초기 상태로 되돌립니다.
        /// </summary>
        protected void ClearPendingDeathState()
        {
            _isDeathPending = false;
            RestoreCharacterBodyCollisionAliveState();
        }

        /// <summary>
        /// 사망 연출 요청을 해석하여 전용 애니메이션, VFX, 마지막 프레임 고정 정책을 실행합니다.
        /// </summary>
        /// <param name="request">실행할 사망 연출 요청입니다.</param>
        /// <param name="allowDefaultDeathAnimation">전용 애니메이션이 없을 때 기본 사망 애니메이션을 재생할지 여부입니다.</param>
        /// <remarks>
        /// 전용 사망 애니메이션이 존재하면 기본 사망 애니메이션보다 우선합니다.
        /// 전용 애니메이션이 없고 기본 폴백도 금지되어 있으면 애니메이션을 재생하지 않습니다.
        /// </remarks>
        private void PlayDeathPresentation(DeathPresentationRequest request, bool allowDefaultDeathAnimation)
        {
            string playedAnimationName = null;

            if (request != null && request.IsConfigured)
            {
                playedAnimationName = TryPlayDeathPresentationAnimation(request);
                TryPlayDeathPresentationVfx(request);
            }

            if (string.IsNullOrWhiteSpace(playedAnimationName) &&
                allowDefaultDeathAnimation &&
                (request == null || !request.SuppressDefaultDeathAnimation))
            {
                CharacterAnimationController?.PlayDeadAnimation();
                playedAnimationName = ICharacterAnimationController.DeadAnim;
            }

            if (request != null && request.FreezeLastFrame && !string.IsNullOrWhiteSpace(playedAnimationName))
            {
                StartDeathPresentationFreeze(playedAnimationName);
            }
        }

        /// <summary>
        /// 사망 연출 요청에 지정된 캐릭터 애니메이션을 재생합니다.
        /// </summary>
        /// <param name="request">애니메이션 이름을 포함한 사망 연출 요청입니다.</param>
        /// <returns>실제로 재생한 애니메이션 이름입니다. 재생하지 못하면 빈 값을 반환합니다.</returns>
        private string TryPlayDeathPresentationAnimation(DeathPresentationRequest request)
        {
            if (request == null || CharacterAnimationController == null)
                return null;

            string animationName = request.AnimationName;
            if (string.IsNullOrWhiteSpace(animationName))
                return null;

            if (!CharacterAnimationController.HasAnimation(animationName))
            {
                GcLogger.LogWarning($"[DeathPresentation] 사망 애니메이션을 찾을 수 없습니다. character={name}, animation={animationName}");
                return null;
            }

            CharacterAnimationController.PlayCharacterAnimation(animationName, loop: false);
            return animationName;
        }

        /// <summary>
        /// 사망 연출 요청에 지정된 VFX를 현재 캐릭터 위치에 재생합니다.
        /// </summary>
        /// <param name="request">VFX UID와 위치 정책을 포함한 사망 연출 요청입니다.</param>
        private void TryPlayDeathPresentationVfx(DeathPresentationRequest request)
        {
            if (request == null || request.VfxUid <= 0)
                return;

            SceneGame scene = SceneGame.Instance;
            if (scene == null || scene.VfxManager == null)
                return;

            var spawnRequest = new VfxSpawnRequest
            {
                VfxUid = request.VfxUid,
                Owner = this,
                Target = this,
                WorldPosition = transform.position,
                DurationOverride = request.VfxDurationOverride,
                ForceOneShot = !request.FollowVfxTarget,
                ScaleOverride = request.VfxScale,
                PositionY = request.VfxOffsetY,
                PositionYType = request.VfxPositionYType,
            };

            if (request.HasVfxSortingLayerOverride)
                spawnRequest.SortingLayerOverride = request.VfxSortingLayerKey;

            if (request.FollowVfxTarget)
            {
                spawnRequest.FollowTarget = this;
            }
            else
            {
                spawnRequest.LifecycleTypeOverride = VfxConstants.LifecycleType.AutoRelease;
            }

            scene.VfxManager.CreateVfx(spawnRequest);
        }

        /// <summary>
        /// 사망 애니메이션 재생 길이에 맞춰 마지막 프레임 고정 코루틴을 시작합니다.
        /// </summary>
        /// <param name="animationName">마지막 프레임에 고정할 애니메이션 이름입니다.</param>
        private void StartDeathPresentationFreeze(string animationName)
        {
            if (CharacterAnimationController == null || string.IsNullOrWhiteSpace(animationName))
                return;

            if (_deathPresentationFreezeCoroutine != null)
            {
                StopCoroutine(_deathPresentationFreezeCoroutine);
                _deathPresentationFreezeCoroutine = null;
            }

            float duration = CharacterAnimationController.GetCharacterAnimationDuration(animationName, isMilliseconds: false);
            if (duration <= 0f)
            {
                CharacterAnimationController.FreezeCurrentAnimationAtLastFrame();
                return;
            }

            _deathPresentationFreezeCoroutine = StartCoroutine(FreezeDeathPresentationAtLastFrame(duration));
        }

        /// <summary>
        /// 지정한 시간 대기 후 현재 사망 애니메이션을 마지막 프레임으로 고정합니다.
        /// </summary>
        /// <param name="delaySeconds">마지막 프레임 고정 전 대기할 시간입니다.</param>
        /// <returns>코루틴 이터레이터입니다.</returns>
        private IEnumerator FreezeDeathPresentationAtLastFrame(float delaySeconds)
        {
            yield return new WaitForSeconds(delaySeconds);
            CharacterAnimationController?.FreezeCurrentAnimationAtLastFrame();
            _deathPresentationFreezeCoroutine = null;
        }

        /// <summary>
        /// 캐릭터 사망 시 파생 클래스가 추가 동작을 구현할 수 있는 확장 지점입니다.
        /// </summary>
        /// <param name="dieReasonType">사망 원인입니다.</param>
        /// <param name="attacker">사망을 유발한 공격자 오브젝트입니다.</param>
        protected virtual void OnDead(CharacterConstants.DieReasonType dieReasonType = CharacterConstants.DieReasonType.None, GameObject attacker = null)
        {
            if (dieReasonType == CharacterConstants.DieReasonType.EndTilemapY)
            {
                Destroy(gameObject);
            }
        }

        /// <summary>
        /// 전달된 데미지 메타데이터를 사용해 피격 처리를 수행합니다.
        /// </summary>
        /// <param name="metadataDamage">데미지 계산과 부가 효과에 사용할 메타데이터입니다.</param>
        public void TakeDamage(MetadataDamage metadataDamage)
        {
            _characterDamageController.TakeDamage(metadataDamage);
        }

        /// <summary>
        /// 피격 직후 파생 클래스가 추가 반응을 구현할 수 있는 확장 지점입니다.
        /// </summary>
        /// <param name="attacker">피격을 유발한 공격자 오브젝트입니다.</param>
        public virtual void OnDamage(GameObject attacker)
        {
        }

        /// <summary>
        /// 마지막 공격 대상을 기록합니다.
        /// </summary>
        /// <param name="attacker">기록할 공격자 Transform입니다.</param>
        public void SetAttackerTarget(Transform attacker)
        {
            attackerTransform = attacker;
        }

        /// <summary>
        /// 기록된 공격 대상이 사망 상태인지 확인합니다.
        /// </summary>
        /// <returns>공격 대상이 존재하고 사망 상태이면 <see langword="true"/>를 반환합니다.</returns>
        public bool IsAttackerStatusDead()
        {
            if (attackerTransform == null || attackerTransform.GetComponent<CharacterBase>() == null) return false;
            return attackerTransform.GetComponent<CharacterBase>().IsStatusDead();
        }

        /// <summary>
        /// 캐릭터의 공격 성향 타입을 반환합니다.
        /// </summary>
        /// <returns>현재 공격 타입입니다.</returns>
        public CharacterConstants.AttackType GetAttackType()
        {
            return _attackType;
        }

        /// <summary>
        /// 캐릭터의 공격 성향 타입을 설정합니다.
        /// </summary>
        /// <param name="attackType">적용할 공격 타입입니다.</param>
        protected void SetAttackType(CharacterConstants.AttackType attackType)
        {
            _attackType = attackType;
        }

        /// <summary>
        /// 지정한 Affect를 현재 캐릭터에 적용합니다.
        /// </summary>
        /// <param name="affectUid">적용할 Affect 식별자입니다.</param>
        /// <param name="duration">적용 지속 시간입니다.</param>
        public void AddAffect(int affectUid, float duration = 0)
        {
            AffectRuntimeBridge.ApplyAffect(gameObject, affectUid, duration);
        }

        /// <summary>
        /// 원인 오브젝트를 포함해 Affect를 현재 캐릭터에 적용합니다.
        /// </summary>
        /// <param name="affectUid">적용할 Affect 식별자입니다.</param>
        /// <param name="source">Affect 원인으로 기록할 오브젝트입니다.</param>
        /// <param name="duration">적용 지속 시간입니다.</param>
        public void AddAffect(int affectUid, GameObject source, float duration = 0)
        {
            AffectRuntimeBridge.ApplyAffect(gameObject, affectUid, source, duration);
        }

        /// <summary>
        /// 메타데이터를 기반으로 발사체 생성을 요청합니다.
        /// </summary>
        /// <param name="metadataProjectile">발사체 생성과 발사에 사용할 메타데이터입니다.</param>
        public void LaunchProjectile(MetadataProjectile metadataProjectile)
        {
            if (_projectileController == null) return;
            _projectileController.Launch(metadataProjectile);
        }

        /// <summary>
        /// 메타데이터를 기반으로 레이저 생성을 요청합니다.
        /// </summary>
        /// <param name="metadataLaser">레이저 생성과 발사에 사용할 메타데이터입니다.</param>
        public void LaunchLaser(MetadataLaser metadataLaser)
        {
            if (_laserController == null) return;
            _laserController.Launch(metadataLaser);
        }

        /// <summary>
        /// 단일 Crowd Control 효과를 현재 캐릭터에 적용합니다.
        /// </summary>
        /// <param name="crowdControlUid">적용할 Crowd Control 식별자입니다.</param>
        /// <param name="metadataDamageAttacker">원인 공격자 오브젝트입니다.</param>
        /// <param name="isEndCharacterStop">CC 완료 후 CharacterBase.Stop 호출 여부.</param>
        public void ApplyCrowdControl(int crowdControlUid, GameObject metadataDamageAttacker, bool isEndCharacterStop = false)
        {
            if (GcLogger.IsNull(_crowdControlController, nameof(CharacterCrowdControlController))) return;
            _crowdControlController.ApplyCrowdControlByUid(crowdControlUid, metadataDamageAttacker, isEndCharacterStop);
        }

        /// <summary>
        /// 여러 Crowd Control 효과를 순차적으로 현재 캐릭터에 적용합니다.
        /// </summary>
        /// <param name="crowdControlUids">적용할 Crowd Control 식별자 목록입니다.</param>
        /// <param name="metadataDamageAttacker">원인 공격자 오브젝트입니다.</param>
        /// <param name="isEndCharacterStop">CC 완료 후 CharacterBase.Stop 호출 여부.</param>
        public void ApplyCrowdControlSequence(IReadOnlyList<int> crowdControlUids, GameObject metadataDamageAttacker, bool isEndCharacterStop)
        {
            if (GcLogger.IsNull(_crowdControlController, nameof(CharacterCrowdControlController))) return;
            _crowdControlController.ApplyCrowdControlSequenceByUid(crowdControlUids, metadataDamageAttacker, gameObject, isEndCharacterStop);
        }

        /// <summary>
        /// 런타임에서 조립된 Crowd Control 효과를 순차적으로 현재 캐릭터에 적용합니다.
        /// </summary>
        /// <param name="crowdControls">테이블 원본을 복제하거나 1회성 오버라이드를 포함한 Crowd Control 데이터 목록입니다.</param>
        /// <param name="metadataDamageAttacker">원인 공격자 오브젝트입니다.</param>
        /// <param name="isEndCharacterStop">CC 완료 후 CharacterBase.Stop 호출 여부입니다.</param>
        public void ApplyCrowdControlSequence(IReadOnlyList<CrowdControlRuntimeData> crowdControls, GameObject metadataDamageAttacker, bool isEndCharacterStop)
        {
            if (GcLogger.IsNull(_crowdControlController, nameof(CharacterCrowdControlController))) return;
            _crowdControlController.ApplyCrowdControlSequence(crowdControls, metadataDamageAttacker, gameObject, isEndCharacterStop);
        }

        /// <summary>
        /// 현재 캐릭터에 적용 중이거나 예약된 Crowd Control을 즉시 중단합니다.
        /// </summary>
        /// <param name="reason">Crowd Control 중단 요청 사유입니다.</param>
        /// <param name="isEndCharacterStop">중단 후 <see cref="Stop(bool)"/>을 강제로 호출할지 여부입니다.</param>
        /// <returns>중단할 Crowd Control 상태가 존재하여 정리를 수행했으면 <see langword="true"/>를 반환합니다.</returns>
        /// <remarks>
        /// 상위 패키지는 <see cref="CharacterCrowdControlController"/>의 내부 구현을 직접 다루지 않고,
        /// 캐릭터 표준 API를 통해 Crowd Control 중단만 요청합니다.
        /// </remarks>
        public bool TryStopCrowdControl(CrowdControlStopReason reason, bool isEndCharacterStop = false)
        {
            if (GcLogger.IsNull(_crowdControlController, nameof(CharacterCrowdControlController)))
                return false;

            return _crowdControlController.TryStopCrowdControl(reason, isEndCharacterStop);
        }
    }
}
