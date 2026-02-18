using UnityEngine;
using Object = UnityEngine.Object;

namespace GGemCo2DCore
{
    public class EffectManager
    {
        private SceneGame _sceneGame;
        private AnimationEventMediator _animationEventMediator;
        
        public void Initialize(SceneGame sceneGame)
        {
            _sceneGame = sceneGame;
        }
        
        public DefaultEffect CreateEffect(int effectUid)
        {
            var info = TableLoaderManager.Instance.GetEffectData(effectUid);
            if (info == null)
            {
                GcLogger.LogError("effect 테이블에 없는 이펙트 입니다. effect Uid: "+effectUid);
                return null;
            }
            // 이펙트는 같은 프리팹으로 베리에이션 해서 사용할 수 있기때문에 info.PrefabName 을 key 로 사용한다.
            string key = $"{ConfigAddressableGroupName.Effect}_{info.PrefabPath}";
            GameObject prefab = AddressableLoaderPrefabEffect.Instance.GetPrefabByName(key);
            if (prefab == null) return null;
            GameObject effect = Object.Instantiate(prefab);
            
            // 레이저 여부를 테이블/프리팹 이름/메타데이터로 판별
            bool isLaser = info.Type == EffectConstants.Type.Laser;

            // 레이저면 EffectLaser, 아니면 DefaultEffect
            DefaultEffect defaultEffect = isLaser 
                ? effect.AddComponent<EffectLaser>()
                : effect.AddComponent<DefaultEffect>();
            
            IEffectAnimationController effectAnimationController = null;
#if GGEMCO_USE_SPINE
            if (info.AnimationController == ConfigCommon.AnimationController.Spine)
            {
                effectAnimationController = effect.AddComponent<EffectAnimationControllerSpine>();
                defaultEffect.effectAnimationController = effectAnimationController;
                
                // Spine2dController 에 EventListener 설정
                var spineController = effect.GetComponent<Spine2dController>();
                if (spineController != null && _animationEventMediator != null)
                {
                    spineController.EventListener = _animationEventMediator;
                }
            }
#endif
            if (info.AnimationController == ConfigCommon.AnimationController.Sprite)
            {
                effectAnimationController = effect.AddComponent<EffectAnimationControllerSprite>();
                defaultEffect.effectAnimationController = effectAnimationController;
                
                // Animator2dController 에 EventListener 설정
                var animatorController = effect.GetComponent<Animation2dController>();
                if (animatorController != null && _animationEventMediator != null)
                {
                    animatorController.EventListener = _animationEventMediator;
                }
            }

            if (effectAnimationController == null)
            {
                GcLogger.LogError($"wrong animation controller. animationController: {info.AnimationController}");
                return null;
            }

            defaultEffect.Initialize(info);
            // defaultEffect.Initialize();
            return defaultEffect;
        }
        
        public DefaultEffect CreateEffect(StruckAnimationEventEffect struckAnimationEventEffect)
        {
            if (struckAnimationEventEffect == null) return null;
            int effectUid = struckAnimationEventEffect.Uid;
            float duration = struckAnimationEventEffect.Duration;
            float scale = struckAnimationEventEffect.Scale;
            string color = struckAnimationEventEffect.Color;
            var effect = CreateEffect(effectUid);
            if (effect == null) return null;
            
            if (duration > 0)
            {
                effect.SetDuration(duration);
            }
            if (scale > 0)
            {
                effect.SetScale(scale);
            }
            if (!string.IsNullOrEmpty(color))
            {
                effect.SetColor(color);
            }

            return effect;
        }
        public void SetAnimationEventMediator(AnimationEventMediator mediator)
        {
            _animationEventMediator = mediator;
        }
    }
}