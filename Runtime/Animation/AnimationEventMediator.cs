using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
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
        
        public void OnAnimationEventComplete(string json, GameObject fromObject)
        {
            try
            {
                var data = JsonConvert.DeserializeObject<StruckAnimationEventComplete>(json);
                fromObject.GetComponent<CharacterBase>()?.AnimationEventComplete(data);
                fromObject.GetComponent<VfxBehaviourEffect>()?.AnimationEventComplete(data);
                fromObject.GetComponent<DefaultObjectTrap>()?.AnimationEventComplete(data);
            }
            catch (Exception e)
            {
                GcLogger.LogError($"animation complete event, json parsing error: {e.Message} / json: {json}");
            }
        }
        
        /// <summary>
        /// 애니메이션 이벤트로 전달된 VFX JSON을 해석해 VFX를 생성합니다.
        /// </summary>
        /// <param name="json">단일 VFX 객체, VFX 객체 배열, 또는 Uid 배열을 가진 VFX 객체 JSON 문자열입니다.</param>
        /// <param name="fromObject">애니메이션 이벤트를 발생시킨 오브젝트입니다.</param>
        public void OnAnimationEventVfx(string json, GameObject fromObject)
        {
            if (string.IsNullOrWhiteSpace(json))
                return;

            try
            {
                foreach (StruckAnimationEventVfx data in EnumerateAnimationEventVfxPayloads(json))
                {
                    if (data == null)
                        continue;

                    _vfxManager.CreateVfx(data, fromObject);
                }
            }
            catch (Exception e)
            {
                GcLogger.LogError($"animation vfx event, json parsing error: {e.Message} / json: {json}");
            }
        }

        /// <summary>
        /// AnimationEvent VFX JSON을 단일 실행 목록으로 변환합니다.
        /// </summary>
        /// <param name="json">단일 VFX 객체, VFX 객체 배열, 또는 Uid 배열을 가진 VFX 객체 JSON 문자열입니다.</param>
        /// <returns>이벤트에서 생성해야 할 VFX 데이터 목록입니다.</returns>
        /// <exception cref="JsonSerializationException">객체 또는 배열 형식이 아닌 JSON이 전달되거나, 배열 내부에 객체가 아닌 항목이 있으면 발생합니다.</exception>
        private static IEnumerable<StruckAnimationEventVfx> EnumerateAnimationEventVfxPayloads(string json)
        {
            JToken rootToken = JToken.Parse(json);
            switch (rootToken.Type)
            {
                case JTokenType.Object:
                    foreach (StruckAnimationEventVfx payload in ExpandAnimationEventVfxPayload((JObject)rootToken))
                        yield return payload;
                    yield break;

                case JTokenType.Array:
                    foreach (JToken itemToken in rootToken.Children())
                    {
                        if (itemToken.Type == JTokenType.Null)
                            continue;

                        if (itemToken.Type != JTokenType.Object)
                        {
                            throw new JsonSerializationException(
                                $"AnimationEvent VFX 배열 항목은 객체 형식이어야 합니다. 현재 형식: {itemToken.Type}");
                        }

                        foreach (StruckAnimationEventVfx payload in ExpandAnimationEventVfxPayload((JObject)itemToken))
                            yield return payload;
                    }
                    yield break;

                default:
                    throw new JsonSerializationException(
                        $"AnimationEvent VFX JSON은 객체 또는 배열 형식이어야 합니다. 현재 형식: {rootToken.Type}");
            }
        }

        /// <summary>
        /// VFX 객체 1개를 실제 생성할 VFX 데이터 목록으로 확장합니다.
        /// </summary>
        /// <param name="payloadToken">단일 VFX 설정을 담은 JSON 객체입니다. Uid가 배열이면 같은 설정으로 Uid별 VFX 데이터를 생성합니다.</param>
        /// <returns>실제로 생성해야 할 VFX 데이터 목록입니다.</returns>
        private static IEnumerable<StruckAnimationEventVfx> ExpandAnimationEventVfxPayload(JObject payloadToken)
        {
            if (payloadToken == null)
                yield break;

            JToken uidToken = GetAnimationEventVfxUidToken(payloadToken);
            if (uidToken == null || uidToken.Type != JTokenType.Array)
            {
                yield return payloadToken.ToObject<StruckAnimationEventVfx>();
                yield break;
            }

            foreach (JToken uidItemToken in uidToken.Children())
            {
                if (uidItemToken.Type == JTokenType.Null)
                    continue;

                JObject clonedPayloadToken = (JObject)payloadToken.DeepClone();
                SetAnimationEventVfxUidToken(clonedPayloadToken, uidItemToken);
                yield return clonedPayloadToken.ToObject<StruckAnimationEventVfx>();
            }
        }

        /// <summary>
        /// VFX JSON 객체에서 Uid 필드를 대소문자 구분 없이 조회합니다.
        /// </summary>
        /// <param name="payloadToken">Uid 필드를 조회할 VFX JSON 객체입니다.</param>
        /// <returns>Uid 필드 토큰입니다. 필드가 없으면 null을 반환합니다.</returns>
        private static JToken GetAnimationEventVfxUidToken(JObject payloadToken)
        {
            if (payloadToken == null)
                return null;

            foreach (JProperty property in payloadToken.Properties())
            {
                if (string.Equals(property.Name, nameof(StruckAnimationEventVfx.Uid), StringComparison.OrdinalIgnoreCase))
                    return property.Value;
            }

            return null;
        }

        /// <summary>
        /// VFX JSON 객체의 Uid 필드를 단일 Uid 값으로 교체합니다.
        /// </summary>
        /// <param name="payloadToken">Uid 값을 교체할 VFX JSON 객체입니다.</param>
        /// <param name="uidToken">적용할 단일 Uid 값 토큰입니다.</param>
        private static void SetAnimationEventVfxUidToken(JObject payloadToken, JToken uidToken)
        {
            if (payloadToken == null)
                return;

            foreach (JProperty property in payloadToken.Properties())
            {
                if (!string.Equals(property.Name, nameof(StruckAnimationEventVfx.Uid), StringComparison.OrdinalIgnoreCase))
                    continue;

                property.Value = uidToken?.DeepClone() ?? JValue.CreateNull();
                return;
            }

            payloadToken[nameof(StruckAnimationEventVfx.Uid)] = uidToken?.DeepClone() ?? JValue.CreateNull();
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
                if (data == null)
                {
                    return;
                }

                _cameraManager.StartShake(
                    data.Duration,
                    data.GetLeftStrength(),
                    data.GetRightStrength(),
                    data.GetDownStrength(),
                    data.GetUpStrength(),
                    data.GetRepeatCount(),
                    CameraShakeChannel.AnimationEvent,
                    data.UseUnscaledTime);
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

        public void OnAnimationEventCrowdControl(string json, GameObject fromObject)
        {
            if (fromObject == null)
                return;

            if (string.IsNullOrWhiteSpace(json))
            {
                fromObject.GetComponent<CharacterBase>()?.AnimationEventCrowdControl(new StruckAnimationEventCrowdControl());
                return;
            }

            try
            {
                var data = JsonConvert.DeserializeObject<StruckAnimationEventCrowdControl>(json);
                fromObject.GetComponent<CharacterBase>()
                    ?.AnimationEventCrowdControl(data ?? new StruckAnimationEventCrowdControl());
            }
            catch (Exception e)
            {
                GcLogger.LogError($"animation crowd control event, json parsing error: {e.Message} / json: {json}");
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

        /// <summary>
        /// 플레이어 피격 애니메이션 이벤트를 캐릭터 런타임으로 전달합니다.
        /// </summary>
        /// <param name="fromObject">이벤트를 발생시킨 오브젝트입니다.</param>
        public void OnAnimationEventPlayerHit(GameObject fromObject)
        {
            if (fromObject == null)
                return;

            fromObject.GetComponent<CharacterBase>()?.AnimationEventPlayerHit();
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
        
        public void OnAnimationEventCaptureAfterimageSnapshot(string json, GameObject fromObject)
        {
            if (fromObject == null)
                return;

            var trail = fromObject.GetComponentInChildren<CharacterAfterimageTrail>(true);
            if (trail == null)
                return;

            if (string.IsNullOrWhiteSpace(json))
            {
                trail.CaptureOnce();
                return;
            }

            try
            {
                var data = JsonConvert.DeserializeObject<StruckAnimationEventAfterimageSnapshot>(json);
                trail.CaptureOnce(data);
            }
            catch (Exception e)
            {
                GcLogger.LogError($"animation afterimage snapshot event, json parsing error: {e.Message} / json: {json}");
                trail.CaptureOnce();
            }
        }

        public void OnAnimationEventDead(string json, GameObject fromObject)
        {
            fromObject.GetComponent<CharacterBase>()?.OnAnimationCompleteDead();
        }
    }
}
