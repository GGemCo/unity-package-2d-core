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
        
        [MenuItem(ConfigEditor.NameToolSettingSceneGame, false, (int)ConfigEditor.ToolOrdering.SettingSceneGame)]
        public static void ShowWindow()
        {
            GetWindow<SceneEditorGame>(Title);
        }

        private void OnGUI()
        {
            if (!CheckCurrentLoadedScene(ConfigDefine.SceneNameGame))
            {
                EditorGUILayout.HelpBox($"게임 씬을 불러와 주세요.", MessageType.Error);
                return;
            }
            DrawRequiredSection();
            Common.GUILine();
            DrawOptionalSection();
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
            SceneGame scene = CreateOrAddComponent<SceneGame>("SceneGame");
            if (scene == null) return;
            // SceneGame 은 싱글톤으로 활용하고 있어 root 로 이동
            scene.gameObject.transform.SetParent(null);
            SetupCamera(scene);
            SetupCanvasUI(scene);
            SetupCanvasFromWorld(scene);
            SetupCanvasBlack(scene);
            SetupSystemMessageManager(scene);
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
            
            // todo 생성한 씬 빌드 프로파일에 넣어야함

        }
        /// <summary>
        /// 기본 canvas
        /// </summary>
        /// <param name="scene"></param>
        private void SetupCanvasUI(SceneGame scene)
        {
            Canvas canvas = CreateUIComponent.CreateObjectCanvas();
            scene.SetCanvasUI(canvas);
        }
        /// <summary>
        /// 월드 좌표 사용하는 canvas
        /// </summary>
        /// <param name="scene"></param>
        private void SetupCanvasFromWorld(SceneGame scene)
        {
            GameObject canvasFromWorld = CreateUIComponent.CreateGameObjectByPrefab("CanvasFromWorld", null, ConfigEditor.PathPrefabCanvasFromWorld);
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
            GameObject canvasBlack = CreateUIComponent.CreateGameObjectByPrefab("CanvasBlack", null, ConfigEditor.PathPrefabCanvasBlack);
            if (!canvasBlack) return;
            scene.SetBgBlackForMapLoading(canvasBlack.transform.GetChild(0).gameObject);
        }
        /// <summary>
        /// 시스템 메시지 매니저 
        /// </summary>
        /// <param name="scene"></param>
        private void SetupSystemMessageManager(SceneGame scene)
        {
            GameObject obj = CreateUIComponent.CreateGameObjectByPrefab("SystemMessageManager", null, ConfigEditor.PathPrefabSystemMessageManager);
            if (!obj) return;
            scene.SetSystemMessageManager(obj.GetComponent<SystemMessageManager>());
        }
        /// <summary>
        /// 팝업 매니저
        /// </summary>
        /// <param name="scene"></param>
        private void SetupPopupManager(SceneGame scene)
        {
            GameObject obj = CreateUIComponent.CreateGameObjectByPrefab("PopupManager", null, ConfigEditor.PathPrefabPopupManager);
            if (!obj) return;
            PopupManager popupManager = obj.GetComponent<PopupManager>();
            Transform transform = GameObject.Find("Canvas").transform;
            popupManager.SetCanvasPopup(transform);
            GameObject[] prefabs = new[] { null, ConfigResources.PopupDefault.Load() };
            popupManager.SetPopupTypePrefabs(prefabs);
            
            scene.SetPopupManager(popupManager);
        }

        /// <summary>
        /// 옵션 항목 셋팅 하기
        /// </summary>
        private void DrawOptionalSection()
        {
        }
    }
}
