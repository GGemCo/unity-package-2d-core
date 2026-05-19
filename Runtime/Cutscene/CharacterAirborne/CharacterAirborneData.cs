using System;
using UnityEngine;

namespace GGemCo2DCore
{
    /// <summary>
    /// 컷신에서 캐릭터의 공중 상태(높이/중력)를 제어할 때 사용하는 데이터입니다.
    /// </summary>
    [Serializable]
    public class CharacterAirborneData
    {
        [Header("Target")]
        [Tooltip("캐릭터 대상 참조 정보입니다. Fixed는 직접 타입/uid를, RuntimeOverride는 런타임 키를 사용합니다.")]
        public CutsceneCharacterReference target = new CutsceneCharacterReference();

        [HideInInspector] public CharacterConstants.Type characterType;
        [HideInInspector] public int characterUid;

        [Header("Airborne State")]
        [Tooltip("true이면 공중 상태를 적용하고 false이면 지면 상태(높이 0)로 복귀합니다.")]
        public bool airborneEnabled = true;

        [Tooltip("airborneEnabled가 true일 때 목표 공중 높이(지면 기준 +Y)입니다.")]
        public float targetAirHeight = 1f;

        [Tooltip("기존 공중 연출 소유자를 강제로 교체할지 여부입니다.")]
        public bool allowReplace = true;

        [Tooltip("Time.timeScale과 무관하게 진행할지 여부입니다.")]
        public bool useUnscaledTime = true;

        [Tooltip("공중 높이 보간 easing입니다.")]
        public Easing.EaseType easing = Easing.EaseType.Linear;

        [Header("Policy")]
        [Tooltip("연출 완료 후에도 공중 중력 오버라이드(중력 0)를 유지할지 여부입니다.")]
        public bool keepAirborneGravity = true;

        [Tooltip("Stop 시 시작 시점의 높이로 복원할지 여부입니다.")]
        public bool restoreHeightOnStop = true;

        [Tooltip("컷신 End 시 시작 시점의 높이로 복원할지 여부입니다.")]
        public bool restoreHeightOnCutsceneEnd = true;

        /// <summary>
        /// 설정값을 기준으로 목표 공중 높이를 계산합니다.
        /// </summary>
        /// <returns>0 이상으로 보정된 목표 공중 높이입니다.</returns>
        public float ResolveTargetAirHeight()
        {
            return airborneEnabled
                ? Mathf.Max(0f, targetAirHeight)
                : 0f;
        }
    }
}
