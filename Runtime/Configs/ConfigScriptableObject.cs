using System;
using System.Collections.Generic;

namespace GGemCo2DCore
{
    /// <summary>
    /// Core 패키지 ScriptableObject 메뉴 설정 정의
    /// </summary>
    public static class ConfigScriptableObject
    {
        /// <summary>
        /// Core 패키지 Settings 식별 키
        /// </summary>
        public enum CoreSettingsKey
        {
            Main,
            Player,
            Monster,
            Map,
            Save,
            Option,
            Sound,
            GameTime,
        }

        /// <summary>
        /// Core 패키지 내부 메뉴 정렬 순서
        /// 중간 삽입을 고려해 10 단위 간격으로 배치한다.
        /// </summary>
        public enum CoreLocalOrder
        {
            MainSettings = 0,
            PlayerSettings = 10,
            MonsterSettings = 20,
            MapSettings = 30,
            SaveSettings = 40,
            OptionSettings = 50,
            SoundSettings = 60,
            GameTimeSettings = 70,
        }

        public const string BasePath = ConfigDefine.NameSDK + "/Settings/";
        public const string BaseName = ConfigDefine.NameSDK;

        /// <summary>
        /// 메인 설정 메뉴 정보
        /// </summary>
        public static class Main
        {
            public const string FileName = BaseName + "Settings";
            public const string MenuName = BasePath + FileName;
            public const int Ordering =
                (int)ConfigScriptableObjectCommon.PackageOrder.Core +
                (int)CoreLocalOrder.MainSettings;
        }

        /// <summary>
        /// 플레이어 설정 메뉴 정보
        /// </summary>
        public static class Player
        {
            public const string FileName = BaseName + "PlayerSettings";
            public const string MenuName = BasePath + FileName;
            public const int Ordering =
                (int)ConfigScriptableObjectCommon.PackageOrder.Core +
                (int)CoreLocalOrder.PlayerSettings;
        }

        /// <summary>
        /// 몬스터 설정 메뉴 정보
        /// </summary>
        public static class Monster
        {
            public const string FileName = BaseName + "MonsterSettings";
            public const string MenuName = BasePath + FileName;
            public const int Ordering =
                (int)ConfigScriptableObjectCommon.PackageOrder.Core +
                (int)CoreLocalOrder.MonsterSettings;
        }

        /// <summary>
        /// 맵 설정 메뉴 정보
        /// </summary>
        public static class Map
        {
            public const string FileName = BaseName + "MapSettings";
            public const string MenuName = BasePath + FileName;
            public const int Ordering =
                (int)ConfigScriptableObjectCommon.PackageOrder.Core +
                (int)CoreLocalOrder.MapSettings;
        }

        /// <summary>
        /// 저장 설정 메뉴 정보
        /// </summary>
        public static class Save
        {
            public const string FileName = BaseName + "SaveSettings";
            public const string MenuName = BasePath + FileName;
            public const int Ordering =
                (int)ConfigScriptableObjectCommon.PackageOrder.Core +
                (int)CoreLocalOrder.SaveSettings;
        }

        /// <summary>
        /// 옵션 설정 메뉴 정보
        /// </summary>
        public static class Option
        {
            public const string FileName = BaseName + "OptionSettings";
            public const string MenuName = BasePath + FileName;
            public const int Ordering =
                (int)ConfigScriptableObjectCommon.PackageOrder.Core +
                (int)CoreLocalOrder.OptionSettings;
        }

        /// <summary>
        /// 사운드 설정 메뉴 정보
        /// </summary>
        public static class Sound
        {
            public const string FileName = BaseName + "SoundSettings";
            public const string MenuName = BasePath + FileName;
            public const int Ordering =
                (int)ConfigScriptableObjectCommon.PackageOrder.Core +
                (int)CoreLocalOrder.SoundSettings;
        }

