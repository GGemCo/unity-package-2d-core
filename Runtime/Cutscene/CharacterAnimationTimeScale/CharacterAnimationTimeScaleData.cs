using System;
using UnityEngine;

namespace GGemCo2DCore
{
    public enum CharacterAnimationTimeScaleActionMode
    {
        BlendAndHold,
        SetAndHold,
        Restore
    }

    [Serializable]
    public class CharacterAnimationTimeScaleData
    {
        [Header("타겟")]
        [Tooltip("캐릭터 타입")]
        public CharacterConstants.Type characterType;

        [Tooltip("Npc/Monster 테이블의 고유 번호입니다. Player는 무시됩니다.")]
        public int characterUid;

        [Header("Action")]
        [Tooltip("BlendAndHold: fromScale -> toScale 보간 후 유지, SetAndHold: 즉시 toScale 적용 후 유지, Restore: 저장된 값 또는 restoreScale로 복구")]
        public CharacterAnimationTimeScaleActionMode actionMode = CharacterAnimationTimeScaleActionMode.SetAndHold;

        [Header("Scale")]
        [Tooltip("BlendAndHold 또는 Restore에서 시작 scale 값입니다. captureOriginalOnTrigger가 켜져 있으면 현재 값을 우선 사용합니다.")]
        [Min(0f)] public float fromScale = 1f;

        [Tooltip("BlendAndHold 또는 SetAndHold에서 적용할 목표 animation time scale 값입니다. 0이면 현재 포즈를 유지한 채 정지한 것처럼 보입니다.")]
        [Min(0f)] public float toScale = 0f;

        [Tooltip("Restore에서 captureOriginalOnTrigger/useCapturedScaleForRestore를 사용하지 않을 때 복구할 animation time scale 값입니다.")]
        [Min(0f)] public float restoreScale = 1f;

        [Header("Playback")]
        [Tooltip("duration 동안의 보간 easing 입니다.")]
        public Easing.EaseType easing = Easing.EaseType.Linear;

        [Tooltip("이 이벤트 자신의 duration을 Time.timeScale과 무관하게 진행할지 여부입니다. 0으로 멈추는 연출에서는 켜두는 것을 권장합니다.")]
        public bool useUnscaledTime = true;

        [Tooltip("Trigger 시 현재 animation time scale을 원본값으로 저장할지 여부입니다.")]
        public bool captureOriginalOnTrigger = true;

        [Tooltip("Restore 시 Trigger에서 저장한 원본 animation time scale 값을 우선 사용할지 여부입니다.")]
        public bool useCapturedScaleForRestore = true;

        [Tooltip("컷씬 종료 시 현재 유지 중인 animation time scale 값을 자동 복구할지 여부입니다.")]
        public bool restoreOnCutsceneEnd = true;
    }
}
