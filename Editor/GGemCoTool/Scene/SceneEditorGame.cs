using System.Collections.Generic;
using GGemCo2DCore;
using UnityEditor;
using UnityEngine;

namespace GGemCo2DCoreEditor
{
    /// <summary>
    /// 게임 씬 설정 툴
    /// </summary>
    public class SceneEditorGame : DefaultSceneEditor
    {
        private const string Title = "게임 씬 셋팅하기";
        private GameObject _objGGemCoCore;
        private TableWindow _tableWindow;
        private Vector2 _scrollPosition;
        
        [MenuItem(ConfigEditor.NameToolSettingSceneGame, false, (int)ConfigEditor.ToolOrdering.SettingSceneGame)]
        public static void ShowWindow()
        {
            GetWindow<SceneEditorGame>(Title);
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            _tableWindow = TableLoaderManager.LoadWindowTable();
        }

        private void OnGUI()
        {
            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);
            if (CheckCurrentLoadedScene(ConfigDefine.SceneNameGame))
            {
                DrawRequiredSection();
                Common.GUILine();
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
            Common.OnGUITitle("필수 항목");
            EditorGUILayout.HelpBox($"* 게임 씬 오브젝트\n* Camera Manager 연결", MessageType.Info);

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
            _objGGemCoCore = GetOrCreateRootPackageGameObject();
            GGemCo2DCore.SceneGame scene = CreateOrAddComponent<GGemCo2DCore.SceneGame>(nameof(SceneGame));
            if (scene == null) return;
            
            // SceneGame 은 싱글톤으로 활용하고 있어 root 로 이동
            scene.gameObject.transform.SetParent(null);
            SetupCamera(scene);
            SetupCanvasUI(scene);
            SetupCanvasFromWorld(scene);
            SetupCanvasBlack(scene);
            SetupSystemMessageManager(scene);
            SetupSoundManager(scene);
            SetupPopupManager(scene);
            
            EditorUtility.SetDirty(scene);
            
        }
        /// <summary>
        /// 메인 카메라
        /// </summary>
        /// <param name="scene"></param>
        private void SetupCamera(SceneGame scene)
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
        }
        /// <summary>
        /// 기본 canvas
        /// </summary>
        /// <param name="scene"></param>
        private void SetupCanvasUI(SceneGame scene)
        {
            Canvas canvas = CreateUIComponent.CreateObjectCanvas(packageType);
            scene.SetCanvasUI(canvas);
            canvas.gameObject.transform.SetParent(_objGGemCoCore.transform);
        }
        /// <summary>
        /// 월드 좌표 사용하는 canvas
        /// </summary>
        /// <param name="scene"></param>
        private void SetupCanvasFromWorld(SceneGame scene)
        {
            GameObject canvasFromWorld = CreateUIComponent.CreateGameObjectByPrefab("CanvasFromWorld", packageType, _objGGemCoCore.transform, ConfigEditor.PathPrefabCanvasFromWorld);
            if (!canvasFromWorld) return;

            scene.SetContainerDropItemName(canvasFromWorld.transform.Find("ContainerDropItemName")?.gameObject);
            scene.SetContainerMonsterHpBar(canvasFromWorld.transform.Find("ContainerMonsterHpBar")?.gameObject);
            scene.SetContainerDialogueBalloon(canvasFromWorld.transform.Find("ContainerDialogueBalloon")?.gameObject);
        }
        /// <summary>
        /// 로딩 화면
        /// </summary>
        /// <param name="scene"></param>
        private void SetupCanvasBlack(SceneGame scene)
        {
            GameObject canvasBlack = CreateUIComponent.CreateGameObjectByPrefab("CanvasBlack", packageType, _objGGemCoCore.transform, ConfigEditor.PathPrefabCanvasBlack);
            if (!canvasBlack) return;
            scene.SetBgBlackForMapLoading(canvasBlack.transform.GetChild(0).gameObject);
        }
        /// <summary>
        /// 시스템 메시지 매니저 
        /// </summary>
        /// <param name="scene"></param>
        private void SetupSystemMessageManager(SceneGame scene)
        {
            GameObject obj = CreateUIComponent.CreateGameObjectByPrefab("SystemMessageManager", packageType, _objGGemCoCore.transform, ConfigEditor.PathPrefabSystemMessageManager);
            if (!obj) return;
            scene.SetSystemMessageManager(obj.GetComponent<SystemMessageManager>());
        }
        /// <summary>
        /// 팝업 매니저 셋팅
        /// </summary>
        /// <param name="scene"></param>
        private void SetupPopupManager(SceneGame scene)
        {
            PopupManager popupManager = CreatePopupManager(_objGGemCoCore.transform);
            if (!popupManager) return;
            scene.SetPopupManager(popupManager);
        }
        /// <summary>
        /// 사운드 매니저 셋팅
        /// </summary>
        /// <param name="scene"></param>
        private void SetupSoundManager(SceneGame scene)
        {
            SoundManager soundManager = CreateSoundManager(_objGGemCoCore.transform);
            if (!soundManager) return;
            scene.SetSoundManager(soundManager);
        }

        /// <summary>
        /// 옵션 항목 셋팅 하기
        /// </summary>
        private void DrawOptionalSection()
        {
            Common.OnGUITitle("선택 항목");
            if (GUILayout.Button("윈도우 매니저 셋팅하기"))
            {
                SetupWindowManager();
            }
            if (GUILayout.Button("모든 테스트 윈도우 셋팅하기"))
            {
                SetupAllTestWindow();
            }
        }
        private UIWindowManager SetupWindowManager()
        {
            SetupRequiredObjects();
            
            SceneGame scene = CreateOrAddComponent<SceneGame>("SceneGame");
            if (scene == null) return null;
            UIWindowManager uiWindowManager = CreateOrAddComponent<UIWindowManager>("UIWindowManager");
            if (!uiWindowManager) return null;
            scene.SetUIWindowManager(uiWindowManager);
            return uiWindowManager;
        }
        private void SetupAllTestWindow()
        {
            SetupRequiredObjects();
            
            SceneGame scene = CreateOrAddComponent<SceneGame>("SceneGame");
            if (scene == null) return;
            UIWindowManager uiWindowManager = SetupWindowManager();
            if (!uiWindowManager) return;
            
            GameObject canvas = CreateUIComponent.Find("Canvas", packageType);
            if (canvas == null)
            {
                Debug.LogError("GGemCo_Core_Canvas 가 없습니다.");
                return;
            }

            List<UIWindow> uiWindows =  new List<UIWindow> { null };
            Dictionary<int, StruckTableWindow> dictionary = _tableWindow.GetDatas();
            
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
                if (!prefab) continue;
                
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
                    Debug.LogError("프리팹 인스턴스 생성 실패");
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
            }

            uiWindowManager.SetUIWindow(uiWindows.ToArray());
            scene.SetUIWindowManager(uiWindowManager);
        }

    }
}
