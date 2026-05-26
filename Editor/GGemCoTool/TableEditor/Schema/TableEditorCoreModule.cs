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

        /// <summary>
        /// Core 런타임 어셈블리에서 테이블 파서를 찾아 TableEditor 정의 목록을 생성합니다.
        /// </summary>
        /// <returns>TableEditor에 표시할 Core 테이블 정의 목록입니다.</returns>
        public IEnumerable<TableEditorTableDefinition> BuildDefinitions()
        {
            List<AddressableAssetInfo> infos = ConfigAddressableTable.All;
            Type defaultTableType = typeof(DefaultTable<>);
            Type runtimeAssemblyType = typeof(DefaultTable<>);

            foreach (Type type in runtimeAssemblyType.Assembly.GetTypes())
            {
                if (type.IsAbstract)
                    continue;

                if (!TryGetDefaultTableBaseType(type, defaultTableType, out Type tableBaseType))
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
                    tableBaseType.GetGenericArguments()[0],
                    TableEditorDefinitionFactory.CreateDefaultReloadAction(addressable.Path),
                    ResolveReference);
            }
        }

        /// <summary>
        /// 테이블 타입의 상속 체인에서 DefaultTable&lt;T&gt; 기반 타입을 찾습니다.
        /// </summary>
        /// <param name="type">검사할 테이블 타입입니다.</param>
        /// <param name="defaultTableType">비교 기준인 DefaultTable 타입입니다.</param>
        /// <param name="tableBaseType">찾은 DefaultTable&lt;T&gt; 기반 타입입니다.</param>
        /// <returns>DefaultTable&lt;T&gt; 기반 타입을 찾으면 true를 반환합니다.</returns>
        private static bool TryGetDefaultTableBaseType(Type type, Type defaultTableType, out Type tableBaseType)
        {
            tableBaseType = null;
            for (Type current = type.BaseType; current != null; current = current.BaseType)
            {
                if (!current.IsGenericType)
                    continue;

                if (current.GetGenericTypeDefinition() != defaultTableType)
                    continue;

                tableBaseType = current;
                return true;
            }

            return false;
        }

        /// <summary>
        /// 컬럼 헤더명을 기준으로 참조 가능한 테이블 정의를 찾습니다.
        /// </summary>
        /// <param name="headerName">참조 컬럼 헤더명입니다.</param>
        /// <returns>참조 테이블 정의입니다. 찾지 못하면 null을 반환합니다.</returns>
        private static TableEditorTableDefinition ResolveReference(string headerName)
        {
            return TableEditorRegistry.FindReferenceTable(headerName);
        }
    }
}
