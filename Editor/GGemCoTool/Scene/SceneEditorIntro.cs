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
                HelperEditorUI.GUILine();
                DrawOptionalSection();
            }
        }
        private void DrawRequiredSection()
        {
            HelperEditorUI.OnGUITitle("필수 항목");
            EditorGUILayout.HelpBox($"* 인트로 씬 오브젝트\n* 게임 시작 버튼\n* 계속 하기 버튼\n* 옵션 버튼\n* 옵션 윈도우", MessageType.Info);
            if (GUILayout.Button("필수 항목 셋팅하기"))
            {
                SetupRequiredObjects();
            }
        }
        /// <summary>
        /// 필수 항목 셋팅
        /// </summary>
        public void SetupRequiredObjects(EditorSetupContext ctx = null)
        {
            _objGGemCoCore = GetOrCreateRootPackageGameObject();
            // GGemCo2DCore.SceneIntro GameObject 만들기
            GGemCo2DCore.SceneIntro scene = CreateOrAddComponent<GGemCo2DCore.SceneIntro>(nameof(SceneIntro));

            CreateUIComponent.CreateObjectCanvas(packageType);
                
            // 인트로 씬에서 사운드를 사용하기때문에, SoundManager 셋팅
            SetupSoundManager(scene, ctx);
            // 옵션 UI에서 팝업을 사용하기때문에, PopupManager 셋팅
            SetupPopupManager(scene, ctx);
            
            // 새 게임 버튼 만들고 연결하기
            MetaDataButton metaDataButton = new MetaDataButton(scene.GetFieldNameButtonNewGame(), "New Game",
                LocalizationConstants.Tables.Scene, LocalizationConstants.Keys.Intro.ButtonNewGame());
            Button createdButton = CreateUIComponent.CreateObjectButton(metaDataButton, packageType);
            createdButton.gameObject.AddComponent<ClickSoundEventBroadcaster>();
            scene.SetButtonNewGame(createdButton);
            HelperLog.Info($"[{nameof(SceneEditorIntro)}] 새 게임 버튼 셋업 완료", ctx);
            
            // 계속 하기 버튼 만들고 연결하기
            metaDataButton = new MetaDataButton(scene.GetFieldNameButtonGameContinue(), "Continue Game",
                LocalizationConstants.Tables.Scene, LocalizationConstants.Keys.Intro.ButtonContinue());
            Button buttonGameContinue = CreateUIComponent.CreateObjectButton(metaDataButton, packageType);
            buttonGameContinue.gameObject.AddComponent<ClickSoundEventBroadcaster>();
            scene.SetButtonGameContinue(buttonGameContinue);
            buttonGameContinue.gameObject.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, 100);
            HelperLog.Info($"[{nameof(SceneEditorIntro)}] 계속 하기 버튼 셋업 완료", ctx);
            
            // 옵션 버튼 만들고 연결하기
            metaDataButton = new MetaDataButton(scene.GetFieldNameButtonOption(), "Option",
                LocalizationConstants.Tables.Scene, LocalizationConstants.Keys.Intro.ButtonOption());
            Button buttonOption = CreateUIComponent.CreateObjectButton(metaDataButton, packageType);
            buttonOption.gameObject.AddComponent<ClickSoundEventBroadcaster>();
            scene.SetButtonOption(buttonOption);
            buttonOption.gameObject.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, -100);
            HelperLog.Info($"[{nameof(SceneEditorIntro)}] 옵션 버튼 셋업 완료", ctx);
            
            // 옵션 윈도우 추가
            GameObject canvas = CreateUIComponent.Find("Canvas", packageType);
            string objectName = scene.GetNameUIWindowOption();
            GameObject prefab = FindPrefabUIWindowByName(objectName);
            if (!prefab)
            {
                HelperLog.Error($"[{nameof(SceneEditorIntro)}] {objectName} 프리팹이 없습니다.", ctx);
                return;
            }
            
            GameObject gameObject = GameObject.Find(objectName);
            if (!gameObject)
            {
                // 프리팹 인스턴스화
                gameObject = PrefabUtility.InstantiatePrefab(prefab, canvas.transform) as GameObject;
                if (!gameObject)
                {
                    HelperLog.Error($"[{nameof(SceneEditorIntro)}] {nameof(objectName)} 프리팹 생성 실패.", ctx);
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
            PopupManager popupManager = CreateUIComponent.Find(scene.GetFieldNamePopupManager(), packageType)?.GetComponent<PopupManager>();
            if (uiWindowOption && popupManager)
            {
                uiWindowOption.SetPopupManager(popupManager);
            }
            SoundManager soundManager = CreateUIComponent.Find(scene.GetFieldNameSoundManager(), packageType)?.GetComponent<SoundManager>();
            if (uiWindowOption && soundManager)
            {
                uiWindowOption.SetSoundManager(soundManager);
            }
            //  UIPanelOptionBase 프리팹을 자동으로 listPrefabPanel 에 등록
            AutoFillPanelPrefabs(uiWindowOption);
            
            HelperLog.Info($"[{nameof(SceneEditorIntro)}] 인트로 씬 필수 셋팅 완료", ctx);
            
            EditorUtility.SetDirty(scene);
        }
        /// <summary>
        /// 옵션 항목 셋팅 하기
        /// </summary>
        private void DrawOptionalSection()
        {
            HelperEditorUI.OnGUITitle("선택 항목");
            EditorGUILayout.HelpBox("불러오기 UI 관련 오브젝트를 셋팅합니다.", MessageType.Info);

            if (GUILayout.Button("불러오기 UI 셋팅하기"))
            {
                SetupLoadUIObjects();
            }
        }
        /// <summary>
        /// 불러오기 셋팅하기
        /// </summary>
        public void SetupLoadUIObjects()
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
        /// <param name="ctx"></param>
        private void SetupPopupManager(SceneIntro scene, EditorSetupContext ctx = null)
        {
            PopupManager popupManager = CreatePopupManager(_objGGemCoCore.transform);
            if (!popupManager) return;
            scene.SetPopupManager(popupManager);
            HelperLog.Info($"[{nameof(SceneEditorIntro)}] {nameof(PopupManager)} 셋업 완료", ctx);
        }
        /// <summary>
        /// 사운드 매니저 셋팅
        /// </summary>
        /// <param name="scene"></param>
        /// <param name="ctx"></param>
        private void SetupSoundManager(SceneIntro scene, EditorSetupContext ctx = null)
        {
            SoundManager soundManager = CreateSoundManager(_objGGemCoCore.transform);
            if (!soundManager) return;
            scene.SetSoundManager(soundManager);
            HelperLog.Info($"[{nameof(SceneEditorIntro)}] {nameof(soundManager)} 셋업 완료", ctx);
        }
        /// <summary>
        /// 불러오기 UI 윈도우 셋팅
        /// </summary>
        /// <param name="scene"></param>
        private void SetupUIWindowLoadSaveData(SceneIntro scene)
        {
            GameObject canvas = CreateUIComponent.Find("Canvas", packageType);
            if (canvas == null)
            {
                Debug.LogError("GGemCo_Core_Canvas 가 없습니다.");
                return;
            }
            
            // 불러오기 버튼 생성
            MetaDataButton metaDataButton = new MetaDataButton(scene.GetFieldNameButtonOpenSaveDataWindow(), "Load Game",
                LocalizationConstants.Tables.Scene, LocalizationConstants.Keys.Intro.ButtonLoad());
            Button createdButton = CreateUIComponent.CreateObjectButton(metaDataButton, packageType);
            createdButton.gameObject.AddComponent<ClickSoundEventBroadcaster>();
            scene.SetButtonOpenSaveDataWindow(createdButton);
            createdButton.gameObject.transform.SetSiblingIndex(0);
            createdButton.gameObject.transform.localPosition = new Vector2(0, -200);

            // 불러오기 UI 생성
            string objectName = scene.GetNameUIWindowLoadSaveData();
            // GameObject prefab = FindPrefabByName(ConfigEditor.PathUIWindow, objectName);
            GameObject prefab = FindPrefabUIWindowByName(objectName);
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
            PopupManager popupManager = CreateUIComponent.Find(scene.GetFieldNamePopupManager(), packageType)?.GetComponent<PopupManager>();
            if (uiWindowLoadSaveData && popupManager)
            {
                uiWindowLoadSaveData.SetPopupManager(popupManager);
            }
            
            scene.SetUIWindowLoadSaveData(uiWindowLoadSaveData);
            EditorUtility.SetDirty(scene);
        }
    }
}