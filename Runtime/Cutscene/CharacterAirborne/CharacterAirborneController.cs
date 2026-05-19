using System.Collections;
using UnityEngine;

namespace GGemCo2DCore
{
    /// <summary>
    /// 컷신 타임라인에 따라 특정 캐릭터의 공중 상태(높이/중력)를 제어하는 컨트롤러입니다.
    /// owner 기반 소유권을 사용해 동일 캐릭터에 대한 동시 제어 충돌을 방지합니다.
    /// </summary>
    public sealed class CharacterAirborneController : CutsceneDefaultController, ICutsceneController
    {
        private const float GroundProbeDistance = 32f;

        private CharacterAirborneData _data;
        private CharacterBase _targetCharacter;
        private Rigidbody2D _targetRigidbody;

        private CharacterPhysicsOverrideController _physicsOverrideController;
        private CharacterPhysicsOverrideHandle _gravityOverrideHandle;

        private float _elapsed;
        private float _duration;
        private float _groundY;
        private float _pivotOffsetFromBottom;
        private float _fromAirHeight;
        private float _toAirHeight;

        private float _capturedWorldPositionY;
        private bool _capturedActiveState;

        private bool _isPlaying;
        private bool _hasOwnership;
        private bool _isMaintainingAirborneAfterComplete;

        /// <summary>
        /// 캐릭터 공중 연출 컨트롤러를 생성합니다.
        /// </summary>
        /// <param name="manager">현재 컷신 흐름을 관리하는 매니저입니다.</param>
        public CharacterAirborneController(CutsceneManager manager)
        {
            CutsceneManager = manager;
        }

        /// <summary>
        /// 다음 프레임 대기 없이 즉시 준비를 지원합니다.
        /// </summary>
        public bool SupportsImmediateReady => true;

        /// <summary>
        /// 다음 프레임 대기 없이 즉시 준비를 수행합니다.
        /// 현재 구현에서는 이벤트 타입 검증만 수행합니다.
        /// </summary>
        /// <param name="evt">준비할 컷신 이벤트입니다.</param>
        public void ReadyImmediate(CutsceneEvent evt)
        {
            if (evt.type != CutsceneEventType.CharacterAirborne)
            {
                return;
            }
        }

        /// <summary>
        /// 캐릭터 공중 연출 이벤트 준비를 수행합니다.
        /// </summary>
        /// <param name="evt">준비할 컷신 이벤트입니다.</param>
        /// <returns>코루틴 열거자입니다.</returns>
        public IEnumerator Ready(CutsceneEvent evt)
        {
            ReadyImmediate(evt);
            yield break;
        }

        /// <summary>
        /// 캐릭터 공중 연출을 시작합니다.
        /// </summary>
        /// <param name="evt">실행할 컷신 이벤트입니다.</param>
        public void Trigger(CutsceneEvent evt)
        {
            if (evt.type != CutsceneEventType.CharacterAirborne)
            {
                return;
            }

            Stop();

            _data = evt.characterAirborne ?? new CharacterAirborneData();
            _targetCharacter = ResolveTargetCharacter(_data);
            if (_targetCharacter == null)
            {
                GcLogger.LogError("CharacterAirborne target 캐릭터를 찾을 수 없습니다.");
                ClearRuntimeState();
                return;
            }

            if (!CutsceneCharacterAirborneOwnershipService.TryAcquire(
                    _targetCharacter,
                    this,
                    _data.allowReplace,
                    out _capturedWorldPositionY,
                    out _capturedActiveState))
            {
                GcLogger.Log(
                    "CharacterAirborne owner 획득에 실패했습니다. type: " +
                    _targetCharacter.type + "/ uid: " + _targetCharacter.uid);
                ClearRuntimeState();
                return;
            }

            _hasOwnership = true;
            _targetRigidbody = _targetCharacter.characterRigidbody2D != null
                ? _targetCharacter.characterRigidbody2D
                : _targetCharacter.GetComponent<Rigidbody2D>();

            _toAirHeight = _data.ResolveTargetAirHeight();
            ResolveCurrentAirState(out _groundY, out _fromAirHeight, out _pivotOffsetFromBottom);

            _duration = evt.duration > 0f ? evt.duration : 0f;
            _elapsed = 0f;
            _isPlaying = _duration > 0f && !Mathf.Approximately(_fromAirHeight, _toAirHeight);
            _isMaintainingAirborneAfterComplete = false;

            bool needGravityOverride =
                _toAirHeight > 0f ||
                _fromAirHeight > 0f ||
                _data.keepAirborneGravity ||
                _isPlaying;

            if (needGravityOverride)
            {
                EnsureGravityOverride();
            }

            if (_duration <= 0f || !_isPlaying)
            {
                ApplyAirHeight(_toAirHeight);
                FinalizeCompletedTransition();
                return;
            }

            ApplyAirHeight(_fromAirHeight);
        }

