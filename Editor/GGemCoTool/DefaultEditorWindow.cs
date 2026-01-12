using System.Collections.Generic;
using System.IO;
using GGemCo2DCore;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace GGemCo2DCoreEditor
{
    /// <summary>
    /// GGemCo 에디터 툴 윈도우들이 공통으로 사용하는 기본 베이스 EditorWindow 입니다.
    /// 테이블 로더(TableLoaderManager)와 패키지 타입(packageType)을 초기화하고,
    /// Hierarchy 오브젝트 생성/컴포넌트 추가, 씬 생성 및 Build Settings 등록, 프리팹 검색 등의 유틸을 제공합니다.
    /// </summary>
    public class DefaultEditorWindow : EditorWindow
    {
        /// <summary>
        /// 기본 패키지 타입입니다.
        /// GameObject 이름 생성/루트 패키지 오브젝트 구성에 사용됩니다.
        /// </summary>
        protected ConfigPackageInfo.PackageType packageType;

        /// <summary>
        /// 에디터 윈도우가 활성화될 때 공용 의존성을 초기화합니다.
        /// </summary>
        protected virtual void OnEnable()
        {
            packageType = ConfigPackageInfo.PackageType.Core;
        }

        /// <summary>
        /// Hierarchy에서 SDK 루트(ConfigDefine.NameSDK) 아래에 패키지 루트 GameObject를 가져오거나 생성합니다.
        /// </summary>
        /// <param name="ppackageType">
        /// 생성/탐색할 패키지 타입입니다.
        /// None이면 이 윈도우의 <see cref="packageType"/> 값을 사용합니다.
        /// </param>
        /// <returns>패키지 루트 GameObject(예: "Core" 등)를 반환합니다.</returns>
        /// <remarks>
        /// 부작용:
        /// - 현재 열린 씬의 Hierarchy에 GameObject를 생성/부모 지정할 수 있습니다.
        /// </remarks>
        protected GameObject GetOrCreateRootPackageGameObject(ConfigPackageInfo.PackageType ppackageType = ConfigPackageInfo.PackageType.None)
        {
            var obj = GameObject.Find(ConfigDefine.NameSDK);
            GameObject objPackage;

            if (ppackageType == ConfigPackageInfo.PackageType.None)
                ppackageType = packageType;

            string packageName = ConfigPackageInfo.GetPackageName(ppackageType);

            if (obj == null)
            {
                obj = new GameObject(ConfigDefine.NameSDK);

                objPackage = new GameObject(packageName);
                objPackage.transform.SetParent(obj.transform);
            }
            else
            {
                var transformPackage = obj.transform.Find(packageName);
                if (transformPackage == null)
                {
                    objPackage = new GameObject(packageName);
                    objPackage.transform.SetParent(obj.transform);
                }
                else
                {
                    objPackage = transformPackage.gameObject;
                }
            }

            return objPackage;
        }

        /// <summary>
        /// 지정한 이름의 GameObject를 Hierarchy에서 찾고, 없으면 생성하여 패키지 루트 아래에 배치합니다.
        /// </summary>
        /// <param name="objectName">생성/탐색할 오브젝트의 베이스 이름입니다.</param>
        /// <param name="ppackageType">
        /// 이름 생성 규칙에 사용할 패키지 타입입니다.
        /// None이면 이 윈도우의 <see cref="packageType"/> 값을 사용합니다.
        /// </param>
        /// <returns>찾거나 생성된 GameObject를 반환합니다.</returns>
        /// <remarks>
        /// - 실제로 생성되는 오브젝트 이름은 CreateUIComponent.GenerateObjectName 규칙에 따라 변환됩니다.
        /// - 부작용으로 현재 씬에 오브젝트가 생성될 수 있습니다.
        /// </remarks>
        private GameObject GetOrCreateGameObject(string objectName, ConfigPackageInfo.PackageType ppackageType = ConfigPackageInfo.PackageType.None)
        {
            if (ppackageType == ConfigPackageInfo.PackageType.None)
                ppackageType = packageType;

            objectName = CreateUIComponent.GenerateObjectName(objectName, ppackageType);

            GameObject obj = GameObject.Find(objectName);
            if (obj == null)
            {
                obj = new GameObject(objectName);

                // NOTE: 내부에서 ppackageType을 전달하지 않고 기본 패키지 루트를 사용합니다(원본 동작 유지).
                GameObject root = GetOrCreateRootPackageGameObject();
                obj.transform.SetParent(root.transform);
            }

            return obj;
        }

        /// <summary>
        /// 지정한 이름의 GameObject가 없으면 생성하고, 해당 컴포넌트를 추가하거나 기존 컴포넌트를 반환합니다.
        /// </summary>
        /// <typeparam name="T">추가/조회할 컴포넌트 타입</typeparam>
        /// <param name="objectName">대상 GameObject의 베이스 이름</param>
        /// <param name="ppackageType">
        /// 이름 생성 규칙에 사용할 패키지 타입입니다.
        /// None이면 이 윈도우의 <see cref="packageType"/> 값을 사용합니다.
        /// </param>
        /// <returns>생성/추가/조회된 컴포넌트를 반환합니다.</returns>
        /// <remarks>
        /// 부작용:
        /// - GameObject가 없으면 생성되어 씬 Hierarchy가 변경됩니다.
        /// - 컴포넌트가 없으면 AddComponent로 추가됩니다.
        /// </remarks>
        protected T CreateOrAddComponent<T>(string objectName, ConfigPackageInfo.PackageType ppackageType = ConfigPackageInfo.PackageType.None)
            where T : Component
        {
            if (ppackageType == ConfigPackageInfo.PackageType.None)
                ppackageType = packageType;

            GameObject targetObj = GetOrCreateGameObject(objectName, ppackageType);

            return targetObj.TryGetComponent<T>(out var comp) ? comp : targetObj.AddComponent<T>();
        }

        /// <summary>
        /// 지정한 씬 경로를 Build Settings(Scene Build Profiles)에 등록합니다.
        /// 씬 파일이 존재하지 않으면 새 씬을 생성하여 저장한 뒤 등록합니다.
        /// </summary>
        /// <param name="scenePath">프로젝트 내 씬 파일 경로(예: "Assets/Scenes/Intro.unity")</param>
        /// <remarks>
        /// 부작용:
        /// - 씬 파일이 없을 경우 새 씬을 생성하고 저장합니다.
        /// - EditorBuildSettings.scenes를 수정하여 빌드 대상 씬 목록을 변경합니다.
        /// </remarks>
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
        /// 지정한 폴더 하위에서 프리팹 이름으로 GameObject(프리팹 에셋)를 찾습니다.
        /// </summary>
        /// <param name="folderPath">검색할 폴더 경로(예: "Assets/Resources/Prefabs")</param>
        /// <param name="prefabName">찾고자 하는 프리팹 이름(확장자 없이)</param>
        /// <returns>찾은 프리팹 GameObject 에셋, 없으면 null</returns>
        /// <remarks>
        /// - AssetDatabase.FindAssets로 후보를 찾은 뒤, 파일 이름을 통해 정확히 일치하는 프리팹만 반환합니다.
        /// - folderPath가 유효하지 않으면 경고 로그를 남기고 null을 반환합니다.
        /// </remarks>
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
