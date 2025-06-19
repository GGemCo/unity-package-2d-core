using System;
using UnityEngine;

namespace GGemCo.Scripts
{
    [Serializable]
    public class CameraZoomData
    {
        [Header("줌 인,아웃. orthographicSize 값")]
        [Tooltip("시작 size")]
        public float startSize;
        [Tooltip("종료 size")]
        public float endSize;

        public Easing.EaseType easing = Easing.EaseType.Linear;
    }
}