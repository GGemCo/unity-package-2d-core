using System.Collections.Generic;
using System.IO;
using GGemCo2DCore;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace GGemCo2DCoreEditor
{
    public class DefaultEditorWindow : EditorWindow
    {
        protected TableLoaderManager TableLoaderManager;

        protected virtual void OnEnable()
        {
            TableLoaderManager = new TableLoaderManager();
        }
        /// <summary>
        /// 모든 오브젝트는 GGemCo 오브젝트 하위에 생성한다.
        /// Core 패키지 이기때문에 Core 오브젝트 하위에 생성한다.
        /// UI 는 Canvas 에 생성한다.
        /// </summary>
        protected GameObject GetOrCreateCoreGameObject()
        {
            var obj = GameObject.Find(ConfigDefine.NameSDK);
            GameObject objPackage;
            if (obj == null)
            {
                obj = new  GameObject(ConfigDefine.NameSDK);
                objPackage = new GameObject(ConfigDefine.NamePackageCore);
                objPackage.transform.SetParent(obj.transform);
            }
            else
            {
                var transformPackage = obj.transform.Find(ConfigDefine.NamePackageCore);
                if (transformPackage == null)
                {
                    objPackage = new GameObject(ConfigDefine.NamePackageCore);
                    objPackage.transform.SetParent(obj.transform);
                }
                else
                {
                    objPackage = transformPackage.gameObject;
                }
            }
            return objPackage;
        }

        private GameObject GetOrCreateGameObject(string objectName)
        {
            if (!objectName.StartsWith($"{ConfigEditor.NamePrefixCore}")) objectName = $"{ConfigEditor.NamePrefixCore}_{objectName}";
            GameObject obj = GameObject.Find(objectName);
            if (obj == null)
            {
                obj = new GameObject(objectName);
                GameObject root = GetOrCreateCoreGameObject();
                obj.transform.SetParent(root.transform);
            }
            return obj;
        }
        /// <summary>
        /// 지정한 이름의 GameObject가 없으면 생성하고, 해당 컴포넌트를 추가 또는 가져옵니다.
        /// </summary>
        protected T CreateOrAddComponent<T>(string objectName) where T : Component
        {
            GameObject targetObj = GetOrCreateGameObject(objectName);
            return !targetObj.TryGetComponent<T>(out _) ? targetObj.AddComponent<T>() : targetObj.GetComponent<T>();
        }
        /// <summary>
        /// Scene Build Profiles 에 등록하기 
        /// </summary>
        protected static void AddSceneToBuildSettings(string scenePath)
        {
            // 씬이 없으면 새로 생성
            if (!File.Exists(scenePath))
            {
                // 새로운 빈 씬 생성
                var newScene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
                EditorSceneManager.SaveScene(newScene, scenePath);
                Debug.Log($"씬이 새로 생성되었습니다: {scenePath}");
            }
            else
            {
                Debug.Log($"씬이 이미 존재합니다: {scenePath}");
            }

            // 현재 Build Settings에 등록된 씬 목록 가져오기
            List<EditorBuildSettingsScene> currentScenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);

            // 이미 등록되어 있는지 확인
            bool alreadyExists = currentScenes.Exists(s => s.path == scenePath);

            if (!alreadyExists)
            {
                // 새 씬 추가
                currentScenes.Add(new EditorBuildSettingsScene(scenePath, true));
                EditorBuildSettings.scenes = currentScenes.ToArray();
                Debug.Log($"Build Settings에 씬이 추가되었습니다: {scenePath}");
            }
            else
            {
                Debug.Log($"이미 등록된 씬입니다: {scenePath}");
            }
        }
        /// <summary>
        /// 지정한 폴더 하위에서 프리팹 이름으로 GameObject를 찾습니다.
        /// </summary>
        /// <param name="folderPath">예: "Assets/Resources/Prefabs"</param>
        /// <param name="prefabName">찾고자 하는 프리팹 이름 (확장자 없이)</param>
        /// <returns>찾은 프리팹 GameObject, 없으면 null</returns>
        protected static GameObject FindPrefabByName(string folderPath, string prefabName)
        {
            if (!AssetDatabase.IsValidFolder(folderPath))
            {
                Debug.LogWarning($"유효하지 않은 폴더 경로: {folderPath}");
                return null;
            }

            string[] guids = AssetDatabase.FindAssets($"{prefabName} t:prefab", new[] { folderPath });

            foreach (string guid in guids)
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(guid);
                string fileName = Path.GetFileNameWithoutExtension(assetPath);
            
                if (fileName == prefabName) // 정확한 이름 일치 확인
                {
                    GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
                    return prefab;
                }
            }

            return null;
        }
    }
}