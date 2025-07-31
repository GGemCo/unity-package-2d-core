namespace GGemCo2DCore
{
    public class MetadataProjectile
    {
        public StruckTableProjectile Info;
        public DefaultEffect Effect;
        public float Damage;

        public MetadataProjectile(StruckTableProjectile info, DefaultEffect effect, float damage = 0)
        {
            Info = info;
            Effect = effect;
            Damage = damage;
        }
    }
    public class ProjectileManager
    {
        private SceneGame _sceneGame;
        private EffectManager _effectManager;

        public void Initialize(SceneGame sceneGame)
        {
            _sceneGame = sceneGame;
            _effectManager = sceneGame.EffectManager;
        }
        public DefaultProjectile CreateProjectile(int projectileUid)
        {
            var info = TableLoaderManager.Instance.TableProjectile.GetDataByUid(projectileUid);
            if (info == null)
            {
                GcLogger.LogError("projectile 테이블에 없는 이펙트 입니다. projectile Uid: "+projectileUid);
                return null;
            }
            DefaultEffect defaultEffect = _effectManager.CreateEffect(info.EffectUid);
            if (!defaultEffect) return null;
            DefaultProjectile defaultProjectile = defaultEffect.gameObject.AddComponent<DefaultProjectile>();
            if (!defaultProjectile)
            {
                GcLogger.LogError("DefaultProjectile 스크립트가 없습니다.");
                return null;
            }

            MetadataProjectile metadataProjectile = new MetadataProjectile(info, defaultEffect);
            defaultProjectile.Initialize(metadataProjectile);
            return defaultProjectile;
        }
    }
}