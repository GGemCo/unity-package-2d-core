using System.Collections.Generic;

namespace GGemCo.Scripts
{
    public static class ConfigAddressables
    {
        // 기본 설정 Scriptable Object
        public const string KeySettings = "GGemCo_Settings";
        public const string KeyPlayerSettings = "GGemCo_PlayerSettings";
        public const string KeyMapSettings = "GGemCo_MapSettings";
        public const string KeySaveSettings = "GGemCo_SaveSettings";
        // 몬스터 데미지 표시 text object
        public const string KeyTextFloatingDamage = "GGemCo_TextFloatingDamage";
        // 드랍 아이템 프리팹
        public const string KeyPrefabDropItem = "GGemCo_PrefabDropItem";
        // 윈도우 slot 프리팹
        public const string KeyPrefabSlot = "GGemCo_PrefabSlot";
        // 아이콘 아이템 프리팹
        public const string KeyPrefabIconItem = "GGemCo_PrefabIconItem";
        // 아이콘 스킬 프리팹
        public const string KeyPrefabIconSkill = "GGemCo_PrefabIconSkill";
        // 몬스터 hp bar
        public const string KeyPrefabSliderMonsterHp = "GGemCo_PrefabSliderMonsterHp";
        // 아이템 이름 태그
        public const string KeyPrefabTextDropItemNameTag = "GGemCo_PrefabTextDropItemNameTag";
        // npc 이름 태그
        public const string KeyPrefabTextNpcNameTag = "GGemCo_PrefabTextNpcNameTag";
        // 말풍선
        public const string KeyPrefabDialogueBalloon = "GGemCo_PrefabDialogueBalloon";
        // 퀘스트 Ready 아이콘. 느낌표
        public const string KeyPrefabIconQuestReady = "GGemCo_PrefabIconQuestReady";
        // 퀘스트 InProgress 아이콘. 물음표
        public const string KeyPrefabIconQuestInProgress = "GGemCo_PrefabIconQuestInProgress";
        
        public static readonly (string key, string path)[] AssetsToAdd =
        {
            (KeySettings, "Assets/GGemCo/Settings/GGemCoSettings.asset"),
            (KeyPlayerSettings, "Assets/GGemCo/Settings/GGemCoPlayerSettings.asset"),
            (KeyMapSettings, "Assets/GGemCo/Settings/GGemCoMapSettings.asset"),
            (KeySaveSettings, "Assets/GGemCo/Settings/GGemCoSaveSettings.asset"),
            
            (KeyTextFloatingDamage, "Assets/Data/Prefabs/UI/TextDamage.prefab"),
            (KeyPrefabSlot, "Assets/Data/Prefabs/UI/Icon/Slot.prefab"),
            (KeyPrefabIconItem, "Assets/Data/Prefabs/UI/Icon/IconItem.prefab"),
            (KeyPrefabIconSkill, "Assets/Data/Prefabs/UI/Icon/IconSkill.prefab"),
            (KeyPrefabSliderMonsterHp, "Assets/Data/Prefabs/UI/SliderMonsterHp.prefab"),
            (KeyPrefabDialogueBalloon, "Assets/Data/Prefabs/UI/UIDialogueBalloon.prefab"),
            
            (KeyPrefabDropItem, "Assets/Data/Prefabs/Item/DropItem.prefab"),
            (KeyPrefabTextDropItemNameTag, "Assets/Data/Prefabs/Item/TextDropItemNameTag.prefab"),
            (KeyPrefabTextNpcNameTag, "Assets/Data/Prefabs/Item/TextNpcNameTag.prefab"),
            
            (KeyPrefabIconQuestReady, "Assets/Data/Prefabs/UI/Icon/IconQuestReady.prefab"),
            (KeyPrefabIconQuestInProgress, "Assets/Data/Prefabs/UI/Icon/IconQuestInProgress.prefab"),
        };
        
        // 게임 에서 사용하는 미리 로드해야하는 프리팹
        public const string LabelPreLoadGamePrefabs = "GGemCo_PreLoadGamePrefabs";
        
