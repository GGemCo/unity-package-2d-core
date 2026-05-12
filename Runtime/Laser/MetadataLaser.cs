using UnityEngine;

namespace GGemCo2DCore
{
    /// <summary>
    /// 레이저 발사 시점의 런타임 파라미터입니다.
    /// - 정적 정의는 기존 projectile 테이블(Type=Laser)을 재사용합니다.
    /// - 동적 값(데미지/지속 시간/사거리/표현 오버라이드)은 이 메타데이터로 전달합니다.
    /// </summary>
    public sealed class MetadataLaser
    {
        public readonly int Uid;
        public readonly CharacterBase Owner;
        public readonly CharacterBase Target;
        public readonly bool UseTargetPositionOverride;
        public readonly Vector2 TargetPositionOverride;
        public readonly ConfigCommon.DamageType DamageType;
        public readonly long Damage;
        public readonly int SkillUid;
        public readonly int AttackId;
        public readonly bool AllowSkillChainOnConfirmedDamage;
        public readonly ElementGaugeApplication[] ElementGaugeApplications;
        public readonly float ScaleMultiplier;
        public readonly ProjectileConstants.ProjectileVisualType VisualType;
        public readonly Sprite VisualSprite;
        public readonly RuntimeAnimatorController VisualAnimatorController;
        public readonly int VisualVfxUidOverride;
        public readonly bool UseDurationOverride;
        public readonly float DurationOverride;
        public readonly bool UseTickIntervalOverride;
        public readonly float TickIntervalOverride;
        public readonly bool UseMaxDistanceOverride;
        public readonly float MaxDistanceOverride;
        public readonly bool UpdateAimContinuously;

        /// <summary>
        /// 레이저 런타임 메타데이터를 생성합니다.
        /// </summary>
        /// <param name="uid">기존 projectile 테이블(Type=Laser) UID입니다.</param>
        /// <param name="damageType">적용할 데미지 타입입니다.</param>
        /// <param name="damage">적용할 데미지 값입니다.</param>
        /// <param name="target">고정 타겟 캐릭터입니다.</param>
        /// <param name="owner">레이저 시전자입니다.</param>
        /// <param name="scaleMultiplier">비주얼 스케일 배율입니다.</param>
        /// <param name="visualType">비주얼 표현 방식 오버라이드입니다.</param>
        /// <param name="visualSprite">스프라이트 비주얼 오버라이드입니다.</param>
        /// <param name="visualAnimatorController">애니메이터 비주얼 오버라이드입니다.</param>
        /// <param name="visualVfxUidOverride">VFX UID 오버라이드입니다.</param>
        /// <param name="useTargetPositionOverride">좌표 타겟 오버라이드 사용 여부입니다.</param>
        /// <param name="targetPositionOverride">좌표 타겟 오버라이드 값입니다.</param>
        /// <param name="skillUid">발생시킨 스킬 UID입니다.</param>
        /// <param name="attackId">연계/피격 추적용 공격 ID입니다.</param>
        /// <param name="allowSkillChainOnConfirmedDamage">실제 데미지 확정 시 스킬 연계를 열지 여부입니다.</param>
        /// <param name="elementGaugeApplications">적중 시 추가할 속성 게이지 목록입니다.</param>
        /// <param name="useDurationOverride">지속 시간 오버라이드 사용 여부입니다.</param>
        /// <param name="durationOverride">지속 시간 오버라이드 값입니다.</param>
        /// <param name="useTickIntervalOverride">틱 간격 오버라이드 사용 여부입니다.</param>
        /// <param name="tickIntervalOverride">틱 간격 오버라이드 값입니다.</param>
        /// <param name="useMaxDistanceOverride">최대 사거리 오버라이드 사용 여부입니다.</param>
        /// <param name="maxDistanceOverride">최대 사거리 오버라이드 값입니다.</param>
        /// <param name="updateAimContinuously">지속 시간 동안 에임을 계속 갱신할지 여부입니다.</param>
        public MetadataLaser(
            int uid,
            ConfigCommon.DamageType damageType,
            long damage,
            CharacterBase target = null,
            CharacterBase owner = null,
            float scaleMultiplier = 1f,
            ProjectileConstants.ProjectileVisualType visualType = ProjectileConstants.ProjectileVisualType.Default,
            Sprite visualSprite = null,
            RuntimeAnimatorController visualAnimatorController = null,
            int visualVfxUidOverride = 0,
            bool useTargetPositionOverride = false,
            Vector2 targetPositionOverride = default,
            int skillUid = 0,
            int attackId = 0,
            bool allowSkillChainOnConfirmedDamage = false,
            ElementGaugeApplication[] elementGaugeApplications = null,
            bool useDurationOverride = false,
            float durationOverride = 0.25f,
            bool useTickIntervalOverride = false,
            float tickIntervalOverride = 0f,
            bool useMaxDistanceOverride = false,
            float maxDistanceOverride = 0f,
            bool updateAimContinuously = false)
        {
            Uid = uid;
            DamageType = damageType;
            Damage = damage;
            Target = target;
            Owner = owner;
            ScaleMultiplier = Mathf.Max(0.01f, scaleMultiplier);
            VisualType = visualType;
            VisualSprite = visualSprite;
            VisualAnimatorController = visualAnimatorController;
            VisualVfxUidOverride = visualVfxUidOverride;
            UseTargetPositionOverride = useTargetPositionOverride;
            TargetPositionOverride = targetPositionOverride;
            SkillUid = skillUid;
            AttackId = attackId;
            AllowSkillChainOnConfirmedDamage = allowSkillChainOnConfirmedDamage;
            ElementGaugeApplications = elementGaugeApplications;
            UseDurationOverride = useDurationOverride;
            DurationOverride = durationOverride;
            UseTickIntervalOverride = useTickIntervalOverride;
            TickIntervalOverride = Mathf.Max(0f, tickIntervalOverride);
            UseMaxDistanceOverride = useMaxDistanceOverride;
            MaxDistanceOverride = Mathf.Max(0f, maxDistanceOverride);
            UpdateAimContinuously = updateAimContinuously;
        }

        /// <summary>
        /// 레이저 비주얼 시스템 재사용을 위해 Projectile 비주얼 메타데이터로 변환합니다.
        /// </summary>
        /// <returns>ProjectileVisualFactory에서 사용할 비주얼 전용 메타데이터입니다.</returns>
        public MetadataProjectile ToVisualMetadata()
        {
            return new MetadataProjectile(
                uid: Uid,
                damageType: DamageType,
                damage: Damage,
                target: Target,
                owner: Owner,
                speedMultiplier: 1f,
                scaleMultiplier: ScaleMultiplier,
                visualType: VisualType,
                visualSprite: VisualSprite,
                visualAnimatorController: VisualAnimatorController,
                visualVfxUidOverride: VisualVfxUidOverride,
                useTargetPositionOverride: UseTargetPositionOverride,
                targetPositionOverride: TargetPositionOverride,
                skillUid: SkillUid,
                attackId: AttackId,
                allowSkillChainOnConfirmedDamage: AllowSkillChainOnConfirmedDamage,
                elementGaugeApplications: ElementGaugeApplications);
        }
    }
}
