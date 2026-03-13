using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace GGemCo2DCoreEditor
{
    /// <summary>
    /// 테이블 Row 편집용 공용 IMGUI 유틸리티.
    /// - StruckTable 계열 public 멤버를 리플렉션으로 읽어 자동으로 필드를 그립니다.
    /// - 멤버 선언 순서를 기본 표시 순서로 사용합니다.
    /// - 필드 라벨/읽기 전용/그룹은 옵션 또는 Attribute로 제어할 수 있습니다.
    /// - clone/copy 도 함께 제공하여 개별 EditorWindow 의 중복 코드를 줄입니다.
    /// </summary>
    public static class TableRowEditorUtility
    {
        [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
        public sealed class TableRowIgnoreAttribute : Attribute
        {
        }

        [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
        public sealed class TableRowEditorAttribute : Attribute
        {
            public string Label { get; set; }
            public string Group { get; set; }
            public bool ReadOnly { get; set; }
        }

        public sealed class TableRowEditorField
        {
            public string MemberName { get; }
            public string Label { get; }
            public string Group { get; }
            public bool ReadOnly { get; }

            public TableRowEditorField(string memberName, string label = null, string group = null, bool readOnly = false)
            {
                MemberName = memberName;
                Label = string.IsNullOrWhiteSpace(label) ? memberName : label;
                Group = group;
                ReadOnly = readOnly;
            }
        }

        public sealed class TableRowEditorBuildOptions
        {
            public HashSet<string> ReadOnlyMembers { get; } = new(StringComparer.Ordinal);
            public Dictionary<string, string> GroupByMemberName { get; } = new(StringComparer.Ordinal);
            public Dictionary<string, string> LabelByMemberName { get; } = new(StringComparer.Ordinal);
            public bool AutoGroupBooleanFieldsToFlags { get; set; } = true;
        }

        public sealed class DrawResult
        {
            public bool Changed { get; internal set; }
        }

        private sealed class MemberAccessor
        {
            public string Name { get; }
            public Type MemberType { get; }
            public int Order { get; }
            private readonly Func<object, object> _getter;
            private readonly Action<object, object> _setter;

            public MemberAccessor(string name, Type memberType, int order, Func<object, object> getter, Action<object, object> setter)
            {
                Name = name;
                MemberType = memberType;
                Order = order;
                _getter = getter;
                _setter = setter;
            }

            public object GetValue(object target) => _getter(target);
            public void SetValue(object target, object value) => _setter(target, value);
        }

        public static TableRowEditorField[] BuildFields<T>(TableRowEditorBuildOptions options = null)
        {
            options ??= new TableRowEditorBuildOptions();

            var fields = new List<TableRowEditorField>();
            foreach (var accessor in GetAllWritableMembers(typeof(T)))
            {
                string memberName = accessor.Name;
                if (string.IsNullOrWhiteSpace(memberName))
                    continue;

                var memberInfo = FindMemberInfo(typeof(T), memberName);
                var attribute = memberInfo?.GetCustomAttribute<TableRowEditorAttribute>();

                string label = null;
                if (!options.LabelByMemberName.TryGetValue(memberName, out label))
                    label = attribute?.Label;

                string group = null;
                if (!options.GroupByMemberName.TryGetValue(memberName, out group))
                    group = attribute?.Group;

                if (string.IsNullOrWhiteSpace(group) &&
                    options.AutoGroupBooleanFieldsToFlags &&
                    accessor.MemberType == typeof(bool))
                {
                    group = "Flags";
                }

                bool readOnly = options.ReadOnlyMembers.Contains(memberName) || (attribute?.ReadOnly ?? false);

                fields.Add(new TableRowEditorField(memberName, label, group, readOnly));
            }

            return fields.ToArray();
        }

        public static T CloneShallow<T>(T source) where T : class, new()
        {
            if (source == null)
                return null;

            var clone = new T();
            CopyMembers(source, clone);
            return clone;
        }

        public static void CopyMembers<T>(T source, T destination, IReadOnlyList<TableRowEditorField> fields = null)
        {
            if (source == null || destination == null)
                return;

            if (fields == null || fields.Count == 0)
            {
                foreach (var accessor in GetAllWritableMembers(typeof(T)))
                {
                    accessor.SetValue(destination, accessor.GetValue(source));
                }

                return;
            }

            for (int i = 0; i < fields.Count; i++)
            {
                var field = fields[i];
                if (field == null || string.IsNullOrWhiteSpace(field.MemberName))
                    continue;

                var accessor = FindWritableMember(typeof(T), field.MemberName);
                if (accessor == null)
                    continue;

                accessor.SetValue(destination, accessor.GetValue(source));
            }
        }

        public static DrawResult DrawObjectEditor(
            object target,
            IReadOnlyList<TableRowEditorField> fields,
            Action<object, string> normalizeAfterField = null)
        {
            var result = new DrawResult();

            if (target == null)
                return result;

            if (fields == null || fields.Count == 0)
            {
                EditorGUILayout.HelpBox("표시할 필드 정의가 없습니다.", MessageType.Info);
                return result;
            }

            string currentGroup = null;

            for (int i = 0; i < fields.Count; i++)
            {
                var field = fields[i];
                if (field == null || string.IsNullOrWhiteSpace(field.MemberName))
                    continue;

                if (!string.Equals(currentGroup, field.Group, StringComparison.Ordinal))
                {
                    currentGroup = field.Group;
                    if (!string.IsNullOrWhiteSpace(currentGroup))
                    {
                        if (i > 0)
                            EditorGUILayout.Space(6);

                        EditorGUILayout.LabelField(currentGroup, EditorStyles.miniBoldLabel);
                    }
                }

                var accessor = FindWritableMember(target.GetType(), field.MemberName);
                if (accessor == null)
                {
                    EditorGUILayout.HelpBox($"멤버를 찾을 수 없습니다: {field.MemberName}", MessageType.Warning);
                    continue;
                }

                using (new EditorGUI.DisabledScope(field.ReadOnly))
                {
                    EditorGUI.BeginChangeCheck();
                    object currentValue = accessor.GetValue(target);
                    object newValue = DrawValueField(field.Label, accessor.MemberType, currentValue);
                    if (EditorGUI.EndChangeCheck())
                    {
                        accessor.SetValue(target, newValue);
                        normalizeAfterField?.Invoke(target, field.MemberName);
                        result.Changed = true;
                    }
                }
            }

            return result;
        }

        private static object DrawValueField(string label, Type memberType, object value)
        {
            var content = new GUIContent(label);

            if (memberType == typeof(string))
                return EditorGUILayout.TextField(content, value as string ?? string.Empty);

            if (memberType == typeof(int))
                return EditorGUILayout.IntField(content, value != null ? (int)value : 0);

            if (memberType == typeof(long))
                return EditorGUILayout.LongField(content, value != null ? (long)value : 0L);

            if (memberType == typeof(float))
                return EditorGUILayout.FloatField(content, value != null ? (float)value : 0f);

            if (memberType == typeof(bool))
                return EditorGUILayout.ToggleLeft(label, value != null && (bool)value);

            if (memberType == typeof(Vector2))
                return EditorGUILayout.Vector2Field(label, value != null ? (Vector2)value : Vector2.zero);

            if (memberType == typeof(Vector3))
                return EditorGUILayout.Vector3Field(content, value != null ? (Vector3)value : Vector3.zero);

            if (memberType.IsEnum)
            {
                var enumValue = value as Enum;
                if (enumValue == null)
                {
                    Array values = Enum.GetValues(memberType);
                    enumValue = values.Length > 0 ? (Enum)values.GetValue(0) : null;
                }

                return enumValue != null
                    ? EditorGUILayout.EnumPopup(content, enumValue)
                    : value;
            }

            EditorGUILayout.LabelField(label, $"지원하지 않는 타입: {memberType.Name}");
            return value;
        }

        private static IEnumerable<MemberAccessor> GetAllWritableMembers(Type type)
        {
            const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public;

            var accessors = new List<MemberAccessor>();

            foreach (var field in type.GetFields(flags))
            {
                if (field.IsInitOnly || field.IsLiteral || field.IsStatic)
                    continue;

                if (field.GetCustomAttribute<TableRowIgnoreAttribute>() != null)
                    continue;

                accessors.Add(new MemberAccessor(
                    field.Name,
                    field.FieldType,
                    field.MetadataToken,
                    target => field.GetValue(target),
                    (target, value) => field.SetValue(target, value)));
            }

            foreach (var property in type.GetProperties(flags))
            {
                if (!property.CanRead || !property.CanWrite)
                    continue;

                if (property.GetMethod == null || property.SetMethod == null)
                    continue;

                if (property.GetMethod.IsStatic || property.SetMethod.IsStatic)
                    continue;

                if (property.GetIndexParameters().Length > 0)
                    continue;

                if (property.GetCustomAttribute<TableRowIgnoreAttribute>() != null)
                    continue;

                accessors.Add(new MemberAccessor(
                    property.Name,
                    property.PropertyType,
                    property.MetadataToken,
                    target => property.GetValue(target, null),
                    (target, value) => property.SetValue(target, value, null)));
            }

            return accessors.OrderBy(x => x.Order);
        }

        private static MemberAccessor FindWritableMember(Type type, string memberName)
        {
            const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public;

            var field = type.GetField(memberName, flags);
            if (field != null && !field.IsInitOnly && !field.IsLiteral && !field.IsStatic)
            {
                if (field.GetCustomAttribute<TableRowIgnoreAttribute>() == null)
                {
                    return new MemberAccessor(
                        field.Name,
                        field.FieldType,
                        field.MetadataToken,
                        target => field.GetValue(target),
                        (target, value) => field.SetValue(target, value));
                }
            }

            var property = type.GetProperty(memberName, flags);
            if (property != null &&
                property.CanRead &&
                property.CanWrite &&
                property.GetMethod != null &&
                property.SetMethod != null &&
                !property.GetMethod.IsStatic &&
                !property.SetMethod.IsStatic &&
                property.GetIndexParameters().Length == 0 &&
                property.GetCustomAttribute<TableRowIgnoreAttribute>() == null)
            {
                return new MemberAccessor(
                    property.Name,
                    property.PropertyType,
                    property.MetadataToken,
                    target => property.GetValue(target, null),
                    (target, value) => property.SetValue(target, value, null));
            }

            return null;
        }

        private static MemberInfo FindMemberInfo(Type type, string memberName)
        {
            const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public;

            var field = type.GetField(memberName, flags);
            if (field != null)
                return field;

            return type.GetProperty(memberName, flags);
        }
    }
}
