using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using GGemCo2DCore;

namespace GGemCo2DCoreEditor
{
    internal static class TableEditorReflectionUtility
    {
        public static IEnumerable<MemberInfo> GetEditableMembers(Type rowType)
        {
            if (rowType == null)
                yield break;

            const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public;

            foreach (FieldInfo field in rowType.GetFields(flags).OrderBy(static f => f.MetadataToken))
            {
                if (field.IsInitOnly)
                    continue;

                yield return field;
            }

            foreach (PropertyInfo property in rowType.GetProperties(flags).OrderBy(static p => p.MetadataToken))
            {
                if (!property.CanRead || !property.CanWrite)
                    continue;

                if (property.GetIndexParameters().Length > 0)
                    continue;

                yield return property;
            }
        }

        public static Type GetMemberType(MemberInfo member)
        {
            if (member is FieldInfo field)
                return field.FieldType;

            if (member is PropertyInfo property)
                return property.PropertyType;

            return typeof(string);
        }

        public static object GetValue(object target, MemberInfo member)
        {
            if (target == null || member == null)
                return null;

            if (member is FieldInfo field)
                return field.GetValue(target);

            if (member is PropertyInfo property)
                return property.GetValue(target);

            return null;
        }

        public static void SetValue(object target, MemberInfo member, object value)
        {
            if (target == null || member == null)
                return;

            if (member is FieldInfo field)
            {
                field.SetValue(target, value);
                return;
            }

            if (member is PropertyInfo property)
            {
                property.SetValue(target, value);
            }
        }

        public static string GetDisplayName(object row, int fallbackIndex)
        {
            if (row == null)
                return $"Row {fallbackIndex + 1}";

            if (row is IUidName uidName)
            {
                if (!string.IsNullOrWhiteSpace(uidName.Name))
                    return $"{uidName.Uid} - {uidName.Name}";

                return uidName.Uid.ToString();
            }

            Type rowType = row.GetType();
            object uidValue = TryGetMemberValue(row, rowType, "Uid");
            object nameValue = TryGetMemberValue(row, rowType, "Name") ?? TryGetMemberValue(row, rowType, "ID") ?? TryGetMemberValue(row, rowType, "Id") ?? TryGetMemberValue(row, rowType, "Memo");

            string uidText = uidValue != null ? uidValue.ToString() : string.Empty;
            string nameText = nameValue != null ? nameValue.ToString() : string.Empty;

            if (!string.IsNullOrWhiteSpace(uidText) && !string.IsNullOrWhiteSpace(nameText))
                return $"{uidText} - {nameText}";

            if (!string.IsNullOrWhiteSpace(uidText))
                return uidText;

            if (!string.IsNullOrWhiteSpace(nameText))
                return nameText;

            return $"Row {fallbackIndex + 1}";
        }

        public static object TryGetMemberValue(object target, Type type, string memberName)
        {
            if (target == null || type == null || string.IsNullOrWhiteSpace(memberName))
                return null;

            const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.IgnoreCase;
            FieldInfo field = type.GetField(memberName, flags);
            if (field != null)
                return field.GetValue(target);

            PropertyInfo property = type.GetProperty(memberName, flags);
            if (property != null && property.CanRead)
                return property.GetValue(target);

            return null;
        }
    }
}
