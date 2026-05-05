using System;
using UnityEngine;

namespace GGemCo2DCore
{
    /// <summary>
    /// 대사 노드의 선택지 데이터입니다.
    /// </summary>
    [Serializable]
    public class DialogueOption
    {
        [Tooltip("선택지 원문입니다. legacy json fallback 용도로 유지됩니다.")]
        public string optionText = "선택지 내용";

        [HideInInspector]
        public string optionTable;

        [HideInInspector]
        public string optionKey;

        public string nextNodeGuid;

        [NonSerialized]
        public Vector2 connectionPoint;
    }
}
