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
        private TableEffect _tableEffect;
        private ProjectileManager _projectileManager;

        private CharacterBase _target;

        public void Initialize(CharacterBase characterBase)
        {
            _character         = characterBase;
            _tableEffect       = TableLoaderManager.Instance.TableEffect;
            _projectileManager = SceneGame.Instance.ProjectileManager;
        }

        /// <summary>
        /// 메타데이터를 받아 지정 발사체를 발사합니다.
        /// - Fixed: 오브젝트 타겟
        /// - Area/None: 좌표 타겟
        /// </summary>
        public void Launch(MetadataProjectile metadataProjectile)
        {
            int uid = metadataProjectile.uid;
            long damage = metadataProjectile.damage;
            _target = metadataProjectile.target;

            var info = TableLoaderManager.Instance.GetProjectileData(uid);
            if (info == null) return;

            _character.StartCoroutine(CreateProjectileBurst(info, damage));
        }

        private IEnumerator CreateProjectileBurst(StruckTableProjectile info, long damage)
        {
            // 목표가 필요한 타입인데 타겟이 없다면 중단
            if (info.TargetType == ProjectileConstants.TargetType.Fixed && !_target)
                yield break;

            int count = Mathf.Max(1, info.Count);
            for (int i = 0; i < count; i++)
            {
                var proj = _projectileManager.CreateProjectile(info.Uid);
                if (proj != null)
                {
                    proj.SetFromCharacter(_character);
                    proj.SetDamage(damage);

                    // 좌표 산출
                    if (info.TargetType == ProjectileConstants.TargetType.Fixed)
                    {
                        proj.Launch(_target);
                    }
                    else
                    {
                        // Area/None: 좌표 기반
                        // 직선형은 X를 고정, 곡선형은 X를 범위에서 샘플
                        float x = _target
                            ? _target.transform.position.x
                            : _character.transform.position.x;

                        bool isArc = (info.ArcHeightMin > 0) || (info.ArcHeightMax > 0);
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

                float delay = Mathf.Max(0f, info.SecDelayByOne);
                if (delay > 0f)
                    yield return new WaitForSeconds(delay);
            }
        }
    }
}
