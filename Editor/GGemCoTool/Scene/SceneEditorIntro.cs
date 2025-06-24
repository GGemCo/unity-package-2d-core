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
            
            EditorUtility.SetDirty(scene);
            Debug.Log($"{fieldName} 버튼이 생성되어 {scene.name} 에 연결되었습니다.");
        }
        /// <summary>
        /// 옵션 항목 셋팅 하기
        /// </summary>
        private void DrawOptionalSection()
        {
            Common.OnGUITitle("옵션 항목");
            EditorGUILayout.HelpBox("불러오기 UI 관련 오브젝트를 셋팅합니다.", MessageType.Info);

            if (GUILayout.Button("불러오기 UI 셋팅하기"))
            {
                SetupLoadUIObjects();
            }
        }
        /// <summary>
        /// 불러오기 UI 셋팅하기
        /// </summary>
        private void SetupLoadUIObjects()
        {
            // 불러오기 UI 에서 팝업을 사용하기때문에, PopupManager 셋팅
            CreateGameObjectPopupManager();
        }
        private void CreateGameObjectPopupManager()
        {
            GGemCo2DCore.PopupManager popupManager = CreateOrAddComponent<GGemCo2DCore.PopupManager>("PopupManager");
        }
    }
}