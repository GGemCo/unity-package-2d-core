using System.Collections;
using UnityEngine;

namespace GGemCo2DCore
{
    /// <summary>
    /// 컷신 중 카메라의 줌 인/줌 아웃 연출을 제어하는 컨트롤러입니다.
    /// 지정한 시작 크기에서 목표 크기까지 easing을 적용해 줌을 진행합니다.
    /// </summary>
    public class CameraZoomController : CutsceneDefaultController, ICutsceneController
    {
        private readonly Camera _cam;
        private float _startSize, _endSize;
        private float _timer;
        private float _duration;
        private bool _isZooming;
        private Easing.EaseType _easing;

        /// <summary>
        /// 카메라 줌 연출 컨트롤러를 생성합니다.
        /// </summary>
        /// <param name="manager">현재 컷신 흐름을 관리하는 매니저입니다.</param>
        public CameraZoomController(CutsceneManager manager)
        {
            CutsceneManager = manager;
            _cam = SceneGame.Instance.mainCamera;
        }

        /// <summary>
        /// 다음 프레임 대기 없이 즉시 준비를 지원합니다.
        /// </summary>
        public bool SupportsImmediateReady => true;


        /// <summary>
        /// 카메라 줌 이벤트 실행 전 필요한 사전 준비를 수행합니다.
        /// 이벤트 타입이 일치하지 않으면 즉시 종료합니다.
        /// </summary>
        /// <param name="evt">준비할 컷신 이벤트 정보입니다.</param>
        /// <returns>준비 과정을 비동기적으로 진행하기 위한 열거자입니다.</returns>
        public void ReadyImmediate(CutsceneEvent evt)
        {
            if (evt.type != CutsceneEventType.CameraZoom)
            {
                return;
            }

            // TODO: 필요 시 줌 크기 유효성 검사 또는 카메라 상태 캐싱 수행
        }

        public IEnumerator Ready(CutsceneEvent evt)
        {
            ReadyImmediate(evt);
            yield break;
        }

        /// <summary>
        /// 카메라 줌 이벤트를 시작하고 시작 크기, 목표 크기, 지속 시간 및 보간 방식을 설정합니다.
        /// 이벤트 타입이 일치하지 않으면 아무 작업도 수행하지 않습니다.
        /// </summary>
        /// <param name="evt">실행할 컷신 이벤트 정보입니다.</param>
        public void Trigger(CutsceneEvent evt)
        {
            if (evt.type != CutsceneEventType.CameraZoom) return;

            var data = evt.cameraZoom;
            _duration = evt.duration;
            _easing = data.easing;
            _startSize = data.startSize;
            _endSize = data.endSize;

            if (_startSize != 0)
            {
                _cam.orthographicSize = _startSize;
            }
            else
            {
                _startSize = _cam.orthographicSize;
            }

            _timer = 0f;
            _isZooming = true;

            var request = new CameraZoomRequest
            {
                Owner = CameraZoomOwner.Cutscene,
                Source = this,
                EndSize = _endSize,
                Duration = _duration,
                Easing = _easing,
                UseUnscaledTime = false,
                ChangeOriginalSize = false,
                ReplaceMode = CameraZoomReplaceMode.ReplaceCurrent,
            };
            SceneGame.Instance.cameraManager.TryStartZoom(request);
        }

        /// <summary>
        /// 카메라 줌 진행 시간을 갱신하고 지정된 지속 시간이 지나면 줌 상태를 종료합니다.
        /// 실제 줌 보간은 카메라 매니저가 담당합니다.
        /// </summary>
        public void Update()
        {
            if (!_isZooming) return;

            _timer += Time.deltaTime;

            if (_timer > _duration)
            {
                Stop();
            }
        }

        /// <summary>
        /// 현재 진행 중인 카메라 줌 상태를 중지합니다.
        /// </summary>
        public void Stop()
        {
            _isZooming = false;
        }

        /// <summary>
        /// 컷신 종료 시 카메라 줌 상태를 종료합니다.
        /// </summary>
        public void End()
        {
            _isZooming = false;
        }
    }
}