        /// <summary>
        /// 시간 경과에 따라 공중 높이를 보간하여 적용합니다.
        /// </summary>
        public void Update()
        {
            if (!_isPlaying && !_isMaintainingAirborneAfterComplete)
            {
                return;
            }

            if (_targetCharacter == null)
            {
                // 대상 참조가 유실된 경우 owner 기준 일괄 해제로 소유권 테이블 누수를 방지합니다.
                CutsceneCharacterAirborneOwnershipService.ReleaseAllByOwner(this);
                ReleaseGravityOverride();
                ClearRuntimeState();
                return;
            }

            if (!CutsceneCharacterAirborneOwnershipService.IsOwnedBy(_targetCharacter, this))
            {
                ReleaseGravityOverride();
                ClearRuntimeState();
                return;
            }

            if (_isPlaying)
            {
                _elapsed += _data != null && _data.useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;

                float t = Mathf.Clamp01(_elapsed / Mathf.Max(0.0001f, _duration));
                float eased = Mathf.Clamp01(Easing.Apply(t, _data.easing));
                float airHeight = Mathf.Lerp(_fromAirHeight, _toAirHeight, eased);
                ApplyAirHeight(airHeight);

                if (_elapsed >= _duration)
                {
                    _isPlaying = false;
                    FinalizeCompletedTransition();
                }

                return;
            }

            if (_isMaintainingAirborneAfterComplete)
            {
                ApplyAirHeight(_toAirHeight);
            }
        }

        /// <summary>
        /// 현재 공중 연출을 중지하고 정책에 따라 높이를 복원합니다.
        /// </summary>
        public void Stop()
        {
            StopInternal(_data != null && _data.restoreHeightOnStop);
        }

        /// <summary>
        /// 컷신 종료 시 정책에 따라 공중 연출 상태를 정리합니다.
        /// </summary>
        public void End()
        {
            StopInternal(_data != null && _data.restoreHeightOnCutsceneEnd);
        }

        /// <summary>
        /// 공중 연출 종료를 공통 처리합니다.
        /// </summary>
        /// <param name="restoreHeight">종료 시 시작 높이로 복원할지 여부입니다.</param>
        private void StopInternal(bool restoreHeight)
        {
            _isPlaying = false;
            _isMaintainingAirborneAfterComplete = false;

            if (!_hasOwnership)
            {
                ReleaseGravityOverride();
                ClearRuntimeState();
                return;
            }

            if (_targetCharacter == null)
            {
                CutsceneCharacterAirborneOwnershipService.ReleaseAllByOwner(this);
                ReleaseGravityOverride();
                ClearRuntimeState();
                return;
            }

            bool isOwner = CutsceneCharacterAirborneOwnershipService.IsOwnedBy(_targetCharacter, this);
            if (isOwner && restoreHeight)
            {
                RestoreCapturedHeight();
            }

            CutsceneCharacterAirborneOwnershipService.Release(_targetCharacter, this);
            ReleaseGravityOverride();
            ClearRuntimeState();
        }

