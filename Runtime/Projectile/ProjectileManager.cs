using UnityEngine;

namespace GGemCo2DCore
{
    /// <summary>
    /// 발사체 발사 시점의 런타임 파라미터.
    /// - Projectile 테이블: 정적인 정의(형태/기본 옵션)
    /// - MetadataProjectile: 상황에 따라 변하는 값(데미지 타입/배율/속도/크기/표현 방식 등)
    /// </summary>
    public sealed class MetadataProjectile
    {
        // --- Static reference ---
        public readonly int Uid;

        // --- Context ---
        public readonly CharacterBase Owner;
        public readonly CharacterBase Target;

        // --- Target Position Override (dynamic) ---
        public readonly bool UseTargetPositionOverride;
        public readonly Vector2 TargetPositionOverride;

        // --- Combat (dynamic) ---
        public readonly ConfigCommon.DamageType DamageType;
        public readonly long Damage;
        public readonly int SkillUid;
        public readonly int AttackId;
        public readonly bool AllowSkillChainOnConfirmedDamage;
        public readonly ElementGaugeApplication[] ElementGaugeApplications;

        // --- Movement/Scale (dynamic) ---
        public readonly float SpeedMultiplier;
        public readonly float ScaleMultiplier;

        // --- Visual (dynamic) ---
        public readonly ProjectileConstants.ProjectileVisualType VisualType;
        public readonly Sprite VisualSprite;
        public readonly RuntimeAnimatorController VisualAnimatorController;
        public readonly int VisualVfxUidOverride;

        // --- Behavior Override (dynamic) ---
        public readonly bool UseHitLifetimeModeOverride;
        public readonly ProjectileConstants.HitLifetimeMode HitLifetimeModeOverride;
        public readonly bool UseDamageApplyModeOverride;
        public readonly ProjectileConstants.DamageApplyMode DamageApplyModeOverride;
        public readonly bool UseTickDamageIntervalOverride;
        public readonly float TickDamageIntervalOverride;
        public readonly bool UseEnvironmentHitPolicyOverride;
        public readonly ProjectileConstants.EnvironmentHitPolicy EnvironmentHitPolicyOverride;
        public readonly bool UseEnvironmentHitLayerMaskOverride;
        public readonly int EnvironmentHitLayerMaskOverride;

        public MetadataProjectile(
            int uid,
            ConfigCommon.DamageType damageType,
            long damage,
            CharacterBase target = null,
            CharacterBase owner = null,
            float speedMultiplier = 1f,
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
            bool useHitLifetimeModeOverride = false,
            ProjectileConstants.HitLifetimeMode hitLifetimeModeOverride = ProjectileConstants.HitLifetimeMode.DestroyOnTargetHit,
            bool useDamageApplyModeOverride = false,
            ProjectileConstants.DamageApplyMode damageApplyModeOverride = ProjectileConstants.DamageApplyMode.OnHit,
            bool useTickDamageIntervalOverride = false,
            float tickDamageIntervalOverride = 0f,
            bool useEnvironmentHitPolicyOverride = false,
            ProjectileConstants.EnvironmentHitPolicy environmentHitPolicyOverride = ProjectileConstants.EnvironmentHitPolicy.Ignore,
            bool useEnvironmentHitLayerMaskOverride = false,
            int environmentHitLayerMaskOverride = 0)
        {
            Uid = uid;
            DamageType = damageType;
            Damage = damage;
            SkillUid = skillUid;
            AttackId = attackId;
            AllowSkillChainOnConfirmedDamage = allowSkillChainOnConfirmedDamage;
            ElementGaugeApplications = elementGaugeApplications;
            Target = target;
            Owner = owner;

            SpeedMultiplier = Mathf.Max(0.01f, speedMultiplier);
            ScaleMultiplier = Mathf.Max(0.01f, scaleMultiplier);

            VisualType = visualType;
            VisualSprite = visualSprite;
            VisualAnimatorController = visualAnimatorController;
            VisualVfxUidOverride = visualVfxUidOverride;

            UseTargetPositionOverride = useTargetPositionOverride;
            TargetPositionOverride = targetPositionOverride;

            UseHitLifetimeModeOverride = useHitLifetimeModeOverride;
            HitLifetimeModeOverride = hitLifetimeModeOverride;
            UseDamageApplyModeOverride = useDamageApplyModeOverride;
            DamageApplyModeOverride = damageApplyModeOverride;
            UseTickDamageIntervalOverride = useTickDamageIntervalOverride;
            TickDamageIntervalOverride = Mathf.Max(0f, tickDamageIntervalOverride);
            UseEnvironmentHitPolicyOverride = useEnvironmentHitPolicyOverride;
            EnvironmentHitPolicyOverride = environmentHitPolicyOverride;
            UseEnvironmentHitLayerMaskOverride = useEnvironmentHitLayerMaskOverride;
            EnvironmentHitLayerMaskOverride = environmentHitLayerMaskOverride;
        }
    }

