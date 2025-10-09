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
            Simulation
        }
        
        private class MetadataPackageInfo
        {
            public ConfigPackageInfo.PackageType PackageType { get; }
            public string Name { get; }
            public string Prefix { get; }
        
            public MetadataPackageInfo(ConfigPackageInfo.PackageType packageType, string name, string prefix)
            {
                PackageType = packageType;
                Name = name;
                Prefix = prefix;
            }
        }

        public const string NamePackageCore = "Core";
        private const string NamePackageControl = "Control";
        private const string NamePackageSimulation = "Simulation";
        
        // 오브젝트 생성시 사용
        private const string NamePrefixCore = ConfigDefine.NameSDK + "_" + ConfigPackageInfo.NamePackageCore;
        private const string NamePrefixControl = ConfigDefine.NameSDK + "_" + ConfigPackageInfo.NamePackageControl;
        private const string NamePrefixSimulation = ConfigDefine.NameSDK + "_" + ConfigPackageInfo.NamePackageSimulation;

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

        private static readonly Dictionary<PackageType, MetadataPackageInfo> PackageInfos =
            new Dictionary<PackageType, MetadataPackageInfo>()
            {
                {PackageType.Core, Core},
                {PackageType.Control, Control},
                {PackageType.Simulation, Simulation},
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