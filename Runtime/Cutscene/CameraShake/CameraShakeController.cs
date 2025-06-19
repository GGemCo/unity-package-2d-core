using System.Collections;
using UnityEngine;

namespace GGemCo.Scripts
{
    /// <summary>
    /// 연출 - 카메라 흔들기
    /// </summary>
    public class CameraShakeController : CutsceneDefaultController, ICutsceneController
    {
        private Camera cam;
        private float duration;
        private float timer;

        private bool isShaking;
        private float shakeIntensity;
        private float shakeDuration;

        public CameraShakeController(CutsceneManager manager)
        {
            CutsceneManager = manager;
            cam = SceneGame.Instance.mainCamera;
        }

        public IEnumerator Ready(CutsceneEvent evt)
        {
            if (evt.type != CutsceneEventType.CameraShake) yield break;
            var data = evt.cameraShake;
            // 캐릭터 타겟 찾기 같은 준비
            yield return null;
        }
        public void Trigger(CutsceneEvent evt)
        {
            if (evt.type != CutsceneEventType.CameraShake) return;
            var data = evt.cameraShake;
            shakeDuration = evt.duration;
            shakeIntensity = data.shakeIntensity;
            timer = 0;
            isShaking = true;
        }
        public void Update()
        {
            if (!isShaking || cam == null || cam.transform == null) return;

            timer += Time.deltaTime;

            Vector2 shakeOffset = isShaking && shakeDuration > 0
                ? Random.insideUnitSphere * shakeIntensity
                : Vector2.zero;
            // 카메라 실시간 위치로 반영해야 한다
            Vector2 newPos = new Vector2(cam.transform.position.x, cam.transform.position.y) + shakeOffset;
            cam.transform.position= new Vector3(newPos.x, newPos.y, cam.transform.position.z);

            shakeDuration -= Time.deltaTime;
            if (shakeDuration <= 0)
            {
                Stop();
            }
        }

        public void Stop()
        {
            isShaking = false;
        }
        public void End()
        {
            isShaking = false;
        }
    }
}
