using UnityEngine;

namespace GGemCo.Scripts
{
    /// <summary>
    /// UIWindowDialogue 정보 가져오기
    /// </summary>
    public static class DialogueCharacterHelper
    {
        /// <summary>
        /// 말하는 캐릭터 이름 가져오기
        /// </summary>
        /// <param name="dialogue"></param>
        /// <returns></returns>
        public static string GetName(DialogueNodeData dialogue)
        {
            if (dialogue.characterType == CharacterConstants.Type.Npc)
            {
                var data = TableLoaderManager.Instance.TableNpc.GetDataByUid(dialogue.characterUid);
                return data?.Name ?? "???";
            }
            return string.Empty;
        }
        /// <summary>
        /// 말하는 캐릭터 썸네일 가져오기
        /// </summary>
        /// <param name="dialogue"></param>
        /// <returns></returns>
        public static Sprite GetThumbnail(DialogueNodeData dialogue)
        {
            if (dialogue == null) return null;
            if (dialogue.thumbnailImage != "")
            {
                return Resources.Load<Sprite>($"Images/Thumbnail/{dialogue.thumbnailImage}");
            }
            if (dialogue.characterType == CharacterConstants.Type.Npc)
            {
                var data = TableLoaderManager.Instance.TableNpc.GetDataByUid(dialogue.characterUid);
                if (data != null)
                    return Resources.Load<Sprite>($"Images/Thumbnail/Npc/{data.ImageThumbnailPath}");
            }
            return null;
        }
    }
}