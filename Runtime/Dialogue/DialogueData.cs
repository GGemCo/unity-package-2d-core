using System;
using System.Collections.Generic;

namespace GGemCo.Scripts
{
    [Serializable]
    public class DialogueData
    {
        public List<DialogueNodeData> nodes = new List<DialogueNodeData>();
    }

    /// <summary>
    /// 대사 json에 저장되는 항목
    /// </summary>
    [Serializable]
    public class DialogueNodeData
    {
        public string guid;
        public string title;
        public string dialogueText;
        public Vec2 position;
        public CharacterConstants.Type characterType;
        public int characterUid;
        public float fontSize;
        public string thumbnailImage;
        public List<DialogueOption> options;
        public string nextNodeGuid;
        public int startQuestUid;
        public int startQuestStep;
    }

}