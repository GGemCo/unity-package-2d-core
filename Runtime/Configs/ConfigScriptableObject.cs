using System;
using System.Collections.Generic;

namespace GGemCo2DCore
{
    /// <summary>
    /// ScriptableObject 관련 설정 정의
    /// </summary>
    public static class ConfigScriptableObject
    {
        /// <summary>
        /// 메뉴 순서 정의
        /// </summary>
        public enum MenuOrdering
        {
            None = -10000,
            MainSettings,
            PlayerSettings,
            MonsterSettings,
            MapSettings,
            SaveSettings,
            OptionSettings,
            SoundSettings,
            AttackComboSettings,
            PlayerActionSettings,
            SimulationSettings,
            GameTimeSettings,
            TcgSettings
        }

        public const string BasePath = ConfigDefine.NameSDK + "/Settings/";
        public const string BaseName = ConfigDefine.NameSDK;

        public static class Main
        {
            public const string FileName = BaseName + "Settings";
            public const string MenuName = BasePath + FileName;
            public const int Ordering = (int)MenuOrdering.MainSettings;
        }

        public static class Player
        {
            public const string FileName = BaseName + "PlayerSettings";
            public const string MenuName = BasePath + FileName;
            public const int Ordering = (int)MenuOrdering.PlayerSettings;
        }
        
        public static class Monster
        {
            public const string FileName = BaseName + "MonsterSettings";
            public const string MenuName = BasePath + FileName;
            public const int Ordering = (int)MenuOrdering.MonsterSettings;
        }

        public static class Map
        {
            public const string FileName = BaseName + "MapSettings";
            public const string MenuName = BasePath + FileName;
            public const int Ordering = (int)MenuOrdering.MapSettings;
        }

        public static class Save
        {
            public const string FileName = BaseName + "SaveSettings";
            public const string MenuName = BasePath + FileName;
            public const int Ordering = (int)MenuOrdering.SaveSettings;
        }

        public static class Option
        {
            public const string FileName = BaseName + "OptionSettings";
            public const string MenuName = BasePath + FileName;
            public const int Ordering = (int)MenuOrdering.OptionSettings;
        }

        public static class Sound
        {
            public const string FileName = BaseName + "SoundSettings";
            public const string MenuName = BasePath + FileName;
            public const int Ordering = (int)MenuOrdering.SoundSettings;
        }
        public static class GameTime
        {
            public const string FileName = BaseName + "GameTimeSettings";
            public const string MenuName = BasePath + FileName;
            public const int Ordering = (int)MenuOrdering.GameTimeSettings;
        }

        public static readonly Dictionary<string, Type> SettingsTypes = new()
        {
            { Main.FileName, typeof(GGemCoSettings) },
            { Map.FileName, typeof(GGemCoMapSettings) },
            { Player.FileName, typeof(GGemCoPlayerSettings) },
            { Save.FileName, typeof(GGemCoSaveSettings) },
            { Option.FileName, typeof(GGemCoOptionSettings) },
            { Sound.FileName, typeof(GGemCoSoundSettings) },
            { GameTime.FileName, typeof(GGemCoGameTimeSettings) },
        };
    }
}