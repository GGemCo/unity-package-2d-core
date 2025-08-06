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
            }
            else
            {
                DrawRequiredSection();
                Common.GUILine();
                DrawOptionalSection();
            }
        }
        private void DrawRequiredSection()
        {
            Common.OnGUITitle("필수 항목");
            EditorGUILayout.HelpBox($"* 인트로 씬 오브젝트\n* 게임 시작 버튼\n* 계속 하기 버튼\n* 옵션 버튼\n* 옵션 윈도우", MessageType.Info);
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
            GGemCo2DCore.SceneIntro scene = CreateOrAddComponent<GGemCo2DCore.SceneIntro>(nameof(SceneIntro));

            CreateUIComponent.CreateObjectCanvas();
                
            // 인트로 씬에서 사운드를 사용하기때문에, SoundManager 셋팅
            SetupSoundManager(scene);
            // 옵션 UI에서 팝업을 사용하기때문에, PopupManager 셋팅
            SetupPopupManager(scene);
            
            // 새 게임 버튼 만들고 연결하기
            MetaDataButton metaDataButton = new MetaDataButton(scene.GetFieldNameButtonNewGame(), "New Game",
                LocalizationConstants.Tables.Scene, LocalizationConstants.Keys.Intro.ButtonNewGame());
            Button createdButton = CreateUIComponent.CreateObjectButton(metaDataButton);
            scene.SetButtonNewGame(createdButton);
            
            // 계속 하기 버튼 만들고 연결하기
            metaDataButton = new MetaDataButton(scene.GetFieldNameButtonGameContinue(), "Continue Game",
                LocalizationConstants.Tables.Scene, LocalizationConstants.Keys.Intro.ButtonContinue());
            Button buttonGameContinue = CreateUIComponent.CreateObjectButton(metaDataButton);
            scene.SetButtonGameContinue(buttonGameContinue);
            buttonGameContinue.gameObject.transform.localPosition = new Vector2(0, 100);
            
            // 옵션 버튼 만들고 연결하기
            metaDataButton = new MetaDataButton(scene.GetFieldNameButtonOption(), "Option",
                LocalizationConstants.Tables.Scene, LocalizationConstants.Keys.Intro.ButtonOption());
            Button buttonOption = CreateUIComponent.CreateObjectButton(metaDataButton);
            scene.SetButtonOption(buttonOption);
            buttonOption.gameObject.transform.localPosition = new Vector2(0, -100);
            
            // 옵션 윈도우 추가
            GameObject canvas = CreateUIComponent.Find("Canvas");
            string objectName = scene.GetNameUIWindowOption();
            GameObject prefab = FindPrefabByName(ConfigEditor.PathUIWindow, objectName);
            if (!prefab) return;
            
            GameObject gameObject = GameObject.Find(objectName);
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
            UIWindowOption uiWindowOption = gameObject.GetComponent<UIWindowOption>();
            scene.SetUIWindowOption(uiWindowOption);
            PopupManager popupManager = CreateUIComponent.Find(scene.GetFieldNamePopupManager())?.GetComponent<PopupManager>();
            if (uiWindowOption && popupManager)
            {
                uiWindowOption.SetPopupManager(popupManager);
            }
            SoundManager soundManager = CreateUIComponent.Find(scene.GetFieldNameSoundManager())?.GetComponent<SoundManager>();
            if (uiWindowOption && soundManager)
            {
                uiWindowOption.SetSoundManager(soundManager);
            }
            
            EditorUtility.SetDirty(scene);
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
            
            // UIWindowLoadSaveData
            SetupUIWindowLoadSaveData(scene);
        }
        /// <summary>
        /// 팝업 매니저 셋팅
        /// </summary>
        /// <param name="scene"></param>
        private void SetupPopupManager(SceneIntro scene)
        {
            PopupManager popupManager = CreatePopupManager(_objGGemCoCore.transform);
            if (!popupManager) return;
            scene.SetPopupManager(popupManager);
        }
        /// <summary>
        /// 사운드 매니저 셋팅
        /// </summary>
        /// <param name="scene"></param>
        private void SetupSoundManager(SceneIntro scene)
        {
            SoundManager soundManager = CreateSoundManager(_objGGemCoCore.transform);
            if (!soundManager) return;
            scene.SetSoundManager(soundManager);
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
            
            // 불러오기 버튼 생성
            MetaDataButton metaDataButton = new MetaDataButton(scene.GetFieldNameButtonOpenSaveDataWindow(), "Load Game",
                LocalizationConstants.Tables.Scene, LocalizationConstants.Keys.Intro.ButtonLoad());
            Button createdButton = CreateUIComponent.CreateObjectButton(metaDataButton);
            scene.SetButtonOpenSaveDataWindow(createdButton);
            createdButton.gameObject.transform.SetSiblingIndex(0);
            createdButton.gameObject.transform.localPosition = new Vector2(0, -200);

            // 불러오기 UI 생성
            string objectName = scene.GetNameUIWindowLoadSaveData();
            GameObject prefab = FindPrefabByName(ConfigEditor.PathUIWindow, objectName);
            if (!prefab) return;
            
            GameObject gameObject = GameObject.Find(objectName);
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
            PopupManager popupManager = CreateUIComponent.Find(scene.GetFieldNamePopupManager())?.GetComponent<PopupManager>();
            if (uiWindowLoadSaveData && popupManager)
            {
                uiWindowLoadSaveData.SetPopupManager(popupManager);
            }
            
            scene.SetUIWindowLoadSaveData(uiWindowLoadSaveData);
            EditorUtility.SetDirty(scene);
        }
    }
}