using System;
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
        public bool isLoading = true;
        public TableLoaderManager TableLoaderManager;

        protected virtual void OnEnable()
        {
            isLoading = true;
            TableLoaderManager = new TableLoaderManager();
        }

        protected void ShowLoadTableException(string ptitle, Exception ex)
        {
            Debug.LogError($"{ptitle} LoadAsync 예외 발생: {ex.Message}");
            EditorUtility.DisplayDialog(ptitle, "테이블 로딩 중 오류가 발생했습니다.", "OK");
            isLoading = false;
        }
        
        // GGemCo GameObject 만들기
            // GGemCo 스크립트 AddComponent 하기
            // 스크립트의 필수 항목을 만들어서 연결하기
            
        // GGemCo 매니저 만들기
            
        // 유니티 Empty GameObject 만들기
        
        // 유니티 UI 컴포넌트 만들기 
            // Canvas
                // Render Mode : World Space
            // 버튼
            // 텍스트 (TMP)
            
        /// <summary>
        /// 모든 오브젝트는 GGemCo 오브젝트 하위에 생성한다.
        /// Core 패키지 이기때문에 Core 오브젝트 하위에 생성한다.
        /// UI 는 Canvas 에 생성한다.
        /// </summary>
        protected GameObject GetOrCreateRootGameObject()
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
        protected GameObject GetOrCreateGameObject(string objectName)
        {
            if (!objectName.StartsWith($"{ConfigDefine.NameSDK}_{ConfigDefine.NamePackageCore}")) objectName = $"{ConfigDefine.NameSDK}_{ConfigDefine.NamePackageCore}_{objectName}";
            var obj = GameObject.Find(objectName);
            if (obj == null)
            {
                obj = new GameObject(objectName);
            }
            GameObject root = GetOrCreateRootGameObject();
            obj.transform.SetParent(root.transform);
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
        public static void AddSceneToBuildSettings(string scenePath)
        {
            // string scenePath = "Assets/Scenes/ExampleScene.unity";

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
    }
}