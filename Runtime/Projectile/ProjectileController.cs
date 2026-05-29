using System.Collections;
using UnityEngine;

namespace GGemCo2DCore
{
    /// <summary>
    /// 캐릭터가 보유한 발사체 컨트롤러
    /// - 테이블 조회 → 발사 횟수/지연/타겟 유형 처리
    /// - 코루틴으로 다발사 타이밍 관리
    /// </summary>
    public class ProjectileController
    {
        private CharacterBase _character;
        private ProjectileManager _projectileManager;

        public void Initialize(CharacterBase characterBase)
        {
            _character = characterBase;
            _projectileManager = SceneGame.Instance.ProjectileManager;
        }

        /// <summary>
        /// 메타데이터를 받아 지정 발사체를 발사합니다.
        /// - Fixed: 오브젝트 타겟
        /// - Area/None: 좌표 타겟
        /// </summary>
        public void Launch(MetadataProjectile metadataProjectile)
        {
            if (metadataProjectile == null) return;

            var info = TableLoaderManager.Instance.GetProjectileData(metadataProjectile.Uid);
            if (info == null) return;

            // owner가 비어 있으면 이 캐릭터를 owner로 사용(기본 정책)
            var meta = metadataProjectile.Owner == null
                ? new MetadataProjectile(
                    uid: metadataProjectile.Uid,
                    damageType: metadataProjectile.DamageType,
                    damage: metadataProjectile.Damage,
                    target: metadataProjectile.Target,
                    owner: _character,
                    speedMultiplier: metadataProjectile.SpeedMultiplier,
                    scaleMultiplier: metadataProjectile.ScaleMultiplier,
                    visualType: metadataProjectile.VisualType,
                    visualSprite: metadataProjectile.VisualSprite,
                    visualAnimatorController: metadataProjectile.VisualAnimatorController,
                    visualVfxUidOverride: metadataProjectile.VisualVfxUidOverride,
                    useTargetPositionOverride: metadataProjectile.UseTargetPositionOverride,
                    targetPositionOverride: metadataProjectile.TargetPositionOverride,
                    skillUid: metadataProjectile.SkillUid,
                    attackId: metadataProjectile.AttackId,
                    allowSkillChainOnConfirmedDamage: metadataProjectile.AllowSkillChainOnConfirmedDamage,
                    elementGaugeApplications: metadataProjectile.ElementGaugeApplications,
                    useHitLifetimeModeOverride: metadataProjectile.UseHitLifetimeModeOverride,
                    hitLifetimeModeOverride: metadataProjectile.HitLifetimeModeOverride,
                    useDamageApplyModeOverride: metadataProjectile.UseDamageApplyModeOverride,
                    damageApplyModeOverride: metadataProjectile.DamageApplyModeOverride,
                    useTickDamageIntervalOverride: metadataProjectile.UseTickDamageIntervalOverride,
                    tickDamageIntervalOverride: metadataProjectile.TickDamageIntervalOverride,
                    useEnvironmentHitPolicyOverride: metadataProjectile.UseEnvironmentHitPolicyOverride,
                    environmentHitPolicyOverride: metadataProjectile.EnvironmentHitPolicyOverride,
                    useEnvironmentHitLayerMaskOverride: metadataProjectile.UseEnvironmentHitLayerMaskOverride,
                    environmentHitLayerMaskOverride: metadataProjectile.EnvironmentHitLayerMaskOverride,
                    useArrivalPolicyOverride: metadataProjectile.UseArrivalPolicyOverride,
                    arrivalPolicyOverride: metadataProjectile.ArrivalPolicyOverride,
                    hitVfxPositionPolicy: metadataProjectile.HitVfxPositionPolicy,
                    hitVfxOffset: metadataProjectile.HitVfxOffset,
                    hitVfxHitAreaNormalized: metadataProjectile.HitVfxHitAreaNormalized)
                : metadataProjectile;

            _character.StartCoroutine(CreateProjectileBurst(info, meta));
        }

