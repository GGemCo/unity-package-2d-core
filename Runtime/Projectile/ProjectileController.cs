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

        private CharacterBase _target;

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

            _target = metadataProjectile.Target;

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
                    allowSkillChainOnConfirmedDamage: metadataProjectile.AllowSkillChainOnConfirmedDamage)
                : metadataProjectile;

            _character.StartCoroutine(CreateProjectileBurst(info, meta));
        }

        private IEnumerator CreateProjectileBurst(StruckTableProjectile info, MetadataProjectile meta)
        {
            // 목표가 필요한 타입인데 타겟이 없다면 중단
            if (info.TargetType == ProjectileConstants.TargetType.Fixed && !_target)
                yield break;

            int count = Mathf.Max(1, info.Count);
            for (int i = 0; i < count; i++)
            {
                var proj = _projectileManager.CreateProjectile(meta);
                if (proj != null)
                {
                    // 좌표 산출
                    if (info.TargetType == ProjectileConstants.TargetType.Fixed)
                    {
                        proj.Launch(_target);
                    }
                    else
                    {
                        // Area/None: 좌표 기반
                        // Skill 등 외부 시스템에서 좌표를 직접 지정하는 경우, override 좌표를 우선 사용한다.
                        if (meta.UseTargetPositionOverride)
                        {
                            proj.Launch(meta.TargetPositionOverride);
                        }
                        else
                        {

                            // 직선형은 X를 고정, 곡선형은 X를 범위에서 샘플
                            float x = _target
                                ? _target.transform.position.x
                                : _character.transform.position.x;

                            bool isArc = info.Type == ProjectileConstants.Type.Arc ||
                                         (info.Type == ProjectileConstants.Type.Default &&
                                          ((info.ArcHeightMin > 0) || (info.ArcHeightMax > 0)));
                            if (isArc && _target)
                            {
                                x = Random.Range(_target.transform.position.x - info.TargetPositionRangeX,
                                    _target.transform.position.x + info.TargetPositionRangeX);
                            }

                            float y = _target
                                ? _target.GetRandomPositionYInHitArea()
                                : _character.transform.position.y;

                            proj.Launch(new Vector2(x, y));
                        }

                    }
                }

                float delay = Mathf.Max(0f, info.SecDelayByOne);
                if (delay > 0f)
                    yield return new WaitForSeconds(delay);
            }
        }
    }
}
