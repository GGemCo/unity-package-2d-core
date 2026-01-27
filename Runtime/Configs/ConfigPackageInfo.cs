using System.Collections.Generic;

namespace GGemCo2DCore
{
    public static class ConfigPackageInfo
    {
        public enum PackageType
        {
            None,
            Core,
            Control,
            Simulation,
            Tcg,
            Affect,
            Skill,
            AiBt
        }
        
        private class MetadataPackageInfo
        {
            public PackageType PackageType { get; }
            public string Name { get; }
            public string Prefix { get; }
        
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
        public const string NamePackageTcg = "Tcg";
        public const string NamePackageAffect = "Affect";
        public const string NamePackageSkill = "Skill";
        public const string NamePackageAiBt = "AiBt";
        
        // 오브젝트 생성시 사용
        private const string NamePrefixCore = ConfigDefine.NameSDK + "_" + ConfigPackageInfo.NamePackageCore;
        private const string NamePrefixControl = ConfigDefine.NameSDK + "_" + ConfigPackageInfo.NamePackageControl;
        private const string NamePrefixSimulation = ConfigDefine.NameSDK + "_" + ConfigPackageInfo.NamePackageSimulation;
        private const string NamePrefixTcg = ConfigDefine.NameSDK + "_" + ConfigPackageInfo.NamePackageTcg;
        private const string NamePrefixAffect = ConfigDefine.NameSDK + "_" + ConfigPackageInfo.NamePackageAffect;
        private const string NamePrefixSkill = ConfigDefine.NameSDK + "_" + ConfigPackageInfo.NamePackageSkill;
        private const string NamePrefixAiBt = ConfigDefine.NameSDK + "_" + ConfigPackageInfo.NamePackageAiBt;

        private static readonly MetadataPackageInfo Core = new (
            PackageType.Core,
            NamePackageCore,
            NamePrefixCore
        );

        private static readonly MetadataPackageInfo Control = new (
            PackageType.Control,
            NamePackageControl,
            NamePrefixControl
        );
        private static readonly MetadataPackageInfo Simulation = new (
            PackageType.Simulation,
            NamePackageSimulation,
            NamePrefixSimulation
        );
        private static readonly MetadataPackageInfo Tcg = new (
            PackageType.Tcg,
            NamePackageTcg,
            NamePrefixTcg
        );
        private static readonly MetadataPackageInfo Affect = new (
            PackageType.Affect,
            NamePackageAffect,
            NamePrefixAffect
        );
        private static readonly MetadataPackageInfo Skill = new (
            PackageType.Skill,
            NamePackageSkill,
            NamePrefixSkill
        );
        private static readonly MetadataPackageInfo AiBt = new (
            PackageType.AiBt,
            NamePackageAiBt,
            NamePrefixAiBt
        );

        private static readonly Dictionary<PackageType, MetadataPackageInfo> PackageInfos =
            new Dictionary<PackageType, MetadataPackageInfo>()
            {
                {PackageType.Core, Core},
                {PackageType.Control, Control},
                {PackageType.Simulation, Simulation},
                {PackageType.Tcg, Tcg},
                {PackageType.Affect, Affect},
                {PackageType.Skill, Skill},
                {PackageType.AiBt, AiBt},
            };

        public static string GetPackageName(PackageType packageType)
        {
            return PackageInfos.GetValueOrDefault(packageType).Name;
        }

        public static string GetPackagePrefix(PackageType packageType)
        {
            return PackageInfos.GetValueOrDefault(packageType).Prefix;
        }
    }
}