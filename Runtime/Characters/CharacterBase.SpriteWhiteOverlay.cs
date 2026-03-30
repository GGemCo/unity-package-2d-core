using UnityEngine;

namespace GGemCo2DCore
{
    public partial class CharacterBase
    {
        private SpriteWhiteOverlayController _spriteWhiteOverlayController;
        
        public readonly struct SpriteWhiteOverlayConfig
        {
            /// <summary>
            /// 오버레이를 사용하지 않는 기본 설정을 반환합니다.
            /// </summary>
            public static SpriteWhiteOverlayConfig Disabled => new SpriteWhiteOverlayConfig(false, Color.white, 0f);

            public readonly bool Enabled;
            public readonly Color Color;
            public readonly float FlashDuration;

            /// <summary>
            /// 화이트 오버레이 설정을 생성합니다.
            /// </summary>
            /// <param name="enabled">오버레이 활성화 여부입니다.</param>
            /// <param name="color">플래시에 사용할 색상입니다.</param>
            /// <param name="flashDuration">플래시 지속 시간입니다.</param>
            public SpriteWhiteOverlayConfig(bool enabled, Color color, float flashDuration)
            {
                Enabled = enabled;
                Color = color;
                FlashDuration = flashDuration;
            }
        }

        /// <summary>
        /// 외부에서 준비한 스프라이트 오버레이 컨트롤러를 바인딩합니다.
        /// </summary>
        /// <param name="controller">바인딩할 오버레이 컨트롤러입니다.</param>
        public void BindSpriteWhiteOverlayController(SpriteWhiteOverlayController controller)
        {
            _spriteWhiteOverlayController = controller;
        }

        /// <summary>
        /// 설정에 따라 스프라이트 오버레이 컨트롤러를 준비합니다.
        /// </summary>
        /// <returns>오버레이가 활성화되어 컨트롤러 준비에 성공했으면 <see langword="true"/>를 반환합니다.</returns>
        public bool TryEnsureSpriteWhiteOverlayController()
        {
            var config = GetSpriteWhiteOverlayConfig();
            if (!config.Enabled)
            {
                return false;
            }

            var controller = _spriteWhiteOverlayController != null
                ? _spriteWhiteOverlayController
                : GetComponent<SpriteWhiteOverlayController>();

            if (controller == null)
            {
                controller = gameObject.AddComponent<SpriteWhiteOverlayController>();
            }

            controller.Configure(config.Color, refreshTargets: true);
            BindSpriteWhiteOverlayController(controller);
            return true;
        }

        /// <summary>
        /// 피격 시 스프라이트 화이트 오버레이 플래시를 재생합니다.
        /// </summary>
        public void TryPlaySpriteWhiteOverlayOnHit()
        {
            var config = GetSpriteWhiteOverlayConfig();
            if (!config.Enabled)
            {
                return;
            }

            var controller = _spriteWhiteOverlayController != null
                ? _spriteWhiteOverlayController
                : GetComponent<SpriteWhiteOverlayController>();

            if (controller == null)
            {
                return;
            }

            controller.Configure(config.Color);
            controller.Flash(Mathf.Max(0.01f, config.FlashDuration));
            BindSpriteWhiteOverlayController(controller);
        }

        /// <summary>
        /// 현재 캐릭터 타입에 맞는 스프라이트 화이트 오버레이 설정을 계산합니다.
        /// </summary>
        /// <returns>현재 캐릭터에 적용할 오버레이 설정입니다.</returns>
        protected virtual SpriteWhiteOverlayConfig GetSpriteWhiteOverlayConfig()
        {
            if (this is Player)
            {
                var playerSettings = AddressableLoaderSettings.Instance != null
                    ? AddressableLoaderSettings.Instance.playerSettings
                    : null;

                if (playerSettings == null || !playerSettings.useSpriteWhiteOverlay)
                {
                    return SpriteWhiteOverlayConfig.Disabled;
                }

                return new SpriteWhiteOverlayConfig(
                    true,
                    playerSettings.spriteWhiteOverlayColor,
                    playerSettings.spriteWhiteOverlayFlashDuration);
            }

            if (this is Monster)
            {
                var monsterSettings = AddressableLoaderSettings.Instance != null
                    ? AddressableLoaderSettings.Instance.monsterSettings
                    : null;

                if (monsterSettings == null || !monsterSettings.useSpriteWhiteOverlay)
                {
                    return SpriteWhiteOverlayConfig.Disabled;
                }

                return new SpriteWhiteOverlayConfig(
                    true,
                    monsterSettings.spriteWhiteOverlayColor,
                    monsterSettings.spriteWhiteOverlayFlashDuration);
            }

            return SpriteWhiteOverlayConfig.Disabled;
        }
    }
}