        /// <summary>
        /// 공중 높이 보간이 정상 완료되었을 때의 후처리를 수행합니다.
        /// </summary>
        private void FinalizeCompletedTransition()
        {
            if (!_hasOwnership || _targetCharacter == null)
            {
                // 완료 시점에 대상이 유실되었으면 owner 기준으로 안전 정리합니다.
                CutsceneCharacterAirborneOwnershipService.ReleaseAllByOwner(this);
                ReleaseGravityOverride();
                ClearRuntimeState();
                return;
            }

            bool isOwner = CutsceneCharacterAirborneOwnershipService.IsOwnedBy(_targetCharacter, this);
            if (!isOwner)
            {
                ReleaseGravityOverride();
                ClearRuntimeState();
                return;
            }

            bool keepAirborne = _toAirHeight > 0f && _data != null && _data.keepAirborneGravity;
            _isMaintainingAirborneAfterComplete = keepAirborne;

            if (keepAirborne)
            {
                return;
            }

            CutsceneCharacterAirborneOwnershipService.Release(_targetCharacter, this);
            ReleaseGravityOverride();
            ClearRuntimeState();
        }

        /// <summary>
        /// 현재 공중 높이를 적용해 캐릭터의 월드 Y를 갱신합니다.
        /// </summary>
        /// <param name="airHeight">적용할 공중 높이(지면 기준 +Y)입니다.</param>
        private void ApplyAirHeight(float airHeight)
        {
            if (_targetCharacter == null)
            {
                return;
            }

            float clampedAirHeight = Mathf.Max(0f, airHeight);
            float targetBottomY = _groundY + clampedAirHeight;
            float targetWorldY = targetBottomY + _pivotOffsetFromBottom;

            var currentPosition = _targetCharacter.transform.position;
            _targetCharacter.transform.position = new Vector3(
                currentPosition.x,
                targetWorldY,
                currentPosition.z);

            ZeroRigidbodyVelocity();
        }

        /// <summary>
        /// 시작 시점의 높이로 캐릭터 위치를 복원합니다.
        /// </summary>
        private void RestoreCapturedHeight()
        {
            if (_targetCharacter == null)
            {
                return;
            }

            if (_capturedActiveState && !_targetCharacter.gameObject.activeSelf)
            {
                _targetCharacter.gameObject.SetActive(true);
            }

            var currentPosition = _targetCharacter.transform.position;
            _targetCharacter.transform.position = new Vector3(
                currentPosition.x,
                _capturedWorldPositionY,
                currentPosition.z);

            ZeroRigidbodyVelocity();

            if (!_capturedActiveState && _targetCharacter.gameObject.activeSelf)
            {
                _targetCharacter.gameObject.SetActive(false);
            }
        }

        /// <summary>
        /// 대상 캐릭터의 현재 공중 상태를 해석합니다.
        /// </summary>
        /// <param name="groundY">해석된 지면 Y입니다.</param>
        /// <param name="currentAirHeight">해석된 현재 공중 높이입니다.</param>
        /// <param name="pivotOffsetFromBottom">캐릭터 pivot과 collider 하단 사이의 오프셋입니다.</param>
        private void ResolveCurrentAirState(out float groundY, out float currentAirHeight, out float pivotOffsetFromBottom)
        {
            groundY = 0f;
            currentAirHeight = 0f;
            pivotOffsetFromBottom = 0f;

            if (_targetCharacter == null)
            {
                return;
            }

            Rigidbody2D rb = _targetRigidbody;
            if (!CharacterGroundProbeUtility.TryGetCharacterWorldBounds(_targetCharacter, rb, out Bounds bounds))
            {
                groundY = _targetCharacter.transform.position.y;
                currentAirHeight = 0f;
                pivotOffsetFromBottom = 0f;
                return;
            }

            float bottomY = bounds.min.y;
            pivotOffsetFromBottom = _targetCharacter.transform.position.y - bottomY;

            if (CharacterGroundProbeUtility.TryProbeGroundBelow(
                    _targetCharacter,
                    rb,
                    GroundProbeDistance,
                    out float probedGroundY,
                    out float probedBottomY))
            {
                groundY = probedGroundY;
                currentAirHeight = Mathf.Max(0f, probedBottomY - probedGroundY);
                return;
            }

            groundY = bottomY;
            currentAirHeight = 0f;
        }

