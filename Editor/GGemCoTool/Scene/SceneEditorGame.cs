using System.Collections.Generic;
using GGemCo2DCore;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace GGemCo2DCoreEditor
{
    /// <summary>
    /// 게임 씬 설정 툴
    /// </summary>
    public class SceneEditorGame : DefaultSceneEditor
    {
        private const string Title = "게임 씬 셋팅하기";
        private GameObject _objGGemCoCore;
        private Vector2 _scrollPosition;
        
        [MenuItem(ConfigEditor.NameToolSettingSceneGame, false, (int)ConfigEditor.ToolOrdering.SettingSceneGame)]
        public static void ShowWindow()
        {
            GetWindow<SceneEditorGame>(Title);
        }

        protected override void OnEnable()
        {
            base.OnEnable();
        }

        private void OnGUI()
        {
            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);
            if (CheckCurrentLoadedScene(ConfigDefine.SceneNameGame))
            {
                DrawRequiredSection();
                HelperEditorUI.GUILine();
                DrawOptionalSection();
            }
            else {
                EditorGUILayout.HelpBox($"게임 씬을 불러와 주세요.", MessageType.Error);
            }
            EditorGUILayout.Space(20);
            EditorGUILayout.EndScrollView();
        }
        private void DrawRequiredSection()
        {
            HelperEditorUI.OnGUITitle("필수 항목");
            EditorGUILayout.HelpBox($"* 게임 씬 오브젝트\n* Camera Manager 연결", MessageType.Info);

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
            GGemCo2DCore.SceneGame scene = CreateOrAddComponent<GGemCo2DCore.SceneGame>(nameof(SceneGame));
            if (scene == null) return;
            
            // SceneGame 은 싱글톤으로 활용하고 있어 root 로 이동
            scene.gameObject.transform.SetParent(null);
            SetupCamera(scene, ctx);
            SetupCanvasUI(scene, ctx);
            SetupCanvasFromWorld(scene, ctx);
            SetupCanvasBlack(scene, ctx);
            SetupSystemMessageManager(scene, ctx);
            SetupSoundManager(scene, ctx);
            SetupPopupManager(scene, ctx);
            
            HelperLog.Info($"[{nameof(SceneEditorGame)}] 게임 씬 필수 셋팅 완료", ctx);
            EditorUtility.SetDirty(scene);
            
        }
        /// <summary>
        /// 메인 카메라
        /// </summary>
        /// <param name="scene"></param>
        /// <param name="ctx"></param>
        private void SetupCamera(SceneGame scene, EditorSetupContext ctx = null)
        {
            GameObject mainCameraObj = GameObject.FindWithTag("MainCamera");
            if (!mainCameraObj)
            {
                EditorUtility.DisplayDialog(Title, "Main Camera 가 없습니다.", "OK");
                return;
            }

            scene.SetMainCamera(mainCameraObj.GetComponent<Camera>());
            CameraManager cameraManager = mainCameraObj.GetComponent<CameraManager>();
            if (!cameraManager)
            {
                cameraManager = mainCameraObj.AddComponent<CameraManager>();
            }
            cameraManager.SetCameraMoveSpeed(10);
            scene.SetCameraManager(cameraManager);
            HelperLog.Info($"[{nameof(SceneEditorGame)}] 메인 카메라 셋팅 완료", ctx);
        }
        /// <summary>
        /// 기본 canvas
        /// </summary>
        /// <param name="scene"></param>
        /// <param name="ctx"></param>
        private void SetupCanvasUI(SceneGame scene, EditorSetupContext ctx = null)
        {
            Canvas canvas = CreateUIComponent.CreateObjectCanvas(packageType);
            scene.SetCanvasUI(canvas);
            canvas.gameObject.transform.SetParent(_objGGemCoCore.transform);
            HelperLog.Info($"[{nameof(SceneEditorGame)}] 메인 캔버스 셋팅 완료", ctx);
        }
        /// <summary>
        /// 월드 좌표 사용하는 canvas
        /// </summary>
        /// <param name="scene"></param>
        /// <param name="ctx"></param>
        private void SetupCanvasFromWorld(SceneGame scene, EditorSetupContext ctx = null)
        {
            GameObject canvasFromWorld = CreateUIComponent.CreateGameObjectByPrefab("CanvasFromWorld", packageType, _objGGemCoCore.transform, ConfigEditor.PathPrefabCanvasFromWorld);
            if (!canvasFromWorld) return;

            scene.SetContainerDropItemName(canvasFromWorld.transform.Find("ContainerDropItemName")?.gameObject);
            scene.SetContainerMonsterHpBar(canvasFromWorld.transform.Find("ContainerMonsterHpBar")?.gameObject);
            scene.SetContainerDialogueBalloon(canvasFromWorld.transform.Find("ContainerDialogueBalloon")?.gameObject);

            HelperLog.Info($"[{nameof(SceneEditorGame)}] 월드 좌표를 사용하는 캔버스 셋팅 완료", ctx);
        }
        /// <summary>
        /// 로딩 화면
        /// </summary>
        /// <param name="scene"></param>
        /// <param name="ctx"></param>
        private void SetupCanvasBlack(SceneGame scene, EditorSetupContext ctx = null)
        {
            GameObject canvasBlack = CreateUIComponent.CreateGameObjectByPrefab("CanvasBlack", packageType, _objGGemCoCore.transform, ConfigEditor.PathPrefabCanvasBlack);
            if (!canvasBlack) return;
            
            var objectImage = canvasBlack.transform.GetChild(0).gameObject;
            if (objectImage == null)
            {
                HelperLog.Info($"[{nameof(SceneEditorGame)}] CanvasBlack 오브젝트 하위에 Image 오브젝트가 없습니다.", ctx);
                return;
            }
            scene.SetBgBlackForMapLoading(objectImage);
            var imageComponent = objectImage.GetComponent<Image>();
            if (imageComponent)
                imageComponent.color = new Color(0, 0, 0, 1);
            
            HelperLog.Info($"[{nameof(SceneEditorGame)}] 로딩 중 인터렉션을 막는 캔버스 셋팅 완료", ctx);
        }
        /// <summary>
        /// 시스템 메시지 매니저 
        /// </summary>
        /// <param name="scene"></param>
        /// <param name="ctx"></param>
        private void SetupSystemMessageManager(SceneGame scene, EditorSetupContext ctx = null)
        {
            GameObject obj = CreateUIComponent.CreateGameObjectByPrefab("SystemMessageManager", packageType, _objGGemCoCore.transform, ConfigEditor.PathPrefabSystemMessageManager);
            if (!obj) return;
            scene.SetSystemMessageManager(obj.GetComponent<SystemMessageManager>());
            HelperLog.Info($"[{nameof(SceneEditorGame)}] {nameof(SystemMessageManager)} 오브젝트 셋팅 완료", ctx);
        }
        /// <summary>
        /// 팝업 매니저 셋팅
        /// </summary>
        /// <param name="scene"></param>
        /// <param name="ctx"></param>
        private void SetupPopupManager(SceneGame scene, EditorSetupContext ctx = null)
        {
            PopupManager popupManager = CreatePopupManager(_objGGemCoCore.transform);
            if (!popupManager) return;
            scene.SetPopupManager(popupManager);
            HelperLog.Info($"[{nameof(SceneEditorGame)}] {nameof(PopupManager)} 오브젝트 셋팅 완료", ctx);
        }
        /// <summary>
        /// 사운드 매니저 셋팅
        /// </summary>
        /// <param name="scene"></param>
        /// <param name="ctx"></param>
        private void SetupSoundManager(SceneGame scene, EditorSetupContext ctx = null)
        {
            SoundManager soundManager = CreateSoundManager(_objGGemCoCore.transform);
            if (!soundManager) return;
            scene.SetSoundManager(soundManager);
            HelperLog.Info($"[{nameof(SceneEditorGame)}] {nameof(SoundManager)} 오브젝트 셋팅 완료", ctx);
        }

        /// <summary>
        /// 옵션 항목 셋팅 하기
        /// </summary>
        private void DrawOptionalSection()
        {
            HelperEditorUI.OnGUITitle("선택 항목");
            if (GUILayout.Button("윈도우 매니저 셋팅하기"))
            {
                SetupWindowManager();
            }
            if (GUILayout.Button("모든 테스트 윈도우 셋팅하기"))
            {
                SetupAllTestWindow();
            }
        }
        public UIWindowManager SetupWindowManager()
        {
            SetupRequiredObjects();
            
            SceneGame scene = CreateOrAddComponent<SceneGame>("SceneGame");
            if (scene == null) return null;
            UIWindowManager uiWindowManager = CreateOrAddComponent<UIWindowManager>("UIWindowManager");
            if (!uiWindowManager) return null;
            scene.SetUIWindowManager(uiWindowManager);
            return uiWindowManager;
        }
        public void SetupAllTestWindow(EditorSetupContext ctx = null)
        {
            SetupRequiredObjects();
            
            SceneGame scene = CreateOrAddComponent<SceneGame>("SceneGame");
            if (scene == null)
            {
                HelperLog.Error($"[{nameof(SceneEditorGame)}] {nameof(SceneGame)} 생성/가져오기를 할 수 없습니다.", ctx);
                return;
            }
            UIWindowManager uiWindowManager = SetupWindowManager();
            if (!uiWindowManager)
            {
                HelperLog.Error($"[{nameof(SceneEditorGame)}] {nameof(UIWindowManager)} 생성/가져오기를 할 수 없습니다.", ctx);
                return;
            }
            
            GameObject canvas = CreateUIComponent.Find("Canvas", packageType);
            if (canvas == null)
            {
                HelperLog.Error($"[{nameof(SceneEditorGame)}] GGemCo_Core_Canvas 가 없습니다.", ctx);
                return;
            }

            List<UIWindow> uiWindows =  new List<UIWindow> { null };
            Dictionary<int, StruckTableWindow> dictionary = tableLoaderManager.LoadWindowTable().GetDatas();
            
            foreach (KeyValuePair<int, StruckTableWindow> outerPair in dictionary)
            {
                var info = outerPair.Value;
                if (info.Uid <= 0) continue;
                if (!info.UseInGame)
                {
                    uiWindows.Add(null);
                    continue;
                }
                string objectName = info.PrefabName;
                
                GameObject prefab = FindPrefabByName(ConfigEditor.PathUIWindow, objectName);
                if (!prefab)
                {
                    HelperLog.Error($"[{nameof(SceneEditorGame)}] {objectName} 프리팹을 찾을 수 없습니다.", ctx);
                    continue;
                }
                
                GameObject gameObject = GameObject.Find(objectName);
                UIWindow window;
                if (gameObject)
                {
                    window = gameObject.GetComponent<UIWindow>();
                    if (window)
                    {
                        uiWindows.Add(window);
                    }
                    continue;
                }
                
                // 프리팹 인스턴스화
                gameObject = PrefabUtility.InstantiatePrefab(prefab, canvas.transform) as GameObject;
                if (!gameObject)
                {
                    HelperLog.Error($"[{nameof(SceneEditorGame)}] {objectName} 프리팹 인스턴스 생성 실패.", ctx);
                    continue;
                }

                window = gameObject.GetComponent<UIWindow>();
                if (window)
                {
                    uiWindows.Add(window);
                }
                gameObject.name = objectName;
                // 프리팹 해제
                PrefabUtility.UnpackPrefabInstance(
                    gameObject,
                    PrefabUnpackMode.Completely,
                    InteractionMode.UserAction
                );

                if (info.Uid == (int)UIWindowConstants.WindowUid.Option)
                {
                    //  UIPanelOptionBase 프리팹을 자동으로 listPrefabPanel 에 등록
                    AutoFillPanelPrefabs(window);
                }
            }

            uiWindowManager.SetUIWindow(uiWindows.ToArray());
            scene.SetUIWindowManager(uiWindowManager);
            HelperLog.Info($"[{nameof(SceneEditorGame)}] 샘플 윈도우 셋업 완료.", ctx);
        }

    }
}
