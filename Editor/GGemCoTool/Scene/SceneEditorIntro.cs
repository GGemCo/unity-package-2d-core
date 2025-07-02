using GGemCo2DCore;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace GGemCo2DCoreEditor
{
    /// <summary>
    /// 인트로 씬 설정 툴
    /// </summary>
    public class SceneEditorIntro : DefaultSceneEditor
    {
        private const string Title = "인트로 씬 셋팅하기";
        private GameObject _objGGemCoCore;
        
        [MenuItem(ConfigEditor.NameToolSettingSceneIntro, false, (int)ConfigEditor.ToolOrdering.SettingSceneIntro)]
        public static void ShowWindow()
        {
            GetWindow<SceneEditorIntro>(Title);
        }

        private void OnGUI()
        {
            if (!CheckCurrentLoadedScene(ConfigDefine.SceneNameIntro))
            {
                EditorGUILayout.HelpBox($"인트로 씬을 불러와 주세요.", MessageType.Error);
                return;
            }
            DrawRequiredSection();
            Common.GUILine();
            DrawOptionalSection();
        }
        private void DrawRequiredSection()
        {
            Common.OnGUITitle("필수 항목");
            EditorGUILayout.HelpBox($"* 인트로 씬 오브젝트\n* 게임 시작 버튼\n* 계속 하기 버튼", MessageType.Info);

            if (GUILayout.Button("필수 항목 셋팅하기"))
            {
                SetupRequiredObjects();
            }
        }
        /// <summary>
        /// 필수 항목 셋팅
        /// </summary>
        private void SetupRequiredObjects()
        {
            _objGGemCoCore = GetOrCreateCoreGameObject();
            // GGemCo2DCore.SceneIntro GameObject 만들기
            GGemCo2DCore.SceneIntro scene = CreateOrAddComponent<GGemCo2DCore.SceneIntro>("SceneIntro");
            
            // 새 게임 버튼 만들고 연결하기
            string fieldName = "buttonNewGame";
            Button createdButton = CreateUIComponent.CreateObjectButton(fieldName, "New Game");
            scene.SetButtonNewGame(createdButton);
            
            // 계속 하기 버튼 만들고 연결하기
            fieldName = "buttonGameContinue";
            Button buttonGameContinue = CreateUIComponent.CreateObjectButton(fieldName, "Continue Game");
            scene.SetButtonGameContinue(buttonGameContinue);
            buttonGameContinue.gameObject.transform.localPosition = new Vector2(0, 100);
            
            EditorUtility.SetDirty(scene);
            Debug.Log($"{fieldName} 버튼이 생성되어 {scene.name} 에 연결되었습니다.");
        }
        /// <summary>
        /// 옵션 항목 셋팅 하기
        /// </summary>
        private void DrawOptionalSection()
        {
            Common.OnGUITitle("선택 항목");
            EditorGUILayout.HelpBox("불러오기 UI 관련 오브젝트를 셋팅합니다.", MessageType.Info);

            if (GUILayout.Button("불러오기 UI 셋팅하기"))
            {
                SetupLoadUIObjects();
            }
        }
        /// <summary>
        /// 불러오기 셋팅하기
        /// </summary>
        private void SetupLoadUIObjects()
        {
            SetupRequiredObjects();
            
            SceneIntro scene = CreateOrAddComponent<SceneIntro>("SceneIntro");
            if (scene == null) return;
            
            // 불러오기 UI 에서 팝업을 사용하기때문에, PopupManager 셋팅
            SetupPopupManager(scene);
            // UIWindowLoadSaveData
            SetupUIWindowLoadSaveData(scene);
        }
        /// <summary>
        /// 팝업 매니저 셋팅
        /// </summary>
        /// <param name="scene"></param>
        private void SetupPopupManager(SceneIntro scene)
        {
            GameObject obj = CreateUIComponent.CreateGameObjectByPrefab("PopupManager", _objGGemCoCore.transform, ConfigEditor.PathPrefabPopupManager);
            if (!obj) return;
            PopupManager popupManager = obj.GetComponent<PopupManager>();
            
            Transform transform = CreateUIComponent.Find("Canvas").transform;
            
            popupManager.SetCanvasPopup(transform);
            GameObject[] prefabs = new[] { null, ConfigResources.PopupDefault.Load() };
            popupManager.SetPopupTypePrefabs(prefabs);
            
            scene.SetPopupManager(popupManager);
        }
        /// <summary>
        /// 불러오기 UI 윈도우 셋팅
        /// </summary>
        /// <param name="scene"></param>
        private void SetupUIWindowLoadSaveData(SceneIntro scene)
        {
            GameObject canvas = CreateUIComponent.Find("Canvas");
            if (canvas == null)
            {
                Debug.LogError("GGemCo_Core_Canvas 가 없습니다.");
                return;
            }

            string objectName = "UIWindowLoadSaveData";
            GameObject prefab = FindPrefabByName(ConfigEditor.PathUIWindow, objectName);
            if (!prefab) return;
            
            GameObject gameObject = GameObject.Find(objectName);
            PopupManager popupManager = CreateUIComponent.Find("PopupManager")?.GetComponent<PopupManager>();
            if (!gameObject)
            {
                // 프리팹 인스턴스화
                gameObject = PrefabUtility.InstantiatePrefab(prefab, canvas.transform) as GameObject;
                if (!gameObject)
                {
                    Debug.LogError("프리팹 인스턴스 생성 실패");
                    return;
                }
                gameObject.name = objectName;
                // 프리팹 해제
                PrefabUtility.UnpackPrefabInstance(
                    gameObject,
                    PrefabUnpackMode.Completely,
                    InteractionMode.UserAction
                );
            }
            UIWindowLoadSaveData uiWindowLoadSaveData = gameObject.GetComponent<UIWindowLoadSaveData>();
            
            if (uiWindowLoadSaveData && popupManager)
            {
                uiWindowLoadSaveData.SetPopupManager(popupManager);
            }
            
            // 불러오기 버튼 생성
            string fieldName = "buttonOpenSaveDataWindow";
            Button createdButton = CreateUIComponent.CreateObjectButton(fieldName, "Load Game");
            scene.SetButtonOpenSaveDataWindow(createdButton);
            createdButton.gameObject.transform.localPosition = new Vector2(0, -100);
            EditorUtility.SetDirty(scene);
            
            scene.SetUIWindowLoadSaveData(uiWindowLoadSaveData);
        }
    }
}