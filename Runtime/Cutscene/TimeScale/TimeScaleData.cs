using System;
using UnityEngine;

namespace GGemCo2DCore
{
    public enum TimeScaleActionMode
    {
        BlendAndHold,
        SetAndHold,
        Restore
    }

    [Serializable]
    public class TimeScaleData
    {
        [Header("Action")]
        [Tooltip("BlendAndHold: fromScale -> toScale 보간 후 유지, SetAndHold: 즉시 toScale 적용 후 유지, Restore: 저장된 값 또는 restoreScale로 복구")]
        public TimeScaleActionMode actionMode = TimeScaleActionMode.BlendAndHold;

        [Header("Scale")]
        [Tooltip("BlendAndHold 또는 Restore에서 시작 scale 값입니다. Restore는 비워두면 현재 값을 시작값으로 사용합니다.")]
        [Min(0f)] public float fromScale = 1f;
        [Tooltip("BlendAndHold 또는 SetAndHold에서 유지할 목표 timeScale 값입니다.")]
        [Min(0f)] public float toScale = 0.2f;
        [Tooltip("Restore에서 useCapturedScaleForRestore가 꺼져 있을 때 복구할 scale 값입니다.")]
        [Min(0f)] public float restoreScale = 1f;

        [Header("Playback")]
        [Tooltip("duration 동안의 보간 easing 입니다.")]
        public Easing.EaseType easing = Easing.EaseType.Linear;
        [Tooltip("Time.timeScale과 무관하게 duration을 진행할지 여부입니다.")]
        public bool useUnscaledTime = true;
        [Tooltip("Restore 시 컷신에서 처음 저장한 timeScale 값으로 복구할지 여부입니다.")]
        public bool useCapturedScaleForRestore = true;
        [Tooltip("컷신 종료 시 현재 유지 중인 timeScale 값을 자동 복구할지 여부입니다.")]
        public bool restoreOnCutsceneEnd = true;

        [Header("Fixed Update")]
        [Tooltip("timeScale 변경 시 Time.fixedDeltaTime도 함께 조정할지 여부입니다.")]
        public bool affectFixedDeltaTime = true;
        [Tooltip("Time.fixedDeltaTime 계산 시 사용할 최소 scale 값입니다. 0이면 FixedUpdate 멈춤을 허용합니다.")]
        [Min(0f)] public float minimumScaleForFixedDeltaTime = 0.0001f;
    }
}
