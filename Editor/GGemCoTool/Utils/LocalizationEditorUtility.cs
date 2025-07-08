#if UNITY_EDITOR
using System.Linq;
using UnityEditor.Localization;
using UnityEngine.Localization.Tables;

namespace GGemCo2DCoreEditor
{
    public static class LocalizationEditorUtility
    {
        /// <summary>
        /// GUID로 저장된 EntryReference로부터 KeyName을 추출합니다.
        /// </summary>
        /// <param name="tableReference">테이블 이름 또는 GUID</param>
        /// <param name="keyId">엔트리 GUID</param>
        /// <returns>Key 이름 문자열. 실패 시 null 반환</returns>
        public static string GetEntryKeyName(TableReference tableReference, string keyId)
        {
            // GUID로 된 엔트리인 경우
            var collection = LocalizationEditorSettings.GetStringTableCollection(tableReference);
            if (collection == null)
                return null;

            var sharedData = collection.SharedData;
            var entry = sharedData.Entries.FirstOrDefault(e => e.Id == long.Parse(keyId));

            return entry?.Key;
        }
    }
}
#endif