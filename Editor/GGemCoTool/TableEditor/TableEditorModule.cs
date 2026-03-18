using System;
using System.Collections.Generic;
using GGemCo2DCore;

namespace GGemCo2DCoreEditor
{
    internal interface ITableEditorModule
    {
        string ModuleName { get; }
        string PackageName { get; }
        IEnumerable<TableEditorTableDefinition> BuildDefinitions();
    }

    internal static class TableEditorDefinitionFactory
    {
        public static TableEditorTableDefinition Create(
            string moduleName,
            string packageName,
            string tableKey,
            string assetPath,
            string displayName,
            Type tableType,
            Type rowType,
            Action reloadAction = null,
            Func<string, TableEditorTableDefinition> resolveReference = null)
        {
            return new TableEditorTableDefinition
            {
                ModuleName = moduleName,
                PackageName = packageName,
                TableKey = tableKey,
                AssetPath = assetPath,
                DisplayName = string.IsNullOrWhiteSpace(displayName) ? tableKey : displayName,
                TableType = tableType,
                RowType = rowType,
                ReloadAction = reloadAction,
                ResolveReferenceTable = resolveReference,
                CreateTableInstanceAndLoad = content =>
                {
                    object instance = Activator.CreateInstance(tableType);
                    if (instance is ITableParser tableParser)
                    {
                        tableParser.LoadData(content);
                        return instance;
                    }

                    return null;
                }
            };
        }

        public static Action CreateDefaultReloadAction(string assetPath)
        {
            return () =>
            {
                if (string.IsNullOrWhiteSpace(assetPath))
                    return;

                TableLoaderManagerBase.Unload(assetPath);
            };
        }
    }
}
