using System;
using System.Collections.Generic;
using System.Reflection;
using GGemCo2DCore;

namespace GGemCo2DCoreEditor
{
    internal sealed class TableEditorReferenceItem
    {
        public int Uid;
        public string DisplayName;
    }

    internal static class TableEditorReferenceCache
    {
        private static readonly Dictionary<string, HashSet<int>> UidsByTableKey = new Dictionary<string, HashSet<int>>(StringComparer.OrdinalIgnoreCase);
        private static readonly Dictionary<string, List<TableEditorReferenceItem>> ItemsByTableKey = new Dictionary<string, List<TableEditorReferenceItem>>(StringComparer.OrdinalIgnoreCase);

        public static void Invalidate(TableEditorTableDefinition definition)
        {
            if (definition == null)
                return;

            UidsByTableKey.Remove(definition.TableKey);
            ItemsByTableKey.Remove(definition.TableKey);
        }

        public static bool Contains(TableEditorTableDefinition definition, int uid)
        {
            if (definition == null)
                return false;

            EnsureLoaded(definition);
            return UidsByTableKey.TryGetValue(definition.TableKey, out HashSet<int> set) && set.Contains(uid);
        }

        public static TableEditorReferenceItem FindItem(TableEditorTableDefinition definition, int uid)
        {
            if (definition == null || uid <= 0)
                return null;

            EnsureLoaded(definition);
            if (!ItemsByTableKey.TryGetValue(definition.TableKey, out List<TableEditorReferenceItem> items))
                return null;

            for (int i = 0; i < items.Count; i++)
            {
                if (items[i].Uid == uid)
                    return items[i];
            }

            return null;
        }

        public static IReadOnlyList<TableEditorReferenceItem> GetItems(TableEditorTableDefinition definition)
        {
            if (definition == null)
                return Array.Empty<TableEditorReferenceItem>();

            EnsureLoaded(definition);
            return ItemsByTableKey.TryGetValue(definition.TableKey, out List<TableEditorReferenceItem> items)
                ? items
                : (IReadOnlyList<TableEditorReferenceItem>)Array.Empty<TableEditorReferenceItem>();
        }

        private static void EnsureLoaded(TableEditorTableDefinition definition)
        {
            if (UidsByTableKey.ContainsKey(definition.TableKey))
                return;

            HashSet<int> uidSet = new HashSet<int>();
            List<TableEditorReferenceItem> items = new List<TableEditorReferenceItem>();

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
                            uidSet.Add(uid);
                            items.Add(new TableEditorReferenceItem
                            {
                                Uid = uid,
                                DisplayName = TableEditorReflectionUtility.GetDisplayName(valueObj, items.Count),
                            });
                        }
                    }
                }
            }
            catch
            {
                // 캐시 실패 시 빈 컬렉션 유지
            }

            items.Sort(static (a, b) => string.Compare(a.DisplayName, b.DisplayName, StringComparison.OrdinalIgnoreCase));
            UidsByTableKey[definition.TableKey] = uidSet;
            ItemsByTableKey[definition.TableKey] = items;
        }
    }
}
