using System.Collections;
using UnityEngine;

namespace GGemCo2DCore
{
    /// <summary>
    /// 컷신 중 카메라 흔들림(Shake) 연출을 실행하는 컨트롤러입니다.
    /// 지정된 지속 시간 동안 흔들림을 적용하고 종료 시 정리합니다.
    /// </summary>
    public class CameraShakeController : CutsceneDefaultController, ICutsceneController
    {
        private readonly CameraManager _cameraManager;
        private float _timer;
        private float _duration;
        private bool _isPlaying;

        /// <summary>
        /// 카메라 흔들기 컨트롤러를 생성합니다.
        /// </summary>
        /// <param name="manager">현재 컷신 흐름을 관리하는 매니저입니다.</param>
        public CameraShakeController(CutsceneManager manager)
        {
            CutsceneManager = manager;
            _cameraManager = SceneGame.Instance.cameraManager;
        }

        /// <summary>
        /// 카메라 흔들기 이벤트 실행 전 필요한 사전 준비를 수행합니다.
        /// 이벤트 타입이 일치하지 않으면 즉시 종료합니다.
        /// </summary>
        /// <param name="evt">준비할 컷신 이벤트 정보입니다.</param>
        /// <returns>준비 과정을 비동기적으로 진행하기 위한 열거자입니다.</returns>
        public IEnumerator Ready(CutsceneEvent evt)
        {
            if (evt.type != CutsceneEventType.CameraShake)
            {
                yield break;
            }

            // TODO: 필요 시 Shake 데이터 유효성 검사 또는 사전 계산 수행
            yield return null;
        }

        /// <summary>
        /// 카메라 흔들기 이벤트를 시작하고 강도, 방향, 반복 횟수 등의 설정을 적용합니다.
        /// 이벤트 타입이 일치하지 않거나 카메라 매니저가 없으면 실행하지 않습니다.
        /// </summary>
        /// <param name="evt">실행할 컷신 이벤트 정보입니다.</param>
        public void Trigger(CutsceneEvent evt)
        {
            if (evt.type != CutsceneEventType.CameraShake || _cameraManager == null)
            {
                return;
            }

            CameraShakeData data = evt.cameraShake ?? new CameraShakeData();

            // 이벤트 duration이 우선, 없으면 데이터 duration 사용
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

        /// <summary>
        /// 카메라 흔들기 진행 시간을 갱신하고 지속 시간이 지나면 자동으로 중지합니다.
        /// </summary>
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

        /// <summary>
        /// 현재 진행 중인 카메라 흔들기 상태를 중지합니다.
        /// 실제 흔들기 중단은 End에서 수행됩니다.
        /// </summary>
        public void Stop()
        {
            _isPlaying = false;
            _cameraManager?.StopShake(CameraShakeChannel.Cutscene);
        }

        /// <summary>
        /// 컷신 종료 시 카메라 흔들기를 강제로 중단하고 상태를 정리합니다.
        /// </summary>
        public void End()
        {
            _isPlaying = false;
            _cameraManager?.StopShake(CameraShakeChannel.Cutscene);
        }
    }
}