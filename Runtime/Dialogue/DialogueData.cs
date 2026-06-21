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
        public ConfigCommon.ThumbnailPositionType thumbnailPositionType;
        /// <summary>
        /// 대사 노드 썸네일의 좌우 반전 적용 정책입니다.
        /// 노드별로 썸네일 배치 또는 화자 방향 기준 반전을 제어할 때 사용합니다.
        /// </summary>
        public DialogueBalloonThumbnailFlipPolicy thumbnailFlipPolicy = DialogueBalloonThumbnailFlipPolicy.AutoByThumbnailPosition;
        /// <summary>
        /// 원본 썸네일 스프라이트가 기본적으로 바라보는 수평 방향입니다.
        /// </summary>
        public DialogueBalloonThumbnailSourceFacing thumbnailSourceFacing = DialogueBalloonThumbnailSourceFacing.Right;
        public List<DialogueOption> options = new List<DialogueOption>();
        public string nextNodeGuid;
    }
}
