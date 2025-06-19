using UnityEngine;

namespace GGemCo.Scripts
{
    public abstract class EffectManager
    {
        public static DefaultEffect CreateEffect(int effectUid)
        {
            var info = TableLoaderManager.Instance.TableEffect.GetDataByUid(effectUid);
            if (info == null)
            {
                GcLogger.LogError("effect 테이블에 없는 이펙트 입니다. effect Uid: "+effectUid);
                return null;
            }
            GameObject prefab = TableLoaderManager.Instance.TableEffect.GetPrefab(effectUid);
            if (prefab == null) return null;
            GameObject effect = Object.Instantiate(prefab);
            DefaultEffect defaultEffect = effect.AddComponent<DefaultEffect>();
#if GGEMCO_USE_SPINE
            EffectAnimationControllerSpine effectAnimationControllerSpine = effect.AddComponent<EffectAnimationControllerSpine>();
            defaultEffect.EffectAnimationController = effectAnimationControllerSpine;
#else
            EffectAnimationControllerSprite effectAnimationControllerSprite = effect.AddComponent<EffectAnimationControllerSprite>();
            defaultEffect.EffectAnimationController = effectAnimationControllerSprite;
#endif
            defaultEffect.Initialize(info);
            // defaultEffect.Initialize();
            return defaultEffect;
        }
    }
}