        /// <summary>
        /// 프로젝타일 테이블의 발사 수와 지연 시간을 기준으로 발사체를 순차 생성합니다.
        /// 좌표 오버라이드가 지정된 경우 TargetType.Fixed라도 고정 타겟 참조보다 좌표를 우선 사용합니다.
        /// </summary>
        /// <param name="info">프로젝타일 테이블에서 조회한 정적 정의입니다.</param>
        /// <param name="meta">이번 발사에만 적용되는 런타임 메타데이터입니다.</param>
        private IEnumerator CreateProjectileBurst(StruckTableProjectile info, MetadataProjectile meta)
        {
            CharacterBase target = meta != null ? meta.Target : null;

            // 목표가 필요한 Fixed 타입은 타겟 캐릭터 또는 좌표 오버라이드 중 하나가 있어야 발사할 수 있습니다.
            if (info.TargetType == ProjectileConstants.TargetType.Fixed && !target && (meta == null || !meta.UseTargetPositionOverride))
                yield break;

            int count = Mathf.Max(1, info.Count);
            for (int i = 0; i < count; i++)
            {
                var proj = _projectileManager.CreateProjectile(meta);
                if (proj != null)
                {
                    LaunchProjectileInstance(info, meta, target, proj);
                }

                float delay = Mathf.Max(0f, info.SecDelayByOne);
                if (delay > 0f)
                    yield return new WaitForSeconds(delay);
            }
        }

        /// <summary>
        /// 생성된 발사체 1개의 최종 목표를 해석하고 발사를 시작합니다.
        /// Skill 등 외부 시스템에서 좌표를 직접 지정한 경우, 프로젝타일 테이블의 TargetType보다 좌표 오버라이드를 우선합니다.
        /// </summary>
        /// <param name="info">프로젝타일 테이블에서 조회한 정적 정의입니다.</param>
        /// <param name="meta">이번 발사에만 적용되는 런타임 메타데이터입니다.</param>
        /// <param name="target">발사 시점에 고정한 타겟 캐릭터입니다.</param>
        /// <param name="projectile">발사를 시작할 프로젝타일 인스턴스입니다.</param>
        private void LaunchProjectileInstance(
            StruckTableProjectile info,
            MetadataProjectile meta,
            CharacterBase target,
            ProjectileBase projectile)
        {
            if (projectile == null)
                return;

            if (meta != null && meta.UseTargetPositionOverride)
            {
                projectile.Launch(meta.TargetPositionOverride);
                return;
            }

            if (info.TargetType == ProjectileConstants.TargetType.Fixed)
            {
                projectile.Launch(target);
                return;
            }

            // Area/None: 좌표 기반
            Vector2 targetPosition = ResolveDefaultProjectileTargetPosition(info, target);
            projectile.Launch(targetPosition);
        }

        /// <summary>
        /// 좌표 오버라이드가 없을 때 Area/None 타입 프로젝타일의 기본 목표 좌표를 계산합니다.
        /// </summary>
        /// <param name="info">프로젝타일 테이블에서 조회한 정적 정의입니다.</param>
        /// <param name="target">현재 타겟 캐릭터입니다. 없으면 발사자 위치를 기준으로 계산합니다.</param>
        /// <returns>발사체가 향할 기본 월드 좌표입니다.</returns>
        private Vector2 ResolveDefaultProjectileTargetPosition(StruckTableProjectile info, CharacterBase target)
        {
            // 직선형은 X를 고정, 곡선형은 X를 범위에서 샘플합니다.
            float x = target
                ? target.transform.position.x
                : _character.transform.position.x;

            bool isArc = info.Type == ProjectileConstants.Type.Arc ||
                         (info.Type == ProjectileConstants.Type.Default &&
                          ((info.ArcHeightMin > 0) || (info.ArcHeightMax > 0)));
            if (isArc && target)
            {
                x = Random.Range(target.transform.position.x - info.TargetPositionRangeX,
                    target.transform.position.x + info.TargetPositionRangeX);
            }

            float y = target
                ? target.GetRandomPositionYInHitArea()
                : _character.transform.position.y;

            return new Vector2(x, y);
        }
    }
}
