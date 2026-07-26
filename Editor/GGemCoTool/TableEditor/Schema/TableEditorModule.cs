using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
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
        /// <summary>
        /// 테이블 로더와 Row 타입을 기반으로 테이블 에디터 등록 정의를 생성합니다.
        /// </summary>
        /// <param name="moduleName">테이블을 제공하는 에디터 모듈명입니다.</param>
        /// <param name="packageName">테이블을 소유한 패키지명입니다.</param>
        /// <param name="tableKey">테이블을 식별하는 고유 키입니다.</param>
        /// <param name="assetPath">편집할 원본 테이블 에셋 경로입니다.</param>
        /// <param name="displayName">테이블 목록에 표시할 이름입니다.</param>
        /// <param name="tableType">데이터를 파싱할 테이블 로더 타입입니다.</param>
        /// <param name="rowType">테이블 한 행을 표현하는 타입입니다.</param>
        /// <param name="reloadAction">저장 후 런타임 테이블 캐시를 갱신할 작업입니다.</param>
        /// <param name="resolveReference">컬럼별 참조 테이블을 결정하는 선택적 resolver입니다.</param>
        /// <param name="resolveColumnTooltip">컬럼별 설명을 결정하는 선택적 resolver입니다.</param>
        /// <returns>테이블 에디터 레지스트리에 등록할 테이블 정의입니다.</returns>
        public static TableEditorTableDefinition Create(
            string moduleName,
            string packageName,
            string tableKey,
            string assetPath,
            string displayName,
            Type tableType,
            Type rowType,
            Action reloadAction = null,
            Func<string, TableEditorTableDefinition> resolveReference = null,
            Func<string, MemberInfo, string> resolveColumnTooltip = null)
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
                ResolveColumnTooltip = resolveColumnTooltip,
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

        /// <summary>
        /// 현재 저장 대상 문서의 데이터 행 목록입니다.
        /// SaveProcessor에서 저장 전후 검증/동기화 로직에 활용합니다.
        /// </summary>
        public IReadOnlyList<TableEditorDocumentRow> Rows { get; set; } = Array.Empty<TableEditorDocumentRow>();

        /// <summary>
        /// 현재 저장 요청에 실제 문서 변경이 포함되었는지 여부입니다.
        /// true일 때만 변경 기반 후처리(파일 검증, Addressables 동기화 등)를 수행합니다.
        /// 기본값은 하위 호환을 위해 true입니다.
        /// </summary>
        public bool HasDocumentChanges { get; set; } = true;

        /// <summary>
        /// 현재 저장 컨텍스트가 특정 테이블 키인지 검사합니다.
        /// </summary>
        /// <param name="tableKey">비교할 테이블 키입니다.</param>
        /// <returns>현재 테이블 키와 일치하면 true를 반환합니다.</returns>
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