        /// <summary>
        /// 게임 시간 설정 메뉴 정보
        /// </summary>
        public static class GameTime
        {
            public const string FileName = BaseName + "GameTimeSettings";
            public const string MenuName = BasePath + FileName;
            public const int Ordering =
                (int)ConfigScriptableObjectCommon.PackageOrder.Core +
                (int)CoreLocalOrder.GameTimeSettings;
        }

        /// <summary>
        /// Core 패키지 전체 메뉴 메타데이터
        /// 에디터 툴, 자동 생성, 검증 로직에서 재사용할 수 있다.
        /// </summary>
        public static readonly IReadOnlyDictionary<CoreSettingsKey, ConfigScriptableObjectCommon.MenuInfo> Infos =
            new Dictionary<CoreSettingsKey, ConfigScriptableObjectCommon.MenuInfo>
            {
                {
                    CoreSettingsKey.Main,
                    new ConfigScriptableObjectCommon.MenuInfo(
                        Main.FileName,
                        Main.MenuName,
                        Main.Ordering,
                        typeof(GGemCoSettings))
                },
                {
                    CoreSettingsKey.Player,
                    new ConfigScriptableObjectCommon.MenuInfo(
                        Player.FileName,
                        Player.MenuName,
                        Player.Ordering,
                        typeof(GGemCoPlayerSettings))
                },
                {
                    CoreSettingsKey.Monster,
                    new ConfigScriptableObjectCommon.MenuInfo(
                        Monster.FileName,
                        Monster.MenuName,
                        Monster.Ordering,
                        typeof(GGemCoMonsterSettings))
                },
                {
                    CoreSettingsKey.Map,
                    new ConfigScriptableObjectCommon.MenuInfo(
                        Map.FileName,
                        Map.MenuName,
                        Map.Ordering,
                        typeof(GGemCoMapSettings))
                },
                {
                    CoreSettingsKey.Save,
                    new ConfigScriptableObjectCommon.MenuInfo(
                        Save.FileName,
                        Save.MenuName,
                        Save.Ordering,
                        typeof(GGemCoSaveSettings))
                },
                {
                    CoreSettingsKey.Option,
                    new ConfigScriptableObjectCommon.MenuInfo(
                        Option.FileName,
                        Option.MenuName,
                        Option.Ordering,
                        typeof(GGemCoOptionSettings))
                },
                {
                    CoreSettingsKey.Sound,
                    new ConfigScriptableObjectCommon.MenuInfo(
                        Sound.FileName,
                        Sound.MenuName,
                        Sound.Ordering,
                        typeof(GGemCoSoundSettings))
                },
                {
                    CoreSettingsKey.GameTime,
                    new ConfigScriptableObjectCommon.MenuInfo(
                        GameTime.FileName,
                        GameTime.MenuName,
                        GameTime.Ordering,
                        typeof(GGemCoGameTimeSettings))
                },
            };

        /// <summary>
        /// 파일명 기준으로 타입을 조회하기 위한 매핑
        /// </summary>
        public static readonly IReadOnlyDictionary<string, Type> SettingsTypes =
            new Dictionary<string, Type>
            {
                { Main.FileName, typeof(GGemCoSettings) },
                { Player.FileName, typeof(GGemCoPlayerSettings) },
                { Monster.FileName, typeof(GGemCoMonsterSettings) },
                { Map.FileName, typeof(GGemCoMapSettings) },
                { Save.FileName, typeof(GGemCoSaveSettings) },
                { Option.FileName, typeof(GGemCoOptionSettings) },
                { Sound.FileName, typeof(GGemCoSoundSettings) },
                { GameTime.FileName, typeof(GGemCoGameTimeSettings) },
            };

        /// <summary>
        /// 설정 키로 메뉴 정보를 조회한다.
        /// </summary>
        public static ConfigScriptableObjectCommon.MenuInfo GetInfo(CoreSettingsKey key)
        {
            return Infos[key];
        }

        /// <summary>
        /// 파일명으로 설정 타입을 조회한다.
        /// </summary>
        public static bool TryGetSettingsType(string fileName, out Type settingsType)
        {
            return SettingsTypes.TryGetValue(fileName, out settingsType);
        }
    }
}