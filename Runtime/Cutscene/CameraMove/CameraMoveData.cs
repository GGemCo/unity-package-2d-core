using System;
using UnityEngine;

namespace GGemCo.Scripts
{
    [Serializable]
    public class CameraMoveData
    {
        [Header("이동")]
        public Vec2 startPosition;
        public Vec2 endPosition;
        [Tooltip("종료 후 카메라 타겟을 player 로 해줄것인지")]
        public bool endTargetPlayer;
        
        public Easing.EaseType easing = Easing.EaseType.Linear;
    }
}