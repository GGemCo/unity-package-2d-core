#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using GGemCo2DCore;
using UnityEditor;
using UnityEngine;

namespace GGemCo2DCoreEditor
{
    /// <summary>
    /// 프로젝트 내 ScriptableObject 에셋 중 <see cref="DebugOptionAttribute"/> 가 지정된 bool 필드를 검색합니다.
    /// </summary>
    public static class DebugOptionAssetScanner
    {
        private const BindingFlags FieldBindingFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        /// <summary>
        /// 현재 프로젝트에서 활성화된 디버그 옵션 목록을 찾습니다.
        /// </summary>
        /// <returns>활성화된 디버그 옵션 메타데이터 목록입니다.</returns>
        public static List<DebugOptionEntry> FindEnabledDebugOptions()
        {
            List<DebugOptionEntry> results = new List<DebugOptionEntry>();

            foreach (string guid in AssetDatabase.FindAssets("t:ScriptableObject"))
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(guid);
                ScriptableObject asset = AssetDatabase.LoadAssetAtPath<ScriptableObject>(assetPath);
                if (asset == null)
                {
                    continue;
                }

                CollectEnabledDebugFields(asset, assetPath, results);
            }

            return results;
        }

        /// <summary>
        /// 현재 프로젝트의 모든 디버그 옵션 필드를 false 로 비활성화합니다.
        /// </summary>
        /// <returns>실제로 변경된 항목 수입니다.</returns>
        public static int DisableAllDebugOptions()
        {
            int changedFieldCount = 0;
            HashSet<UnityEngine.Object> dirtyAssets = new HashSet<UnityEngine.Object>();

            foreach (string guid in AssetDatabase.FindAssets("t:ScriptableObject"))
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(guid);
                ScriptableObject asset = AssetDatabase.LoadAssetAtPath<ScriptableObject>(assetPath);
                if (asset == null)
                {
                    continue;
                }

                if (DisableDebugFields(asset))
                {
                    dirtyAssets.Add(asset);
                }
            }

            foreach (UnityEngine.Object dirtyAsset in dirtyAssets)
            {
                EditorUtility.SetDirty(dirtyAsset);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            foreach (UnityEngine.Object dirtyAsset in dirtyAssets)
            {
                changedFieldCount += CountDebugFields(dirtyAsset);
            }

            return changedFieldCount;
        }

        /// <summary>
        /// 빌드 실패 메시지에 사용할 텍스트를 생성합니다.
        /// </summary>
        /// <param name="entries">활성화된 디버그 옵션 목록입니다.</param>
        /// <returns>줄바꿈이 포함된 메시지 문자열입니다.</returns>
        public static string BuildSummaryMessage(IReadOnlyList<DebugOptionEntry> entries)
        {
            if (entries == null || entries.Count == 0)
            {
                return "활성화된 디버그 옵션이 없습니다.";
            }

            StringBuilder builder = new StringBuilder();
            builder.AppendLine($"활성화된 디버그 옵션 {entries.Count}건을 찾았습니다.");

            foreach (DebugOptionEntry entry in entries.OrderBy(e => e.AssetPath).ThenBy(e => e.FieldName))
            {
                builder.Append("- ")
                    .Append(entry.AssetPath)
                    .Append(" | ")
                    .Append(entry.AssetTypeName)
                    .Append('.')
                    .Append(entry.FieldName);

                if (!string.IsNullOrWhiteSpace(entry.Description))
                {
                    builder.Append(" (").Append(entry.Description).Append(')');
                }

                builder.AppendLine();
            }

            return builder.ToString();
        }

        private static void CollectEnabledDebugFields(ScriptableObject asset, string assetPath, ICollection<DebugOptionEntry> results)
        {
            Type type = asset.GetType();
            FieldInfo[] fields = type.GetFields(FieldBindingFlags);

            foreach (FieldInfo field in fields)
            {
                if (field.FieldType != typeof(bool))
                {
                    continue;
                }

                DebugOptionAttribute attribute = field.GetCustomAttribute<DebugOptionAttribute>();
                if (attribute == null)
                {
                    continue;
                }

                bool value = (bool)field.GetValue(asset);
                if (!value)
                {
                    continue;
                }

                results.Add(new DebugOptionEntry(
                    asset,
                    assetPath,
                    type.Name,
                    field.Name,
                    attribute.Description,
                    value));
            }
        }

        private static bool DisableDebugFields(ScriptableObject asset)
        {
            bool hasChanged = false;
            Type type = asset.GetType();
            FieldInfo[] fields = type.GetFields(FieldBindingFlags);

            foreach (FieldInfo field in fields)
            {
                if (field.FieldType != typeof(bool))
                {
                    continue;
                }

                if (field.GetCustomAttribute<DebugOptionAttribute>() == null)
                {
                    continue;
                }

                bool currentValue = (bool)field.GetValue(asset);
                if (!currentValue)
                {
                    continue;
                }

                field.SetValue(asset, false);
                hasChanged = true;
            }

            return hasChanged;
        }

        private static int CountDebugFields(UnityEngine.Object asset)
        {
            if (asset == null)
            {
                return 0;
            }

            FieldInfo[] fields = asset.GetType().GetFields(FieldBindingFlags);
            int count = 0;
            foreach (FieldInfo field in fields)
            {
                if (field.FieldType == typeof(bool) && field.GetCustomAttribute<DebugOptionAttribute>() != null)
                {
                    count++;
                }
            }

            return count;
        }

        /// <summary>
        /// 디버그 옵션 한 건에 대한 검색 결과입니다.
        /// </summary>
        public readonly struct DebugOptionEntry
        {
            public DebugOptionEntry(UnityEngine.Object asset, string assetPath, string assetTypeName, string fieldName, string description, bool value)
            {
                Asset = asset;
                AssetPath = assetPath;
                AssetTypeName = assetTypeName;
                FieldName = fieldName;
                Description = description;
                Value = value;
            }

            public UnityEngine.Object Asset { get; }
            public string AssetPath { get; }
            public string AssetTypeName { get; }
            public string FieldName { get; }
            public string Description { get; }
            public bool Value { get; }
        }
    }
}
#endif
