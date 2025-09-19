using UnityEngine;

namespace GGemCo2DCore
{
    public class MetadataProjectile
    {
        public readonly int uid;
        public readonly SkillConstants.DamageType damageType;
        public readonly long damage;
        public readonly CharacterBase target;
        public MetadataProjectile(int uid, SkillConstants.DamageType damageType, long damage, CharacterBase target = null)
        {
            this.uid = uid;
            this.damageType = damageType;
            this.damage = damage;
            this.target = target;
        }
    }
    public class ProjectileManager
    {
        private SceneGame _sceneGame;
        private EffectManager _effectManager;
        private TableLoaderManager _tableLoaderManager;

        public void Initialize(SceneGame sceneGame)
        {
            _sceneGame = sceneGame;
            _effectManager = sceneGame.EffectManager;
            _tableLoaderManager = TableLoaderManager.Instance;
        }
        public DefaultProjectile CreateProjectile(int projectileUid)
        {
            var info = _tableLoaderManager.GetProjectileData(projectileUid);
            if (info == null)
            {
                GcLogger.LogError("projectile 테이블에 없는 이펙트 입니다. projectile Uid: "+projectileUid);
                return null;
            }
            GameObject projectile = new GameObject();
            DefaultProjectile defaultProjectile = projectile.AddComponent<DefaultProjectile>();
            
            // DefaultEffect defaultEffect = _effectManager.CreateEffect(info.EffectUid);
            // if (!defaultEffect) return null;
            // DefaultProjectile defaultProjectile = defaultEffect.gameObject.AddComponent<DefaultProjectile>();
            if (!defaultProjectile)
            {
                GcLogger.LogError("DefaultProjectile 스크립트가 없습니다.");
                return null;
            }

            // MetadataProjectile metadataProjectile = new MetadataProjectile(info, defaultEffect);
            defaultProjectile.Initialize(info);
            return defaultProjectile;
        }
    }
}