        public static readonly Dictionary<string, string> AssetsToAddLabel = new Dictionary<string, string>
        {
            { KeyTextFloatingDamage, LabelPreLoadGamePrefabs },
            { KeyPrefabSlot, LabelPreLoadGamePrefabs },
            { KeyPrefabIconItem, LabelPreLoadGamePrefabs },
            { KeyPrefabIconSkill, LabelPreLoadGamePrefabs },
            { KeyPrefabDropItem, LabelPreLoadGamePrefabs },
            { KeyPrefabSliderMonsterHp, LabelPreLoadGamePrefabs },
            { KeyPrefabTextDropItemNameTag, LabelPreLoadGamePrefabs },
            { KeyPrefabTextNpcNameTag, LabelPreLoadGamePrefabs },
            { KeyPrefabDialogueBalloon, LabelPreLoadGamePrefabs },
            { KeyPrefabIconQuestReady, LabelPreLoadGamePrefabs },
            { KeyPrefabIconQuestInProgress, LabelPreLoadGamePrefabs },
        };
        
        // 테이블
        public const string LabelTable = "GGemCo_Table";
        public static readonly (string key, string path)[] AssetsToAddTable =
        {
            ($"{LabelTable}_{ConfigTableFileName.Map}", $"{ConfigTableFileName.Path}/{ConfigTableFileName.Map}.txt"),
            ($"{LabelTable}_{ConfigTableFileName.Monster}", $"{ConfigTableFileName.Path}/{ConfigTableFileName.Monster}.txt"),
            ($"{LabelTable}_{ConfigTableFileName.Npc}", $"{ConfigTableFileName.Path}/{ConfigTableFileName.Npc}.txt"),
            ($"{LabelTable}_{ConfigTableFileName.Animation}", $"{ConfigTableFileName.Path}/{ConfigTableFileName.Animation}.txt"),
            ($"{LabelTable}_{ConfigTableFileName.Item}", $"{ConfigTableFileName.Path}/{ConfigTableFileName.Item}.txt"),
            ($"{LabelTable}_{ConfigTableFileName.MonsterDropRate}", $"{ConfigTableFileName.Path}/{ConfigTableFileName.MonsterDropRate}.txt"),
            ($"{LabelTable}_{ConfigTableFileName.ItemDropGroup}", $"{ConfigTableFileName.Path}/{ConfigTableFileName.ItemDropGroup}.txt"),
            ($"{LabelTable}_{ConfigTableFileName.Exp}", $"{ConfigTableFileName.Path}/{ConfigTableFileName.Exp}.txt"),
            ($"{LabelTable}_{ConfigTableFileName.Window}", $"{ConfigTableFileName.Path}/{ConfigTableFileName.Window}.txt"),
            ($"{LabelTable}_{ConfigTableFileName.Status}", $"{ConfigTableFileName.Path}/{ConfigTableFileName.Status}.txt"),
            ($"{LabelTable}_{ConfigTableFileName.Skill}", $"{ConfigTableFileName.Path}/{ConfigTableFileName.Skill}.txt"),
            ($"{LabelTable}_{ConfigTableFileName.Affect}", $"{ConfigTableFileName.Path}/{ConfigTableFileName.Affect}.txt"),
            ($"{LabelTable}_{ConfigTableFileName.Effect}", $"{ConfigTableFileName.Path}/{ConfigTableFileName.Effect}.txt"),
            ($"{LabelTable}_{ConfigTableFileName.Interaction}", $"{ConfigTableFileName.Path}/{ConfigTableFileName.Interaction}.txt"),
            ($"{LabelTable}_{ConfigTableFileName.Shop}", $"{ConfigTableFileName.Path}/{ConfigTableFileName.Shop}.txt"),
            ($"{LabelTable}_{ConfigTableFileName.ItemUpgrade}", $"{ConfigTableFileName.Path}/{ConfigTableFileName.ItemUpgrade}.txt"),
            ($"{LabelTable}_{ConfigTableFileName.ItemSalvage}", $"{ConfigTableFileName.Path}/{ConfigTableFileName.ItemSalvage}.txt"),
            ($"{LabelTable}_{ConfigTableFileName.ItemCraft}", $"{ConfigTableFileName.Path}/{ConfigTableFileName.ItemCraft}.txt"),
            ($"{LabelTable}_{ConfigTableFileName.Cutscene}", $"{ConfigTableFileName.Path}/{ConfigTableFileName.Cutscene}.txt"),
            ($"{LabelTable}_{ConfigTableFileName.Dialogue}", $"{ConfigTableFileName.Path}/{ConfigTableFileName.Dialogue}.txt"),
            ($"{LabelTable}_{ConfigTableFileName.Quest}", $"{ConfigTableFileName.Path}/{ConfigTableFileName.Quest}.txt"),
        };

    }
}