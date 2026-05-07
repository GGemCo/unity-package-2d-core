using System;
using UnityEngine;

namespace GGemCo2DCore
{
    /// <summary>
    /// 컷신에서 캐릭터 위에 표시할 대사 말풍선 데이터를 정의합니다.
    /// </summary>
    [Serializable]
    public class DialogueBalloonData
    {
        /// <summary>
        /// 타자 효과 속도가 지정되지 않았을 때 사용할 기본 초당 글자 수입니다.
        /// </summary>
        public const float DefaultTypewriterCharactersPerSecond = 30f;

        [Header("타겟")]
        [Tooltip("카메라가 타겟을 따라갈 것인지")]
        public bool isFollowTarget = false;
        [Tooltip("캐릭터 타입")]
        public CharacterConstants.Type characterType;
        [Tooltip("npc, monster 테이블의 고유번호")]
        public int characterUid;
        
        [Header("메시지 텍스트")]
        [Tooltip("말풍선 내용")]
        public string message;
        [Tooltip("폰트 크기")]
        public float fontSize;

        [Header("타자 효과")]
        [Tooltip("말풍선 내용을 한 글자씩 표시할지 여부")]
        public bool useTypewriter;
        [Tooltip("타자 효과일 때 초당 표시할 글자 수")]
        public float typewriterCharactersPerSecond = DefaultTypewriterCharactersPerSecond;

        /// <summary>
        /// 타자 효과 속도가 지정되지 않았으면 기본값으로 보정해서 반환합니다.
        /// </summary>
        /// <returns>0보다 큰 유효한 초당 글자 수입니다.</returns>
        public float GetSafeTypewriterCharactersPerSecond()
        {
            return typewriterCharactersPerSecond > 0f
                ? typewriterCharactersPerSecond
                : DefaultTypewriterCharactersPerSecond;
        }
    }
}
