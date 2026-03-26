using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using GGemCo2DCore;

namespace GGemCo2DCoreEditor
{
    public class AddClickSoundBroadcasterToAllButtons : DefaultEditorWindow
    {
        private const string Title = "UI 버튼 사운드 적용툴";
        private string tagFilter = "";
        private string nameContains = "";
        // private string parentNameContains = "";
        private string prefabFolderPath = "Assets/Resources/GGemCo/";
        private SoundConstants.UIButtonType selectedButtonType = SoundConstants.UIButtonType.Default;
        private Vector2 _scrollPos = Vector2.zero;
        
        [MenuItem(ConfigEditor.NameToolSoundUIButton, false, (int)ConfigEditor.ToolOrdering.SoundUIButton)]
        public static void ShowWindow()
        {
            GetWindow<AddClickSoundBroadcasterToAllButtons>(Title);
        }

        private void OnGUI()
        {
            _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos);
            
            HelperEditorUI.OnGUITitle("버튼 타입 선택");
            selectedButtonType = (SoundConstants.UIButtonType)EditorGUILayout.EnumPopup("UIButtonType", selectedButtonType);
            HelperEditorUI.OnGUITitle("조건 필터링 (비워두면 전체)");
            tagFilter = EditorGUILayout.TagField("Tag", tagFilter);
            nameContains = EditorGUILayout.TextField("이름 포함 문자열", nameContains);
            // parentNameContains = EditorGUILayout.TextField("부모 이름 포함 문자열", parentNameContains);
            
            HelperEditorUI.GUILineBlue();
            HelperEditorUI.OnGUITitle("현재 로드된 씬 추가하기");

            if (GUILayout.Button("적용하기"))
                AddToSceneButtons();
            
            HelperEditorUI.GUILineBlue();
            HelperEditorUI.OnGUITitle("폴더 지정 후 추가하기");
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

            HelperEditorUI.GUILineBlue();
            HelperEditorUI.OnGUITitle("선택 후 추가하기");
            EditorGUILayout.HelpBox("스크립트를 추가하고 싶은 프리팹을 선택하고 적용하기 버튼을 클릭해주세요.\n여러개의 프리팹을 선택할 수 있습니다.", MessageType.Info);
            if (GUILayout.Button("적용하기"))
                AddToSelectedPrefabs();
            
            GUILayout.Space(20);
            EditorGUILayout.EndScrollView();
        }

        private void AddToSceneButtons()
        {
            bool result = EditorUtility.DisplayDialog(Title, "현재 씬에 있는 모든 버튼에 적용하시겠습니까?", "네", "아니요");
            if (!result) return;

            int count = 0;

            var buttons = CompatObjectFind.FindAll<Button>(includeInactive: true);            

            foreach (var btn in buttons)
            {
                if (!Filter(btn.gameObject)) continue;

                var broadcaster = btn.GetComponent<ClickSoundEventBroadcaster>();
                if (broadcaster == null)
                {
                    broadcaster = Undo.AddComponent<ClickSoundEventBroadcaster>(btn.gameObject);
                    count++;
                }

                if (broadcaster != null)
                {
                    Undo.RecordObject(broadcaster, "Set UIButtonType");
                    broadcaster.type = selectedButtonType;
                    EditorUtility.SetDirty(broadcaster);
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
            bool result = EditorUtility.DisplayDialog(Title, $"'{prefabFolderPath}' 폴더의 프리팹에서 필터 조건을 만족하는 버튼에 컴포넌트를 추가하시겠습니까?", "네", "아니요");
            if (!result) return;

            string[] guids = AssetDatabase.FindAssets("t:Prefab", new[] { prefabFolderPath });
            int totalAdded = 0;

            foreach (var guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefab == null) continue;

                totalAdded += ProcessGameObject(prefab, isPrefabAsset: true);
            }

            Debug.Log($"[ClickSoundBroadcaster] '{prefabFolderPath}' 폴더 내 프리팹에서 필터 조건에 맞는 {totalAdded}개 버튼에 컴포넌트 추가됨.");
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
        
                var broadcaster = btn.GetComponent<ClickSoundEventBroadcaster>();
                if (broadcaster == null)
                {
                    if (isPrefabAsset)
                    {
                        Undo.RecordObject(root, "Add ClickSoundEventBroadcaster");
                        broadcaster = btn.gameObject.AddComponent<ClickSoundEventBroadcaster>();
                        EditorUtility.SetDirty(root);
                    }
                    else
                    {
                        broadcaster = Undo.AddComponent<ClickSoundEventBroadcaster>(btn.gameObject);
                    }
                    count++;
                }

                // UIButtonType 적용
                if (broadcaster != null)
                {
                    Undo.RecordObject(broadcaster, "Set UIButtonType");
                    broadcaster.type = selectedButtonType;
                    EditorUtility.SetDirty(broadcaster);
                }
            }

            if (isPrefabAsset)
                AssetDatabase.SaveAssets();

            return count;
        }

        private bool Filter(GameObject go)
        {
            if (!string.IsNullOrEmpty(tagFilter) && tagFilter != "Untagged" && !go.CompareTag(tagFilter))
                return false;

            if (!string.IsNullOrEmpty(nameContains) && !go.name.Contains(nameContains))
                return false;

            // if (!string.IsNullOrEmpty(parentNameContains))
            // {
            //     Transform parent = go.transform.parent;
            //     bool found = false;
            //     while (parent != null)
            //     {
            //         if (parent.name.Contains(parentNameContains))
            //         {
            //             found = true;
            //             break;
            //         }
            //         parent = parent.parent;
            //     }
            //     if (!found) return false;
            // }

            return true;
        }
    }
}
