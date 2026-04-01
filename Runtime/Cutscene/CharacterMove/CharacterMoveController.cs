using System.Collections;
using UnityEngine;

namespace GGemCo2DCore
{
    /// <summary>
    /// 컷신 중 캐릭터를 지정된 위치까지 이동시키는 컨트롤러입니다.
    /// 이동 속도, 스텝(step), 방향 및 애니메이션을 함께 제어합니다.
    /// </summary>
    public class CharacterMoveController : CutsceneDefaultController, ICutsceneController
    {
        private Camera _cam;

        private Vector2 _startPosition, _endPosition;
        private float _characterMoveStep;
        private float _characterMoveSpeed;
        private float _distance;
        private float _timer;
        private bool _isMoving;
        private bool _isFollowTarget;
        
        private float _duration;

        private Transform _target;
        private CharacterBase _targetCharacter;

        /// <summary>
        /// 캐릭터 이동 컨트롤러를 생성합니다.
        /// </summary>
        /// <param name="manager">컷신 흐름을 관리하는 매니저입니다.</param>
        public CharacterMoveController(CutsceneManager manager)
        {
            CutsceneManager = manager;
        }

        /// <summary>
        /// 이 컨트롤러는 캐릭터 생성 시 한 프레임 대기가 필요할 수 있으므로 즉시 준비를 지원하지 않습니다.
        /// </summary>
        public bool SupportsImmediateReady => false;

        /// <summary>
        /// 즉시 준비 경로에서는 별도 동작을 수행하지 않습니다.
        /// </summary>
        public void ReadyImmediate(CutsceneEvent evt)
        {
        }


        /// <summary>
        /// 이동 대상 캐릭터를 준비합니다.
        /// 존재하지 않을 경우 생성 및 초기화를 수행합니다.
        /// </summary>
        /// <param name="evt">준비할 컷신 이벤트입니다.</param>
        /// <returns>캐릭터 생성 및 초기화를 위한 비동기 처리 열거자입니다.</returns>
        public IEnumerator Ready(CutsceneEvent evt)
        {
            if (evt.type != CutsceneEventType.CharacterMove)
                yield break;

            var data = evt.characterMove;
            
            Transform character = GetTargetTransform(data.characterType, data.characterUid);

            // 현재 맵에 캐릭터가 없으면 생성
            if (character == null)
            {
                character = CutsceneManager.GetCharacter(data.characterType, data.characterUid);

                if (character == null)
                {
                    character = SceneGame.Instance.CharacterManager
                        .CreateCharacter(data.characterType, data.characterUid)?.transform;

                    if (character == null)
                        yield break;
                    
                    // TODO: startPosition이 아직 설정되지 않았을 수 있음 (Trigger 의존)
                    character.transform.position = _startPosition;

                    character.transform.SetParent(
                        SceneGame.Instance.mapManager.GetCurrentMap()?.transform);

                    character.position = _startPosition;
                    
                    CharacterBase characterBase = character.GetComponent<CharacterBase>();
                    characterBase.uid = data.characterUid;

                    // Awake/Start 호출 보장
                    yield return null;

                    character.gameObject.SetActive(false);

                    // 컷신 매니저 등록
                    CutsceneManager.AddCharacter(
                        data.characterType,
                        data.characterUid,
                        character.gameObject);
                }
            }
            
            yield return null;
        }

