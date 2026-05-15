using System.Collections;
using UnityEngine;

namespace GGemCo2DCore
{
    /// <summary>
    /// 캐릭터가 사용하는 레이저 발사 컨트롤러입니다.
    /// - 기존 ProjectileController와 분리된 전용 경로입니다.
    /// - laser 테이블의 Count / SecDelayByOne 규칙을 사용합니다.
    /// </summary>
    public sealed class LaserController
    {
        private CharacterBase _character;
        private LaserManager _laserManager;
        private CharacterBase _target;

        /// <summary>
        /// 컨트롤러를 초기화합니다.
        /// </summary>
        /// <param name="characterBase">레이저를 발사할 소유 캐릭터입니다.</param>
        public void Initialize(CharacterBase characterBase)
        {
            _character = characterBase;
            _laserManager = SceneGame.Instance != null ? SceneGame.Instance.LaserManager : null;
        }

        /// <summary>
        /// 메타데이터를 기반으로 레이저를 발사합니다.
        /// </summary>
        /// <param name="metadataLaser">발사할 레이저 메타데이터입니다.</param>
        public void Launch(MetadataLaser metadataLaser)
        {
            if (metadataLaser == null || _character == null)
                return;

            _target = metadataLaser.Target;

            StruckTableLaser info = TableLoaderManager.Instance.GetLaserData(metadataLaser.LaserUid);
            if (info == null)
                return;

            MetadataLaser meta = metadataLaser.Owner == null
                ? new MetadataLaser(
                    uid: metadataLaser.Uid,
                    damageType: metadataLaser.DamageType,
                    damage: metadataLaser.Damage,
                    target: metadataLaser.Target,
                    owner: _character,
                    scaleMultiplier: metadataLaser.ScaleMultiplier,
                    visualType: metadataLaser.VisualType,
                    visualSprite: metadataLaser.VisualSprite,
                    visualAnimatorController: metadataLaser.VisualAnimatorController,
                    visualVfxUidOverride: metadataLaser.VisualVfxUidOverride,
                    useTargetPositionOverride: metadataLaser.UseTargetPositionOverride,
                    targetPositionOverride: metadataLaser.TargetPositionOverride,
                    skillUid: metadataLaser.SkillUid,
                    attackId: metadataLaser.AttackId,
                    allowSkillChainOnConfirmedDamage: metadataLaser.AllowSkillChainOnConfirmedDamage,
                    elementGaugeApplications: metadataLaser.ElementGaugeApplications,
                    useDurationOverride: metadataLaser.UseDurationOverride,
                    durationOverride: metadataLaser.DurationOverride,
                    useDamageTimingOverride: metadataLaser.UseDamageTimingOverride,
                    damageStartDelayOverride: metadataLaser.DamageStartDelayOverride,
                    damageActiveDurationOverride: metadataLaser.DamageActiveDurationOverride,
                    damageTickIntervalOverride: metadataLaser.DamageTickIntervalOverride,
                    damageTickOnStartOverride: metadataLaser.DamageTickOnStartOverride,
                    useMaxDistanceOverride: metadataLaser.UseMaxDistanceOverride,
                    maxDistanceOverride: metadataLaser.MaxDistanceOverride,
                    updateAimContinuously: metadataLaser.UpdateAimContinuously,
                    useRaycastDirectionModeOverride: metadataLaser.UseRaycastDirectionModeOverride,
                    raycastDirectionModeOverride: metadataLaser.RaycastDirectionModeOverride,
                    useRaycastAngleOverride: metadataLaser.UseRaycastAngleOverride,
                    raycastAngleOverrideDeg: metadataLaser.RaycastAngleOverrideDeg,
                    useVfxAngleSyncModeOverride: metadataLaser.UseVfxAngleSyncModeOverride,
                    vfxAngleSyncModeOverride: metadataLaser.VfxAngleSyncModeOverride,
                    startPositionOverrideMode: metadataLaser.StartPositionOverrideMode,
                    startPositionOverride: metadataLaser.StartPositionOverride,
                    startPointUpdateMode: metadataLaser.StartPointUpdateMode,
                    useCasterFlipStartOffsetX: metadataLaser.UseCasterFlipStartOffsetX)
                : metadataLaser;

            _character.StartCoroutine(CreateLaserBurst(info, meta));
        }

        /// <summary>
        /// Count / SecDelayByOne 규칙에 따라 레이저를 순차 발사합니다.
        /// </summary>
        /// <param name="info">정적 레이저 테이블 데이터입니다.</param>
        /// <param name="meta">런타임 레이저 메타데이터입니다.</param>
        /// <returns>다발사 처리를 위한 코루틴입니다.</returns>
        private IEnumerator CreateLaserBurst(StruckTableLaser info, MetadataLaser meta)
        {
            if (_laserManager == null || info == null || meta == null)
                yield break;

            int count = Mathf.Max(1, info.Count);
            for (int i = 0; i < count; i++)
            {
                LaserBeam laser = _laserManager.CreateLaser(meta);
                if (laser != null)
                {
                    if (meta.UseTargetPositionOverride)
                    {
                        laser.Launch(meta.TargetPositionOverride);
                    }
                    else if (_target)
                    {
                        laser.Launch(_target);
                    }
                    else
                    {
                        laser.Launch(_character.transform.position);
                    }
                }

                float delay = Mathf.Max(0f, info.SecDelayByOne);
                if (delay > 0f)
                    yield return new WaitForSeconds(delay);
            }
        }
    }
}
