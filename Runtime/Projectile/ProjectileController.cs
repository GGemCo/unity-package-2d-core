using System.Collections;
using UnityEngine;

namespace GGemCo2DCore
{
    /// <summary>
    /// 캐릭터가 가지고 있는 프로젝타일 컨트롤러
    /// </summary>
    public class ProjectileController
    {
        private CharacterBase _character;
        private TableSkill _tableSkill;
        private TableEffect _tableEffect;
        private ProjectileManager _projectileManager;

        private CharacterBase _target;
        public void Initialize(CharacterBase characterBase)
        {
            _character = characterBase;
            _tableSkill = TableLoaderManager.Instance.TableSkill;
            _tableEffect = TableLoaderManager.Instance.TableEffect;
            _projectileManager = SceneGame.Instance.ProjectileManager;
        }

        /// <summary>
        /// </summary>
        /// <param name="metadataProjectile"></param>
        public void Launch(MetadataProjectile metadataProjectile)
        {
            int uid = metadataProjectile.uid;
            SkillConstants.DamageType damageType = metadataProjectile.damageType;
            long damage = metadataProjectile.damage;
            _target = metadataProjectile.target;
            var info = TableLoaderManager.Instance.GetProjectileData(uid);
            if (info == null) return;
            
            _character.StartCoroutine(CreateProjectile(info, damageType, damage));
        }
        private IEnumerator CreateProjectile(StruckTableProjectile info, SkillConstants.DamageType damageType, long damage)
        {
            if (!_target || info == null) yield break;
            
            for (int i = 0; i < info.Count; i++)
            {
                DefaultProjectile projectile = _projectileManager.CreateProjectile(info.Uid);
                projectile?.SetFromCharacter(_character);
                projectile?.SetDamage(damage);
                float positionX =
                    Random.Range(_target.transform.position.x - info.TargetPositionRangeX,
                        _target.transform.position.x + info.TargetPositionRangeX);
                float positionY = _target.GetRandomPositionYInHitArea();
                if (info.TargetType == ProjectileConstants.TargetType.Fixed)
                {
                    projectile?.Launch(_target);
                }
                else
                {
                    // 직선형일때는 타겟 x 좌표를 범위로 하지 않는다. 
                    if (info.ArcHeightMin == 0 && info.ArcHeightMax == 0)
                    {
                        positionX = _target.transform.position.x;
                    }
        
                    // positionY = mapSettings.projectilePositionY;
                    projectile?.Launch(new Vector2(positionX, positionY));
                }
                yield return new WaitForSeconds(info.SecDelayByOne);
            }
        }

    }
}