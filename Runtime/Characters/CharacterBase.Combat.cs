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

        /// <summary>
        /// 캐릭터를 사망 상태로 전환하고 사망 후처리를 실행합니다.
        /// </summary>
        /// <param name="dieReasonType">사망 원인입니다.</param>
        /// <param name="attacker">사망을 유발한 공격자 오브젝트입니다.</param>
        /// <param name="playDeadAnimation">사망 애니메이션 재생 여부.</param>
        public void Dead(CharacterConstants.DieReasonType dieReasonType = CharacterConstants.DieReasonType.None, GameObject attacker = null, bool playDeadAnimation = true)
        {
            SetStatusDead();
            SetBattleStatusNone();
            if (dieReasonType != CharacterConstants.DieReasonType.EndTilemapY && playDeadAnimation)
                CharacterAnimationController.PlayDeadAnimation();

            AffectRuntimeBridge.RemoveAll(gameObject);
            OnDead(dieReasonType, attacker);
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
    }
}
