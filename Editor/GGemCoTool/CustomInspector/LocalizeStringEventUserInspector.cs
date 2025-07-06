/*
#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Components;
using UnityEngine.Localization.Settings;
using UnityEngine.Localization.Tables;

namespace GGemCo2DCoreEditor
{
    [CustomEditor(typeof(LocalizeStringEvent))]
    public class LocalizeStringEventUserInspector : UnityEditor.Editor
    {
        private string _prevTable;
        private string _prevKey;
        private string _prevKeyId;
        private bool _initialized;

        private void OnEnable()
        {
            var component = (LocalizeStringEvent)target;
            _prevTable = component.StringReference.TableReference.TableCollectionName;
            _prevKey = component.StringReference.TableEntryReference.Key;
            _prevKeyId = component.StringReference.TableEntryReference.KeyId.ToString();
            if (_prevKeyId == "0") _prevKeyId = null;
            _initialized = false;
            Debug.Log($"_prevTable : {_prevTable}");
            Debug.Log($"_prevEntry Key: {_prevKey}");
            Debug.Log($"_prevEntry KeyId: {_prevKeyId}");
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            var component = (LocalizeStringEvent)target;
            string currentTable = component.StringReference.TableReference.TableCollectionName;
            string currentKey = component.StringReference.TableEntryReference.Key;
            string currentKeyId = component.StringReference.TableEntryReference.KeyId.ToString();
            if (currentKeyId == "0") currentKeyId = null;

            Debug.Log($"현재 설정된 currentTable: {currentTable}");
            Debug.Log($"현재 설정된 Entry Key: {currentKey}");
            Debug.Log($"현재 설정된 Entry KeyId: {currentKeyId}");

            if (string.IsNullOrEmpty(currentTable))
            {
                Debug.LogError("table empty");
                return;
            }
            
            // 변경되었거나 초기화되지 않은 경우 한 번만 실행
            // if (!_initialized || !currentTable.Equals(_prevTable) || !currentEntry.Equals(_prevEntry))
            if (!_initialized || 
                !currentTable.Equals(_prevTable) ||
                (!string.IsNullOrEmpty(currentKey) && !currentKey.Equals(_prevKey)) ||
                (!string.IsNullOrEmpty(currentKeyId) && !currentKeyId.Equals(_prevKeyId)))
            {
                string userTable = currentTable;
                if (!userTable.EndsWith("_User"))
                {
                    userTable = $"{userTable}_User";
                }
                Debug.Log($"현재 설정된 userTable: {userTable}");

                if (string.IsNullOrEmpty(currentKey) && !string.IsNullOrEmpty(currentKeyId))
                {
                    currentKey = LocalizationEditorUtility.GetEntryKeyName(currentTable, currentKeyId);
                }

                string valueUser =
                    LocalizationSettings.StringDatabase.GetLocalizedString(userTable, currentKey,
                        LocalizationSettings.SelectedLocale);
                string value = LocalizationSettings.StringDatabase.GetLocalizedString(currentTable, currentKey,
                    LocalizationSettings.SelectedLocale);

                if (!string.IsNullOrEmpty(valueUser))
                {
                    component.StringReference = new LocalizedString
                    {
                        TableReference = userTable,
                        TableEntryReference = currentKey
                    };
                }
                else
                {
                    if (currentTable.EndsWith("_User"))
                    {
                        currentTable = currentTable.Replace("_User", "");
                    }
                    component.StringReference = new LocalizedString
                    {
                        TableReference = currentTable,
                        TableEntryReference = currentKey
                    };
                }

                _initialized = true;
                _prevTable = currentTable;
                _prevKey = currentKey;
                _prevKeyId = currentKeyId;

                // 변경 사항 반영
                EditorUtility.SetDirty(component);
            }
        }
    }
}
#endif
*/