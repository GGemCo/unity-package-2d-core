using System;
using System.Collections.Generic;
using System.Linq;
using GGemCo2DCore;
using UnityEditor;
using UnityEngine;

namespace GGemCo2DCoreEditor
{
    public interface ITableEditorModule
    {
        string ModuleName { get; }
        string PackageName { get; }
        IEnumerable<TableEditorTableDefinition> BuildDefinitions();
    }

    public static class TableEditorDefinitionFactory
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

    public sealed class TableEditorSaveContext
    {
        public TableEditorTableDefinition TableDefinition { get; set; }
        public bool IsTable(string tableKey)
        {
            return TableDefinition != null
                   && !string.IsNullOrWhiteSpace(tableKey)
                   && string.Equals(TableDefinition.TableKey, tableKey, StringComparison.OrdinalIgnoreCase);
        }
    }

    public interface ITableEditorSaveProcessor
    {
        int Order { get; }

        bool CanProcess(TableEditorSaveContext context);

        void BeforeSave(TableEditorSaveContext context);

        void AfterSave(TableEditorSaveContext context);
    }

    internal static class TableEditorSaveProcessorRegistry
    {
        private static List<ITableEditorSaveProcessor> _processors;

        public static IReadOnlyList<ITableEditorSaveProcessor> GetAll()
        {
            if (_processors != null)
                return _processors;

            _processors = new List<ITableEditorSaveProcessor>();
            foreach (Type type in TypeCache.GetTypesDerivedFrom<ITableEditorSaveProcessor>())
            {
                if (type.IsAbstract || type.IsInterface)
                    continue;

                try
                {
                    if (Activator.CreateInstance(type) is ITableEditorSaveProcessor processor)
                        _processors.Add(processor);
                }
                catch (Exception ex)
                {
                    Debug.LogException(ex);
                }
            }

            _processors = _processors
                .OrderBy(static processor => processor.Order)
                .ToList();

            return _processors;
        }

        public static void Invalidate()
        {
            _processors = null;
        }
    }
}
