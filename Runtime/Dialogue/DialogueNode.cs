using System;
using System.Collections.Generic;
using UnityEngine;

namespace GGemCo2DCore
{
    /// <summary>
    /// 대사 노드
    /// </summary>
    [Serializable]
    public class DialogueNode : ScriptableObject
    {
        public string guid;
        [TextArea(3, 10)]
        [Tooltip("최대 10줄까지 가능합니다.")] 
        public string dialogueText = "대사를 입력하세요";
        [Tooltip("폰트 사이즈")]
        public float fontSize;
        // 대사 텍스트의 연결 대상
        [Tooltip("선택지가 없을때 다음 Node Guid")]
        public string nextNodeGuid;
        public Vector2 nodeConnectionPoint;
        
        public Vector2 position;
        [Header("캐릭터")]
        [Tooltip("대화를 하는 캐릭터 타입")] 
        public CharacterConstants.Type characterType;
        [Tooltip("대화를 하는 캐릭터 고유번호")]
        public int characterUid;
        [Tooltip("썸네일 이미지. npc, monster 테이블의 썸네일을 사용하지 않을때 입력해주세요.\nResouces/Images/Thumbnail/ 다음부터 입력해주세요.")]
        public string thumbnailImage;
        [Tooltip("썸네일 이미지 위치")]
        public ConfigCommon.ThumbnailPositionType thumbnailPositionType = ConfigCommon.ThumbnailPositionType.Right;
        /// <summary>
        /// 대사 노드 썸네일의 좌우 반전 적용 정책입니다.
        /// </summary>
        [Tooltip("썸네일 Flip 적용 정책")]
        public DialogueBalloonThumbnailFlipPolicy thumbnailFlipPolicy = DialogueBalloonThumbnailFlipPolicy.AutoByThumbnailPosition;
        /// <summary>
        /// 원본 썸네일 스프라이트가 기본적으로 바라보는 수평 방향입니다.
        /// </summary>
        [Tooltip("원본 썸네일 이미지의 기본 바라보기 방향")]
        public DialogueBalloonThumbnailSourceFacing thumbnailSourceFacing = DialogueBalloonThumbnailSourceFacing.Right;
        public Vector2 cachedSize = Vector2.zero;
        
        [Header("현재 대화가 끝났을때 시작되는 퀘스트 고유번호")]
        public int startQuestUid;
        [Header("현재 대화가 끝났을때 startQuestUid 퀘스트 step")]
        public int startQuestStep;
        
        [Header("선택지")]
        public List<DialogueOption> options = new List<DialogueOption>();
        
        public DialogueNode()
        {
            guid = Guid.NewGuid().ToString();
        }
    }
}