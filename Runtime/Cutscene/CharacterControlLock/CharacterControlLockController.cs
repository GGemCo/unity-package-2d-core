using System.Collections;
using UnityEngine;

namespace GGemCo2DCore
{
    /// <summary>
    /// 컷신 이벤트 구간 동안 대상 캐릭터의 조작을 잠그고, 설정된 시점에 잠금을 해제하는 컨트롤러입니다.
    /// </summary>
    public sealed class CharacterControlLockController : CutsceneDefaultController, ICutsceneController
    {
        private CharacterControlLockData _data;
        private CharacterBase _lockedCharacter;
        private object _lockToken;
        private float _elapsed;
        private float _duration;

        /// <summary>
        /// 캐릭터 조작 잠금 컨트롤러를 생성합니다.
        /// </summary>
        /// <param name="manager">컷신 흐름을 관리하는 매니저입니다.</param>
        public CharacterControlLockController(CutsceneManager manager)
        {
            CutsceneManager = manager;
        }

        /// <summary>
        /// 별도 리소스 로드가 필요하지 않으므로 즉시 준비를 지원합니다.
        /// </summary>
        public bool SupportsImmediateReady => true;

        /// <summary>
        /// 즉시 준비 단계입니다.
        /// 조작 잠금은 실제 트리거 시점에만 적용해야 하므로 준비 단계에서는 별도 처리를 하지 않습니다.
        /// </summary>
        /// <param name="evt">준비할 컷신 이벤트입니다.</param>
        public void ReadyImmediate(CutsceneEvent evt)
        {
        }

        /// <summary>
        /// 컷신 이벤트 실행 전에 필요한 준비를 수행합니다.
        /// 이 컨트롤러는 즉시 준비만으로 충분하므로 바로 종료합니다.
        /// </summary>
        /// <param name="evt">준비할 컷신 이벤트입니다.</param>
        /// <returns>준비 완료를 나타내는 코루틴입니다.</returns>
        public IEnumerator Ready(CutsceneEvent evt)
        {
            ReadyImmediate(evt);
            yield break;
        }

        /// <summary>
        /// 대상 캐릭터를 해석하고 조작 잠금 토큰을 획득합니다.
        /// 이미 이 컨트롤러가 보유한 잠금이 있으면 먼저 해제한 뒤 새 대상에 적용합니다.
        /// </summary>
        /// <param name="evt">실행할 컷신 이벤트입니다.</param>
        public void Trigger(CutsceneEvent evt)
        {
            if (evt.type != CutsceneEventType.CharacterControlLock)
            {
                return;
            }

            ReleaseControlLock();

            _data = evt.characterControlLock ?? new CharacterControlLockData();
            _duration = Mathf.Max(0f, evt.duration);
            _elapsed = 0f;

            CharacterBase target = ResolveTargetCharacter(_data);
            if (target == null)
            {
                GcLogger.LogError("조작 잠금을 적용할 캐릭터를 찾을 수 없습니다.");
                return;
            }

            _lockToken = target.AcquireControlLock(this);
            _lockedCharacter = target;
        }

        /// <summary>
        /// 클립 지속 시간이 지난 경우 설정에 따라 조작 잠금을 해제합니다.
        /// duration이 0 이하이면 컷신 종료까지 잠금을 유지합니다.
        /// </summary>
        public void Update()
        {
            if (_lockedCharacter == null || _data == null || !_data.releaseOnClipEnd || _duration <= 0f)
            {
                return;
            }

            _elapsed += CutsceneManager != null ? CutsceneManager.GetTimelineDeltaTime() : Time.deltaTime;
            if (_elapsed < _duration)
            {
                return;
            }

            ReleaseControlLock();
        }

        /// <summary>
        /// 현재 진행 중인 조작 잠금을 중단합니다.
        /// 클립 종료 해제 옵션이 켜져 있을 때만 이 컨트롤러가 획득한 잠금을 해제합니다.
        /// </summary>
        public void Stop()
        {
            if (_data == null || _data.releaseOnClipEnd)
            {
                ReleaseControlLock();
            }
        }

        /// <summary>
        /// 컷신 종료 시 이 컨트롤러가 사용한 조작 잠금 상태를 정리합니다.
        /// </summary>
        public void End()
        {
            if (_data == null || _data.releaseOnCutsceneEnd)
            {
                ReleaseControlLock();
            }
        }

        /// <summary>
        /// 이벤트 데이터에 정의된 캐릭터 참조를 실제 캐릭터 인스턴스로 해석합니다.
        /// 런타임 오버라이드 키가 지정되어 있으면 CutsceneManager에 등록된 대상이 우선됩니다.
        /// </summary>
        /// <param name="data">조작 잠금 이벤트 데이터입니다.</param>
        /// <returns>해석된 캐릭터입니다. 찾지 못하면 <see langword="null"/>을 반환합니다.</returns>
        private CharacterBase ResolveTargetCharacter(CharacterControlLockData data)
        {
            CutsceneCharacterReference reference = data?.target;
            if (reference != null && reference.sourceMode == CutsceneCharacterTargetSourceMode.RuntimeOverride)
            {
                return ResolveRuntimeOverrideTarget(reference);
            }

            CharacterConstants.Type characterType = reference?.characterType ?? CharacterConstants.Type.Player;
            int characterUid = reference?.characterUid ?? 0;

            Transform target = GetTargetTransform(characterType, characterUid);
            if (target == null && CutsceneManager != null)
            {
                target = CutsceneManager.GetCharacter(characterType, characterUid);
            }

            return target != null ? target.GetComponent<CharacterBase>() : null;
        }

        /// <summary>
        /// 런타임 오버라이드 키를 사용해 조작 잠금 대상을 조회합니다.
        /// 키가 없거나 등록된 대상이 없으면 로그를 남기고 실패로 처리합니다.
        /// </summary>
        /// <param name="reference">런타임 대상 키를 포함한 캐릭터 참조입니다.</param>
        /// <returns>등록된 런타임 캐릭터입니다. 찾지 못하면 <see langword="null"/>을 반환합니다.</returns>
        private CharacterBase ResolveRuntimeOverrideTarget(CutsceneCharacterReference reference)
        {
            if (reference.runtimeTargetKey == CutsceneKeyCharacterTarget.None)
            {
                GcLogger.Log("CharacterControlLock runtime override key가 None입니다.");
                return null;
            }

            if (CutsceneManager != null &&
                CutsceneManager.TryGetCharacterTargetOverride(reference.runtimeTargetKey, out CharacterBase runtimeCharacter))
            {
                return runtimeCharacter;
            }

            GcLogger.Log($"CharacterControlLock runtime override not found. key={reference.runtimeTargetKey}");
            return null;
        }

        /// <summary>
        /// 현재 컨트롤러가 획득한 조작 잠금 토큰을 해제하고 내부 상태를 초기화합니다.
        /// 다른 시스템이 획득한 잠금 토큰은 건드리지 않습니다.
        /// </summary>
        private void ReleaseControlLock()
        {
            if (_lockedCharacter != null && _lockToken != null)
            {
                _lockedCharacter.ReleaseControlLock(_lockToken);
            }

            _lockedCharacter = null;
            _lockToken = null;
            _elapsed = 0f;
        }
    }
}
