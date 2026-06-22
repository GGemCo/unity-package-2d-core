using System.Collections.Generic;

namespace GGemCo2DCore
{
    /// <summary>
    /// 패키지 타입별 이름과 오브젝트 생성용 Prefix 정보를 관리하는 설정 클래스입니다.
    /// </summary>
    public static class ConfigPackageInfo
    {
        /// <summary>
        /// SDK에서 사용하는 패키지 유형을 정의합니다.
        /// </summary>
        public enum PackageType
        {
            None,
            Core,
            Control,
            Simulation,
            Tcg,
            Affect,
            Skill,
            AiBt,
            Quest,
            Tutorial
        }

        /// <summary>
        /// 패키지 메타데이터를 보관하는 내부 클래스입니다.
        /// </summary>
        private class MetadataPackageInfo
        {
            /// <summary>
            /// 패키지 유형입니다.
            /// </summary>
            public PackageType PackageType { get; }

            /// <summary>
            /// 패키지 이름입니다.
            /// </summary>
            public string Name { get; }

            /// <summary>
            /// 오브젝트 생성 시 사용하는 Prefix입니다.
            /// </summary>
            public string Prefix { get; }

            /// <summary>
            /// 패키지 메타데이터를 생성합니다.
            /// </summary>
            /// <param name="packageType">패키지 유형입니다.</param>
            /// <param name="name">패키지 이름입니다.</param>
            /// <param name="prefix">오브젝트 생성 시 사용할 Prefix입니다.</param>
            public MetadataPackageInfo(PackageType packageType, string name, string prefix)
            {
                PackageType = packageType;
                Name = name;
                Prefix = prefix;
            }
        }

        private const string NamePackageCore = "Core";
        private const string NamePackageControl = "Control";
        private const string NamePackageSimulation = "Simulation";

        /// <summary>
        /// TCG 패키지 이름입니다.
        /// </summary>
        public const string NamePackageTcg = "Tcg";

        /// <summary>
        /// Affect 패키지 이름입니다.
        /// </summary>
        public const string NamePackageAffect = "Affect";

        /// <summary>
        /// Skill 패키지 이름입니다.
        /// </summary>
        public const string NamePackageSkill = "Skill";

        /// <summary>
        /// AI Behavior Tree 패키지 이름입니다.
        /// </summary>
        public const string NamePackageAiBt = "AiBt";

        /// <summary>
        /// Quest 패키지 이름입니다.
        /// </summary>
        public const string NamePackageQuest = "Quest";

        /// <summary>
        /// Tutorial 패키지 이름입니다.
        /// </summary>
        public const string NamePackageTutorial = "Tutorial";

        // 오브젝트 생성시 사용
        private const string NamePrefixCore = ConfigDefine.NameSDK + "_" + ConfigPackageInfo.NamePackageCore;
        private const string NamePrefixControl = ConfigDefine.NameSDK + "_" + ConfigPackageInfo.NamePackageControl;
        private const string NamePrefixSimulation = ConfigDefine.NameSDK + "_" + ConfigPackageInfo.NamePackageSimulation;
        private const string NamePrefixTcg = ConfigDefine.NameSDK + "_" + ConfigPackageInfo.NamePackageTcg;
        private const string NamePrefixAffect = ConfigDefine.NameSDK + "_" + ConfigPackageInfo.NamePackageAffect;
        private const string NamePrefixSkill = ConfigDefine.NameSDK + "_" + ConfigPackageInfo.NamePackageSkill;
        private const string NamePrefixAiBt = ConfigDefine.NameSDK + "_" + ConfigPackageInfo.NamePackageAiBt;
        private const string NamePrefixQuest = ConfigDefine.NameSDK + "_" + ConfigPackageInfo.NamePackageQuest;
        private const string NamePrefixTutorial = ConfigDefine.NameSDK + "_" + ConfigPackageInfo.NamePackageTutorial;

        private static readonly MetadataPackageInfo Core = new(
            PackageType.Core,
            NamePackageCore,
            NamePrefixCore
        );

        private static readonly MetadataPackageInfo Control = new(
            PackageType.Control,
            NamePackageControl,
            NamePrefixControl
        );

        private static readonly MetadataPackageInfo Simulation = new(
            PackageType.Simulation,
            NamePackageSimulation,
            NamePrefixSimulation
        );

        private static readonly MetadataPackageInfo Tcg = new(
            PackageType.Tcg,
            NamePackageTcg,
            NamePrefixTcg
        );

        private static readonly MetadataPackageInfo Affect = new(
            PackageType.Affect,
            NamePackageAffect,
            NamePrefixAffect
        );

        private static readonly MetadataPackageInfo Skill = new(
            PackageType.Skill,
            NamePackageSkill,
            NamePrefixSkill
        );

        private static readonly MetadataPackageInfo AiBt = new(
            PackageType.AiBt,
            NamePackageAiBt,
            NamePrefixAiBt
        );

        private static readonly MetadataPackageInfo Quest = new(
            PackageType.Quest,
            NamePackageQuest,
            NamePrefixQuest
        );

        private static readonly MetadataPackageInfo Tutorial = new(
            PackageType.Tutorial,
            NamePackageTutorial,
            NamePrefixTutorial
        );

        /// <summary>
        /// 패키지 유형별 메타데이터 조회 테이블입니다.
        /// </summary>
        private static readonly Dictionary<PackageType, MetadataPackageInfo> PackageInfos =
            new Dictionary<PackageType, MetadataPackageInfo>()
            {
                { PackageType.Core, Core },
                { PackageType.Control, Control },
                { PackageType.Simulation, Simulation },
                { PackageType.Tcg, Tcg },
                { PackageType.Affect, Affect },
                { PackageType.Skill, Skill },
                { PackageType.AiBt, AiBt },
                { PackageType.Quest, Quest },
                { PackageType.Tutorial, Tutorial },
            };

        /// <summary>
        /// 지정한 패키지 유형에 대응하는 패키지 이름을 반환합니다.
        /// </summary>
        /// <param name="packageType">조회할 패키지 유형입니다.</param>
        /// <returns>패키지 이름입니다. 등록되지 않은 유형인 경우 null을 반환합니다.</returns>
        public static string GetPackageName(PackageType packageType)
        {
            return PackageInfos.GetValueOrDefault(packageType)?.Name;
        }

        /// <summary>
        /// 지정한 패키지 유형에 대응하는 오브젝트 생성용 Prefix를 반환합니다.
        /// </summary>
        /// <param name="packageType">조회할 패키지 유형입니다.</param>
        /// <returns>패키지 Prefix입니다. 등록되지 않은 유형인 경우 null을 반환합니다.</returns>
        public static string GetPackagePrefix(PackageType packageType)
        {
            return PackageInfos.GetValueOrDefault(packageType)?.Prefix;
        }
    }
}