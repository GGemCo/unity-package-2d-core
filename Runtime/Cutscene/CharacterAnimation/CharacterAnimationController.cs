using System.Collections;
using UnityEngine;

namespace GGemCo2DCore
{
    /// <summary>
    /// 컷신 중 캐릭터를 생성/설정하고 지정된 애니메이션을 재생하는 컨트롤러입니다.
    /// 필요 시 캐릭터를 스폰하고 카메라 추적 대상 및 상태를 함께 제어합니다.
    /// </summary>
    public class CharacterAnimationController : CutsceneDefaultController, ICutsceneController
    {
        private Camera _cam;
        
        private bool _isFollowTarget;
        private CharacterConstants.Type _characterType;
        private int _characterUid;
        private float _characterScale;
        private Vec2 _spawnPosition;
        private bool _isFlip;
        
        private string _animationName;
        private bool _animationLoop;
        private float _animationTimeScale;
        
        private float _timer;
        private float _duration;
        private bool _isAnimation;

        private Transform _target;
        private CharacterBase _targetCharacter;

        /// <summary>
        /// 캐릭터 애니메이션 연출 컨트롤러를 생성합니다.
        /// </summary>
        /// <param name="manager">현재 컷신 흐름을 관리하는 매니저입니다.</param>
        public CharacterAnimationController(CutsceneManager manager)
        {
            CutsceneManager = manager;
        }

        /// <summary>
        /// 컷신 이벤트 데이터에서 캐릭터 및 애니메이션 관련 파라미터를 추출하여 내부 상태에 설정합니다.
        /// </summary>
        /// <param name="evt">설정에 사용할 컷신 이벤트 정보입니다.</param>
        private void SetParameter(CutsceneEvent evt)
        {
            _duration = evt.duration;

            var data = evt.characterAnimation;
            _isFollowTarget = data.isFollowTarget;
            _characterType = data.characterType;
            _characterUid = data.characterUid;
            _characterScale = data.characterScale;
            _spawnPosition = data.spawnPosition;
            _isFlip = data.isFlip;
            
            _animationName = data.animationName;
            _animationLoop = data.animationLoop;
            _animationTimeScale = data.animationTimeScale;
        }

        /// <summary>
        /// 캐릭터 애니메이션 실행 전 필요한 캐릭터를 준비합니다.
        /// 현재 맵에 존재하지 않는 경우 생성 및 초기 설정을 수행합니다.
        /// </summary>
        /// <param name="evt">준비할 컷신 이벤트 정보입니다.</param>
        /// <returns>캐릭터 생성 및 초기화를 위한 비동기 처리 열거자입니다.</returns>
        public IEnumerator Ready(CutsceneEvent evt)
        {
            if (evt.type != CutsceneEventType.CharacterAnimation)
                yield break;

            SetParameter(evt);
            
            Transform character = GetTargetTransform(_characterType, _characterUid);

            // 현재 맵에 캐릭터가 없으면 생성
            if (character == null)
            {
                character = CutsceneManager.GetCharacter(_characterType, _characterUid);

                if (character == null)
                {
                    character = SceneGame.Instance.CharacterManager
                        .CreateCharacter(_characterType, _characterUid)?.transform;

                    if (character == null)
                        yield break;

                    // 스폰 위치 지정
                    if (_spawnPosition.ToVector2() != Vector2.zero)
                    {
                        character.transform.position = _spawnPosition.ToVector2();
                    }

                    // 현재 맵에 부모 설정
                    character.transform.SetParent(
                        SceneGame.Instance.mapManager.GetCurrentMap()?.transform);

                    CharacterBase characterBase = character.GetComponent<CharacterBase>();
                    characterBase.uid = _characterUid;

                    // Awake/Start 호출 보장
                    yield return null;

                    character.gameObject.SetActive(false);

                    // 컷신 매니저에 등록
                    CutsceneManager.AddCharacter(
                        _characterType,
                        _characterUid,
                        character.gameObject);
                }
            }

            yield return null;
        }

        /// <summary>
        /// 캐릭터를 활성화하고 위치, 방향, 스케일을 설정한 뒤 애니메이션을 재생합니다.
        /// 필요 시 카메라 추적 대상으로 지정합니다.
        /// </summary>
        /// <param name="evt">실행할 컷신 이벤트 정보입니다.</param>
        public void Trigger(CutsceneEvent evt)
        {
            if (evt.type != CutsceneEventType.CharacterAnimation)
                return;

            SetParameter(evt);
            
            _target = GetTargetTransform(_characterType, _characterUid);

            if (_target == null)
            {
                _target = CutsceneManager.GetCharacter(_characterType, _characterUid);

                if (_target == null)
                {
                    GcLogger.LogError(
                        "이동 시킬 캐릭터가 없습니다. type: " +
                        _characterType + "/ uid: " + _characterUid);
                    return;
                }
            }

            if (_target.gameObject.activeSelf == false)
            {
                _target.gameObject.SetActive(true);
            }
            
            if (_target != null)
            {
                _targetCharacter = _target.GetComponent<CharacterBase>();

                // 크기 설정
                if (_characterScale > 0)
                {
                    _targetCharacter?.SetScale(_characterScale);
                }

                // 위치 설정
                if (_spawnPosition.ToVector2() != Vector2.zero)
                {
                    _target.transform.position = _spawnPosition.ToVector2();
                }

                // 방향 설정
                _targetCharacter?.SetFlip(_isFlip);

                // 카메라 추적 대상 설정
                if (_isFollowTarget)
                {
                    SceneGame.Instance.cameraManager.SetFollowTarget(_target);
                }

                // 강제 이동 상태 설정
                _targetCharacter?.SetStatusMoveForce();

                // 애니메이션 재생
                if (_animationName != "")
                {
                    _targetCharacter?.CharacterAnimationController
                        ?.PlayCharacterAnimation(
                            _animationName,
                            _animationLoop,
                            _animationTimeScale);
                }
            }

            _timer = 0f;
            _isAnimation = true;
        }

        /// <summary>
        /// 애니메이션 진행 시간을 갱신하고 지정된 시간이 지나면 자동으로 종료합니다.
        /// </summary>
        public void Update()
        {
            if (!_isAnimation) return;
            
            _timer += Time.deltaTime;
            
            if (_timer > _duration)
            {
                Stop();
            }
        }

        /// <summary>
        /// 현재 캐릭터의 동작을 중지하고 애니메이션 상태를 종료합니다.
        /// </summary>
        public void Stop()
        {
            _targetCharacter?.Stop();
            _isAnimation = false;
        }

        /// <summary>
        /// 컷신 종료 시 추가 정리는 수행하지 않습니다.
        /// </summary>
        public void End()
        {
        }
    }
}