        /// <summary>
        /// 캐릭터 이동을 시작하고 위치, 속도, 방향, 애니메이션을 설정합니다.
        /// 이동 시간은 거리와 속도를 기반으로 계산됩니다.
        /// </summary>
        /// <param name="evt">실행할 컷신 이벤트입니다.</param>
        public void Trigger(CutsceneEvent evt)
        {
            if (evt.type != CutsceneEventType.CharacterMove)
                return;

            _duration = evt.duration;
            var data = evt.characterMove;

            _isFollowTarget = data.isFollowTarget;

            _target = GetTargetTransform(data.characterType, data.characterUid);

            if (_target == null)
            {
                _target = CutsceneManager.GetCharacter(data.characterType, data.characterUid);

                if (_target == null)
                {
                    GcLogger.LogError(
                        "이동 시킬 캐릭터가 없습니다. type: " +
                        data.characterType + "/ uid: " + data.characterUid);
                    return;
                }
            }

            if (_target.gameObject.activeSelf == false)
            {
                _target.gameObject.SetActive(true);
            }

            _startPosition = data.startPosition.ToVector2();
            _endPosition = data.endPosition.ToVector2();

            // 시작 위치 미지정 시 현재 위치 사용
            if (_startPosition == Vector2.zero)
            {
                _startPosition = _target.position;
            }

            _distance = Vector2.Distance(_startPosition, _endPosition);
            
            if (_target != null)
            {
                _targetCharacter = _target.GetComponent<CharacterBase>();

                // 이동 step 설정 (플레이어 vs NPC)
                _characterMoveStep = AddressableLoaderSettings.Instance.playerSettings.statMoveStep;

                if (data.characterType != CharacterConstants.Type.Player)
                {
                    _characterMoveStep =
                        TableLoaderManager.Instance.GetCharacterMoveStep(
                            data.characterType,
                            data.characterUid);
                }

                // 이동 속도 설정
                if (data.characterMoveSpeed > 0)
                {
                    _targetCharacter?.SetCurrentMoveSpeed(data.characterMoveSpeed);
                    _characterMoveSpeed = data.characterMoveSpeed;
                }

                // 크기 설정
                if (data.characterScale > 0)
                {
                    _targetCharacter?.SetScale(data.characterScale);
                }

                // 카메라 추적 설정
                if (_isFollowTarget)
                {
                    SceneGame.Instance.cameraManager.SetFollowTarget(_target);
                }

                // 이동 상태 강제 적용
                _targetCharacter?.SetStatusMoveForce();

                // 이동 애니메이션 실행
                _targetCharacter?.CharacterAnimationController?.PlayRunAnimation();
            }

            // 거리 / 속도 기반 이동 시간 계산
            _duration = _distance / (_characterMoveStep * (_characterMoveSpeed / 100f));
            
            _timer = 0f;

            // 이동 방향에 따른 flip 설정
            UpdateFacing();

            _isMoving = true;
        }

        /// <summary>
        /// 캐릭터 위치를 시간 기반으로 보간하여 이동시키고 완료 시 종료합니다.
        /// </summary>
        public void Update()
        {
            if (_target == null || !_isMoving) return;

            _timer += Time.deltaTime;

            float t = _timer * _characterMoveStep * (_characterMoveSpeed / 100f) / _distance;
            t = Mathf.Clamp01(t);

            Vector2 interpolated = Vector2.Lerp(_startPosition, _endPosition, t);

            _target.position = new Vector3(
                interpolated.x,
                interpolated.y,
                _target.position.z);

            if (_timer > _duration)
            {
                Stop();
            }
        }

        /// <summary>
        /// 이동 방향을 기준으로 캐릭터 좌우 방향(flip)을 설정합니다.
        /// </summary>
        private void UpdateFacing()
        {
            if (_target == null) return;

            Vector2 direction = _endPosition - _startPosition;

            // 좌우 기준 flip 처리
            if (Mathf.Abs(direction.x) > Mathf.Abs(direction.y))
            {
                bool movingRight = direction.x > 0f;

                bool defaultIsRight =
                    _targetCharacter?.defaultFacingDirection8 ==
                    CharacterConstants.FacingDirection8.Right;

                bool shouldFlip = (movingRight != defaultIsRight);

                _targetCharacter?.SetFlip(shouldFlip);
            }

            // TODO: 상하 방향 처리 필요 시 확장 가능
        }

        /// <summary>
        /// 캐릭터 이동을 중지하고 상태를 정리합니다.
        /// </summary>
        public void Stop()
        {
            _targetCharacter?.Stop();
            _isMoving = false;
        }

        /// <summary>
        /// 컷신 종료 시 추가 정리는 수행하지 않습니다.
        /// </summary>
        public void End()
        {
        }
    }
}