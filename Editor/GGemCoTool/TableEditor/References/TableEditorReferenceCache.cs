using System;
using System.Collections.Generic;
using System.Reflection;
using GGemCo2DCore;

namespace GGemCo2DCoreEditor
{
    internal sealed class TableEditorReferenceItem
    {
        public int Uid;
        public string StringId;
        public string DisplayName;
    }

    internal static class TableEditorReferenceCache
    {
        private sealed class ReferenceCacheEntry
        {
            public readonly HashSet<int> Uids = new HashSet<int>();
            public readonly List<TableEditorReferenceItem> Items = new List<TableEditorReferenceItem>();
            public readonly Dictionary<int, TableEditorReferenceItem> ItemsByUid = new Dictionary<int, TableEditorReferenceItem>();
            public readonly Dictionary<string, TableEditorReferenceItem> ItemsByStringId = new Dictionary<string, TableEditorReferenceItem>(StringComparer.OrdinalIgnoreCase);
        }

        private static readonly Dictionary<string, ReferenceCacheEntry> EntriesByTableKey = new Dictionary<string, ReferenceCacheEntry>(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// 지정 테이블의 참조 캐시를 제거합니다.
        /// 테이블 저장, 강제 리로드처럼 디스크 기준 참조 데이터가 바뀔 수 있는 시점에 호출합니다.
        /// </summary>
        /// <param name="definition">캐시를 제거할 테이블 정의입니다.</param>
        public static void Invalidate(TableEditorTableDefinition definition)
        {
            if (definition == null)
                return;

            EntriesByTableKey.Remove(definition.TableKey);
        }

        /// <summary>
        /// 모든 참조 캐시를 제거합니다.
        /// 패키지 테이블 전체를 다시 읽어야 하는 큰 변경 시점에 사용합니다.
        /// </summary>
        public static void InvalidateAll()
        {
            EntriesByTableKey.Clear();
        }

        public static bool Contains(TableEditorTableDefinition definition, int uid)
        {
            if (definition == null)
                return false;

            ReferenceCacheEntry entry = EnsureLoaded(definition);
            return entry != null && entry.Uids.Contains(uid);
        }

        public static bool Contains(TableEditorTableDefinition definition, string stringId)
        {
            return FindItem(definition, stringId) != null;
        }

        /// <summary>
        /// Uid 기준으로 참조 항목을 조회합니다.
        /// 기존 리스트 순회 대신 Dictionary 조회를 사용하여 그리드 셀 표시 비용을 줄입니다.
        /// </summary>
        /// <param name="definition">참조 대상 테이블 정의입니다.</param>
        /// <param name="uid">조회할 Uid입니다.</param>
        /// <returns>참조 항목입니다. 없으면 null입니다.</returns>
        public static TableEditorReferenceItem FindItem(TableEditorTableDefinition definition, int uid)
        {
            if (definition == null || uid <= 0)
                return null;

            ReferenceCacheEntry entry = EnsureLoaded(definition);
            if (entry == null)
                return null;

            return entry.ItemsByUid.TryGetValue(uid, out TableEditorReferenceItem item) ? item : null;
        }

        /// <summary>
        /// 문자열 ID 기준으로 참조 항목을 조회합니다.
        /// StringComparer.OrdinalIgnoreCase Dictionary를 사용하여 대소문자 차이를 허용하면서 선형 검색을 피합니다.
        /// </summary>
        /// <param name="definition">참조 대상 테이블 정의입니다.</param>
        /// <param name="stringId">조회할 문자열 ID입니다.</param>
        /// <returns>참조 항목입니다. 없으면 null입니다.</returns>
        public static TableEditorReferenceItem FindItem(TableEditorTableDefinition definition, string stringId)
        {
            if (definition == null || string.IsNullOrWhiteSpace(stringId))
                return null;

            ReferenceCacheEntry entry = EnsureLoaded(definition);
            if (entry == null)
                return null;

            return entry.ItemsByStringId.TryGetValue(stringId, out TableEditorReferenceItem item) ? item : null;
        }

        public static IReadOnlyList<TableEditorReferenceItem> GetItems(TableEditorTableDefinition definition)
        {
            if (definition == null)
                return Array.Empty<TableEditorReferenceItem>();

            ReferenceCacheEntry entry = EnsureLoaded(definition);
            return entry != null ? entry.Items : (IReadOnlyList<TableEditorReferenceItem>)Array.Empty<TableEditorReferenceItem>();
        }

        /// <summary>
        /// 참조 테이블을 필요할 때 한 번만 로드하고, 정렬 리스트와 빠른 조회용 Dictionary를 함께 구성합니다.
        /// </summary>
        /// <param name="definition">로드할 참조 테이블 정의입니다.</param>
        /// <returns>구성된 캐시 엔트리입니다.</returns>
        private static ReferenceCacheEntry EnsureLoaded(TableEditorTableDefinition definition)
        {
            if (definition == null)
                return null;

            if (EntriesByTableKey.TryGetValue(definition.TableKey, out ReferenceCacheEntry cachedEntry))
                return cachedEntry;

            ReferenceCacheEntry entry = new ReferenceCacheEntry();

            try
            {
                object instance = Activator.CreateInstance(definition.TableType);
                if (instance is ITableParser tableParser)
                {
                    string content = AssetDatabaseLoaderManager.LoadFileText(definition.AssetPath);
                    tableParser.LoadData(content);

                    MethodInfo getAllMethod = definition.TableType.GetMethod("GetAll", BindingFlags.Instance | BindingFlags.Public);
                    object dictionaryObj = getAllMethod != null ? getAllMethod.Invoke(instance, null) : null;
                    if (dictionaryObj is System.Collections.IEnumerable enumerable)
                    {
                        foreach (object pair in enumerable)
                        {
                            Type pairType = pair.GetType();
                            object keyObj = pairType.GetProperty("Key")?.GetValue(pair);
                            object valueObj = pairType.GetProperty("Value")?.GetValue(pair);
                            if (keyObj == null || valueObj == null)
                                continue;

                            int uid = Convert.ToInt32(keyObj);
                            TableEditorReferenceItem item = new TableEditorReferenceItem
                            {
                                Uid = uid,
                                StringId = TableEditorReflectionUtility.TryGetMemberValue(valueObj, valueObj.GetType(), "ID")?.ToString() ?? TableEditorReflectionUtility.TryGetMemberValue(valueObj, valueObj.GetType(), "Id")?.ToString() ?? string.Empty,
                                DisplayName = TableEditorReflectionUtility.GetDisplayName(valueObj, entry.Items.Count),
                            };

                            AddItem(entry, item);
                        }
                    }
                }
            }
            catch
            {
                // keep empty cache on failure
            }

            entry.Items.Sort(static (a, b) =>
            {
                string left = string.IsNullOrWhiteSpace(a.StringId) ? a.DisplayName : $"{a.StringId} {a.DisplayName}";
                string right = string.IsNullOrWhiteSpace(b.StringId) ? b.DisplayName : $"{b.StringId} {b.DisplayName}";
                return string.Compare(left, right, StringComparison.OrdinalIgnoreCase);
            });

            EntriesByTableKey[definition.TableKey] = entry;
            return entry;
        }

        /// <summary>
        /// 참조 항목을 리스트와 조회용 인덱스에 동시에 추가합니다.
        /// 같은 Uid 또는 StringId가 중복될 경우 마지막 항목을 조회 결과로 사용합니다.
        /// </summary>
        /// <param name="entry">항목을 추가할 캐시 엔트리입니다.</param>
        /// <param name="item">추가할 참조 항목입니다.</param>
        private static void AddItem(ReferenceCacheEntry entry, TableEditorReferenceItem item)
        {
            if (entry == null || item == null)
                return;

            entry.Uids.Add(item.Uid);
            entry.Items.Add(item);
            if (item.Uid > 0)
                entry.ItemsByUid[item.Uid] = item;
            if (!string.IsNullOrWhiteSpace(item.StringId))
                entry.ItemsByStringId[item.StringId] = item;
        }
    }
}
