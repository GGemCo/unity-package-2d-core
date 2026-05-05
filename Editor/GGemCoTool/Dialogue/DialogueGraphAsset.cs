using System.Collections.Generic;
using GGemCo2DCore;
using UnityEngine;

namespace GGemCo2DCoreEditor
{
    /// <summary>
    /// 대사 그래프 편집 원본을 저장하는 ScriptableObject 에셋입니다.
    /// 런타임 export json 과 분리하여, 툴 재편집 시 원문과 노드 구조를 그대로 복원하기 위해 사용합니다.
    /// </summary>
    public sealed class DialogueGraphAsset : ScriptableObject
    {
        [Tooltip("dialogue.txt 의 Uid 입니다.")]
        public int DialogueUid;

        [Tooltip("dialogue.txt 의 FileName 입니다.")]
        public string DialogueFileName;

        [Tooltip("대사 그래프 노드 목록입니다. 각 노드는 서브 에셋으로 저장됩니다.")]
        public List<DialogueNode> Nodes = new List<DialogueNode>();
    }
}
