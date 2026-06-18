using System.Collections.Generic;
using UnityEngine;

namespace GGemCo2DCore
{
    /// <summary>
    /// 캐릭터 피격 VFX의 설정 해석, 재생 간격 제한, 생성 요청을 담당합니다.
    /// </summary>
    internal sealed class CharacterIncomingHitVfxController
    {
        private const int PlayerCooldownKey = -1;

        private readonly Dictionary<int, float> _nextPlayableTimesByKey = new Dictionary<int, float>();

        private CharacterBase _owner;
        private GGemCoPlayerSettings _playerSettings;
        private GGemCoMonsterSettings _monsterSettings;
        private bool _suppressNextAnimationEventVfx;

        /// <summary>
        /// 피격 VFX 대상과 캐릭터별 설정을 연결합니다.
        /// </summary>
        public void Initialize(
            CharacterBase owner,
            GGemCoPlayerSettings playerSettings,
            GGemCoMonsterSettings monsterSettings)
        {
            _owner = owner;
            _playerSettings = playerSettings;
            _monsterSettings = monsterSettings;
            ResetRuntimeState();
        }

        /// <summary>
        /// 다음 피격 애니메이션 이벤트 VFX를 억제할지 설정합니다.
        /// </summary>
        public void SetSuppressNextAnimationEventVfx(bool suppress)
        {
            _suppressNextAnimationEventVfx = suppress;
        }

        /// <summary>
        /// 피격 요청을 새로 처리하기 전에 이벤트 억제 상태를 초기화합니다.
        /// </summary>
        public void BeginDamageRequest()
        {
            _suppressNextAnimationEventVfx = false;
        }

        /// <summary>
        /// 재생 간격과 이벤트 억제 상태를 초기화합니다.
        /// </summary>
        public void ResetRuntimeState()
        {
            _nextPlayableTimesByKey.Clear();
            _suppressNextAnimationEventVfx = false;
        }

        /// <summary>
        /// 기존 플레이어 전용 트리거 타입을 공통 타입으로 변환하여 재생을 시도합니다.
        /// </summary>
        public void TryPlay(GGemCoPlayerSettings.IncomingHitVfxTriggerType triggerType)
        {
            TryPlay(IncomingHitVfxSettings.ConvertTriggerType(triggerType));
        }

        /// <summary>
        /// 현재 캐릭터 종류와 설정된 트리거 정책에 따라 피격 VFX 재생을 시도합니다.
        /// </summary>
        public void TryPlay(IncomingHitVfxTriggerType triggerType)
        {
            if (_owner == null)
                return;

            if (triggerType == IncomingHitVfxTriggerType.OnAnimationEventHit &&
                _suppressNextAnimationEventVfx)
            {
                _suppressNextAnimationEventVfx = false;
                return;
            }

            if (_owner is Player)
            {
                TryPlayPlayer(triggerType);
                return;
            }

            if (_owner is Monster)
                TryPlayMonster(triggerType);
        }

        /// <summary>
        /// 플레이어 설정에 저장된 단일 피격 VFX 재생을 시도합니다.
        /// </summary>
        private void TryPlayPlayer(IncomingHitVfxTriggerType triggerType)
        {
            if (_playerSettings == null && AddressableLoaderSettings.Instance != null)
                _playerSettings = AddressableLoaderSettings.Instance.playerSettings;
            if (_playerSettings == null)
                return;

            IncomingHitVfxSettings settings =
                IncomingHitVfxSettings.FromPlayerSettings(_playerSettings.incomingHitVfx);
            TryPlaySettings(settings, PlayerCooldownKey, triggerType);
        }

        /// <summary>
        /// 몬스터 설정에 등록된 피격 VFX 목록을 순서대로 검사합니다.
        /// </summary>
        private void TryPlayMonster(IncomingHitVfxTriggerType triggerType)
        {
            if (_monsterSettings == null && AddressableLoaderSettings.Instance != null)
                _monsterSettings = AddressableLoaderSettings.Instance.monsterSettings;
            if (_monsterSettings == null || _monsterSettings.incomingHitVfxList == null)
                return;

            for (int i = 0; i < _monsterSettings.incomingHitVfxList.Count; i++)
                TryPlaySettings(_monsterSettings.incomingHitVfxList[i], i, triggerType);
        }

        /// <summary>
        /// 단일 피격 VFX 설정의 트리거와 최소 재생 간격을 검사한 뒤 생성 요청을 전달합니다.
        /// </summary>
        private bool TryPlaySettings(
            IncomingHitVfxSettings settings,
            int cooldownKey,
            IncomingHitVfxTriggerType triggerType)
        {
            if (!settings.enabled)
                return false;

            StruckAnimationEventVfx payload = settings.GetRuntimeVfx();
            if (payload == null || payload.Uid <= 0 || !IsTriggerMatched(settings.triggerType, triggerType))
                return false;

            if (settings.minIntervalSeconds > 0f &&
                _nextPlayableTimesByKey.TryGetValue(cooldownKey, out float nextPlayableTime) &&
                Time.time < nextPlayableTime)
            {
                return false;
            }

            SceneGame scene = SceneGame.Instance;
            if (scene == null || scene.VfxManager == null)
                return false;

            VfxSpawnRequest request = VfxSpawnRequest.FromAnimationEvent(payload, _owner.gameObject);
            request.Owner = _owner;
            request.Target = _owner;
            request.OwnerGameObject = _owner.gameObject;
            request.ForceOneShot = !IncomingHitVfxSettings.IsFollowVfx(payload);
            ApplyFollowMode(ref request, settings, payload);
            scene.VfxManager.CreateVfx(request);

            if (settings.minIntervalSeconds > 0f)
                _nextPlayableTimesByKey[cooldownKey] = Time.time + settings.minIntervalSeconds;

            return true;
        }

        /// <summary>
        /// Follow 모드와 위치 기준을 VFX 생성 요청에 반영합니다.
        /// </summary>
        private void ApplyFollowMode(
            ref VfxSpawnRequest request,
            IncomingHitVfxSettings settings,
            StruckAnimationEventVfx payload)
        {
            VfxConstants.FollowMode followMode = settings.GetRuntimeFollowMode(payload);
            if (followMode == VfxConstants.FollowMode.None)
            {
                request.ForceOneShot = true;
                return;
            }

            request.FollowTarget = _owner;
            request.FollowModeOverride = followMode;
            request.FollowAnchorModeOverride = settings.GetRuntimeFollowAnchorMode(payload);
            request.ForceOneShot = false;
        }

        /// <summary>
        /// 설정된 트리거 정책이 현재 호출 경로를 허용하는지 확인합니다.
        /// </summary>
        private static bool IsTriggerMatched(
            IncomingHitVfxTriggerType configured,
            IncomingHitVfxTriggerType current)
        {
            switch (configured)
            {
                case IncomingHitVfxTriggerType.OnDamageConfirmed:
                    return current == IncomingHitVfxTriggerType.OnDamageConfirmed;
                case IncomingHitVfxTriggerType.OnAnimationEventHit:
                    return current == IncomingHitVfxTriggerType.OnAnimationEventHit;
                case IncomingHitVfxTriggerType.Both:
                    return current == IncomingHitVfxTriggerType.OnDamageConfirmed ||
                           current == IncomingHitVfxTriggerType.OnAnimationEventHit;
                default:
                    return current == IncomingHitVfxTriggerType.OnDamageConfirmed;
            }
        }
    }
}
