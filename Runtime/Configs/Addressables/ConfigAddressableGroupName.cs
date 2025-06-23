namespace GGemCo.Scripts
{
    public static class ConfigAddressableGroupName
    {
        // Common
        public const string Common = ConfigDefine.NameSDK+"_Common";
        
        // 캐릭터
        public const string Monster = ConfigAddressables.KeyPrefabMonster;
        public const string Npc = ConfigAddressables.KeyPrefabNpc;
        public const string Player = ConfigAddressables.KeyPrefabPlayer;

        // 이펙트
        public const string Effect = ConfigDefine.NameSDK+"_Effect";
        
        // 아이템
        public const string Item = ConfigDefine.NameSDK+"_Item";
        public const string ItemDropImage = Item +"_DropImage";
        public const string ItemIconImage = Item +"_IconImage";
        public const string ItemEquipImage = Item +"_EquipImage";
     
        // 맵
        public const string Map = ConfigDefine.NameSDK+"_Map";
        
        // 테이블
        public const string Table = ConfigDefine.NameSDK+"_Table";
    }
}