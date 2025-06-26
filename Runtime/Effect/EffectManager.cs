using UnityEngine;

namespace GGemCo2DCore
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
            // 이펙트는 같은 프리팹으로 베리에이션 해서 사용할 수 있기때문에 info.PrefabName 을 key 로 사용한다.
            string key = $"{ConfigAddressableGroupName.Effect}_{info.PrefabName}";
            GameObject prefab = AddressableLoaderPrefabEffect.Instance.GetPrefabByName(key);
            if (prefab == null) return null;
            GameObject effect = Object.Instantiate(prefab);
            DefaultEffect defaultEffect = effect.AddComponent<DefaultEffect>();
#if GGEMCO_USE_SPINE
            EffectAnimationControllerSpine effectAnimationController = effect.AddComponent<EffectAnimationControllerSpine>();
            defaultEffect.EffectAnimationController = effectAnimationController;
#else
            EffectAnimationControllerSprite effectAnimationController = effect.AddComponent<EffectAnimationControllerSprite>();
            defaultEffect.EffectAnimationController = effectAnimationController;
#endif
            effectAnimationController.Initialize(defaultEffect);
            defaultEffect.Initialize(info);
            // defaultEffect.Initialize();
            return defaultEffect;
        }
    }
}