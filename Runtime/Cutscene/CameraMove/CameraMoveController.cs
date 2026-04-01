using System.Collections;
using UnityEngine;

namespace GGemCo2DCore
{
    /// <summary>
    /// 컷신 중 카메라를 지정한 시작 위치에서 목표 위치까지 이동시키는 컨트롤러입니다.
    /// 이동 완료 후 설정에 따라 카메라 추적 대상을 플레이어로 복원할 수 있습니다.
    /// </summary>
    public class CameraMoveController : CutsceneDefaultController, ICutsceneController
    {
        private readonly Camera _cam;
        private Vector2 _startPosition, _endPosition;
        private float _duration;
        private float _timer;
        private bool _isMoving;
        private bool _endTargetPlayer;
        private Easing.EaseType _easing;

        /// <summary>
        /// 카메라 이동 연출을 처리하는 컨트롤러를 생성합니다.
        /// </summary>
        /// <param name="manager">현재 컷신 흐름을 관리하는 매니저입니다.</param>
        public CameraMoveController(CutsceneManager manager)
        {
            CutsceneManager = manager;
            _cam = SceneGame.Instance.mainCamera;
        }

        /// <summary>
        /// 다음 프레임 대기 없이 즉시 준비를 지원합니다.
        /// </summary>
        public bool SupportsImmediateReady => true;


        /// <summary>
        /// 카메라 이동 이벤트 실행 전 필요한 사전 준비를 수행합니다.
        /// 현재는 별도의 준비 작업 없이 한 프레임을 양보합니다.
        /// </summary>
        /// <param name="evt">준비할 컷신 이벤트 정보입니다.</param>
        /// <returns>준비 과정을 비동기적으로 진행하기 위한 열거자입니다.</returns>
        public void ReadyImmediate(CutsceneEvent evt)
        {
            // TODO: 필요 시 위치 유효성 검사나 카메라 상태 캐싱을 이 단계에서 수행합니다.
        }

        public IEnumerator Ready(CutsceneEvent evt)
        {
            ReadyImmediate(evt);
            yield break;
        }

        /// <summary>
        /// 카메라 이동 이벤트를 시작하고 시작 위치, 목표 위치, 이동 시간 및 보간 방식을 설정합니다.
        /// 이벤트 타입이 일치하지 않으면 아무 작업도 수행하지 않습니다.
        /// </summary>
        /// <param name="evt">실행할 컷신 이벤트 정보입니다.</param>
        public void Trigger(CutsceneEvent evt)
        {
            if (evt.type != CutsceneEventType.CameraMove) return;

            _duration = evt.duration;
            var data = evt.cameraMove;

            _startPosition = data.startPosition.ToVector2();
            if (_startPosition == Vector2.zero)
            {
                _startPosition = _cam.transform.position;
            }

            _endPosition = data.endPosition.ToVector2();
            _easing = data.easing;
            _endTargetPlayer = data.endTargetPlayer;

            _timer = 0f;
            _isMoving = true;
            SceneGame.Instance.cameraManager.RemoveFollowTarget();
        }

        /// <summary>
        /// 카메라 이동 진행 상태를 갱신하고 easing을 적용하여 현재 위치를 보간합니다.
        /// 이동이 완료되면 자동으로 중지 처리합니다.
        /// </summary>
        public void Update()
        {
            if (!_isMoving) return;

            _timer += Time.deltaTime;
            float t = Mathf.Clamp01(_timer / _duration);
            float easedT = Easing.Apply(t, _easing);

            Vector2 basePos = Vector2.Lerp(_startPosition, _endPosition, easedT);
            _cam.transform.position = new Vector3(basePos.x, basePos.y, _cam.transform.position.z);

            if (t >= 1f)
            {
                Stop();
            }
        }

        /// <summary>
        /// 현재 진행 중인 카메라 이동을 중지하고 필요 시 추적 대상을 플레이어로 복원합니다.
        /// </summary>
        public void Stop()
        {
            _isMoving = false;

            if (_endTargetPlayer)
            {
                SceneGame.Instance.cameraManager.SetFollowPlayer();
            }
        }

        /// <summary>
        /// 컷신 종료 시 카메라 이동 상태를 종료합니다.
        /// </summary>
        public void End()
        {
            _isMoving = false;
        }
    }
}