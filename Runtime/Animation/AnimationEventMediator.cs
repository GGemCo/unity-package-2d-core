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
        private VfxManager _vfxManager;

        public void Initialize(SceneGame sceneGame)
        {
            _cameraManager = sceneGame.cameraManager;
            _soundManager = sceneGame.soundManager;
            _vfxManager = sceneGame.VfxManager;
        }

        public void OnAnimationEventVfx(string json, GameObject fromObject)
        {
            try
            {
                var data = JsonConvert.DeserializeObject<StruckAnimationEventVfx>(json);
                var vfx = _vfxManager.CreateVfx(data);
                if (vfx == null) return;
                vfx.SetCreateCharacter(fromObject);
            }
            catch (Exception e)
            {
                GcLogger.LogError($"animation vfx event, json parsing error: {e.Message} / json: {json}");
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
                GcLogger.LogError($"animation sound event, json parsing error: {e.Message} / json: {json}");
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
                GcLogger.LogError($"animation camera shake event, json parsing error: {e.Message} / json: {json}");
            }
        }

        public void OnAnimationEventAttack(string json, GameObject fromObject)
        {
            // 기존에 json 파라미터를 사용안했기 때문에, 거기에 맞춰서 대응 하는 코드 추가
            if (string.IsNullOrEmpty(json))
            {
                fromObject.GetComponent<CharacterBase>()?.OnEventAttack(new StruckAnimationEventAttack());
                return;
            }
            
            try
            {
                var data = JsonConvert.DeserializeObject<StruckAnimationEventAttack>(json);
                fromObject.GetComponent<CharacterBase>()?.OnEventAttack(data);
            }
            catch (Exception e)
            {
                GcLogger.LogError($"animation attack event, json parsing error: {e.Message} / json: {json}");
            }
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
                GcLogger.LogError($"animation skill event, json parsing error: {e.Message} / json: {json}");
            }
        }
        
        public void OnAnimationEventJump(GameObject fromObject, string eventName)
        {
            fromObject.GetComponent<CharacterBase>()?.AnimationEventJump(eventName);
        }
        public void OnAnimationEventDash(GameObject fromObject, string eventName)
        {
            fromObject.GetComponent<CharacterBase>()?.AnimationEventDash(eventName);
        }

        public void OnAnimationEventMotion(string json, GameObject fromObject)
        {
            if (fromObject == null)
                return;

            if (string.IsNullOrWhiteSpace(json))
            {
                fromObject.GetComponent<CharacterBase>()?.AnimationEventMotion(new StruckAnimationEventMotion());
                return;
            }

            try
            {
                var data = JsonConvert.DeserializeObject<StruckAnimationEventMotion>(json);
                fromObject.GetComponent<CharacterBase>()
                    ?.AnimationEventMotion(data ?? new StruckAnimationEventMotion());
            }
            catch (Exception e)
            {
                GcLogger.LogError($"animation motion event, json parsing error: {e.Message} / json: {json}");
            }
        }

        /// <summary>
        /// 시뮬레이션 패키지, 도구 사용
        /// </summary>
        /// <param name="json"></param>
        /// <param name="fromObject"></param>
        public void OnAnimationEventUseTool(string json, GameObject fromObject)
        {
            fromObject.GetComponent<CharacterBase>()?.UseTool();
        }
        /// <summary>
        /// 시뮬레이션 패키지, 씨앗 사용
        /// </summary>
        /// <param name="json"></param>
        /// <param name="fromObject"></param>
        public void OnAnimationEventUseSeed(string json, GameObject fromObject)
        {
            fromObject.GetComponent<CharacterBase>()?.UseSeed();
        }

        public void OnAnimationEventGuardEnd(GameObject fromObject)
        {
            fromObject.GetComponent<CharacterBase>()?.AnimationEventGuardEnd();
        }

        public void OnAnimationEventStartBackstepTrail(string json, GameObject fromObject)
        {
            if (fromObject == null) return;

            var trail = fromObject.GetComponentInChildren<CharacterAfterimageTrail>(true);
            if (trail == null)
            {
                // CharacterAfterimageTrail이 없는 캐릭터는 이벤트를 무시합니다.
                return;
            }

            // JSON이 없으면 컴포넌트 기본 설정으로 시작합니다.
            if (string.IsNullOrWhiteSpace(json))
            {
                trail.StartTrail();
                return;
            }

            try
            {
                var data = JsonConvert.DeserializeObject<StruckAnimationEventBackstepTrail>(json);
                trail.StartTrail(data);
            }
            catch (Exception e)
            {
                GcLogger.LogError($"animation backstep trail(start) event, json parsing error: {e.Message} / json: {json}");
                // 파싱 실패시에도 기본값으로는 동작하게 합니다.
                trail.StartTrail();
            }
        }

        public void OnAnimationEventStopBackstepTrail(string json, GameObject fromObject)
        {
            if (fromObject == null) return;
            var trail = fromObject.GetComponentInChildren<CharacterAfterimageTrail>(true);
            if (trail == null) return;
            trail.StopTrail();
        }
    }
}