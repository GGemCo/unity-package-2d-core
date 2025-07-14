using System.Linq;
using UnityEditor;
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

        [MenuItem(ConfigEditor.NameToolLocalizationFind, false, (int)ConfigEditor.ToolOrdering.LocalizationFind)]
        public static void ShowWindow()
        {
            GetWindow<LocalizationUsageFinder>(Title);
        }

        private void OnGUI()
        {
            _tableName = EditorGUILayout.TextField("Table Name", _tableName);
            _keyName = EditorGUILayout.TextField("Key Name", _keyName);

            if (GUILayout.Button("현재 씬에서 찾기"))
            {
                FindInOpenScenes();
            }

            if (GUILayout.Button("모든 Prefab에서 찾기"))
            {
                FindInAllPrefabs();
            }

            if (GUILayout.Button("모든 Scene에서 찾기 (느릴 수 있음)"))
            {
                FindInAllScenes();
            }
        }

        private bool IsMatching(LocalizeStringEvent evt)
        {
            if (evt == null) return false;

            string tableName = evt.StringReference.TableReference.TableCollectionName;
            if (string.IsNullOrEmpty(tableName)) return false;
            
            var tableEntryResult = LocalizationSettings.StringDatabase.GetTableEntry(tableName,
                evt.StringReference.TableEntryReference);
            if (tableEntryResult.Entry == null) return false;
            return tableName == _tableName && tableEntryResult.Entry.Key == _keyName;
        }

        private void FindInOpenScenes()
        {
            int total = 0;

#if UNITY_6000_0_OR_NEWER
            foreach (var evt in GameObject.FindObjectsByType<LocalizeStringEvent>(FindObjectsSortMode.None))
#else
            foreach (var evt in GameObject.FindObjectsOfType<LocalizeStringEvent>(true))
#endif
            {
                if (IsMatching(evt))
                {
                    Debug.Log($"[현재 씬] {evt.gameObject.name} (Scene: {evt.gameObject.scene.name})", evt.gameObject);
                    total++;
                }
            }

            Debug.Log($"✅ 현재 씬에서 찾은 오브젝트 수: {total}");
        }

        private void FindInAllPrefabs()
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
                    if (IsMatching(evt))
                    {
                        Debug.Log($"[프리팹] {prefab.name} at {path}", prefab);
                        total++;
                    }
                }
            }

            Debug.Log($"✅ 프리팹에서 찾은 오브젝트 수: {total}");
        }

        private void FindInAllScenes()
        {
            // string[] sceneGuids = AssetDatabase.FindAssets("t:Scene");
            int total = 0;
            string currentScene = SceneManager.GetActiveScene().path;

            var scenes = EditorBuildSettings.scenes.Where(s => s.enabled).ToList();
            for (int i = 0; i < scenes.Count; i++)
            {
                string path = scenes[i].path;

                // if (path == currentScene)
                //     continue; // 현재 열려있는 씬은 중복 방지

                Scene scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
                var rootObjects = scene.GetRootGameObjects();

                foreach (var root in rootObjects)
                {
                    var events = root.GetComponentsInChildren<LocalizeStringEvent>(true);
                    foreach (var evt in events)
                    {
                        if (IsMatching(evt))
                        {
                            Debug.Log($"[씬] {evt.gameObject.name} in Scene: {path}", evt.gameObject);
                            total++;
                        }
                    }
                }
            }

            EditorSceneManager.OpenScene(currentScene, OpenSceneMode.Single); // 원래 씬 복원
            Debug.Log($"✅ 전체 씬에서 찾은 오브젝트 수: {total}");
        }
    }
}
