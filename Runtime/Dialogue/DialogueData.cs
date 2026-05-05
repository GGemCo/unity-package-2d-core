using System;
using System.Collections.Generic;

namespace GGemCo2DCore
{
    /// <summary>
    /// 대사 런타임 데이터 루트입니다.
    /// </summary>
    [Serializable]
    public class DialogueData
    {
        public List<DialogueNodeData> nodes = new List<DialogueNodeData>();
    }

    /// <summary>
    /// 대사 json 에 저장되는 노드 데이터입니다.
    /// editor 원문과 분리된 런타임 export 모델이며, localization table/key 참조를 포함할 수 있습니다.
    /// </summary>
    [Serializable]
    public class DialogueNodeData
    {
        public string guid;
        public string title;
        public string dialogueText;
        public string dialogueTable;
        public string dialogueKey;
        public Vec2 position;
        public CharacterConstants.Type characterType;
        public int characterUid;
        public float fontSize;
        public string thumbnailImage;
        public List<DialogueOption> options = new List<DialogueOption>();
        public string nextNodeGuid;
        public int startQuestUid;
        public int startQuestStep;
    }
}
