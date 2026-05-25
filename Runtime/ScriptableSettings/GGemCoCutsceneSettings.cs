using UnityEngine;

namespace GGemCo2DCore
{
    /// <summary>
    /// 컷신 디버그 표시 옵션을 관리하는 설정 에셋입니다.
    /// </summary>
    [CreateAssetMenu(
        fileName = ConfigScriptableObject.Cutscene.FileName,
        menuName = ConfigScriptableObject.Cutscene.MenuName,
        order = ConfigScriptableObject.Cutscene.Ordering)]
    public class GGemCoCutsceneSettings : ScriptableObject
    {
        /// <summary>
        /// 말풍선 입력 대기 시간의 기본값(초)입니다.
        /// </summary>
        public const float DefaultDialogueBalloonInputWaitTimeoutSeconds = 5f;

        [Header("Dialogue Balloon")]
        [SerializeField]
        [Tooltip("DialogueBalloon.waitForUserInput=true 일 때 유저 입력을 기다리는 최대 시간(초)입니다.")]
        private float dialogueBalloonInputWaitTimeoutSeconds = DefaultDialogueBalloonInputWaitTimeoutSeconds;

#region 디버그
        
        [Header("Cutscene Debug")]
        [SerializeField, DebugOption("컷신 디버그 기능 전체 On/Off")]
        private bool enableCutsceneDebug;
        public bool EnableCutsceneDebug => DebugOptionRuntimeUtility.Resolve(enableCutsceneDebug);

        [SerializeField, DebugOption("컷신 디버그 HUD 출력 On/Off")]
        private bool enableCutsceneDebugHud = true;
        public bool EnableCutsceneDebugHud => EnableCutsceneDebug && DebugOptionRuntimeUtility.Resolve(enableCutsceneDebugHud);

        [SerializeField, DebugOption("컷신 UID 출력")]
        private bool enableCutsceneUid = true;
        public bool EnableCutsceneUid => EnableCutsceneDebugHud && DebugOptionRuntimeUtility.Resolve(enableCutsceneUid);

        [SerializeField, DebugOption("컷신 Json 파일명 출력")]
        private bool enableCutsceneJsonFileName = true;
        public bool EnableCutsceneJsonFileName => EnableCutsceneDebugHud && DebugOptionRuntimeUtility.Resolve(enableCutsceneJsonFileName);

        [SerializeField, DebugOption("컷신 총 시간/경과 시간 출력")]
        private bool enableCutsceneTime = true;
        public bool EnableCutsceneTime => EnableCutsceneDebugHud && DebugOptionRuntimeUtility.Resolve(enableCutsceneTime);

        [SerializeField, DebugOption("컷신 재생 중일 때만 HUD 출력")]
        private bool showHudOnlyWhilePlaying = true;
        public bool ShowHudOnlyWhilePlaying => EnableCutsceneDebugHud && DebugOptionRuntimeUtility.Resolve(showHudOnlyWhilePlaying);

        [Tooltip("컷신 디버그 HUD 갱신 주기(초)")]
        [Min(0.05f)]
        public float cutsceneDebugHudUpdateInterval = 0.1f;

#endregion
        /// <summary>
        /// 말풍선 유저 입력 대기 최대 시간을 안전한 값으로 반환합니다.
        /// </summary>
        /// <returns>0보다 큰 유효한 대기 시간(초)입니다.</returns>
        public float GetSafeDialogueBalloonInputWaitTimeoutSeconds()
        {
            return dialogueBalloonInputWaitTimeoutSeconds > 0f
                ? dialogueBalloonInputWaitTimeoutSeconds
                : DefaultDialogueBalloonInputWaitTimeoutSeconds;
        }

        /// <summary>
        /// 에셋이 처음 생성될 때 권장 기본값을 설정합니다.
        /// </summary>
        private void Reset()
        {
            enableCutsceneDebug = false;
            enableCutsceneDebugHud = true;
            enableCutsceneUid = true;
            enableCutsceneJsonFileName = true;
            enableCutsceneTime = true;
            showHudOnlyWhilePlaying = true;
            cutsceneDebugHudUpdateInterval = 0.1f;
            dialogueBalloonInputWaitTimeoutSeconds = DefaultDialogueBalloonInputWaitTimeoutSeconds;
        }

        /// <summary>
        /// 값 보정이 필요한 항목을 안전한 범위로 맞춥니다.
        /// </summary>
        private void OnEnable()
        {
            if (cutsceneDebugHudUpdateInterval <= 0f)
            {
                cutsceneDebugHudUpdateInterval = 0.1f;
            }

            if (dialogueBalloonInputWaitTimeoutSeconds <= 0f)
            {
                dialogueBalloonInputWaitTimeoutSeconds = DefaultDialogueBalloonInputWaitTimeoutSeconds;
            }
        }
    }
}
