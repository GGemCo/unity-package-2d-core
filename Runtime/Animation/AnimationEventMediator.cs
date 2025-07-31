using System;
using Newtonsoft.Json;
using UnityEngine;

namespace GGemCo2DCore
{
    /// <summary>
    /// 애니메이션 Event 처리 
    /// </summary>
    public class AnimationEventMediator : IAnimationEventListener
    {
        private CameraManager _cameraManager;
        private SoundManager _soundManager;
        private EffectManager _effectManager;

        public void Initialize(SceneGame sceneGame)
        {
            _cameraManager = sceneGame.cameraManager;
            _soundManager = sceneGame.soundManager;
            _effectManager = sceneGame.EffectManager;
        }

        public void OnAnimationEventEffect(string json, GameObject fromObject)
        {
            try
            {
                var data = JsonConvert.DeserializeObject<StruckAnimationEventEffect>(json);
                var effect = _effectManager.CreateEffect(data);
                if (effect == null) return;
                effect.transform.position = fromObject.transform.position;
            }
            catch (Exception e)
            {
                GcLogger.LogError($"animation effect event, json parsing error: {e.Message}");
            }
        }

        public void OnAnimationEventSound(string json)
        {
            try
            {
                var data = JsonConvert.DeserializeObject<StruckAnimationEventSound>(json);
                _soundManager.PlayByUid(data.Uid);
            }
            catch (Exception e)
            {
                GcLogger.LogError($"animation sound event, json parsing error: {e.Message}");
            }
        }

        public void OnAnimationEventCameraShake(string json)
        {
            try
            {
                var data = JsonConvert.DeserializeObject<StruckAnimationEventCameraShake>(json);
                _cameraManager.StartShake(data.Duration, data.Magnitude);
            }
            catch (Exception e)
            {
                GcLogger.LogError($"animation camera shake event, json parsing error: {e.Message}");
            }
        }

        public void OnAnimationEventAttack(GameObject fromObject)
        {
            fromObject.GetComponent<CharacterBase>()?.OnEventAttack();
        }
        
        public void OnAnimationEventProjectile(int projectileUid, GameObject fromObject)
        {
            fromObject.GetComponent<CharacterBase>()?.LaunchProjectile(projectileUid);
        }

        public void OnAnimationEventSkill(string json, GameObject fromObject)
        {
            try
            {
                var data = JsonConvert.DeserializeObject<StruckAnimationEventSkill>(json);
                fromObject.GetComponent<CharacterBase>()?.UseSkill(data.Uid, data.Level);
            }
            catch (Exception e)
            {
                GcLogger.LogError($"animation skill event, json parsing error: {e.Message}");
            }
        }
    }
}