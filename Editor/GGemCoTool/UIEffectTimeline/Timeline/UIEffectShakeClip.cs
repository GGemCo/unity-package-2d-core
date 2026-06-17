using System;
using GGemCo2DCore;
using UnityEngine;

namespace GGemCo2DCoreEditor
{
    /// <summary>
    /// UI RectTransform 흔들림을 적용하는 Timeline Clip입니다.
    /// </summary>
    [Serializable]
    public sealed class UIEffectShakeClip : UIEffectClipBase
    {
        public override UIEffectTimelineEventType EventType => UIEffectTimelineEventType.Shake;

        [Header("Shake")]
        [Tooltip("흔들림 최대 이동 강도입니다. RectTransform anchoredPosition 기준 오프셋으로 적용됩니다.")]
        public float strength = 8f;

        [Tooltip("클립 지속 시간 동안 흔들림 방향을 갱신할 횟수입니다.")]
        public int vibrato = 14;

        [Tooltip("흔들림을 적용할 축입니다.")]
        public UIEffectShakeAxis axis = UIEffectShakeAxis.XY;

        [Tooltip("흔들림 방향을 선택하는 방식입니다.")]
        public UIEffectShakeDirectionMode directionMode = UIEffectShakeDirectionMode.RandomHorizontal;
    }
}