    /// <summary>
    /// 발사체 생성기(Factory).
    /// 테이블 정보를 바탕으로 적절한 발사체 컴포넌트를 생성/부착하고, 런타임 파라미터를 초기화한다.
    /// </summary>
    public sealed class ProjectileManager
    {
        private TableLoaderManager _table;

        public void Initialize(SceneGame sceneGame)
        {
            _table = TableLoaderManager.Instance;
        }

        /// <summary>
        /// 발사체를 생성합니다(테이블 조회 + 런타임 파라미터 적용).
        /// </summary>
        public ProjectileBase CreateProjectile(MetadataProjectile metadata)
        {
            if (metadata == null)
            {
                GcLogger.LogError("[ProjectileManager] metadata is null.");
                return null;
            }

            var info = _table.GetProjectileData(metadata.Uid);
            if (info == null)
            {
                GcLogger.LogError($"[ProjectileManager] Unknown projectile uid={metadata.Uid}");
                return null;
            }

            return CreateProjectileInternal(info, metadata);
        }

        /// <summary>
        /// 레거시 호환용: uid만으로 발사체를 생성합니다.
        /// - 런타임 파라미터가 필요하다면 CreateProjectile(MetadataProjectile)를 사용하세요.
        /// </summary>
        public ProjectileBase CreateProjectile(int projectileUid)
        {
            var info = _table.GetProjectileData(projectileUid);
            if (info == null)
            {
                GcLogger.LogError($"[ProjectileManager] Unknown projectile uid={projectileUid}");
                return null;
            }

            var meta = new MetadataProjectile(
                uid: projectileUid,
                damageType: ConfigCommon.DamageType.Physic,
                damage: 0,
                target: null,
                owner: null);

            return CreateProjectileInternal(info, meta);
        }

#if UNITY_EDITOR
        public ProjectileBase CreateProjectile(StruckTableProjectile info)
        {
            // Editor에서 단독 테스트 용도: 기본 메타데이터로 초기화
            var meta = new MetadataProjectile(
                uid: info?.Uid ?? 0,
                damageType: ConfigCommon.DamageType.Physic,
                damage: 0);

            return CreateProjectileInternal(info, meta);
        }
#endif

        /// <summary>
        /// 테이블 타입에 맞는 Projectile 컴포넌트를 생성하고 초기화합니다.
        /// - 분리 테이블 타입(Path/Arc/Linear)을 우선 사용하고, legacy Default는 ArcHeight 값으로 기존 동작을 유지합니다.
        /// </summary>
        /// <param name="info">정적 Projectile 테이블 Row입니다.</param>
        /// <param name="meta">런타임 발사 메타데이터입니다.</param>
        /// <returns>생성된 Projectile 컴포넌트입니다.</returns>
        private ProjectileBase CreateProjectileInternal(StruckTableProjectile info, MetadataProjectile meta)
        {
            var go = new GameObject($"Projectile_{info.Uid}");
            ProjectileBase comp;

            bool isArc = info.Type == ProjectileConstants.Type.Arc ||
                         (info.Type == ProjectileConstants.Type.Default &&
                          ((info.ArcHeightMin > 0) || (info.ArcHeightMax > 0)));

            if (info.Type == ProjectileConstants.Type.Laser)
            {
                comp = go.AddComponent<ProjectileLaser>();
            }
            else if (info.Type == ProjectileConstants.Type.LinearThenSegments)
            {
                comp = go.AddComponent<ProjectileLinearThenSegments>();
            }
            else if (info.Type == ProjectileConstants.Type.Path)
            {
                comp = go.AddComponent<ProjectilePath>();
            }
            else if (isArc)
            {
                comp = go.AddComponent<ProjectileArc>();
            }
            else
            {
                comp = go.AddComponent<ProjectileLinear>();
            }

            if (comp == null)
            {
                GcLogger.LogError("[ProjectileManager] Component add failed.");
                Object.Destroy(go);
                return null;
            }

            comp.Initialize(info, meta);
            return comp;
        }
    }
}
