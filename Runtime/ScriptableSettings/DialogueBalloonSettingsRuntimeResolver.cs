using UnityEngine;

namespace GGemCo2DCore
{
    /// <summary>
    /// 말풍선 관련 ScriptableObject 기본값을 런타임에서 안전하게 조회/보정하는 유틸리티입니다.
    /// </summary>
    public static class DialogueBalloonSettingsRuntimeResolver
    {
        /// <summary>
        /// Addressables 로더에서 프로젝트 말풍선 설정을 조회합니다.
        /// </summary>
        /// <param name="settings">조회된 프로젝트 말풍선 설정입니다.</param>
        /// <returns>설정 조회에 성공하면 <see langword="true"/>를 반환합니다.</returns>
        public static bool TryGetSettings(out GGemCoDialogueBalloonSettings settings)
        {
            settings = null;
            if (AddressableLoaderSettings.Instance != null &&
                AddressableLoaderSettings.Instance.dialogueBalloonSettings != null)
            {
                settings = AddressableLoaderSettings.Instance.dialogueBalloonSettings;
                return true;
            }

            if (AddressableLoaderSettingsRegist.Instance != null &&
                AddressableLoaderSettingsRegist.Instance.dialogueBalloonSettings != null)
            {
                settings = AddressableLoaderSettingsRegist.Instance.dialogueBalloonSettings;
                return true;
            }

            return false;
        }

        /// <summary>
        /// 입력 안내 이미지 기본값을 프로젝트 설정과 로컬 fallback을 조합해 안전하게 결정합니다.
        /// </summary>
        /// <param name="fallbackGapPx">로컬 fallback 간격(px)입니다.</param>
        /// <param name="fallbackBlinkHz">로컬 fallback 깜빡임 속도(Hz)입니다.</param>
        /// <param name="fallbackMinAlpha">로컬 fallback 최소 알파값입니다.</param>
        /// <param name="resolvedGapPx">결정된 간격(px)입니다.</param>
        /// <param name="resolvedBlinkHz">결정된 깜빡임 속도(Hz)입니다.</param>
        /// <param name="resolvedMinAlpha">결정된 최소 알파값입니다.</param>
        /// <param name="resolvedSprite">결정된 입력 안내 이미지 스프라이트입니다.</param>
        public static void ResolveEnterIndicatorDefaults(
            float fallbackGapPx,
            float fallbackBlinkHz,
            float fallbackMinAlpha,
            out float resolvedGapPx,
            out float resolvedBlinkHz,
            out float resolvedMinAlpha,
            out Sprite resolvedSprite)
        {
            resolvedGapPx = Mathf.Max(0f, fallbackGapPx);
            resolvedBlinkHz = Mathf.Max(0f, fallbackBlinkHz);
            resolvedMinAlpha = Mathf.Clamp01(fallbackMinAlpha);
            resolvedSprite = null;

            if (!TryGetSettings(out GGemCoDialogueBalloonSettings settings) || settings == null)
            {
                return;
            }

            resolvedGapPx = settings.GetSafeEnterIndicatorGapPx();
            resolvedBlinkHz = settings.GetSafeEnterIndicatorBlinkHz();
            resolvedMinAlpha = settings.GetSafeEnterIndicatorMinAlpha();
            resolvedSprite = settings.enterIndicatorSprite;
        }

        /// <summary>
        /// 프로젝트 말풍선 월드 오프셋 기본값을 settings 또는 로컬 fallback으로 결정합니다.
        /// </summary>
        /// <param name="fallbackOffset">로컬 fallback 오프셋입니다.</param>
        /// <param name="fallbackXPolicy">로컬 fallback X 정책입니다.</param>
        /// <param name="resolvedOffset">결정된 오프셋입니다.</param>
        /// <param name="resolvedXPolicy">결정된 X 정책입니다.</param>
        public static void ResolveProjectWorldOffsetDefaults(
            Vector3 fallbackOffset,
            DialogueBalloonWorldOffsetXPolicy fallbackXPolicy,
            out Vector3 resolvedOffset,
            out DialogueBalloonWorldOffsetXPolicy resolvedXPolicy)
        {
            resolvedOffset = fallbackOffset;
            resolvedXPolicy = DialogueBalloonWorldOffsetUtility.GetSafeWorldOffsetXPolicy(fallbackXPolicy);

            if (!TryGetSettings(out GGemCoDialogueBalloonSettings settings) || settings == null)
            {
                return;
            }

            resolvedOffset = settings.worldOffset;
            resolvedXPolicy = settings.GetSafeWorldOffsetXPolicy();
        }
    }
}
