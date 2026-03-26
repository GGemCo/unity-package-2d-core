#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using System.Linq;
using GGemCo2DCore;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Components;
using UnityEngine.Localization.Settings;
using SceneManager = UnityEngine.SceneManagement.SceneManager;

namespace GGemCo2DCoreEditor
{
    public class LocalizationUpdateEditor : EditorWindow
    {
        private const string Title = "LocalizeStringEvent 일괄 업데이트 툴";
        private bool _includeScenes = true;
        private bool _includePrefabs = true;
        private Vector2 _scrollPos;
        private string _logOutput = "";

        [MenuItem(ConfigEditor.NameToolLocalizationUpdate, false, (int)ConfigEditor.ToolOrdering.LocalizationUpdate)]
        public static void ShowWindow()
        {
            GetWindow<LocalizationUpdateEditor>(Title);
        }

        private void OnGUI()
        {
            EditorGUILayout.Space();

            _includeScenes = EditorGUILayout.ToggleLeft("Build Scenes 포함", _includeScenes);
            _includePrefabs = EditorGUILayout.ToggleLeft("Project Prefabs 포함", _includePrefabs);

            EditorGUILayout.Space();

            if (GUILayout.Button("업데이트 실행", GUILayout.Height(40)))
            {
                if (!_includeScenes && !_includePrefabs)
                {
                    EditorUtility.DisplayDialog("실행 불가", "최소 하나의 대상(씬 또는 프리팹)을 선택해야 합니다.", "확인");
                }
                else
                {
                    _logOutput = "";
                    RunUpdateProcess();
                }
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("📋 결과 로그", EditorStyles.boldLabel);

            _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos, GUILayout.Height(250));
            EditorGUILayout.TextArea(_logOutput, GUILayout.ExpandHeight(true));
            EditorGUILayout.EndScrollView();
        }

        private void RunUpdateProcess()
        {
            int totalUpdated = 0;
            string currentScene = SceneManager.GetActiveScene().path;

            try
            {
                List<string> logs = new();

                if (_includeScenes)
                {
                    var scenes = EditorBuildSettings.scenes.Where(s => s.enabled).ToList();
                    for (int i = 0; i < scenes.Count; i++)
                    {
                        string path = scenes[i].path;
                        EditorUtility.DisplayProgressBar("🔄 씬 업데이트 중", Path.GetFileName(path), i / (float)(scenes.Count + 1));
                        int count = ProcessScene(path);
                        totalUpdated += count;
                        if (count > 0) logs.Add($"[씬] {Path.GetFileName(path)}: {count}개 업데이트됨");
                    }
                }

                if (_includePrefabs)
                {
                    string[] prefabGuids = AssetDatabase.FindAssets("t:Prefab");
                    for (int i = 0; i < prefabGuids.Length; i++)
                    {
                        string path = AssetDatabase.GUIDToAssetPath(prefabGuids[i]);
                        EditorUtility.DisplayProgressBar("🔄 프리팹 업데이트 중", Path.GetFileName(path), (i + 1) / (float)(prefabGuids.Length + 1));
                        int count = ProcessPrefab(path);
                        totalUpdated += count;
                        if (count > 0) logs.Add($"[프리팹] {Path.GetFileName(path)}: {count}개 업데이트됨");
                    }
                }

                logs.Add($"✅ 전체 완료: 총 {totalUpdated}개 항목 수정됨.");
                _logOutput = string.Join("\n", logs);
                // Debug.Log(logOutput);
            }
            finally
            {
                EditorUtility.ClearProgressBar();
                if (!string.IsNullOrEmpty(currentScene))
                    EditorSceneManager.OpenScene(currentScene);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }
        }

