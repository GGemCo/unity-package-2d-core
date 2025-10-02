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
        private TableLoaderManager _table;

        public void Initialize(SceneGame sceneGame)
        {
            _sceneGame = sceneGame;
            _effectManager = sceneGame.EffectManager;
            _table = TableLoaderManager.Instance;
        }

        /// <summary>
        /// 테이블 정보를 바탕으로 적절한 발사체 클래스를 생성/부착합니다.
        /// - arcHeight가 0보다 크면 ArcProjectile
        /// - 아니면 LinearProjectile
        /// </summary>
        public ProjectileBase CreateProjectile(int projectileUid)
        {
            var info = _table.GetProjectileData(projectileUid);
            if (info == null)
            {
                GcLogger.LogError($"[ProjectileManager] Unknown projectile uid={projectileUid}");
                return null;
            }

            var go = new GameObject($"Projectile_{projectileUid}");
            ProjectileBase comp;

            bool isArc = (info.ArcHeightMin > 0) || (info.ArcHeightMax > 0);
            if (info.Type == ProjectileConstants.Type.Laser)
            {
                comp = go.AddComponent<ProjectileLaser>();
            }
            else if (isArc)
                comp = go.AddComponent<ProjectileArc>();
            else
                comp = go.AddComponent<ProjectileLinear>();

            if (!comp)
            {
                GcLogger.LogError("[ProjectileManager] Component add failed.");
                Object.Destroy(go);
                return null;
            }

            comp.Initialize(info);
            return comp;
        }
#if UNITY_EDITOR
        public ProjectileBase CreateProjectile(StruckTableProjectile info)
        {
            int projectileUid = info.Uid;
            var go = new GameObject($"Projectile_{projectileUid}");
            ProjectileBase comp;

            bool isArc = (info.ArcHeightMin > 0) || (info.ArcHeightMax > 0);
            if (info.Type == ProjectileConstants.Type.Laser)
            {
                comp = go.AddComponent<ProjectileLaser>();
            }
            else if (isArc)
                comp = go.AddComponent<ProjectileArc>();
            else
                comp = go.AddComponent<ProjectileLinear>();

            if (!comp)
            {
                GcLogger.LogError("[ProjectileManager] Component add failed.");
                Object.Destroy(go);
                return null;
            }

            comp.Initialize(info);
            return comp;
        }
#endif
    }
}