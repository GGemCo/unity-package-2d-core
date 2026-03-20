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
                return fallbackIndex.ToString();

            Type rowType = row.GetType();

            // IUidName 우선 처리
            if (row is IUidName uidName)
            {
                string name = uidName.Name;

                if (!string.IsNullOrWhiteSpace(name) && uidName.Uid.ToString() != name)
                    return name;

                return uidName.Uid.ToString();
            }

            object uidValue = TryGetMemberValue(row, rowType, "Uid");
            string uidText = uidValue?.ToString();

            // 공백까지 고려한 fallback 체인
            string nameText =
                GetValidString(row, rowType, "Name") ??
                GetValidString(row, rowType, "ID") ??
                GetValidString(row, rowType, "Id") ??
                GetValidString(row, rowType, "Memo");

            if (!string.IsNullOrWhiteSpace(nameText))
                return nameText;

            if (!string.IsNullOrWhiteSpace(uidText))
                return uidText;

            return fallbackIndex.ToString();
        }

        private static string GetValidString(object row, Type rowType, string memberName)
        {
            object value = TryGetMemberValue(row, rowType, memberName);
            if (value == null)
                return null;

            string text = value.ToString();
            return string.IsNullOrWhiteSpace(text) ? null : text;
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