        /// <summary>
        /// 캐릭터의 중력 오버라이드를 획득해 공중 연출 중 낙하를 방지합니다.
        /// </summary>
        private void EnsureGravityOverride()
        {
            if (_targetCharacter == null)
            {
                return;
            }

            if (_gravityOverrideHandle.IsValid && _physicsOverrideController != null)
            {
                return;
            }

            _physicsOverrideController = _targetCharacter.PhysicsOverrideController;
            if (_physicsOverrideController == null)
            {
                return;
            }

            _gravityOverrideHandle = _physicsOverrideController.AcquireGravityOverride(
                ownerKey: this,
                lifecycleOwner: _targetCharacter,
                channel: CharacterPhysicsOverrideChannel.System,
                priority: CharacterPhysicsOverridePriority.System,
                gravityScale: 0f,
                reason: "CutsceneCharacterAirborne");
        }

        /// <summary>
        /// 보유 중인 중력 오버라이드 핸들을 해제합니다.
        /// </summary>
        private void ReleaseGravityOverride()
        {
            if (_physicsOverrideController != null && _gravityOverrideHandle.IsValid)
            {
                _physicsOverrideController.ReleaseGravityOverride(ref _gravityOverrideHandle);
            }
            else
            {
                _gravityOverrideHandle = default;
            }

            _physicsOverrideController = null;
        }

        /// <summary>
        /// 리지드바디의 선속도/각속도를 0으로 고정해 물리 누적 오차를 방지합니다.
        /// </summary>
        private void ZeroRigidbodyVelocity()
        {
            Rigidbody2D rb = _targetRigidbody;
            if (rb == null || rb.bodyType != RigidbodyType2D.Dynamic)
            {
                return;
            }

            rb.SetLinearVelocity(Vector2.zero);
            rb.angularVelocity = 0f;
        }

        /// <summary>
        /// 캐릭터 공중 연출 대상 캐릭터를 해석합니다.
        /// Fixed 모드는 type/uid를, RuntimeOverride 모드는 CutsceneManager 런타임 키를 사용합니다.
        /// </summary>
        /// <param name="data">캐릭터 공중 연출 데이터입니다.</param>
        /// <returns>해석된 대상 캐릭터입니다. 찾지 못하면 <see langword="null"/>을 반환합니다.</returns>
        private CharacterBase ResolveTargetCharacter(CharacterAirborneData data)
        {
            if (data == null)
            {
                return null;
            }

            var reference = data.target;
            if (reference != null && reference.sourceMode == CutsceneCharacterTargetSourceMode.RuntimeOverride)
            {
                if (reference.runtimeTargetKey != CutsceneKeyCharacterTarget.None &&
                    CutsceneManager.TryGetCharacterTargetOverride(reference.runtimeTargetKey, out var runtimeTarget) &&
                    runtimeTarget != null)
                {
                    return runtimeTarget;
                }

                if (reference.runtimeTargetKey != CutsceneKeyCharacterTarget.None)
                {
                    GcLogger.Log($"CharacterAirborne runtime override not found. key={reference.runtimeTargetKey}");
                    return null;
                }
            }

            CharacterConstants.Type resolvedType = CharacterConstants.Type.None;
            int resolvedUid = 0;

            if (reference != null)
            {
                resolvedType = reference.characterType;
                resolvedUid = reference.characterUid;
            }

            if (resolvedType == CharacterConstants.Type.None && data.characterType != CharacterConstants.Type.None)
            {
                resolvedType = data.characterType;
            }

            if (resolvedUid == 0 && data.characterUid != 0)
            {
                resolvedUid = data.characterUid;
            }

            if (resolvedType == CharacterConstants.Type.None)
            {
                return null;
            }

            var target = GetTargetTransform(resolvedType, resolvedUid);
            if (target == null)
            {
                target = CutsceneManager.GetCharacter(resolvedType, resolvedUid);
            }

            return target != null ? target.GetComponent<CharacterBase>() : null;
        }

        /// <summary>
        /// 컨트롤러의 런타임 상태를 초기화합니다.
        /// </summary>
        private void ClearRuntimeState()
        {
            _data = null;
            _targetCharacter = null;
            _targetRigidbody = null;
            _elapsed = 0f;
            _duration = 0f;
            _groundY = 0f;
            _pivotOffsetFromBottom = 0f;
            _fromAirHeight = 0f;
            _toAirHeight = 0f;
            _capturedWorldPositionY = 0f;
            _capturedActiveState = true;
            _isPlaying = false;
            _hasOwnership = false;
            _isMaintainingAirborneAfterComplete = false;
        }
    }
}
