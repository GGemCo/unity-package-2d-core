using System.Collections;
using UnityEngine;

namespace GGemCo2DCore
{
    /// <summary>
    /// 컷신 중 카메라의 추적 대상을 다른 캐릭터로 전환하는 컨트롤러입니다.
    /// 지정된 시간 동안 전환 상태를 유지한 뒤 종료할 수 있습니다.
    /// </summary>
    public class CameraChangeTargetController : CutsceneDefaultController, ICutsceneController
    {
        private CharacterConstants.Type _characterType;
        private float _timer;
        private float _duration;

        private Transform _newTarget;
        private bool _isChange;

        /// <summary>
        /// 카메라 대상 전환 컨트롤러를 생성합니다.
        /// </summary>
        /// <param name="manager">현재 컷신 흐름을 관리하는 매니저입니다.</param>
        public CameraChangeTargetController(CutsceneManager manager)
        {
            CutsceneManager = manager;
        }

        /// <summary>
        /// 다음 프레임 대기 없이 즉시 준비를 지원합니다.
        /// </summary>
        public bool SupportsImmediateReady => true;


        /// <summary>
        /// 컷신 이벤트 실행 전 카메라 대상 전환에 필요한 사전 준비를 수행합니다.
        /// 현재는 별도의 준비 작업 없이 한 프레임을 양보합니다.
        /// </summary>
        /// <param name="evt">준비할 컷신 이벤트 정보입니다.</param>
        public void ReadyImmediate(CutsceneEvent evt)
        {
            // TODO: 필요 시 캐릭터 타겟 캐싱 또는 유효성 검사를 이 단계에서 수행합니다.
        }

        /// <summary>
        /// 카메라 대상 전환 이벤트를 코루틴 준비 경로에서도 즉시 준비합니다.
        /// </summary>
        /// <param name="evt">준비할 컷신 이벤트 정보입니다.</param>
        /// <returns>추가 대기 없이 종료되는 열거자입니다.</returns>
        public IEnumerator Ready(CutsceneEvent evt)
        {
            ReadyImmediate(evt);
            yield break;
        }

        /// <summary>
        /// 카메라 대상 전환 이벤트를 실행하고 새 추적 대상과 추가 Offset을 카메라에 적용합니다.
        /// 이벤트 타입이 일치하지 않으면 아무 작업도 수행하지 않습니다.
        /// </summary>
        /// <param name="evt">실행할 컷신 이벤트 정보입니다.</param>
        public void Trigger(CutsceneEvent evt)
        {
            if (evt.type != CutsceneEventType.CameraChangeTarget) return;

            _duration = evt.duration;
            CameraChangeTargetData data = evt.cameraChangeTarget;
            if (data == null)
            {
                GcLogger.LogWarning($"[{nameof(CameraChangeTargetController)}] CameraChangeTarget 데이터가 없어 이벤트를 실행하지 못했습니다.");
                Stop();
                return;
            }

            _newTarget = GetTargetTransform(data.characterType, data.characterUid);
            if (_newTarget == null)
            {
                GcLogger.LogWarning(
                    $"[{nameof(CameraChangeTargetController)}] 카메라 추적 대상을 찾지 못했습니다. " +
                    $"characterType: {data.characterType}, characterUid: {data.characterUid}");
                Stop();
                return;
            }

            CameraManager cameraManager = SceneGame.Instance?.cameraManager;
            if (cameraManager == null)
            {
                GcLogger.LogWarning($"[{nameof(CameraChangeTargetController)}] CameraManager가 없어 이벤트를 실행하지 못했습니다.");
                Stop();
                return;
            }

            cameraManager.SetFollowTarget(_newTarget);
            cameraManager.SetCutsceneFollowOffset(data.offset);

            _timer = 0f;
            _isChange = true;
        }

        /// <summary>
        /// 카메라 대상 전환 진행 시간을 갱신하고 지정 시간이 지나면 전환 상태를 종료합니다.
        /// </summary>
        public void Update()
        {
            if (!_isChange) return;

            _timer += Time.deltaTime;

            if (_timer > _duration)
            {
                Stop();
            }
        }

        /// <summary>
        /// 현재 진행 중인 카메라 대상 전환 상태를 중지합니다.
        /// </summary>
        public void Stop()
        {
            _isChange = false;
        }

        /// <summary>
        /// 컷신 종료 시 추가 Offset을 제거하고 카메라 추적 대상을 플레이어로 복원합니다.
        /// </summary>
        public void End()
        {
            CameraManager cameraManager = SceneGame.Instance?.cameraManager;
            if (cameraManager == null)
            {
                return;
            }

            cameraManager.ClearCutsceneFollowOffset();
            cameraManager.SetFollowPlayer();
        }
    }
}
