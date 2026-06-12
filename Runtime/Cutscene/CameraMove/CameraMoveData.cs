using System;
using UnityEngine;

namespace GGemCo2DCore
{
    /// <summary>
    /// CameraMove 이벤트가 CameraManager의 일반 카메라 계산을 어떻게 다룰지 결정하는 정책입니다.
    /// </summary>
    public enum CameraMoveControlPolicy
    {
        /// <summary>
        /// 기존 방식에 가깝게 카메라 위치만 이동하고 다른 카메라 효과와의 병행을 허용합니다.
        /// </summary>
        Normal = 0,

        /// <summary>
        /// CameraMove가 진행되는 동안 추적, 경계, Dead Zone, Shake, Zoom 계산을 차단합니다.
        /// </summary>
        Exclusive = 1,
    }

    /// <summary>
    /// 컷신 CameraMove 이벤트에서 사용할 카메라 이동 데이터입니다.
    /// </summary>
    [Serializable]
    public class CameraMoveData
    {
        [Header("이동")]
        public Vec2 startPosition;
        public Vec2 endPosition;
        [Tooltip("종료 후 카메라 타겟을 player 로 해줄것인지")]
        public bool endTargetPlayer;

        [Header("제어 정책")]
        [Tooltip("CameraMove 실행 중 CameraManager의 일반 계산과 다른 카메라 효과를 차단할지 결정합니다.")]
        public CameraMoveControlPolicy controlPolicy = CameraMoveControlPolicy.Normal;

        [Tooltip("이동이 끝난 뒤에도 컷신 종료 전까지 마지막 위치를 고정합니다. Exclusive 정책에서만 적용됩니다.")]
        public bool holdEndPositionUntilCutsceneEnd;
        
        public Easing.EaseType easing = Easing.EaseType.Linear;
    }
}
