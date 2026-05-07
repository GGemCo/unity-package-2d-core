using System.Threading.Tasks;
using UnityEngine;

namespace GGemCo2DCore
{
    /// <summary>
    /// 대화 UI에서 사용하는 캐릭터 표시 정보를 조회하는 헬퍼입니다.
    /// </summary>
    public static class DialogueCharacterHelper
    {
        /// <summary>
        /// 대사 노드에 설정된 말하는 캐릭터 이름을 가져옵니다.
        /// </summary>
        /// <param name="dialogue">이름을 조회할 대사 노드 데이터입니다.</param>
        /// <returns>캐릭터 이름입니다. 찾지 못하면 기본 표시 문자열을 반환합니다.</returns>
        public static string GetName(DialogueNodeData dialogue)
        {
            if (dialogue == null)
            {
                return string.Empty;
            }

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
        /// <param name="dialogue">썸네일을 조회할 대사 노드 데이터입니다.</param>
        /// <returns>로드된 썸네일 Sprite입니다. 찾지 못하면 <see langword="null"/>을 반환합니다.</returns>
        public static async Task<Sprite> GetThumbnail(DialogueNodeData dialogue)
        {
            if (dialogue == null) return null;
            return await GetThumbnail(dialogue.characterType, dialogue.characterUid, dialogue.thumbnailImage);
        }

        /// <summary>
        /// 캐릭터 타입, UID, 직접 지정 썸네일 이름을 기준으로 말하는 캐릭터 썸네일을 가져옵니다.
        /// </summary>
        /// <param name="characterType">썸네일을 찾을 캐릭터 타입입니다.</param>
        /// <param name="characterUid">NPC 또는 Monster 테이블에서 사용할 캐릭터 UID입니다.</param>
        /// <param name="thumbnailImage">테이블 썸네일 대신 사용할 직접 지정 썸네일 이미지 이름입니다.</param>
        /// <returns>로드된 썸네일 Sprite입니다. 찾지 못하면 <see langword="null"/>을 반환합니다.</returns>
        public static async Task<Sprite> GetThumbnail(CharacterConstants.Type characterType, int characterUid, string thumbnailImage)
        {
            if (!string.IsNullOrEmpty(thumbnailImage))
            {
                string key = $"{ConfigAddressableKey.CharacterThumbnail}_{thumbnailImage}";
                return await AddressableLoaderController.LoadByKeyAsync<Sprite>(key);
            }

            if (characterType == CharacterConstants.Type.Npc)
            {
                var data = TableLoaderManager.Instance.GetNpcData(characterUid);
                if (data != null && !string.IsNullOrEmpty(data.ImageThumbnailFileName))
                {
                    string key = $"{ConfigAddressableKey.CharacterThumbnailNpc}_{data.ImageThumbnailFileName}";
                    return await AddressableLoaderController.LoadByKeyAsync<Sprite>(key);
                }
            }
            else if (characterType == CharacterConstants.Type.Monster)
            {
                var data = TableLoaderManager.Instance.GetMonsterData(characterUid);
                if (data != null && !string.IsNullOrEmpty(data.ImageThumbnailFileName))
                {
                    string key = $"{ConfigAddressableKey.CharacterThumbnailMonster}_{data.ImageThumbnailFileName}";
                    return await AddressableLoaderController.LoadByKeyAsync<Sprite>(key);
                }
            }
            else if (characterType == CharacterConstants.Type.Player)
            {
                string key = ConfigAddressableKey.CharacterThumbnailPlayer;
                return await AddressableLoaderController.LoadByKeyAsync<Sprite>(key);
            }

            return null;
        }
    }
}
