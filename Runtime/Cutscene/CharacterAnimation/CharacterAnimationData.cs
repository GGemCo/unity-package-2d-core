using System;
using UnityEngine;

namespace GGemCo2DCore
{
    /// <summary>
    /// CharacterAnimation 이벤트 실행 시 캐릭터의 바라보기 적용 정책입니다.
    /// </summary>
    public enum CutsceneCharacterAnimationFacingMode
    {
        /// <summary>
        /// 명시한 8방향 값으로 바라보기를 적용합니다.
        /// </summary>
        FaceExplicit = 0,

        /// <summary>
        /// 플레이어 위치를 바라보도록 방향을 계산해 적용합니다.
        /// </summary>
        FacePlayer = 1,
    }

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
        [Tooltip("적용 위치. 0이면 현재 위치를 유지합니다.")]
        public Vec2 spawnPosition;

        [Header("바라보기")]
        [Tooltip("바라보기 적용 방식")]
        public CutsceneCharacterAnimationFacingMode facingMode = CutsceneCharacterAnimationFacingMode.FaceExplicit;
        [Tooltip("facingMode가 FaceExplicit일 때 사용할 방향")]
        public CharacterConstants.FacingDirection8 explicitFacing = CharacterConstants.FacingDirection8.Right;
        
        [Header("애니메이션")] 
        [Tooltip("플레이할 애니메이션 이름")]
        public string animationName;
        [Tooltip("플레이할 애니메이션 loop")]
        public bool animationLoop;
        [Tooltip("애니메이션 속도")]
        public float animationTimeScale;

    }
}
