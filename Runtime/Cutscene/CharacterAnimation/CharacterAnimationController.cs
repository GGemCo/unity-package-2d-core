using System.Collections;
using UnityEngine;

namespace GGemCo2DCore
{
    /// <summary>
    /// 컷신 중 지정된 캐릭터의 상태를 설정하고 애니메이션을 재생하는 컨트롤러입니다.
    /// 캐릭터 생성 책임은 CharacterSpawn 이벤트로 분리되어, 이 컨트롤러는 기존 대상만 제어합니다.
    /// </summary>
    public class CharacterAnimationController : CutsceneDefaultController, ICutsceneController
    {
        private Camera _cam;
        
        private bool _isFollowTarget;
        private CharacterConstants.Type _characterType;
        private int _characterUid;
        private float _characterScale;
        private Vec2 _spawnPosition;
        private CutsceneCharacterAnimationFacingMode _facingMode;
        private CharacterConstants.FacingDirection8 _explicitFacing;
        
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
        /// 캐릭터 생성 책임이 분리되어 즉시 준비를 지원합니다.
        /// </summary>
        public bool SupportsImmediateReady => true;

        /// <summary>
        /// 즉시 준비 단계에서 이벤트 타입을 검증하고 파라미터를 캐시합니다.
        /// </summary>
        /// <param name="evt">준비할 컷신 이벤트 정보입니다.</param>
        public void ReadyImmediate(CutsceneEvent evt)
        {
            if (evt.type != CutsceneEventType.CharacterAnimation)
            {
                return;
            }

            SetParameter(evt);
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
            _facingMode = data.facingMode;
            _explicitFacing = data.explicitFacing;
            
            _animationName = data.animationName;
            _animationLoop = data.animationLoop;
            _animationTimeScale = data.animationTimeScale;
        }

        /// <summary>
        /// 캐릭터 애니메이션 실행 전 준비 단계를 수행합니다.
        /// 현재 구현에서는 즉시 준비 경로만 사용하며 비동기 준비는 수행하지 않습니다.
        /// </summary>
        /// <param name="evt">준비할 컷신 이벤트 정보입니다.</param>
        /// <returns>즉시 종료되는 코루틴 열거자입니다.</returns>
        public IEnumerator Ready(CutsceneEvent evt)
        {
            ReadyImmediate(evt);
            yield break;
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
                        "애니메이션 대상 캐릭터가 없습니다. CharacterSpawn 이벤트를 먼저 실행했는지 확인하세요. type: " +
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
                ApplyFacingPolicy();

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
        /// CharacterAnimation 이벤트의 바라보기 정책을 적용합니다.
        /// </summary>
        private void ApplyFacingPolicy()
        {
            if (_targetCharacter == null)
            {
                return;
            }

            switch (_facingMode)
            {
                case CutsceneCharacterAnimationFacingMode.FacePlayer:
                    ApplyFacePlayerPolicy();
                    return;

                case CutsceneCharacterAnimationFacingMode.FaceExplicit:
                default:
                    ApplyExplicitFacingPolicy();
                    return;
            }
        }

        /// <summary>
        /// 이벤트에 지정된 8방향 값을 기준으로 바라보기 방향을 적용합니다.
        /// </summary>
        private void ApplyExplicitFacingPolicy()
        {
            if (_explicitFacing == CharacterConstants.FacingDirection8.None)
            {
                return;
            }

            _targetCharacter.SetFacing(_explicitFacing);
        }

        /// <summary>
        /// 플레이어 위치를 바라보도록 방향을 계산해 적용합니다.
        /// 플레이어를 찾지 못했거나 방향 벡터를 계산할 수 없는 경우 explicitFacing으로 폴백합니다.
        /// </summary>
        private void ApplyFacePlayerPolicy()
        {
            Transform player = ResolvePlayerTransform();
            if (player == null || player == _target)
            {
                ApplyExplicitFacingPolicy();
                return;
            }

            Vector2 direction = player.position - _target.position;
            if (direction.sqrMagnitude <= 0.0001f)
            {
                ApplyExplicitFacingPolicy();
                return;
            }

            _targetCharacter.SetFacing(direction.normalized);
        }

        /// <summary>
        /// 현재 씬에서 플레이어 Transform을 조회합니다.
        /// 맵 배치 플레이어를 우선 탐색하고, 필요 시 컷신 추적 캐시를 폴백으로 사용합니다.
        /// </summary>
        /// <returns>조회된 플레이어 Transform이며, 없으면 <see langword="null"/>을 반환합니다.</returns>
        private Transform ResolvePlayerTransform()
        {
            Transform player = GetTargetTransform(CharacterConstants.Type.Player, 0);
            if (player != null)
            {
                return player;
            }

            return CutsceneManager.GetCharacter(CharacterConstants.Type.Player, 0);
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
