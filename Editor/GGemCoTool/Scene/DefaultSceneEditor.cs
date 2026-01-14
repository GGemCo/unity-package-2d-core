using System.IO;
using GGemCo2DCore;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using SceneManager = UnityEngine.SceneManagement.SceneManager;

namespace GGemCo2DCoreEditor
{
    public class DefaultSceneEditor : DefaultEditorWindow
    {
        private Scene GetActiveSceneInEditor()
        {
            // 현재 에디터에서 활성화된 씬을 가져옴
            return SceneManager.GetActiveScene();
        }
        protected bool CheckCurrentLoadedScene(string sceneName)
        {
            Scene scene = GetActiveSceneInEditor();
            return scene.name == sceneName;
        }
        /// <summary>
        /// 사운드 매니저 셋팅
        /// </summary>
        protected SoundManager CreateSoundManager(Transform parent = null)
        {
            GameObject obj = CreateUIComponent.CreateGameObjectByPrefab("SoundManager", packageType, parent);
            if (!obj) return null;
            SoundManager soundManager = obj.GetComponent<SoundManager>();
            if (soundManager == null)
            {
                soundManager = obj.AddComponent<SoundManager>();
            }
            return soundManager;
        }
        /// <summary>
        /// 팝업 매니저 셋팅
        /// </summary>
        protected PopupManager CreatePopupManager(Transform parent = null)
        {
            GameObject obj = CreateUIComponent.CreateGameObjectByPrefab("PopupManager", packageType, parent, ConfigEditor.PathPrefabPopupManager);
            if (!obj) return null;
            PopupManager popupManager = obj.GetComponent<PopupManager>();
            if (popupManager == null)
            {
                obj.AddComponent<PopupManager>();
            }
            
            Transform transform = CreateUIComponent.Find("Canvas", packageType).transform;
            
            popupManager.SetCanvasPopup(transform);
            GameObject[] prefabs = new[] { null, ConfigResources.PopupDefault.Load() };
            popupManager.SetPopupTypePrefabs(prefabs);
            return popupManager;
        }

        protected TextMeshProUGUI CreateLoadingText()
        {
            string fieldName = "textLoadingPercent";
            MetaDataTextMeshProGUI metaDataTextMeshProGUI =
                new MetaDataTextMeshProGUI(new Vector2(1, 0), new Vector2(-100, 100), AnchorPresets.BottomRight, 1000,
                    50, 0, TextMeshProHelper.HorizontalAlignment.Right, TextMeshProHelper.VerticalAlignment.Middle,
                    LocalizationConstants.Tables.Scene,
                    LocalizationConstants.Keys.Loading.TextLoadingPercent());
            TextMeshProUGUI textMeshProUGUI = CreateUIComponent.CreateObjectText(fieldName, packageType, metaDataTextMeshProGUI);
            return textMeshProUGUI;
        }
        /// <summary>
        /// UIWindowOption 내부 listPrefabPanel 에 UIPanelOptionBase 프리팹 자동 등록
        /// </summary>
        protected void AutoFillPanelPrefabs(UIWindow uiWindowOption)
        {
            if (uiWindowOption == null)
            {
                Debug.LogError("UIWindowOption 이 null 입니다.");
                return;
            }

            // 예: new[] { "Assets/GGemCo/UIWindows/Option" }
            // null 이면 Project 전체 검색
            string[] searchFolders = 
            {
                "Assets/GGemCo/UIWindows/Option",
            };

            HelperFile.ImportAssetForDirectory(searchFolders);
            
            // 1) 모든 Prefab 검색
            // string[] guids = AssetDatabase.FindAssets("t:Prefab");
            // 1) 모든 Prefab GUID 검색
            string filter = "t:Prefab";
            string[] guids = searchFolders.Length == 0
                ? AssetDatabase.FindAssets(filter)
                : AssetDatabase.FindAssets(filter, searchFolders);

            var result = new System.Collections.Generic.List<GameObject>();

            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var go = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (go == null) continue;

                // UIPanelOptionBase 붙어 있는 프리팹만 대상
                if (go.GetComponent<UIPanelOptionBase>() != null)
                    result.Add(go);
            }

            // 정렬 (원하는 기준으로 바꿀 수 있음)
            result.Sort((a, b) => string.Compare(a.name, b.name, System.StringComparison.Ordinal));

            // 2) SerializedObject 로 listPrefabPanel 채우기
            SerializedObject so = new SerializedObject(uiWindowOption);
            SerializedProperty listProp = so.FindProperty("listPrefabPanel");

            listProp.ClearArray();

            for (int i = 0; i < result.Count; i++)
            {
                listProp.InsertArrayElementAtIndex(i);
                listProp.GetArrayElementAtIndex(i).objectReferenceValue = result[i];
            }

            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(uiWindowOption);

            Debug.Log($"[SceneEditorIntro] UIPanelOptionBase 프리팹 자동 등록 완료 → {result.Count} 개");
        }
    }
}