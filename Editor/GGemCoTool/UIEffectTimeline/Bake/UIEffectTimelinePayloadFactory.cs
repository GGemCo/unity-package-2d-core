using GGemCo2DCore;
using UnityEngine;

namespace GGemCo2DCoreEditor
{
    /// <summary>
    /// UI 효과 Timeline Clip을 런타임 Payload 에셋으로 변환하는 팩토리입니다.
    /// </summary>
    internal static class UIEffectTimelinePayloadFactory
    {
        /// <summary>
        /// Clip 타입에 맞는 Payload를 생성합니다.
        /// </summary>
        /// <param name="clip">변환할 UI 효과 Clip입니다.</param>
        /// <returns>생성된 Payload입니다.</returns>
        public static UIEffectPayloadBase CreatePayload(UIEffectClipBase clip)
        {
            switch (clip)
            {
                case UIEffectFadeClip fadeClip:
                    return CreateFadePayload(fadeClip);
                case UIEffectMoveClip moveClip:
                    return CreateMovePayload(moveClip);
                case UIEffectScaleClip scaleClip:
                    return CreateScalePayload(scaleClip);
                case UIEffectShakeClip shakeClip:
                    return CreateShakePayload(shakeClip);
                case UIEffectFlashClip flashClip:
                    return CreateFlashPayload(flashClip);
                default:
                    return null;
            }
        }

        /// <summary>
        /// 공통 Clip 필드를 Payload에 복사합니다.
        /// </summary>
        private static void CopyCommon(UIEffectClipBase source, UIEffectPayloadBase target)
        {
            target.targetKey = source.targetKey;
            target.channel = source.channel;
            target.playPolicy = source.playPolicy;
            target.easeType = source.easeType;
        }

        private static UIEffectFadePayload CreateFadePayload(UIEffectFadeClip clip)
        {
            var payload = ScriptableObject.CreateInstance<UIEffectFadePayload>();
            CopyCommon(clip, payload);
            payload.fromAlpha = clip.fromAlpha;
            payload.toAlpha = clip.toAlpha;
            payload.useCurrentAlphaAsFrom = clip.useCurrentAlphaAsFrom;
            payload.updateInteractableOnComplete = clip.updateInteractableOnComplete;
            payload.updateBlocksRaycastsOnComplete = clip.updateBlocksRaycastsOnComplete;
            payload.disableInputWhenInvisible = clip.disableInputWhenInvisible;
            return payload;
        }

        /// <summary>
        /// Move Clip 필드를 런타임 Move Payload로 복사합니다.
        /// </summary>
        /// <param name="clip">베이크할 Move Clip입니다.</param>
        /// <returns>생성된 Move Payload입니다.</returns>
        private static UIEffectMovePayload CreateMovePayload(UIEffectMoveClip clip)
        {
            var payload = ScriptableObject.CreateInstance<UIEffectMovePayload>();
            CopyCommon(clip, payload);
            payload.easeType = clip.moveEaseType;
            payload.fromOffset = clip.fromOffset;
            payload.toOffset = clip.toOffset;
            payload.useCurrentPositionAsFrom = clip.useCurrentPositionAsFrom;
            payload.destinationPolicy = clip.destinationPolicy;
            payload.snapToTargetOnComplete = clip.snapToTargetOnComplete;
            return payload;
        }

        private static UIEffectScalePayload CreateScalePayload(UIEffectScaleClip clip)
        {
            var payload = ScriptableObject.CreateInstance<UIEffectScalePayload>();
            CopyCommon(clip, payload);
            payload.easeType = clip.scaleEaseType;
            payload.fromScale = clip.fromScale;
            payload.toScale = clip.toScale;
            payload.useCurrentScaleAsFrom = clip.useCurrentScaleAsFrom;
            return payload;
        }

        private static UIEffectShakePayload CreateShakePayload(UIEffectShakeClip clip)
        {
            var payload = ScriptableObject.CreateInstance<UIEffectShakePayload>();
            CopyCommon(clip, payload);
            payload.strength = clip.strength;
            payload.vibrato = clip.vibrato;
            payload.axis = clip.axis;
            payload.directionMode = clip.directionMode;
            return payload;
        }

        private static UIEffectFlashPayload CreateFlashPayload(UIEffectFlashClip clip)
        {
            var payload = ScriptableObject.CreateInstance<UIEffectFlashPayload>();
            CopyCommon(clip, payload);
            payload.easeType = clip.flashEaseType;
            payload.flashColor = clip.flashColor;
            payload.peakAlpha = clip.peakAlpha;
            payload.repeatCount = clip.repeatCount;
            payload.restoreOriginalColorOnComplete = clip.restoreOriginalColorOnComplete;
            return payload;
        }
    }
}
