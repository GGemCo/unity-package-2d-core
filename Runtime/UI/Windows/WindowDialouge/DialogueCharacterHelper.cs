using System.Threading.Tasks;
using UnityEngine;

namespace GGemCo2DCore
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
                var data = TableLoaderManager.Instance.GetNpcData(dialogue.characterUid);
                return data?.Name ?? "???";
            }
            return string.Empty;
        }
        /// <summary>
        /// 말하는 캐릭터 썸네일 가져오기
        /// </summary>
        /// <param name="dialogue"></param>
        /// <returns></returns>
        public static async Task<Sprite> GetThumbnail(DialogueNodeData dialogue)
        {
            if (dialogue == null) return null;
            if (dialogue.thumbnailImage != "")
            {
                string key = $"{ConfigAddressableKey.CharacterThumbnail}_{dialogue.thumbnailImage}";
                return await AddressableLoaderController.LoadByKeyAsync<Sprite>(key);
            }
            if (dialogue.characterType == CharacterConstants.Type.Npc)
            {
                var data = TableLoaderManager.Instance.GetNpcData(dialogue.characterUid);
                if (data != null)
                {
                    string key = $"{ConfigAddressableKey.CharacterThumbnailNpc}_{data.ImageThumbnailFileName}";
                    return await AddressableLoaderController.LoadByKeyAsync<Sprite>(key);
                }
            }
            else if (dialogue.characterType == CharacterConstants.Type.Monster)
            {
                var data = TableLoaderManager.Instance.GetMonsterData(dialogue.characterUid);
                if (data != null)
                {
                    string key = $"{ConfigAddressableKey.CharacterThumbnailMonster}_{data.ImageThumbnailFileName}";
                    return await AddressableLoaderController.LoadByKeyAsync<Sprite>(key);
                }
            }
            return null;
        }
    }
}