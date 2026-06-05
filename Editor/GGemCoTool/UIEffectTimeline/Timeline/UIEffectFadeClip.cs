using System;
using GGemCo2DCore;
using UnityEngine;

namespace GGemCo2DCoreEditor
{
    /// <summary>
    /// UI 알파 값을 보간하는 Timeline Clip입니다.
    /// </summary>
    [Serializable]
    public sealed class UIEffectFadeClip : UIEffectClipBase
    {
        public override UIEffectTimelineEventType EventType => UIEffectTimelineEventType.Fade;

        [Range(0f, 1f)] public float fromAlpha = 0f;
        [Range(0f, 1f)] public float toAlpha = 1f;
        public bool useCurrentAlphaAsFrom;
        public bool updateInteractableOnComplete = true;
        public bool updateBlocksRaycastsOnComplete = true;
        public bool disableInputWhenInvisible = true;
    }
}
