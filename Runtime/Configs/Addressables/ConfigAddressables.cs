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
        
        public const string PathCharacter = Path + "/Characters";
        public const string PathPrefabMonster = PathCharacter + "/Monster";
        public const string PathPrefabNpc = PathCharacter + "/Npc";
        public const string PathPrefabPlayer = PathCharacter + "/Player";
        
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
        public const string PathEffectPlayer = PathEffect + "/Player";
        public const string PathEffectMonster = PathEffect + "/Monster";

        public static string GetPathEffect(EffectConstants.Category category)
        {
            return category switch
            {
                EffectConstants.Category.Player => PathEffectPlayer,
                EffectConstants.Category.Monster => PathEffectMonster,
                _ => PathEffectSkill
            };
        }
        
        // 어펙트
        public const string KeyImageIconAffect = ConfigDefine.NameSDK+"_Affect_Icon";
        public const string PathImageIconAffect = Path + "/Images/Icon/Affect";

        // 사운드
        public const string KeySound = ConfigDefine.NameSDK+"_Sound";
        public const string PathSound = Path + "/Sounds";
        public static string GetPathSound(StruckTableSound info)
        {
            if (info.Type != SoundConstants.Type.None && info.SubType != SoundConstants.SubType.None)
            {
                return $"{PathSound}/{info.Type}/{info.SubType}";
            }
            if (info.Type != SoundConstants.Type.None)
            {
                return $"{PathSound}/{info.Type}";
            }

            return "";
        }
    }
}