        private static int ProcessScene(string scenePath)
        {
            var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            var components = CompatObjectFind.FindAll<LocalizeStringEvent>(includeInactive: true);
            
            int updated = 0;
            foreach (var comp in components)
            {
                if (TryUpdateLocalizeStringEvent(comp))
                {
                    Undo.RecordObject(comp, "Update LocalizeStringEvent");
                    EditorUtility.SetDirty(comp);
                    updated++;
                }
            }

            if (updated > 0)
            {
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
            }

            return updated;
        }

        private static int ProcessPrefab(string path)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null) return 0;

            var components = prefab.GetComponentsInChildren<LocalizeStringEvent>(true);
            int updated = 0;

            foreach (var comp in components)
            {
                if (TryUpdateLocalizeStringEvent(comp))
                {
                    Undo.RecordObject(comp, "Update LocalizeStringEvent (Prefab)");
                    EditorUtility.SetDirty(comp);
                    updated++;
                }
            }

            return updated;
        }

        private static bool TryUpdateLocalizeStringEvent(LocalizeStringEvent localizeStringEvent)
        {
            var stringTable = LocalizationSettings.StringDatabase.GetTable(localizeStringEvent.StringReference
                .TableReference);
            if (stringTable == null)
            {
                string table = localizeStringEvent.StringReference.TableReference.TableCollectionName;
                string key = localizeStringEvent.StringReference.TableEntryReference.Key;
                if (!string.IsNullOrEmpty(table) && !string.IsNullOrEmpty(key))
                {
                    table = table.Replace("_User", "");
                    var tableEntryResult2 = LocalizationSettings.StringDatabase.GetTableEntry(table, key);
                    if (tableEntryResult2.Entry != null)
                    {
                        localizeStringEvent.StringReference = new LocalizedString
                        {
                            TableReference = table,
                            TableEntryReference = key
                        };
                        return true;
                    }
                }
                return false;
            }
            string currentTable = localizeStringEvent.StringReference.TableReference.TableCollectionName;
            var tableEntryResult = LocalizationSettings.StringDatabase.GetTableEntry(
                currentTable,
                localizeStringEvent.StringReference.TableEntryReference);
            
            if (string.IsNullOrEmpty(currentTable))
            {
                Debug.LogError("currentTable empty");
                return false;
            }
            if (tableEntryResult.Entry == null)
            {
                // user 였는데 없어졌으면 되돌리기
                if (currentTable.EndsWith("_User"))
                {
                    var table = currentTable.Replace("_User", "");
                    string key = localizeStringEvent.StringReference.TableEntryReference.Key;
                    var tableEntryResult2 = LocalizationSettings.StringDatabase.GetTableEntry(table, key);
                    if (tableEntryResult2.Entry != null)
                    {
                        localizeStringEvent.StringReference = new LocalizedString
                        {
                            TableReference = table,
                            TableEntryReference = key
                        };
                        return true;
                    }
                }
                Debug.LogError("tableEntryResult null");
                return false;
            }
            string currentKey = tableEntryResult.Entry.Key;
            
            string userTable = currentTable;
            if (!userTable.EndsWith("_User"))
            {
                userTable = $"{userTable}_User";
            }
            if (currentTable.EndsWith("_User"))
            {
                // 유저 테이블이 적용되고 있으면 넘어가기
                var tableEntryResultUser1 = LocalizationSettings.StringDatabase.GetTableEntry(currentTable, currentKey);
                if (tableEntryResultUser1.Entry != null)
                {
                    return false;
                }
                currentTable = currentTable.Replace("_User", "");
            }
            else
            {
                // 유저 테이블이 없으면 넘어가기
                var table = LocalizationSettings.StringDatabase.GetTable(userTable);
                if (table == null)
                {
                    return false;
                }
            }
            
            var tableEntryResultUser = LocalizationSettings.StringDatabase.GetTableEntry(userTable, currentKey);
            if (tableEntryResultUser.Entry != null)
            {
                localizeStringEvent.StringReference = new LocalizedString
                {
                    TableReference = userTable,
                    TableEntryReference = currentKey
                };
                return true;
            }

            return false;
        }
    }
}
#endif
