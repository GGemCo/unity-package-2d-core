#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEditor.Localization;
using UnityEngine.Localization;
using UnityEngine.Localization.Tables;

namespace GGemCo2DCoreEditor
{
    public static class HelperLocalization
    {
        public static StringTableCollection EnsureStringTableCollection(string name, string outputPath)
        {
            var collection = LocalizationEditorSettings.GetStringTableCollection(name);
            if (collection != null) return collection;

            // Editor 전용: StringTableCollection 생성
            collection = LocalizationEditorSettings.CreateStringTableCollection(name, outputPath);
            if (collection == null)
                throw new InvalidOperationException($"StringTableCollection 생성 실패: {name}");

            return collection;
        }

        public static StringTable EnsureLocaleTable(StringTableCollection collection, Locale locale)
        {
            var table = collection.GetTable(locale.Identifier) as StringTable;
            if (table != null) return table;

            // Editor 전용: 로케일별 테이블 추가
            collection.AddNewTable(locale.Identifier);
            table = collection.GetTable(locale.Identifier) as StringTable;
            if (table == null)
                throw new InvalidOperationException($"로케일 테이블 생성 실패: {locale.Identifier}");

            EditorUtility.SetDirty(collection);
            return table;
        }
    }
}
#endif