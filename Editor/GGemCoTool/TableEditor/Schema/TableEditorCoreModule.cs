using System;
using System.Collections.Generic;
using System.Linq;
using GGemCo2DCore;

namespace GGemCo2DCoreEditor
{
    internal sealed class TableEditorCoreModule : ITableEditorModule
    {
        public string ModuleName => "Core";
        public string PackageName => "Core";

        public IEnumerable<TableEditorTableDefinition> BuildDefinitions()
        {
            List<AddressableAssetInfo> infos = ConfigAddressableTable.All;
            Type defaultTableType = typeof(DefaultTable<>);
            Type runtimeAssemblyType = typeof(DefaultTable<>);

            foreach (Type type in runtimeAssemblyType.Assembly.GetTypes())
            {
                if (type.IsAbstract)
                    continue;

                Type baseType = type.BaseType;
                if (baseType == null || !baseType.IsGenericType || baseType.GetGenericTypeDefinition() != defaultTableType)
                    continue;

                object tableInstance;
                try
                {
                    tableInstance = Activator.CreateInstance(type);
                }
                catch
                {
                    continue;
                }

                if (!(tableInstance is ITableParser parser))
                    continue;

                string key = parser.Key;
                if (string.IsNullOrWhiteSpace(key) || string.Equals(key, ConfigAddressableTable.None, StringComparison.OrdinalIgnoreCase))
                    continue;

                AddressableAssetInfo addressable = infos.FirstOrDefault(a => string.Equals(a.Etc1, key, StringComparison.OrdinalIgnoreCase));
                if (addressable == null)
                    continue;

                yield return TableEditorDefinitionFactory.Create(
                    ModuleName,
                    PackageName,
                    key,
                    addressable.Path,
                    key,
                    type,
                    baseType.GetGenericArguments()[0],
                    TableEditorDefinitionFactory.CreateDefaultReloadAction(addressable.Path),
                    ResolveReference);
            }
        }

        private static TableEditorTableDefinition ResolveReference(string headerName)
        {
            return TableEditorRegistry.FindReferenceTable(headerName);
        }
    }
}
