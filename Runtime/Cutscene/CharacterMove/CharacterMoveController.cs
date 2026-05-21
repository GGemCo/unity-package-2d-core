using System.Collections;
using UnityEngine;

namespace GGemCo2DCore
{
    /// <summary>
    /// 컷신 중 캐릭터를 지정된 방식으로 이동시키는 컨트롤러입니다.
    /// 절대 좌표 이동과 상대 좌표 이동(현재 위치/플레이어 위치 기준)을 모두 지원합니다.
    /// </summary>
    public class CharacterMoveController : CutsceneDefaultController, ICutsceneController
    {
        private const float MinimumDistanceEpsilon = 0.001f;
        private const float FallbackMoveStep = 1f;
        private const float FallbackMoveSpeedPercent = 100f;

        private Vector2 _startPosition;
        private Vector2 _endPosition;
        private float _characterMoveStep;
        private float _characterMoveSpeed;
        private float _distance;
        private float _timer;
        private float _duration;
        private bool _isMoving;
        private bool _isFollowTarget;

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
        /// 캐릭터 생성 책임이 CharacterSpawn 이벤트로 분리되어 즉시 준비를 지원합니다.
        /// </summary>
        public bool SupportsImmediateReady => true;

        /// <summary>
        /// 즉시 준비 단계에서 이벤트 타입만 검증합니다.
        /// </summary>
        /// <param name="evt">준비할 컷신 이벤트입니다.</param>
        public void ReadyImmediate(CutsceneEvent evt)
        {
            if (evt.type != CutsceneEventType.CharacterMove)
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
        /// 캐릭터 이동을 시작하고 위치, 속도, 방향, 애니메이션을 설정합니다.
        /// 이동 시간은 거리와 속도를 기반으로 계산됩니다.
        /// </summary>
        /// <param name="evt">실행할 컷신 이벤트입니다.</param>
        public void Trigger(CutsceneEvent evt)
        {
            if (evt.type != CutsceneEventType.CharacterMove)
            {
                return;
            }

            CharacterMoveData data = evt.characterMove ?? new CharacterMoveData();
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
            ConfigureMoveAttributes(data);
            ResolveMoveRange(data);

            _distance = Vector2.Distance(_startPosition, _endPosition);
            _duration = CalculateMoveDuration();
            _timer = 0f;
            _isMoving = false;

            ApplyFacingPolicy(data);

            if (_distance <= MinimumDistanceEpsilon || _duration <= 0f)
            {
                _target.position = new Vector3(_endPosition.x, _endPosition.y, _target.position.z);
                _targetCharacter?.Stop();
                return;
            }

            _targetCharacter?.SetStatusMoveForce();
            _targetCharacter?.CharacterAnimationController?.PlayRunAnimation();
            _isMoving = true;
        }

        /// <summary>
        /// 캐릭터 위치를 시간 기반으로 보간하여 이동시키고 완료 시 종료합니다.
        /// </summary>
        public void Update()
        {
            if (_target == null || !_isMoving)
            {
                return;
            }

            _timer += Time.deltaTime;
            float t = Mathf.Clamp01(_timer / _duration);

            Vector2 interpolated = Vector2.Lerp(_startPosition, _endPosition, t);
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

        /// <summary>
        /// CharacterMove 이벤트가 제어할 대상 캐릭터를 조회합니다.
        /// 맵 배치 대상 우선, 컷신에서 생성한 대상을 후순위로 조회합니다.
        /// </summary>
        /// <param name="data">대상 조회에 사용할 CharacterMove 데이터입니다.</param>
        /// <returns>조회한 대상 Transform이며, 없으면 <see langword="null"/>을 반환합니다.</returns>
        private Transform ResolveMoveTarget(CharacterMoveData data)
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
                "이동 대상 캐릭터가 없습니다. CharacterSpawn 이벤트를 먼저 실행했는지 확인하세요. type: " +
                data.characterType + "/ uid: " + data.characterUid);
            return null;
        }

        /// <summary>
        /// 이동 속도, 크기, 카메라 추적 등 이동 보조 속성을 설정합니다.
        /// </summary>
        /// <param name="data">이동 보조 속성 설정에 사용할 데이터입니다.</param>
        private void ConfigureMoveAttributes(CharacterMoveData data)
        {
            _characterMoveStep = ResolveMoveStep(data);
            _characterMoveSpeed = ResolveMoveSpeedPercent(data);

            if (_targetCharacter != null && data.characterScale > 0f)
            {
                _targetCharacter.SetScale(data.characterScale);
            }

            if (_isFollowTarget)
            {
                SceneGame.Instance?.cameraManager?.SetFollowTarget(_target);
            }
        }

