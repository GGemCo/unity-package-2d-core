using System;
using UnityEngine;

namespace GGemCo.Scripts
{
    /// <summary>
    /// 대사 노드 - 선택지
    /// </summary>
    [Serializable]
    public class DialogueOption
    {
        public string optionText = "선택지 내용";
        public string nextNodeGuid; // 연결될 다음 노드의 GUID
        
        [NonSerialized]
        public Vector2 connectionPoint;
    }
}