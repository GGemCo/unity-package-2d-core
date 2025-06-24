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
            var scene = CreateOrAddComponent<SceneGame>("SceneGame");
            SetupCamera(scene);
            SetupCanvasUI(scene);
            SetupCanvasFromWorld(scene);
            SetupCanvasBlack(scene);
            
            // CanvasBlack 프리팹 복사하기

            EditorUtility.SetDirty(scene);
            
        }

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

        private void SetupCanvasUI(SceneGame scene)
        {
            Canvas canvas = CreateUIComponent.CreateObjectCanvas();
            scene.SetCanvasUI(canvas);
        }

        private void SetupCanvasFromWorld(SceneGame scene)
        {
            GameObject canvasFromWorld = CreateUIComponent.CreateGameObjectByPrefab("CanvasFromWorld", null, ConfigEditor.PrefabPathCanvasFromWorld);
            if (!canvasFromWorld) return;

            scene.SetContainerDropItemName(canvasFromWorld.transform.Find("ContainerDropItemName")?.gameObject);
            scene.SetContainerMonsterHpBar(canvasFromWorld.transform.Find("ContainerMonsterHpBar")?.gameObject);
            scene.SetContainerDialogueBalloon(canvasFromWorld.transform.Find("ContainerDialogueBalloon")?.gameObject);
        }

        private void SetupCanvasBlack(SceneGame scene)
        {
            GameObject canvasBlack = CreateUIComponent.CreateGameObjectByPrefab("CanvasBlack", null, ConfigEditor.PrefabPathCanvasBlack);
            if (!canvasBlack) return;
            scene.SetBgBlackForMapLoading(canvasBlack.transform.GetChild(0).gameObject);
        }
        /// <summary>
        /// 옵션 항목 셋팅 하기
        /// </summary>
        private void DrawOptionalSection()
        {
        }
    }
}
