using System;
using GGemCo2DCore;
using UnityEngine;

namespace GGemCo2DCoreEditor
{
    /// <summary>
    /// UI Graphic 색상을 플래시하는 Timeline Clip입니다.
    /// </summary>
    [Serializable]
    public sealed class UIEffectFlashClip : UIEffectClipBase
    {
        public override UIEffectTimelineEventType EventType => UIEffectTimelineEventType.Flash;

        [Header("Flash")]
        [Tooltip("플래시 정점에서 적용할 색상입니다.")]
        public Color flashColor = Color.white;

        [Tooltip("플래시 정점에서 사용할 알파 값입니다.")]
        [Range(0f, 1f)] public float peakAlpha = 0.8f;

        [Tooltip("클립 지속 시간 안에서 플래시를 반복할 횟수입니다.")]
        public int repeatCount = 1;

        [Tooltip("효과가 끝난 뒤 시작 전 색상으로 복원할지 여부입니다.")]
        public bool restoreOriginalColorOnComplete = true;

        [Tooltip("각 플래시 반복 구간의 진행률에 적용할 이징 타입입니다.")]
        public Easing.EaseType flashEaseType = Easing.EaseType.EaseOutCubic;
    }
}
