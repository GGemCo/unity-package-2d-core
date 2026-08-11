using System;
using UnityEngine;

namespace GGemCo2DCore
{
    /// <summary>
    /// 컷신에서 카메라가 추적할 캐릭터와 대상 기준 추가 위치 보정을 정의합니다.
    /// </summary>
    [Serializable]
    public class CameraChangeTargetData
    {
        /// <summary>
        /// 카메라가 추적할 캐릭터 타입입니다.
        /// </summary>
        [Header("캐릭터 타입")]
        public CharacterConstants.Type characterType;

        /// <summary>
        /// 카메라가 추적할 캐릭터 고유번호입니다.
        /// </summary>
        [Header("캐릭터 고유번호")]
        public int characterUid;

        /// <summary>
        /// 대상 위치와 맵 기본 Follow Offset에 추가할 월드 좌표 보정값입니다.
        /// X 양수는 오른쪽, Y 양수는 위쪽으로 카메라를 이동시킵니다.
        /// </summary>
        [Header("카메라 추가 오프셋")]
        public Vector2 offset = Vector2.zero;
    }
}
