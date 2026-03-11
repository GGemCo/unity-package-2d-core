using System;
using System.Collections.Generic;
using System.IO;
using GGemCo2DCore;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

namespace GGemCo2DCoreEditor
{
    /// <summary>
    /// GGemCo 에디터 툴 윈도우들이 공통으로 사용하는 기본 베이스 EditorWindow 입니다.
    /// 테이블 로더(TableLoaderManager)와 패키지 타입(packageType)을 초기화하고,
    /// Hierarchy 오브젝트 생성/컴포넌트 추가, 씬 생성 및 Build Settings 등록, 프리팹 검색,
    /// 런타임 CharacterBase 대상 선택 UI를 제공합니다.
    /// </summary>
    public class DefaultEditorWindow : EditorWindow
    {
        /// <summary>
        /// 기본 패키지 타입입니다.
        /// GameObject 이름 생성/루트 패키지 오브젝트 구성에 사용됩니다.
        /// </summary>
        protected ConfigPackageInfo.PackageType packageType;

        /// <summary>
        /// 공통 대상 캐릭터입니다.
        /// 런타임 테스트 툴에서 직접 대상 지정/ObjectField/Popup 선택에 공통 사용됩니다.
        /// </summary>
        protected CharacterBase selectedCharacter;

        /// <summary>
        /// 현재 씬에서 찾은 CharacterBase 목록입니다.
        /// </summary>
        protected readonly List<CharacterBase> sceneCharacters = new();

        /// <summary>
        /// 팝업 표시용 캐릭터 이름 목록입니다.
        /// </summary>
        protected readonly List<string> sceneCharacterNames = new();

        /// <summary>
        /// 현재 팝업 선택 인덱스입니다.
        /// </summary>
        protected int selectedCharacterIndex;

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

        protected static GameObject FindPrefabUIWindowByName(string prefabName)
        {
            var folderName = prefabName.Replace("UIWindow", "");
            var assetPath = $"{ConfigEditor.PathUIWindow}/{folderName}/{prefabName}.prefab";
            AssetDatabase.ImportAsset(assetPath,
                ImportAssetOptions.ForceUpdate | ImportAssetOptions.ForceSynchronousImport);

            return AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
        }

        /// <summary>
        /// 현재 Hierarchy 선택에서 CharacterBase를 찾아 공통 대상 캐릭터로 지정합니다.
        /// </summary>
        /// <param name="dialogTitle">실패 시 표시할 다이얼로그 제목입니다.</param>
        /// <returns>지정 성공 시 true, 실패 시 false를 반환합니다.</returns>
        protected bool TryAssignCharacterFromSelection(string dialogTitle)
        {
            var go = Selection.activeGameObject;
            if (go == null)
            {
                EditorUtility.DisplayDialog(dialogTitle, "Hierarchy에서 대상 오브젝트를 선택해주세요.", "OK");
                return false;
            }

            if (!go.TryGetComponent<CharacterBase>(out var character))
                character = go.GetComponentInParent<CharacterBase>();

            if (character == null)
            {
                EditorUtility.DisplayDialog(dialogTitle, "선택한 오브젝트에서 CharacterBase를 찾지 못했습니다.", "OK");
                return false;
            }

            selectedCharacter = character;
            SyncSelectedCharacterIndex();
            Repaint();
            OnSelectedCharacterChanged(character);
            return true;
        }

        /// <summary>
        /// 현재 씬에서 CharacterBase를 다시 수집합니다.
        /// </summary>
        protected void RefreshSceneCharacters()
        {
            sceneCharacters.Clear();
            sceneCharacterNames.Clear();

            if (!Application.isPlaying)
                return;

#if UNITY_2023_1_OR_NEWER
            var characters = Object.FindObjectsByType<CharacterBase>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
#else
            var characters = Object.FindObjectsOfType<CharacterBase>();
#endif
            foreach (var character in characters)
            {
                if (character == null)
                    continue;

                sceneCharacters.Add(character);
                sceneCharacterNames.Add($"{character.name} (id:{character.GetInstanceID()})");
            }

            selectedCharacterIndex = Mathf.Clamp(selectedCharacterIndex, 0, Mathf.Max(0, sceneCharacters.Count - 1));

            if (sceneCharacters.Count <= 0)
            {
                selectedCharacter = null;
                return;
            }

            if (selectedCharacter == null)
            {
                selectedCharacter = sceneCharacters[selectedCharacterIndex];
                OnSelectedCharacterChanged(selectedCharacter);
                return;
            }

            SyncSelectedCharacterIndex();
        }

