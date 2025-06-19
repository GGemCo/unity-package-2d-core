using System;
using UnityEngine;

namespace GGemCo.Scripts
{
    [Serializable]
    public class CharacterAnimationData
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
        [Tooltip("스폰 위치. 0 이면 기존 만들어진 위치")]
        public Vec2 spawnPosition;
        [Tooltip("flip")]
        public bool isFlip;
        
        [Header("애니메이션")] 
        [Tooltip("플레이할 애니메이션 이름")]
        public string animationName;
        [Tooltip("플레이할 애니메이션 loop")]
        public bool animationLoop;
        [Tooltip("애니메이션 속도")]
        public float animationTimeScale;

    }
}