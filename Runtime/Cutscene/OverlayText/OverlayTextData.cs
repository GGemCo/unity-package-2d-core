using System;
using UnityEngine;

namespace GGemCo2DCore
{
    [Serializable]
    public class OverlayTextData
    {
        [Header("Content")]
        [Tooltip("화면에 출력할 텍스트입니다.")]
        public string text;

        [Header("Layout")]
        [Tooltip("Canvas 중앙 기준 anchoredPosition 입니다.")]
        public Vec2 anchoredPosition;
        [Tooltip("텍스트 영역 크기입니다.")]
        public Vec2 sizeDelta = new Vec2(new Vector2(1000f, 220f));
        [Tooltip("폰트 크기입니다.")]
        public int fontSize = 72;

        [Header("Style")]
        [Tooltip("텍스트 색상입니다.")]
        public Color textColor = Color.white;
        [Tooltip("텍스트 최대 알파값입니다.")]
        [Range(0f, 1f)] public float maxAlpha = 1f;
        [Tooltip("클립 시작 시 서서히 나타날지 여부입니다.")]
        public bool fadeIn = true;
        [Tooltip("클립 종료 시 서서히 사라질지 여부입니다.")]
        public bool fadeOut = true;
        [Tooltip("텍스트 등장/퇴장 easing 입니다.")]
        public Easing.EaseType easing = Easing.EaseType.Linear;
        [Tooltip("Time.timeScale과 무관하게 진행할지 여부입니다.")]
        public bool useUnscaledTime = true;
    }
}
