using UnityEngine;

namespace GGemCo2DCore
{
    /// <summary>
    /// 말풍선의 프로젝트 기본 배치 정책을 정의하는 ScriptableObject 설정입니다.
    /// </summary>
    [CreateAssetMenu(
        fileName = ConfigScriptableObject.DialogueBalloon.FileName,
        menuName = ConfigScriptableObject.DialogueBalloon.MenuName,
        order = ConfigScriptableObject.DialogueBalloon.Ordering)]
    public class GGemCoDialogueBalloonSettings : ScriptableObject
    {
        [Header("말풍선 월드 위치 기본값")]
        [Tooltip("말풍선 기본 위치(캐릭터 X + 높이) 기준 프로젝트 전역 오프셋입니다.")]
        public Vector3 worldOffset = Vector3.zero;

        [Tooltip("월드 오프셋 X값의 화자 방향 연동 정책입니다.")]
        public DialogueBalloonWorldOffsetXPolicy worldOffsetXPolicy = DialogueBalloonWorldOffsetXPolicy.KeepOriginal;

        /// <summary>
        /// 프로젝트 기본 오프셋 X 정책을 유효 범위로 보정해 반환합니다.
        /// </summary>
        /// <returns>유효한 프로젝트 기본 오프셋 X 정책입니다.</returns>
        public DialogueBalloonWorldOffsetXPolicy GetSafeWorldOffsetXPolicy()
        {
            return worldOffsetXPolicy switch
            {
                DialogueBalloonWorldOffsetXPolicy.KeepOriginal => DialogueBalloonWorldOffsetXPolicy.KeepOriginal,
                DialogueBalloonWorldOffsetXPolicy.MirrorBySpeakerFacing => DialogueBalloonWorldOffsetXPolicy.MirrorBySpeakerFacing,
                DialogueBalloonWorldOffsetXPolicy.UseProjectPolicy => DialogueBalloonWorldOffsetXPolicy.KeepOriginal,
                _ => DialogueBalloonWorldOffsetXPolicy.KeepOriginal
            };
        }

        /// <summary>
        /// 에셋 생성 시 프로젝트 기본값을 초기화합니다.
        /// </summary>
        private void Reset()
        {
            worldOffset = Vector3.zero;
            worldOffsetXPolicy = DialogueBalloonWorldOffsetXPolicy.KeepOriginal;
        }
    }
}
