using System;
using UnityEngine;

namespace GGemCo2DCore
{
    [Serializable]
    public class TimeScaleData
    {
        [Header("Scale")]
        [Tooltip("클립 시작 시 적용할 timeScale 값입니다. duration이 0이면 toScale만 적용합니다.")]
        [Min(0f)] public float fromScale = 1f;
        [Tooltip("클립 종료 시 적용할 목표 timeScale 값입니다.")]
        [Min(0f)] public float toScale = 0.2f;

        [Header("Playback")]
        [Tooltip("timeScale 보간 easing 입니다.")]
        public Easing.EaseType easing = Easing.EaseType.Linear;
        [Tooltip("Time.timeScale과 무관하게 duration을 진행할지 여부입니다.")]
        public bool useUnscaledTime = true;
        [Tooltip("클립 종료 시 원래 timeScale 값을 복원할지 여부입니다.")]
        public bool restoreOnStop = true;
        [Tooltip("컷신 종료 시 원래 timeScale 값을 복원할지 여부입니다.")]
        public bool restoreOnCutsceneEnd = true;

        [Header("Fixed Update")]
        [Tooltip("timeScale 변경 시 Time.fixedDeltaTime도 함께 비율 조정할지 여부입니다.")]
        public bool affectFixedDeltaTime = true;
        [Tooltip("Time.fixedDeltaTime 계산 시 사용할 최소 scale 값입니다. 0이면 FixedUpdate 멈춤을 허용합니다.")]
        [Min(0f)] public float minimumScaleForFixedDeltaTime = 0.0001f;
    }
}
