using System;
using UnityEngine;

namespace GGemCo.Scripts
{
    [Serializable]
    public class CharacterMoveData
    {
        [Header("타겟")]
        [Tooltip("카메라가 타겟을 따라갈 것인지")]
        public bool isFollowTarget = false;
        [Tooltip("캐릭터 타입")]
        public CharacterConstants.Type characterType;
        [Tooltip("npc, monster 테이블의 고유번호")]
        public int characterUid;
        [Tooltip("크기")]
        public float characterScale;
        [Tooltip("이동 속도")]
        public int characterMoveSpeed;

        [Header("이동")] 
        public Vec2 startPosition;
        public Vec2 endPosition;
    }
}