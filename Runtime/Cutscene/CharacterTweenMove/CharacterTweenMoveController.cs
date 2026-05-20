using System.Collections;
using UnityEngine;

namespace GGemCo2DCore
{
    /// <summary>
    /// 컷신 중 캐릭터를 Duration 기반 트윈 보간으로 이동시키는 컨트롤러입니다.
    /// Run 애니메이션과 이동 속도 변경 없이 오브젝트 이동 연출에 집중합니다.
    /// </summary>
    public sealed class CharacterTweenMoveController : CutsceneDefaultController, ICutsceneController
    {
        private const float MinimumDistanceEpsilon = 0.001f;

        private Vector2 _startPosition;
        private Vector2 _endPosition;
        private float _duration;
        private float _timer;
        private bool _isMoving;
        private bool _isFollowTarget;
        private Easing.EaseType _easing;

        private Transform _target;
        private CharacterBase _targetCharacter;

        /// <summary>
        /// CharacterTweenMove 컨트롤러를 생성합니다.
        /// </summary>
        /// <param name="manager">컷신 흐름을 관리하는 매니저입니다.</param>
        public CharacterTweenMoveController(CutsceneManager manager)
        {
            CutsceneManager = manager;
        }

        /// <summary>
        /// 캐릭터 생성 책임이 CharacterSpawn 이벤트로 분리되어 즉시 준비를 지원합니다.
        /// </summary>
        public bool SupportsImmediateReady => true;

        /// <summary>
        /// 즉시 준비 단계에서 이벤트 타입만 검증합니다.
        /// </summary>
        /// <param name="evt">준비할 컷신 이벤트입니다.</param>
        public void ReadyImmediate(CutsceneEvent evt)
        {
            if (evt.type != CutsceneEventType.CharacterTweenMove)
            {
                return;
            }
        }

        /// <summary>
        /// 이동 대상 준비 단계를 수행합니다.
        /// 현재 구현에서는 비동기 준비가 필요 없어 즉시 종료됩니다.
        /// </summary>
        /// <param name="evt">준비할 컷신 이벤트입니다.</param>
        /// <returns>즉시 종료되는 코루틴 열거자입니다.</returns>
        public IEnumerator Ready(CutsceneEvent evt)
        {
            ReadyImmediate(evt);
            yield break;
        }

        /// <summary>
        /// CharacterTweenMove 이벤트를 시작합니다.
        /// Duration과 Easing을 기반으로 시작/종료 위치를 보간합니다.
        /// </summary>
        /// <param name="evt">실행할 컷신 이벤트입니다.</param>
        public void Trigger(CutsceneEvent evt)
        {
            if (evt.type != CutsceneEventType.CharacterTweenMove)
            {
                return;
            }

            CharacterTweenMoveData data = evt.characterTweenMove ?? new CharacterTweenMoveData();

            _isFollowTarget = data.isFollowTarget;
            _target = ResolveMoveTarget(data);
            if (_target == null)
            {
                return;
            }

            if (!_target.gameObject.activeSelf)
            {
                _target.gameObject.SetActive(true);
            }

            _targetCharacter = _target.GetComponent<CharacterBase>();
            ResolveMoveRange(data);
            ApplyFacingPolicy(data);

            _duration = Mathf.Max(0f, evt.duration);
            _timer = 0f;
            _easing = data.easing;
            _isMoving = false;

            if (_isFollowTarget)
            {
                SceneGame.Instance?.cameraManager?.SetFollowTarget(_target);
            }

            if (Vector2.Distance(_startPosition, _endPosition) <= MinimumDistanceEpsilon || _duration <= 0f)
            {
                _target.position = new Vector3(_endPosition.x, _endPosition.y, _target.position.z);
                return;
            }

            _isMoving = true;
        }

