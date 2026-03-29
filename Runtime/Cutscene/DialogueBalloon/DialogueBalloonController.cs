using System.Collections;
using UnityEngine;

namespace GGemCo2DCore
{
    /// <summary>
    /// 컷신 중 특정 캐릭터에 대사 말풍선을 표시하는 컨트롤러입니다.
    /// 필요 시 카메라 추적 대상을 함께 변경하며, 지정 시간이 지나면 말풍선을 회수합니다.
    /// </summary>
    public class DialogueBalloonController : CutsceneDefaultController, ICutsceneController
    {
        private Camera _cam;
        private string _message;
        private float _timer;
        private float _duration;
        private bool _isFollowTarget;
        private bool _isBalloon;

        private Transform _newTarget;
        private CharacterBase _newTargetCharacter;
        private readonly DialogueBalloonPool _dialogueBalloonPool;
        private GameObject _currentDialogueBalloon;

        /// <summary>
        /// 대사 말풍선 연출 컨트롤러를 생성합니다.
        /// </summary>
        /// <param name="manager">현재 컷신 흐름을 관리하는 매니저입니다.</param>
        /// <param name="dialogueBalloonPool">말풍선 오브젝트를 재사용하기 위한 풀입니다.</param>
        public DialogueBalloonController(CutsceneManager manager, DialogueBalloonPool dialogueBalloonPool)
        {
            CutsceneManager = manager;
            _dialogueBalloonPool = dialogueBalloonPool;
        }

        /// <summary>
        /// 말풍선 이벤트 실행 전 필요한 사전 준비를 수행합니다.
        /// 현재는 별도의 준비 작업 없이 한 프레임을 양보합니다.
        /// </summary>
        /// <param name="evt">준비할 컷신 이벤트 정보입니다.</param>
        /// <returns>준비 과정을 비동기적으로 진행하기 위한 열거자입니다.</returns>
        public IEnumerator Ready(CutsceneEvent evt)
        {
            if (evt.type != CutsceneEventType.DialogueBalloon)
                yield break;
            
            // TODO: 필요 시 대상 캐릭터 유효성 검사나 말풍선 사전 할당을 이 단계에서 수행합니다.
            yield return null;
        }

        /// <summary>
        /// 대상 캐릭터에 말풍선을 생성하고 대사 내용을 설정합니다.
        /// 필요 시 카메라 추적 대상을 해당 캐릭터로 변경합니다.
        /// </summary>
        /// <param name="evt">실행할 컷신 이벤트 정보입니다.</param>
        public void Trigger(CutsceneEvent evt)
        {
            if (evt.type != CutsceneEventType.DialogueBalloon)
                return;

            // 이전 말풍선이 비정상적으로 종료되지 않은 경우 정리
            if (_isBalloon)
            {
                Stop();
            }

            var data = evt.dialogueBalloon;
            _isFollowTarget = data.isFollowTarget;
            _duration = evt.duration;

            // 말풍선을 표시할 대상 캐릭터 탐색
            _newTarget = GetTargetTransform(data.characterType, data.characterUid);
            if (_newTarget == null)
            {
                _newTarget = CutsceneManager.GetCharacter(data.characterType, data.characterUid);
            }

            if (_newTarget == null)
            {
                GcLogger.LogError("대사를 하는 캐릭터가 없습니다. type: " + data.characterType + "/ uid: " + data.characterUid);
                return;
            }

            _newTargetCharacter = _newTarget.GetComponent<CharacterBase>();
            if (_newTargetCharacter == null)
            {
                GcLogger.LogError("CharacterBase 가 없습니다. type: " + data.characterType + "/ uid: " + data.characterUid);
                return;
            }

            // 말풍선 오브젝트 확보
            _currentDialogueBalloon = _dialogueBalloonPool?.Get();
            if (_currentDialogueBalloon == null)
            {
                GcLogger.LogError("말풍선이 만들어지지 않았습니다.");
                return;
            }

            // 말풍선 초기화 및 텍스트 설정
            _currentDialogueBalloon
                .GetComponent<UIDialogueBalloon>()
                .Initialize(_newTargetCharacter, data);
            
            // 카메라 추적 대상 설정
            if (_isFollowTarget)
            {
                SceneGame.Instance.cameraManager.SetFollowTarget(_newTarget);
            }

            if (_newTarget.gameObject.activeSelf == false)
            {
                _newTarget.gameObject.SetActive(true);
            }

            _timer = 0f;
            _isBalloon = true;
        }

        /// <summary>
        /// 말풍선 표시 시간을 갱신하고 지정된 시간이 지나면 자동으로 종료합니다.
        /// </summary>
        public void Update()
        {
            if (!_isBalloon) return;
            
            _timer += Time.deltaTime;

            if (_timer >= _duration)
            {
                Stop();
            }
        }

        /// <summary>
        /// 현재 표시 중인 말풍선을 회수하고 상태를 초기화합니다.
        /// </summary>
        public void Stop()
        {
            _timer = 0f;
            _isBalloon = false;
            _dialogueBalloonPool?.Return(_currentDialogueBalloon);
            _currentDialogueBalloon = null;
        }

        /// <summary>
        /// 컷신 종료 시 추가 정리는 수행하지 않습니다.
        /// </summary>
        public void End()
        {
        }
    }
}