        /// <summary>
        /// 공통 대상 캐릭터와 팝업 인덱스를 동기화합니다.
        /// </summary>
        protected void SyncSelectedCharacterIndex()
        {
            if (selectedCharacter == null || sceneCharacters.Count <= 0)
                return;

            int index = sceneCharacters.IndexOf(selectedCharacter);
            if (index >= 0)
                selectedCharacterIndex = index;
        }

        /// <summary>
        /// CharacterBase 선택 공통 UI를 그립니다.
        /// </summary>
        /// <param name="dialogTitle">Selection 지정 실패 시 표시할 다이얼로그 제목입니다.</param>
        /// <param name="sectionTitle">섹션 제목입니다.</param>
        protected void DrawCharacterSelectionSection(string dialogTitle, string sectionTitle = "대상 캐릭터")
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField(sectionTitle, EditorStyles.boldLabel);

                using (new EditorGUI.DisabledScope(!Application.isPlaying))
                {
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        if (GUILayout.Button("현재 선택 오브젝트로 지정", GUILayout.Height(22)))
                            TryAssignCharacterFromSelection(dialogTitle);

                        if (GUILayout.Button("씬 캐릭터 목록 새로고침", GUILayout.Height(22)))
                            RefreshSceneCharacters();
                    }

                    if (sceneCharacterNames.Count == 0)
                    {
                        EditorGUILayout.HelpBox("씬에서 CharacterBase를 찾지 못했습니다. (비활성 오브젝트는 제외됩니다)", MessageType.Info);
                    }
                    else
                    {
                        selectedCharacterIndex = Mathf.Clamp(selectedCharacterIndex, 0, sceneCharacterNames.Count - 1);
                        int newIndex = EditorGUILayout.Popup("캐릭터 목록", selectedCharacterIndex, sceneCharacterNames.ToArray());
                        if (newIndex != selectedCharacterIndex)
                        {
                            selectedCharacterIndex = newIndex;
                            selectedCharacter = sceneCharacters[selectedCharacterIndex];
                            OnSelectedCharacterChanged(selectedCharacter);
                        }
                    }

                    var newCharacter = (CharacterBase)EditorGUILayout.ObjectField("대상(직접 지정)", selectedCharacter, typeof(CharacterBase), true);
                    if (newCharacter != selectedCharacter)
                    {
                        selectedCharacter = newCharacter;
                        SyncSelectedCharacterIndex();
                        OnSelectedCharacterChanged(selectedCharacter);
                    }

