using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Localization;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Localization.Components;
using UnityEngine.Localization.Settings;
using UnityEngine.SceneManagement;

namespace GGemCo2DCoreEditor
{
    public class LocalizationUsageFinder : EditorWindow
    {
        private const string Title = "Localization Key 사용처 검색기";
        private string _tableName = "GGemCo_Scene";
        private string _keyName = "Intro_Button_NewGame";
        private string _searchText = "";
        private bool _toggleUserTable = true;

        private Dictionary<string, string> _matchedKeys = new();

        [MenuItem(ConfigEditor.NameToolLocalizationFind, false, (int)ConfigEditor.ToolOrdering.LocalizationFind)]
        public static void ShowWindow()
        {
            GetWindow<LocalizationUsageFinder>(Title);
        }

        private void OnGUI()
        {
            Common.OnGUITitle("Table, Key 이름으로 검색");
            _tableName = EditorGUILayout.TextField("Table Name", _tableName);
            _keyName = EditorGUILayout.TextField("Key Name", _keyName);
            _toggleUserTable = EditorGUILayout.ToggleLeft("유저 언어 테이블도 같이 검색", _toggleUserTable);

            if (GUILayout.Button("현재 씬에서 Key로 찾기"))
                FindInOpenScenes(_tableName, _keyName);

            if (GUILayout.Button("모든 Prefab에서 Key로 찾기"))
                FindInAllPrefabs(_tableName, _keyName);

            if (GUILayout.Button("모든 Scene에서 Key로 찾기 (느릴 수 있음)"))
                FindInAllScenes(_tableName, _keyName);

            Common.GUILine();
            Common.OnGUITitle("문자열로 Key 찾기");
            EditorGUILayout.HelpBox("대소문자를 구분합니다.", MessageType.Info);
            _searchText = EditorGUILayout.TextField("검색할 문자열", _searchText);

            if (GUILayout.Button("Key 검색"))
            {
                _matchedKeys = FindKeysByLocalizedString(_searchText);
                Debug.Log($"🔍 {_matchedKeys.Count}개의 키가 검색되었습니다.");
            }

            if (_matchedKeys.Count > 0)
            {
                if (GUILayout.Button("모든 Prefab에서 검색된 Key들 찾기"))
                {
                    foreach (var kvp in _matchedKeys)
                        FindInAllPrefabs(kvp.Key, kvp.Value);
                }

                if (GUILayout.Button("모든 Scene에서 검색된 Key들 찾기"))
                {
                    foreach (var kvp in _matchedKeys)
                        FindInAllScenes(kvp.Key, kvp.Value);
                }
            }
        }

        private Dictionary<string, string> FindKeysByLocalizedString(string searchText)
        {
            var result = new Dictionary<string, string>();

            var tableGuids = AssetDatabase.FindAssets("t:StringTableCollection");
            foreach (var guid in tableGuids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var collection = AssetDatabase.LoadAssetAtPath<StringTableCollection>(path);
                if (collection == null) continue;

                foreach (var table in collection.StringTables)
                {
                    foreach (var entry in table.Values)
                    {
                        if (!entry.LocalizedValue.Contains(searchText)) continue;
                        result[collection.TableCollectionName] = entry.Key;
                        Debug.Log($"✅ 일치 항목: {collection.TableCollectionName} / {entry.Key} / {entry.LocalizedValue}");
                    }
                }
            }

            return result;
        }

        private bool IsMatching(LocalizeStringEvent evt, string tableName, string keyName)
        {
            if (evt == null) return false;

            string currentTableName = evt.StringReference.TableReference.TableCollectionName;
            if (string.IsNullOrEmpty(currentTableName)) return false;
            
            var tableEntryResult = LocalizationSettings.StringDatabase.GetTableEntry(currentTableName,
                evt.StringReference.TableEntryReference);
            if (tableEntryResult.Entry == null) return false;

            return (currentTableName == tableName || (_toggleUserTable && currentTableName == $"{tableName}_User")) &&
                   tableEntryResult.Entry.Key == keyName;
        }

        private void FindInOpenScenes(string tableName, string keyName)
        {
            int total = 0;

#if UNITY_6000_0_OR_NEWER
            var events = GameObject.FindObjectsByType<LocalizeStringEvent>(FindObjectsSortMode.None);
#else
            var events = GameObject.FindObjectsOfType<LocalizeStringEvent>(true);
#endif
            foreach (var evt in events)
            {
                if (!IsMatching(evt, tableName, keyName)) continue;
                Debug.Log($"[현재 씬] {evt.gameObject.name} (Scene: {evt.gameObject.scene.name})", evt.gameObject);
                total++;
            }

            Debug.Log($"✅ 현재 씬에서 찾은 오브젝트 수: {total}");
        }

        private void FindInAllPrefabs(string tableName, string keyName)
        {
            int total = 0;
            string[] guids = AssetDatabase.FindAssets("t:Prefab");

            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefab == null) continue;

                var events = prefab.GetComponentsInChildren<LocalizeStringEvent>(true);
                foreach (var evt in events)
                {
                    if (!IsMatching(evt, tableName, keyName)) continue;
                    Debug.Log($"[프리팹] {prefab.name} at {path}", prefab);
                    total++;
                }
            }

            Debug.Log($"✅ 프리팹에서 찾은 오브젝트 수: {total}");
        }

        private void FindInAllScenes(string tableName, string keyName)
        {
            int total = 0;
            string currentScene = SceneManager.GetActiveScene().path;
            var scenes = EditorBuildSettings.scenes.Where(s => s.enabled).ToList();

            foreach (var t in scenes)
            {
                string path = t.path;
                Scene scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);

                foreach (var root in scene.GetRootGameObjects())
                {
                    var events = root.GetComponentsInChildren<LocalizeStringEvent>(true);
                    foreach (var evt in events)
                    {
                        if (!IsMatching(evt, tableName, keyName)) continue;
                        Debug.Log($"[씬] {evt.gameObject.name} in Scene: {path}", evt.gameObject);
                        total++;
                    }
                }
            }

            EditorSceneManager.OpenScene(currentScene, OpenSceneMode.Single);
            Debug.Log($"✅ 전체 씬에서 찾은 오브젝트 수: {total}");
        }
    }
}
