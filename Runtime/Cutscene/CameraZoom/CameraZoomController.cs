using System.Collections;
using UnityEngine;

namespace GGemCo.Scripts
{
    /// <summary>
    /// 연출 - 카메라 줌 인,아웃
    /// </summary>
    public class CameraZoomController : CutsceneDefaultController, ICutsceneController
    {
        private Camera cam;
        private float startSize, endSize;
        private float timer;
        private float duration;
        private bool isZooming;
        private Easing.EaseType easing;

        public CameraZoomController(CutsceneManager manager)
        {
            CutsceneManager = manager;
            cam = SceneGame.Instance.mainCamera;
        }

        public IEnumerator Ready(CutsceneEvent evt)
        {
            if (evt.type != CutsceneEventType.CameraZoom) yield break;
            var data = evt.cameraZoom;
            yield return null;
        }
        public void Trigger(CutsceneEvent evt)
        {
            if (evt.type != CutsceneEventType.CameraZoom) return;
            var data = evt.cameraZoom;
            duration = evt.duration;
            easing = data.easing;
            startSize = data.startSize;
            endSize = data.endSize;

            if (startSize != 0)
            {
                cam.orthographicSize = startSize;
            }
            else
            {
                startSize = cam.orthographicSize;
            }
            timer = 0f;
            isZooming = true;
            SceneGame.Instance.cameraManager.StartZoom(endSize, duration, easing);
        }

        public void Update()
        {
            if (!isZooming) return;
            timer += Time.deltaTime;
            float t = Mathf.Clamp01(timer / duration);
            
            if (timer > duration)
            {
                Stop();
            }
        }

        public void Stop()
        {
            isZooming = false;
        }
        public void End()
        {
            isZooming = false;
        }
    }
}
