
using UnityEngine;

namespace GGemCo2DCore
{
    public partial class CharacterBase
    {
        private CharacterHitStopController _hitStopController;
        
        /// <summary>
        /// 스프라이트 화이트 오버레이 연출 설정을 표현하는 값 타입입니다.
        /// </summary>
        public readonly struct HitStopConfig
        {
            /// <summary>
            /// 경직을 사용하지 않는 기본 설정을 반환합니다.
            /// </summary>
            public static HitStopConfig Disabled => new HitStopConfig(false, 0f, 0f, true, true, true, true);

            public readonly bool Enabled;
            public readonly float DefaultSelfSeconds;
            public readonly float DefaultReceiveSeconds;
            public readonly bool PauseAnimation;
            public readonly bool FreezePhysics;
            public readonly bool LockControl;
            public readonly bool LockMovement;

            public HitStopConfig(
                bool enabled,
                float defaultSelfSeconds,
                float defaultReceiveSeconds,
                bool pauseAnimation,
                bool freezePhysics,
                bool lockControl,
                bool lockMovement)
            {
                Enabled = enabled;
                DefaultSelfSeconds = Mathf.Max(0f, defaultSelfSeconds);
                DefaultReceiveSeconds = Mathf.Max(0f, defaultReceiveSeconds);
                PauseAnimation = pauseAnimation;
                FreezePhysics = freezePhysics;
                LockControl = lockControl;
                LockMovement = lockMovement;
            }
        }

        /// <summary>
        /// 현재 캐릭터에 바인딩된 경직 컨트롤러를 반환합니다. 필요하면 자동으로 추가합니다.
        /// </summary>
        public CharacterHitStopController HitStopController
        {
            get
            {
                if (_hitStopController == null)
                {
                    _hitStopController = GetComponent<CharacterHitStopController>();
                    if (_hitStopController == null)
                    {
                        _hitStopController = gameObject.AddComponent<CharacterHitStopController>();
                    }
                }

                return _hitStopController;
            }
        }
        
        /// <summary>
        /// 현재 경직이 활성화되어 있는지 여부입니다.
        /// </summary>
        public bool IsHitStopped => _hitStopController != null && _hitStopController.IsActive;

        /// <summary>
        /// 캐릭터 타입에 맞는 기본 경직 설정을 계산합니다.
        /// </summary>
        protected virtual HitStopConfig GetHitStopConfig()
        {
            if (this is Player)
            {
                var playerSettings = AddressableLoaderSettings.Instance != null
                    ? AddressableLoaderSettings.Instance.playerSettings
                    : null;

                if (playerSettings == null)
                {
                    return HitStopConfig.Disabled;
                }

                return new HitStopConfig(
                    true,
                    playerSettings.defaultSelfHitStopSeconds,
                    playerSettings.defaultReceiveHitStopSeconds,
                    playerSettings.hitStopPauseAnimation,
                    playerSettings.hitStopFreezePhysics,
                    playerSettings.hitStopLockControl,
                    playerSettings.hitStopLockMovement);
            }

            if (this is Monster)
            {
                var monsterSettings = AddressableLoaderSettings.Instance != null
                    ? AddressableLoaderSettings.Instance.monsterSettings
                    : null;

                if (monsterSettings == null)
                {
                    return HitStopConfig.Disabled;
                }

                return new HitStopConfig(
                    true,
                    monsterSettings.defaultSelfHitStopSeconds,
                    monsterSettings.defaultReceiveHitStopSeconds,
                    monsterSettings.hitStopPauseAnimation,
                    monsterSettings.hitStopFreezePhysics,
                    monsterSettings.hitStopLockControl,
                    monsterSettings.hitStopLockMovement);
            }

            return HitStopConfig.Disabled;
        }

        /// <summary>
        /// 현재 캐릭터에 해석된 기본 경직 설정을 반환합니다.
        /// </summary>
        public HitStopConfig GetResolvedHitStopConfig() => GetHitStopConfig();

        /// <summary>
        /// 기본 설정을 사용해 자신에게 경직을 적용합니다.
        /// </summary>
        /// <param name="seconds">적용할 경직 시간(초)입니다.</param>
        /// <param name="sourceSkillUid">원인 스킬 UID입니다.</param>
        public void ApplyHitStop(float seconds, int sourceSkillUid = 0)
        {
            var config = GetHitStopConfig();
            if (!config.Enabled || seconds <= 0f)
            {
                return;
            }

            ApplyHitStop(new HitStopRequest(
                seconds,
                lockControl: config.LockControl,
                lockMovement: config.LockMovement,
                pauseAnimation: config.PauseAnimation,
                freezePhysics: config.FreezePhysics,
                sourceSkillUid: sourceSkillUid));
        }

        /// <summary>
        /// 지정한 요청으로 자신에게 경직을 적용합니다.
        /// </summary>
        public void ApplyHitStop(in HitStopRequest request)
        {
            if (request.DurationSeconds <= 0f || IsStatusDead())
            {
                return;
            }

            HitStopController.Apply(in request);
        }

    }
}
