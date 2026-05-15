using UnityEngine;

namespace GGemCo2DCore
{
    /// <summary>
    /// 레이저 발사 시점의 런타임 파라미터입니다.
    /// - 정적 정의는 laser 테이블을 참조합니다.
    /// - 동적 값(데미지/지속 시간/사거리/표현 오버라이드)은 이 메타데이터로 전달합니다.
    /// </summary>
    public sealed class MetadataLaser
    {
        public readonly int Uid;

        /// <summary>
        /// 레이저 정적 테이블 UID입니다.
        /// </summary>
        public int LaserUid => Uid;
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
        public readonly bool UseDamageTimingOverride;
        public readonly float DamageStartDelayOverride;
        public readonly float DamageActiveDurationOverride;
        public readonly float DamageTickIntervalOverride;
        public readonly bool DamageTickOnStartOverride;
        public readonly bool UseMaxDistanceOverride;
        public readonly float MaxDistanceOverride;
        public readonly bool UpdateAimContinuously;
        public readonly bool UseRaycastDirectionModeOverride;
        public readonly LaserConstants.RaycastDirectionMode RaycastDirectionModeOverride;
        public readonly bool UseRaycastAngleOverride;
        public readonly float RaycastAngleOverrideDeg;
        public readonly bool UseVfxAngleSyncModeOverride;
        public readonly LaserConstants.VfxAngleSyncMode VfxAngleSyncModeOverride;
        public readonly LaserConstants.StartPositionOverrideMode StartPositionOverrideMode;
        public readonly Vector2 StartPositionOverride;
        public readonly LaserConstants.StartPointUpdateMode StartPointUpdateMode;
        public readonly bool UseCasterFlipStartOffsetX;

        /// <summary>
        /// 레이저 런타임 메타데이터를 생성합니다.
        /// </summary>
        /// <param name="uid">laser 테이블 UID입니다.</param>
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
        /// <param name="useDamageTimingOverride">데미지 타이밍 오버라이드 사용 여부입니다.</param>
        /// <param name="damageStartDelayOverride">발사 후 데미지 판정을 시작하기까지 대기할 시간입니다.</param>
        /// <param name="damageActiveDurationOverride">데미지 판정을 유지할 시간입니다. 0 이하이면 레이저 종료까지 유지합니다.</param>
        /// <param name="damageTickIntervalOverride">같은 대상에게 반복 데미지를 줄 간격입니다. 0이면 진입 시 1회만 적용합니다.</param>
        /// <param name="damageTickOnStartOverride">데미지 활성 구간에서 처음 감지된 대상에게 즉시 데미지를 줄지 여부입니다.</param>
        /// <param name="useMaxDistanceOverride">최대 사거리 오버라이드 사용 여부입니다.</param>
        /// <param name="maxDistanceOverride">최대 사거리 오버라이드 값입니다.</param>
        /// <param name="updateAimContinuously">지속 시간 동안 에임을 계속 갱신할지 여부입니다.</param>
        /// <param name="useRaycastDirectionModeOverride">레이캐스트 방향 모드 오버라이드 사용 여부입니다.</param>
        /// <param name="raycastDirectionModeOverride">레이캐스트 방향 모드 오버라이드 값입니다.</param>
        /// <param name="useRaycastAngleOverride">레이캐스트 각도 오버라이드 사용 여부입니다.</param>
        /// <param name="raycastAngleOverrideDeg">레이캐스트 각도 오버라이드 값(도)입니다.</param>
        /// <param name="useVfxAngleSyncModeOverride">VFX 각도 동기화 모드 오버라이드 사용 여부입니다.</param>
        /// <param name="vfxAngleSyncModeOverride">VFX 각도 동기화 모드 오버라이드 값입니다.</param>
        /// <param name="startPositionOverrideMode">시작점 오버라이드 해석 방식입니다.</param>
        /// <param name="startPositionOverride">시작점 오버라이드 값입니다.</param>
        /// <param name="startPointUpdateMode">시작점 갱신 방식입니다.</param>
        /// <param name="useCasterFlipStartOffsetX">시전자 좌우 반전 상태에 따라 시작점 오프셋 X 값을 반전할지 여부입니다.</param>
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
            bool useDamageTimingOverride = false,
            float damageStartDelayOverride = 0f,
            float damageActiveDurationOverride = -1f,
            float damageTickIntervalOverride = 0f,
            bool damageTickOnStartOverride = true,
            bool useMaxDistanceOverride = false,
            float maxDistanceOverride = 0f,
            bool updateAimContinuously = false,
            bool useRaycastDirectionModeOverride = false,
            LaserConstants.RaycastDirectionMode raycastDirectionModeOverride = LaserConstants.RaycastDirectionMode.TowardTarget,
            bool useRaycastAngleOverride = false,
            float raycastAngleOverrideDeg = 0f,
            bool useVfxAngleSyncModeOverride = false,
            LaserConstants.VfxAngleSyncMode vfxAngleSyncModeOverride = LaserConstants.VfxAngleSyncMode.FollowRaycast,
            LaserConstants.StartPositionOverrideMode startPositionOverrideMode = LaserConstants.StartPositionOverrideMode.UseLaserTable,
            Vector2 startPositionOverride = default,
            LaserConstants.StartPointUpdateMode startPointUpdateMode = LaserConstants.StartPointUpdateMode.FollowOwner,
            bool useCasterFlipStartOffsetX = false)
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
            UseDamageTimingOverride = useDamageTimingOverride;
            DamageStartDelayOverride = Mathf.Max(0f, damageStartDelayOverride);
            DamageActiveDurationOverride = damageActiveDurationOverride <= 0f ? -1f : damageActiveDurationOverride;
            DamageTickIntervalOverride = Mathf.Max(0f, damageTickIntervalOverride);
            DamageTickOnStartOverride = damageTickOnStartOverride;
            UseMaxDistanceOverride = useMaxDistanceOverride;
            MaxDistanceOverride = Mathf.Max(0f, maxDistanceOverride);
            UpdateAimContinuously = updateAimContinuously;
            UseRaycastDirectionModeOverride = useRaycastDirectionModeOverride;
            RaycastDirectionModeOverride = raycastDirectionModeOverride;
            UseRaycastAngleOverride = useRaycastAngleOverride;
            RaycastAngleOverrideDeg = raycastAngleOverrideDeg;
            UseVfxAngleSyncModeOverride = useVfxAngleSyncModeOverride;
            VfxAngleSyncModeOverride = vfxAngleSyncModeOverride;
            StartPositionOverrideMode = startPositionOverrideMode;
            StartPositionOverride = startPositionOverride;
            StartPointUpdateMode = startPointUpdateMode;
            UseCasterFlipStartOffsetX = useCasterFlipStartOffsetX;
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
