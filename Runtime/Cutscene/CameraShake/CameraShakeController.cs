using System.Collections;
using UnityEngine;

namespace GGemCo2DCore
{
    /// <summary>
    /// 연출 - 카메라 흔들기
    /// </summary>
    public class CameraShakeController : CutsceneDefaultController, ICutsceneController
    {
        private CameraManager _cameraManager;
        private float _timer;
        private float _duration;
        private bool _isPlaying;

        public CameraShakeController(CutsceneManager manager)
        {
            CutsceneManager = manager;
            _cameraManager = SceneGame.Instance.cameraManager;
        }

        public IEnumerator Ready(CutsceneEvent evt)
        {
            if (evt.type != CutsceneEventType.CameraShake)
            {
                yield break;
            }

            yield return null;
        }

        public void Trigger(CutsceneEvent evt)
        {
            if (evt.type != CutsceneEventType.CameraShake || _cameraManager == null)
            {
                return;
            }

            CameraShakeData data = evt.cameraShake ?? new CameraShakeData();
            _duration = evt.duration > 0f ? evt.duration : data.duration;
            _timer = 0f;
            _isPlaying = _duration > 0f;

            _cameraManager.StartShake(
                _duration,
                data.GetLeftStrength(),
                data.GetRightStrength(),
                data.GetDownStrength(),
                data.GetUpStrength(),
                data.GetRepeatCount(),
                CameraShakeChannel.Cutscene,
                data.useUnscaledTime);
        }

        public void Update()
        {
            if (!_isPlaying)
            {
                return;
            }

            _timer += Time.deltaTime;
            if (_timer >= _duration)
            {
                Stop();
            }
        }

        public void Stop()
        {
            _isPlaying = false;
        }

        public void End()
        {
            _isPlaying = false;
            _cameraManager?.StopShake(CameraShakeChannel.Cutscene);
        }
    }
}