                    if (selectedCharacter != null)
                        EditorGUILayout.LabelField("대상 이름", selectedCharacter.name);
                }
            }
        }


        /// <summary>
        /// 지정한 CharacterBase를 기준으로 테스트용 더미 Target을 생성하거나 기존 더미를 재사용합니다.
        /// </summary>
        protected bool TryCreateOrReuseDummyTargetCharacter(
            string dialogTitle,
            CharacterBase sourceCharacter,
            out CharacterBase dummyCharacter,
            string toolOwnerKey = null,
            string dummyName = null,
            Vector3? spawnPosition = null,
            Vector3? spawnOffset = null)
        {
            dummyCharacter = null;

            var options = new CharacterTestDummyFactory.CreateOptions
            {
                ToolOwnerKey = string.IsNullOrEmpty(toolOwnerKey) ? GetType().FullName : toolOwnerKey,
                DummyName = string.IsNullOrEmpty(dummyName) ? "CharacterTest_DummyTarget" : dummyName,
                SpawnPosition = spawnPosition,
                SpawnOffset = spawnOffset ?? new Vector3(150f, 0f, 0f)
            };

            if (CharacterTestDummyFactory.TryCreateOrReuseDummyTarget(sourceCharacter, options, out dummyCharacter, out var error))
                return true;

            EditorUtility.DisplayDialog(dialogTitle, error, "OK");
            return false;
        }

        /// <summary>
        /// 현재 선택된 공통 대상 캐릭터(<see cref="selectedCharacter"/>)를 기준으로 테스트용 더미 Target을 생성하거나 재사용합니다.
        /// </summary>
        protected bool TryCreateOrReuseDummyTargetCharacter(
            string dialogTitle,
            out CharacterBase dummyCharacter,
            string toolOwnerKey = null,
            string dummyName = null,
            Vector3? spawnPosition = null,
            Vector3? spawnOffset = null)
        {
            return TryCreateOrReuseDummyTargetCharacter(
                dialogTitle,
                selectedCharacter,
                out dummyCharacter,
                toolOwnerKey,
                dummyName,
                spawnPosition,
                spawnOffset);
        }

        /// <summary>
        /// 공통 대상 캐릭터가 변경되었을 때 하위 클래스가 후처리할 수 있는 훅입니다.
        /// </summary>
        protected virtual void OnSelectedCharacterChanged(CharacterBase character)
        {
        }

        protected void DrawPlayModeGate()
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField("실행 조건", EditorStyles.boldLabel);

                if (!Application.isPlaying)
                {
                    EditorGUILayout.HelpBox("Play Mode에서만 동작합니다.", MessageType.Warning);
                    return;
                }

                if (!SceneGame.Instance)
                {
                    EditorGUILayout.HelpBox("SceneGame.Instance를 찾지 못했습니다. 게임 씬이 로드되어 있는지 확인해주세요.", MessageType.Warning);
                }
                else
                {
                    EditorGUILayout.HelpBox("Play Mode에서 동작 중입니다.", MessageType.Info);
                }
            }
        }
        
        /// <summary>
        /// 테이블 재로딩 섹션을 공통 UI로 그립니다.
        /// 하위 클래스는 실제 재로딩 로직과 버튼 라벨만 제공하면 됩니다.
        /// </summary>
        /// <param name="lastReloadMessage">최근 재로딩 결과 메시지</param>
        /// <param name="buttonLabel">재로딩 버튼 라벨</param>
        /// <param name="reloadAction">실제 재로딩 처리</param>
        protected void DrawTableReloadSection(string lastReloadMessage, string buttonLabel, System.Action reloadAction)
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField("테이블 재로딩", EditorStyles.boldLabel);

                if (GUILayout.Button(buttonLabel, GUILayout.Height(24)))
                    ExecuteTableReload(reloadAction);

                if (!string.IsNullOrEmpty(lastReloadMessage))
                    EditorGUILayout.HelpBox(lastReloadMessage, MessageType.Info);
            }
        }
        /// <summary>
        /// 테이블 재로딩의 공통 실행 흐름을 처리합니다.
        /// 성공/실패 메시지 저장은 콜백 내부에서 수행합니다.
        /// </summary>
        /// <param name="reloadAction">실제 재로딩 처리</param>
        protected void ExecuteTableReload(System.Action reloadAction)
        {
            reloadAction?.Invoke();
            Repaint();
        }
        protected void RebuildDropdownOptions<TRow>(
            IEnumerable<TRow> source,
            List<SearchableDropdownUtility.Option<TRow>> targetOptions,
            Func<TRow, bool> isValidRow,
            Func<TRow, string> keySelector,
            Func<TRow, string> valueSelector,
            Action<TRow> assignSelected,
            Func<TRow, bool> filter = null)
            where TRow : class
        {
            targetOptions.Clear();

            if (source == null)
            {
                assignSelected?.Invoke(null);
                return;
            }

            foreach (var row in source)
            {
                if (row == null)
                    continue;

                if (isValidRow != null && !isValidRow(row))
                    continue;

                if (filter != null && !filter(row))
                    continue;

                targetOptions.Add(new SearchableDropdownUtility.Option<TRow>(
                    key: keySelector(row),
                    value: valueSelector(row),
                    data: row));
            }

            assignSelected?.Invoke(targetOptions.Count > 0 ? targetOptions[0].Data : null);
        }
    }
}
