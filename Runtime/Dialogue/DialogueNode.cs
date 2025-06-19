using System;
using System.Collections.Generic;
using UnityEngine;

namespace GGemCo.Scripts
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
        public Vector2 cachedSize = Vector2.zero;
        
        [Header("퀘스트")]
        [Tooltip("현재 대화가 끝났을때 시작되는 퀘스트 고유번호")]
        public int startQuestUid;
        [Tooltip("현재 대화가 끝났을때 startQuestUid 퀘스트 step")]
        public int startQuestStep;
        
        [Header("선택지")]
        public List<DialogueOption> options = new List<DialogueOption>();
        
        public DialogueNode()
        {
            guid = Guid.NewGuid().ToString();
        }
    }
}