        /// <summary>
        /// 설정된 이동 모드에 따라 시작/종료 좌표를 계산합니다.
        /// </summary>
        /// <param name="data">좌표 계산에 사용할 CharacterMove 데이터입니다.</param>
        private void ResolveMoveRange(CharacterMoveData data)
        {
            if (data.moveMode == CutsceneCharacterMoveMode.RelativeFromCurrent ||
                data.moveMode == CutsceneCharacterMoveMode.RelativeFromPlayer)
            {
                ResolveRelativeMoveRange(data);
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
        /// 상대 이동 모드에서 시작/종료 좌표를 계산합니다.
        /// 시작점은 항상 대상의 현재 위치를 사용하고, 종료점은 선택된 기준점(현재/플레이어)에서 계산합니다.
        /// </summary>
        /// <param name="data">상대 이동 계산에 사용할 CharacterMove 데이터입니다.</param>
        private void ResolveRelativeMoveRange(CharacterMoveData data)
        {
            Vector2 basePosition = ResolveRelativeBasePosition(data.moveMode);
            Vector2 direction = data.relativeDirection == CharacterConstants.FacingDirection8.None
                ? Vector2.zero
                : CharacterConstants.FacingToVector2(data.relativeDirection);
            float distance = Mathf.Max(0f, data.relativeDistance);
            Vector2 offset = data.relativeOffset.ToVector2();

            _startPosition = _target.position;
            _endPosition = basePosition + (direction * distance) + offset;
        }

        /// <summary>
        /// 상대 이동 모드에 맞는 기준 좌표를 계산합니다.
        /// </summary>
        /// <param name="moveMode">상대 이동 기준 모드입니다.</param>
        /// <returns>상대 이동 계산에 사용할 기준 좌표입니다.</returns>
        private Vector2 ResolveRelativeBasePosition(CutsceneCharacterMoveMode moveMode)
        {
            if (moveMode == CutsceneCharacterMoveMode.RelativeFromPlayer)
            {
                Transform player = ResolvePlayerTransform();
                if (player != null)
                {
                    return player.position;
                }

                GcLogger.Log(
                    "CharacterMove RelativeFromPlayer 모드에서 플레이어를 찾지 못해 현재 위치 기준으로 대체합니다.");
            }

            return _target.position;
        }

        /// <summary>
        /// 현재 씬 컨텍스트에서 플레이어 Transform을 조회합니다.
        /// 맵 배치 플레이어를 우선 탐색하고, 필요 시 컷신 런타임 캐시를 폴백으로 사용합니다.
        /// </summary>
        /// <returns>조회된 플레이어 Transform이며, 없으면 <see langword="null"/>을 반환합니다.</returns>
        private Transform ResolvePlayerTransform()
        {
            Transform player = GetTargetTransform(CharacterConstants.Type.Player, 0);
            if (player != null)
            {
                return player;
            }

            return CutsceneManager?.GetCharacter(CharacterConstants.Type.Player, 0);
        }

        /// <summary>
        /// 이동 시작 시 바라보기 정책을 적용합니다.
        /// </summary>
        /// <param name="data">바라보기 정책 정보를 포함한 CharacterMove 데이터입니다.</param>
        private void ApplyFacingPolicy(CharacterMoveData data)
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

        /// <summary>
        /// 이동 거리를 기준으로 현재 이벤트의 이동 시간을 계산합니다.
        /// </summary>
        /// <returns>이동 가능한 경우 계산된 이동 시간(초), 불가능하면 0을 반환합니다.</returns>
        private float CalculateMoveDuration()
        {
            if (_distance <= MinimumDistanceEpsilon)
            {
                return 0f;
            }

            float movePerSecond = _characterMoveStep * (_characterMoveSpeed / 100f);
            if (movePerSecond <= MinimumDistanceEpsilon)
            {
                return 0f;
            }

            return _distance / movePerSecond;
        }

        /// <summary>
        /// 이동 스텝 값을 계산합니다.
        /// 플레이어는 설정값을 사용하고, NPC/Monster는 테이블 이동 스텝을 우선 적용합니다.
        /// </summary>
        /// <param name="data">이동 대상 정보를 포함한 CharacterMove 데이터입니다.</param>
        /// <returns>최종 이동 스텝 값을 반환합니다.</returns>
        private static float ResolveMoveStep(CharacterMoveData data)
        {
            float step = AddressableLoaderSettings.Instance != null &&
                         AddressableLoaderSettings.Instance.playerSettings != null
                ? AddressableLoaderSettings.Instance.playerSettings.statMoveStep
                : FallbackMoveStep;

            if (data.characterType != CharacterConstants.Type.Player)
            {
                float tableStep = TableLoaderManager.Instance != null
                    ? TableLoaderManager.Instance.GetCharacterMoveStep(data.characterType, data.characterUid)
                    : 0f;

                if (tableStep > 0f)
                {
                    step = tableStep;
                }
            }

            return step > 0f ? step : FallbackMoveStep;
        }

        /// <summary>
        /// 이동 속도(%)를 계산합니다.
        /// 이벤트 값이 있으면 우선 적용하고, 없으면 현재 캐릭터 이동 속도를 사용합니다.
        /// </summary>
        /// <param name="data">이동 속도 설정을 포함한 CharacterMove 데이터입니다.</param>
        /// <returns>100 기준 퍼센트 이동 속도 값을 반환합니다.</returns>
        private float ResolveMoveSpeedPercent(CharacterMoveData data)
        {
            if (_targetCharacter != null && data.characterMoveSpeed > 0)
            {
                _targetCharacter.SetCurrentMoveSpeed(data.characterMoveSpeed);
            }

            float speed = _targetCharacter != null
                ? _targetCharacter.GetCurrentMoveSpeed(isPercent: false)
                : 0f;

            if (speed > 0f)
            {
                return speed;
            }

            return data.characterMoveSpeed > 0f
                ? data.characterMoveSpeed
                : FallbackMoveSpeedPercent;
        }
    }
}