        /// <summary>
        /// 트윈 이동 진행 상태를 갱신합니다.
        /// </summary>
        public void Update()
        {
            if (_target == null || !_isMoving)
            {
                return;
            }

            _timer += Time.deltaTime;
            float t = Mathf.Clamp01(_timer / Mathf.Max(0.0001f, _duration));
            float eased = Mathf.Clamp01(Easing.Apply(t, _easing));

            Vector2 interpolated = Vector2.Lerp(_startPosition, _endPosition, eased);
            _target.position = new Vector3(
                interpolated.x,
                interpolated.y,
                _target.position.z);

            if (t >= 1f)
            {
                Stop();
            }
        }

        /// <summary>
        /// 트윈 이동을 중지합니다.
        /// </summary>
        public void Stop()
        {
            _isMoving = false;
        }

        /// <summary>
        /// 컷신 종료 시 트윈 이동 상태를 정리합니다.
        /// </summary>
        public void End()
        {
            _isMoving = false;
        }

        /// <summary>
        /// CharacterTweenMove 이벤트가 제어할 대상 캐릭터를 조회합니다.
        /// 맵 배치 대상 우선, 컷신에서 생성한 대상을 후순위로 조회합니다.
        /// </summary>
        /// <param name="data">대상 조회에 사용할 CharacterTweenMove 데이터입니다.</param>
        /// <returns>조회한 대상 Transform이며, 없으면 <see langword="null"/>을 반환합니다.</returns>
        private Transform ResolveMoveTarget(CharacterTweenMoveData data)
        {
            Transform target = GetTargetTransform(data.characterType, data.characterUid);
            if (target != null)
            {
                return target;
            }

            target = CutsceneManager.GetCharacter(data.characterType, data.characterUid);
            if (target != null)
            {
                return target;
            }

            GcLogger.LogError(
                "트윈 이동 대상 캐릭터가 없습니다. CharacterSpawn 이벤트를 먼저 실행했는지 확인하세요. type: " +
                data.characterType + "/ uid: " + data.characterUid);
            return null;
        }

        /// <summary>
        /// 설정된 이동 모드에 따라 시작/종료 좌표를 계산합니다.
        /// </summary>
        /// <param name="data">좌표 계산에 사용할 CharacterTweenMove 데이터입니다.</param>
        private void ResolveMoveRange(CharacterTweenMoveData data)
        {
            if (data.moveMode == CutsceneCharacterMoveMode.RelativeFromCurrent)
            {
                Vector2 basePosition = _target.position;
                Vector2 direction = data.relativeDirection == CharacterConstants.FacingDirection8.None
                    ? Vector2.zero
                    : CharacterConstants.FacingToVector2(data.relativeDirection);
                float distance = Mathf.Max(0f, data.relativeDistance);
                Vector2 offset = data.relativeOffset.ToVector2();

                _startPosition = basePosition;
                _endPosition = basePosition + (direction * distance) + offset;
                return;
            }

            _startPosition = data.startPosition.ToVector2();
            _endPosition = data.endPosition.ToVector2();

            // 레거시 호환: startPosition이 (0,0)인 경우 "미지정"으로 간주하고 현재 위치를 시작점으로 사용합니다.
            if (_startPosition == Vector2.zero)
            {
                _startPosition = _target.position;
            }
        }

        /// <summary>
        /// 이동 시작 시 바라보기 정책을 적용합니다.
        /// </summary>
        /// <param name="data">바라보기 정책 정보를 포함한 CharacterTweenMove 데이터입니다.</param>
        private void ApplyFacingPolicy(CharacterTweenMoveData data)
        {
            if (_targetCharacter == null)
            {
                return;
            }

            switch (data.facingMode)
            {
                case CutsceneCharacterMoveFacingMode.KeepCurrent:
                    return;

                case CutsceneCharacterMoveFacingMode.FaceExplicit:
                    if (data.explicitFacing != CharacterConstants.FacingDirection8.None)
                    {
                        _targetCharacter.SetFacing(data.explicitFacing);
                    }

                    return;

                case CutsceneCharacterMoveFacingMode.FaceMoveDirection:
                default:
                    Vector2 direction = _endPosition - _startPosition;
                    if (direction.sqrMagnitude > MinimumDistanceEpsilon * MinimumDistanceEpsilon)
                    {
                        _targetCharacter.SetFacing(direction.normalized);
                    }

                    return;
            }
        }
    }
}
