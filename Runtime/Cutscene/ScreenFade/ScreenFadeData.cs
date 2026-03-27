using System;
using UnityEngine;

namespace GGemCo2DCore
{
    [Serializable]
    public class ScreenFadeData
    {
        [Header("Fade")]
        [Tooltip("페이드 색상입니다.")]
        public Color color = Color.black;
        [Tooltip("시작 알파값입니다.")]
        [Range(0f, 1f)] public float fromAlpha = 0f;
        [Tooltip("종료 알파값입니다.")]
        [Range(0f, 1f)] public float toAlpha = 1f;
        [Tooltip("클립 종료 후 마지막 알파 상태를 유지할지 여부입니다.")]
        public bool holdFinalState = true;
        [Tooltip("Time.timeScale과 무관하게 진행할지 여부입니다.")]
        public bool useUnscaledTime = true;
        [Tooltip("알파 보간 easing 입니다.")]
        public Easing.EaseType easing = Easing.EaseType.Linear;
    }
}
