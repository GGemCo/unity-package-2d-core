using System.Collections.Generic;

namespace GGemCo2DCore
{
    public static class ConfigAddressables
    {
        public const string Path = "Assets/"+ConfigDefine.NameSDK+"/DataAddressable";
        public const string PathSpriteAtlas = Path+"/SpriteAtlas";
        /// <summary>
        /// 로딩 씬에서 로드 해야 되는 리스트
        /// </summary>
        public static readonly List<AddressableAssetInfo> NeedLoadInLoadingScene = new()
        {
        };
        
        // 아이템
        public const string PathItemParts = Path + "/Images/Parts";
        
        // 플레이어
        public const string KeyCharacter = ConfigDefine.NameSDK+"_Character";
        
        public const string KeyPrefabMonster = KeyCharacter + "_Monster";
        public const string KeyPrefabNpc = KeyCharacter + "_Npc";
        public const string KeyPrefabPlayer = KeyCharacter + "_Player";
        
        // 대사
        public const string KeyDialogue = ConfigDefine.NameSDK+"_Dialogue";
        public const string PathJsonDialogue = Path + "/Dialogue";
        
        // 퀘스트
        public const string KeyQuest = ConfigDefine.NameSDK+"_Quest";
        public const string PathJsonQuest = Path + "/Quests";
        
        // 썸네일
        public const string KeyCharacterThumbnail = ConfigDefine.NameSDK+"_CharacterThumbnail";
        public const string KeyCharacterThumbnailNpc = KeyCharacterThumbnail+"_Npc";
        public const string KeyCharacterThumbnailMonster = KeyCharacterThumbnail+"_Monster";

        public const string PathCharacterThumbnail = Path + "/Images/Thumbnail";
        public const string PathCharacterThumbnailNpc = PathCharacterThumbnail + "/Npc";
        public const string PathCharacterThumbnailMonster = PathCharacterThumbnail + "/Monster";

        // 연출
        public const string KeyCutscene = ConfigDefine.NameSDK+"_Cutscene";
        public const string PathJsonCutscene = Path + "/Cutscene";
        
        // 스킬
        public const string KeyImageIconSkill = ConfigDefine.NameSDK+"_Skill_Icon";
        public const string PathImageIconSkill = Path + "/Images/Icon/Skill";
        
        // 이펙트
        public const string PathEffect = Path + "/Effects";
        public const string PathEffectSkill = PathEffect + "/Skills";
    }
}