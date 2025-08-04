using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using GGemCo2DCore;

namespace GGemCo2DCoreEditor
{
    public class AddClickSoundBroadcasterToAllButtons : DefaultEditorWindow
    {
        private const string Title = "Click Sound Broadcaster 추가하기";
        private string tagFilter = "";
        private string nameContains = "";
        private string parentNameContains = "";
        private string prefabFolderPath = "Assets/Resources/GGemCo/";

        [MenuItem("GGemCoTool/개발툴/사운드/버튼 Broadcaster 일괄 추가")]
        public static void ShowWindow()
        {
            GetWindow<AddClickSoundBroadcasterToAllButtons>(Title);
        }

        private void OnGUI()
        {
            GUILayout.Label("조건 필터링 (비워두면 전체)", EditorStyles.boldLabel);
            tagFilter = EditorGUILayout.TagField("Tag", tagFilter);
            nameContains = EditorGUILayout.TextField("이름 포함 문자열", nameContains);
            parentNameContains = EditorGUILayout.TextField("부모 이름 포함 문자열", parentNameContains);

            if (GUILayout.Button("적용하기"))
                AddToSceneButtons();

            Common.GUILineBlue();
            EditorGUILayout.HelpBox("스크립트를 추가하고 싶은 프리팹이 있는 폴더를 선택하고 적용하기 버튼을 클릭해주세요.", MessageType.Info);
            prefabFolderPath = EditorGUILayout.TextField("프리팹 폴더 경로", prefabFolderPath);
            if (GUILayout.Button("폴더 선택"))
            {
                string selected = EditorUtility.OpenFolderPanel("프리팹 폴더 선택", "Assets/", "");
                if (!string.IsNullOrEmpty(selected))
                {
                    if (selected.StartsWith(Application.dataPath))
                    {
                        prefabFolderPath = "Assets" + selected.Substring(Application.dataPath.Length);
                    }
                }
            }

            if (GUILayout.Button("적용하기"))
                AddToPrefabButtonsInFolder();

            Common.GUILineBlue();
            EditorGUILayout.HelpBox("스크립트를 추가하고 싶은 프리팹을 선택하고 적용하기 버튼을 클릭해주세요.\n여러개의 프리팹을 선택할 수 있습니다.", MessageType.Info);
            if (GUILayout.Button("적용하기"))
                AddToSelectedPrefabs();
        }

        private void AddToSceneButtons()
        {
            bool result = EditorUtility.DisplayDialog(Title, "현재 씬에 있는 모든 버튼에 적용하시겠습니까?", "네", "아니요");
            if (!result) return;
            
            int count = 0;
#if UNITY_6000_0_OR_NEWER
            var buttons = Object.FindObjectsByType<Button>(FindObjectsSortMode.None);
#else
            var buttons = FindObjectsOfType<Button>(true);
#endif
            
            foreach (var btn in buttons)
            {
                if (Filter(btn.gameObject) && btn.GetComponent<ClickSoundEventBroadcaster>() == null)
                {
                    Undo.AddComponent<ClickSoundEventBroadcaster>(btn.gameObject);
                    count++;
                }
            }

            Debug.Log($"[ClickSoundBroadcaster] 씬 내 버튼 중 {count}개에 컴포넌트 추가됨.");
        }

        private void AddToPrefabButtonsInFolder()
        {
            if (string.IsNullOrEmpty(prefabFolderPath) || !AssetDatabase.IsValidFolder(prefabFolderPath))
            {
                Debug.LogWarning($"[ClickSoundBroadcaster] 유효하지 않은 폴더 경로입니다: {prefabFolderPath}");
                return;
            }
            bool result = EditorUtility.DisplayDialog(Title, "현재 선택된 폴더에 있는 프리팹에 적용하시겠습니까?", "네", "아니요");
            if (!result) return;

            string[] guids = AssetDatabase.FindAssets("t:Prefab", new[] { prefabFolderPath });
            int count = 0;

            foreach (var guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefab == null) continue;

                count += ProcessGameObject(prefab, isPrefabAsset: true);
            }

            Debug.Log($"[ClickSoundBroadcaster] '{prefabFolderPath}' 폴더 내 프리팹에서 {count}개 버튼에 컴포넌트 추가됨.");
        }

        private void AddToSelectedPrefabs()
        {
            var selected = Selection.gameObjects;
            if (selected.Length <= 0)
            {
                EditorUtility.DisplayDialog(Title, "선택된 프리팹이 없습니다.", "OK");
                return;
            }
            int totalAdded = 0;

            foreach (var obj in selected)
            {
                var type = PrefabUtility.GetPrefabAssetType(obj);
                bool isPrefabAsset = type == PrefabAssetType.Regular || type == PrefabAssetType.Variant;
                bool isSceneInstance = type == PrefabAssetType.NotAPrefab;

                if (!isPrefabAsset && !isSceneInstance)
                {
                    Debug.Log($"[ClickSoundBroadcaster] 무시됨: {obj.name}");
                    continue;
                }

                totalAdded += ProcessGameObject(obj, isPrefabAsset);
            }

            Debug.Log($"[ClickSoundBroadcaster] 선택한 오브젝트 중 {totalAdded}개 버튼에 컴포넌트 추가됨.");
        }

        private int ProcessGameObject(GameObject root, bool isPrefabAsset)
        {
            int count = 0;
            var buttons = root.GetComponentsInChildren<Button>(true);

            foreach (var btn in buttons)
            {
                if (!Filter(btn.gameObject)) continue;
                if (btn.GetComponent<ClickSoundEventBroadcaster>() != null) continue;

                if (isPrefabAsset)
                {
                    Undo.RecordObject(root, "Add ClickSoundEventBroadcaster");
                    btn.gameObject.AddComponent<ClickSoundEventBroadcaster>();
                    EditorUtility.SetDirty(root);
                }
                else
                {
                    Undo.AddComponent<ClickSoundEventBroadcaster>(btn.gameObject);
                }

                count++;
            }

            if (isPrefabAsset)
                AssetDatabase.SaveAssets();

            return count;
        }

        private bool Filter(GameObject go)
        {
            if (!string.IsNullOrEmpty(tagFilter) && tagFilter != "Untagged" && go.tag != tagFilter)
                return false;

            if (!string.IsNullOrEmpty(nameContains) && !go.name.Contains(nameContains))
                return false;

            if (!string.IsNullOrEmpty(parentNameContains))
            {
                Transform parent = go.transform.parent;
                bool found = false;
                while (parent != null)
                {
                    if (parent.name.Contains(parentNameContains))
                    {
                        found = true;
                        break;
                    }
                    parent = parent.parent;
                }
                if (!found) return false;
            }

            return true;
        }
    }
}
